using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Maui.Controls;
using Avalonia.Threading;
using AvaloniaApplication.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

// ========================================================
// 类型别名（SPIKE 验证用）
// --------------------------------------------------------
// Microsoft.Maui.Controls 和 Avalonia.Controls 里都有 Button / Grid 这些同名类型，
// 两个命名空间全量 using 会冲突，所以给 MAUI 侧的类型起别名。
// 下面代码里 MauiButton / MauiGrid / MauiColor / MauiFontAttributes / MauiLayoutOptions
// 都明确表示"这是 MAUI 控件"，避免和 Avalonia 的同名控件混淆。
// ========================================================
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiFontAttributes = Microsoft.Maui.Controls.FontAttributes;
using MauiGrid = Microsoft.Maui.Controls.Grid;
using MauiLayoutOptions = Microsoft.Maui.Controls.LayoutOptions;

namespace AvaloniaApplication;

/// <summary>
/// 扫码页面视图。
///
/// 当前处于"最小验证（SPIKE）"状态：相机预览和悬浮探针按钮由代码组装成一个
/// MAUI Grid，作为 MauiControlHost.Content 交给原生层渲染。
///
/// 背景知识（决定本文件机制的关键约束）：
/// - Avalonia 自己画的任何内容（包括按钮）都画在同一个 SurfaceView 里，这个表面位于
///   原生视图（MauiControlHost 承载的 MAUI 视图）的下面一层，所以 Avalonia 按钮永远
///   盖不住相机预览。
/// - 悬浮在预览上的控件必须和相机待在同一个 MAUI 视图树里：MAUI Grid 在 Android 上的
///   平台视图是 LayoutViewGroup，子控件按 Children 下标顺序绘制，下标大的在上层。
/// 详细说明与验证数据见 docs/maui-overlay-verification.md。
/// </summary>
public partial class ScannerView : UserControl
{
    /// <summary>
    /// 当前使用的 ZXing 相机条码识别视图（MAUI 控件）。
    /// 在 OnLoaded 里由 BuildSpikeContent 构建；Torch / CameraLocation / StartDetecting / BarcodesDetected
    /// 都通过它操作相机。
    /// </summary>
    private CameraBarcodeReaderView? cameraBarcodeReaderView;

    /// <summary>
    /// SPIKE：缓存 BuildSpikeContent 里创建好的相机实例。
    /// OnLoaded 可能被多次调用（详见 OnLoaded），用它做幂等保护，避免重复构建视图和重复订阅事件。
    /// </summary>
    private CameraBarcodeReaderView? spikeCamera;

    /// <summary>
    /// 无参构造：留给 XAML 设计器使用（设计时 DataContext 由 Design.DataContext 提供）。
    /// </summary>
    public ScannerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 带 ViewModel 的构造：运行时由 MainViewModel.ScanCommand 以
    /// new ScannerView(new ScannerViewModel(this)) 的方式创建，因此 DataContext 在
    /// InitializeComponent() 之前赋值，确保 XAML 绑定从第一帧开始就有数据。
    /// </summary>
    public ScannerView(ScannerViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// 视图挂载到可视化树时触发（每次进入扫描页都会执行，注意可能被多次调用，本文档的实现是幂等的）。
    /// 职责：
    /// 1) 构建"相机 + 悬浮按钮"的 MAUI Grid 并赋给 MauiControlHost.Content（SPIKE）；
    /// 2) 设置条码识别参数（只识别一维条码、自动旋转、一次只出一个结果）；
    /// 3) 把 ViewModel 的事件桥接到本视图（MAUI 控件无法用 Avalonia 绑定，走事件机制）；
    /// 4) 启动识别。
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(ScannerView)}.{nameof(OnLoaded)}", "[TRACE]");

        // SPIKE：相机不再从 XAML 挂载，改为在代码中构建整棵 MAUI 视图树并赋给 Content。
        this.cameraBarcodeReaderView = BuildSpikeContent(this.Get<MauiControlHost>("cameraBarcodeReaderHost"));

        // ---------- 条码识别参数 ----------
        // Formats 仅保留一维条码（若需要可加回 QrCode）。
        // Multiple=false + 下方 BarcodesDetected 中的 Length==1 双重保证一次只接受一个结果。
        // TryHarder / TryInverted 曾开启，为识别速度优化已关闭（见提交历史）。
        this.cameraBarcodeReaderView.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.OneDimensional, //| BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false,
            TryHarder = false,
            TryInverted = false,
        };

        // ---------- 把 ViewModel 的事件桥接到本视图 ----------
        // ScannerViewModel.ToggleTorch() / ToggleCameraLocation() 只负责发事件不做实际操作，
        // 真正的相机操作（IsTorchOn / CameraLocation 属性）在这里执行。
        if (DataContext is ScannerViewModel vm)
        {
            vm.TorchToggled += TorchToggled;
            vm.CameraLocationToggled += CameraLocationToggled;
        }

        // ---------- 开始识别 ----------
        this.cameraBarcodeReaderView.IsDetecting = true;
        System.Diagnostics.Debug.WriteLine($"IsDetecting: {this.cameraBarcodeReaderView.IsDetecting}", "[INFO]");

        base.OnLoaded(e);
    }

    /// <summary>
    /// SPIKE（最小验证）：证明"MAUI 按钮可以显示在相机预览之上"。
    /// 这是第一条路（把按钮下沉到 MAUI 层）的核心机制实现。
    /// </summary>
    /// <remarks>
    /// 机制说明：
    /// - MauiControlHost.Content 的类型是 Microsoft.Maui.Controls.View，MAUI 的 Grid 也继承自 View，
    ///   所以 Content 可以是一棵包含相机和按钮的 MAUI 视图树，而不仅仅是一个叶子控件。
    /// - MAUI Grid 在 Android 上的平台视图是 LayoutViewGroup：子控件按 Children 下标顺序绘制，
    ///   下标小的在下层，下标大的在上层。
    /// - 因此相机放 index 0、按钮放 index 1，按钮就会渲染在相机预览之上，且点击事件可以直接
    ///   命中悬浮按钮。此能力是 Avalonia 自绘按钮不具备的。
    /// </remarks>
    private CameraBarcodeReaderView BuildSpikeContent(MauiControlHost host)
    {
        // ---------- 幂等保护 ----------
        // OnLoaded 会被多次调用（视图再次进入可视化树时），第一次构建后把相机实例缓存起来，
        // 之后直接复用，避免重复创建视图、重复订阅 BarcodesDetected 导致识别流程错乱。
        if (this.spikeCamera is not null)
        {
            return this.spikeCamera;
        }

        // ---------- 相机（下层，index 0） ----------
        // 创建 ZXing 的相机识别视图，检出事件接到本类的 BarcodesDetected 处理器。
        var camera = new CameraBarcodeReaderView();
        camera.BarcodesDetected += BarcodesDetected;

        // 组装 MAUI Grid：相机先放入 index 0 作为背景预览。
        // 正式实现时，这里就是放相机 + Torch / Cancel / Camera 三个真实按钮的地方。
        var grid = new MauiGrid();
        grid.Children.Add(camera);

        // ============ 探针按钮（SPIKE，上层，index 1） ============
        // 目的：像素级验证"按钮确实画在相机预览之上"。
        // - 选品红 #FF00FF：与相机画面（棕色调虚拟场景）反差最大，便于自动化截图分析。
        // - 200x80 是设备无关单位（dpi），Android 端按屏幕密度放大为约 525x210 物理像素。
        // - 居中放置，确保必然压在相机画面中央，让"谁盖住谁"一目了然。
        bool probeHighlighted = true; // 探针当前颜色状态：true=品红，false=青色。
        var probe = new MauiButton
        {
            Text = "SPIKE",
            WidthRequest = 200,
            HeightRequest = 80,
            CornerRadius = 12,
            FontSize = 24,
            FontAttributes = MauiFontAttributes.Bold,
            BackgroundColor = MauiColor.FromArgb("#FF00FF"),
            TextColor = MauiColor.FromArgb("#000000"),
            HorizontalOptions = MauiLayoutOptions.Center,
            VerticalOptions = MauiLayoutOptions.Center,
        };

        // 探针的点击行为：特意做成"非破坏性"，只切换背景色（品红 ⇄ 青色），不碰相机。
        // 它用来证明两件事：
        //   ① 点击能命中悬浮在预览上方的 MAUI 按钮（事件穿透正常）；
        //   ② 相机视图始终挂在 Grid 里、预览持续运行。
        // 注意：千万不要把相机从 Grid 里摘下来再放回去——ZXing 的相机会话不会自动重建，
        // 预览会永久空白（详见 docs/maui-overlay-verification.md 踩坑记录第 3 条）。
        probe.Clicked += (_, _) =>
        {
            probeHighlighted = !probeHighlighted;
            probe.BackgroundColor = MauiColor.FromArgb(probeHighlighted ? "#FF00FF" : "#00FFFF");

            SpikeLog($"probe tapped highlighted={probeHighlighted} children={grid.Children.Count}");
            DumpSpikeTree(grid, probe, camera);
            Services.ToastService.ShowToastShort(probeHighlighted ? "probe magenta" : "probe cyan");
        };

        // 按钮后加入，落在 index 1 → 绘制在相机（index 0）之上。
        grid.Children.Add(probe);

        // ---------- 交给宿主 ----------
        // 把整棵 MAUI 视图树赋给 MauiControlHost.Content：宿主经 MAUI handler 管线
        //（IViewHandler.CreatePlatformView）生成平台视图，再由 NativeControlHost
        // 挂进 Avalonia 视图层（该原生视图位于 Avalonia 渲染表面之上）。
        host.Content = grid;
        this.spikeCamera = camera;

        SpikeLog($"built children={grid.Children.Count} (tap the probe to repaint it)");
        DumpSpikeTree(grid, probe, camera);
        return camera;
    }

    /// <summary>
    /// SPIKE：诊断日志出口，统一打 [SPIKE] 前缀便于过滤。
    /// Android 上 System.Diagnostics.Debug.WriteLine 会以 [类别] 格式出现在
    /// logcat 的 app_process64 标签下，可在 adb logcat -s 时直接 grep。
    /// </summary>
    private static void SpikeLog(string message)
    {
        System.Diagnostics.Debug.WriteLine(message, "[SPIKE]");
    }

    /// <summary>
    /// SPIKE 诊断：延迟 3 秒（等待 MAUI handler 完成连接、平台视图就绪）后，把
    /// MAUI 视图树 / handler / 平台视图的现状打到日志，用于确认：
    /// - Grid 的平台视图（期望 LayoutViewGroup）是否挂进了宿主；
    /// - 探针按钮（期望 MauiMaterialButton）和相机（期望 CameraX PreviewView）
    ///   是否同属一个 ViewGroup —— 这是它们能互相层叠的前提。
    /// </summary>
    private static async void DumpSpikeTree(MauiGrid grid, MauiButton probe, CameraBarcodeReaderView camera)
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(3000);

            var gridPv = grid.Handler?.PlatformView;   // Grid 的原生平台视图
            var probePv = probe.Handler?.PlatformView; // 按钮的原生平台视图
            var camPv = camera.Handler?.PlatformView;  // 相机的原生平台视图

            var sb = new System.Text.StringBuilder();
            sb.Append($"children={grid.Children.Count}");
            sb.Append($" gridPv={Describe(gridPv)}");
            sb.Append($" probeFrame={probe.Frame} probeW={probe.Width} probeH={probe.Height} probeVisible={probe.IsVisible}");
            sb.Append($" probePv={Describe(probePv)}");
            sb.Append($" probePvParentIsGridPv={ReferenceEquals(GetProp(probePv, "Parent"), gridPv)}");
            sb.Append($" camPv={Describe(camPv)}");
            sb.Append($" camPvParentIsGridPv={ReferenceEquals(GetProp(camPv, "Parent"), gridPv)}");

            SpikeLog(sb.ToString());
        }
        catch (System.Exception ex)
        {
            // 诊断代码不应影响主流程：任何异常只记录，不上抛。
            SpikeLog($"ERROR {ex}");
        }
    }

    /// <summary>
    /// SPIKE：用反射读取对象属性。
    /// 共享工程目标是 net9.0（非 -android），引用不了 Android 类型，反射是最省事的兼容写法。
    /// </summary>
    private static object? GetProp(object? o, string name) =>
        o is null ? null : o.GetType().GetProperty(name)?.GetValue(o);

    /// <summary>
    /// SPIKE：把平台视图描述成字符串（类型名 + 父类型 + 子视图数量），
    /// 比直接打印对象更直观，且不需要引用平台特定 API。
    /// </summary>
    private static string Describe(object? view)
    {
        if (view is null)
        {
            return "null";
        }

        var t = view.GetType();
        var parent = GetProp(view, "Parent");
        var childCount = GetProp(view, "ChildCount");
        return $"{t.Name}(parent={parent?.GetType().Name ?? "null"},children={childCount ?? "-"})";
    }

    /// <summary>手电筒当前开关状态：避免每次切换都去查询相机状态。</summary>
    bool torchState = false;

    /// <summary>
    /// 手电筒切换：由 ScannerViewModel.ToggleTorch() 触发。
    /// ViewModel 只负责发事件，真正切换 ZXing 相机的 IsTorchOn 在这里执行。
    /// </summary>
    private void TorchToggled()
    {
        System.Diagnostics.Debug.WriteLine(nameof(TorchToggled), "[TRACE]");

        torchState = !torchState;
        if (this.cameraBarcodeReaderView is not null)
        {
            this.cameraBarcodeReaderView.IsTorchOn = torchState;
        }
    }

    /// <summary>当前使用的摄像头方位（后置 / 前置），切换逻辑见 CameraLocationToggled。</summary>
    CameraLocation cameraLocation = CameraLocation.Rear;

    /// <summary>
    /// 摄像头方位切换：由 ScannerViewModel.ToggleCameraLocation() 触发，
    /// 在后置 / 前置之间轮换。
    /// </summary>
    private void CameraLocationToggled()
    {
        System.Diagnostics.Debug.WriteLine(nameof(CameraLocationToggled), "[TRACE]");

        this.cameraLocation = this.cameraLocation switch
        {
            CameraLocation.Rear => CameraLocation.Front,
            _ => CameraLocation.Rear,
        };

        if (this.cameraBarcodeReaderView is not null)
        {
            this.cameraBarcodeReaderView.CameraLocation = this.cameraLocation;
        }
    }

    /// <summary>
    /// 恢复识别：由 MainViewModel.ScanCommand 在重新进入扫描页时调用。
    /// BarcodesDetected 检出后会先停扫（IsDetecting=false），这里把它拨回 true 继续扫描。
    /// </summary>
    public void StartDetecting()
    {
        if (this.cameraBarcodeReaderView is not null)
        {
            this.cameraBarcodeReaderView.IsDetecting = true;
            System.Diagnostics.Debug.WriteLine($"IsDetecting: {this.cameraBarcodeReaderView.IsDetecting}", "[INFO]");
        }
    }

    /// <summary>
    /// 条码检出回调（由 ZXing 触发，运行在相机/后台线程，非 UI 线程）。
    /// 流程：停扫 → 若手电筒开着则关闭 → 切回 UI 线程 → 恰好一个结果时交给 ViewModel 展示。
    /// 结果展示（隐藏扫描页、弹出结果卡片）由 MainViewModel.ScanResult 完成，界面在 MainView.axaml。
    /// </summary>
    private void BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (this.cameraBarcodeReaderView is not null)
        {
            // 先停扫：避免同一画面连续触发多次识别。
            this.cameraBarcodeReaderView.IsDetecting = false;
            System.Diagnostics.Debug.WriteLine($"IsDetecting: {this.cameraBarcodeReaderView.IsDetecting}", "[INFO]");

            // 识别瞬间如果开着闪光灯，检出后关掉，避免一直亮着。
            if (torchState)
            {
                TorchToggled();
            }
        }

        System.Diagnostics.Debug.WriteLine(nameof(BarcodesDetected), "[TRACE]");

        // ZXing 回调不在 UI 线程，必须切回 UI 线程再改 ViewModel 状态。
        Dispatcher.UIThread.Post(() =>
        {
            // 只处理"恰好一个结果"的情况；多个 / 零个结果静默忽略（与 Options.Multiple=false 呼应）。
            if (e.Results.Length == 1 && DataContext is ScannerViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"{nameof(BarcodesDetected)} {e.Results[0].Format} --> {e.Results[0].Value}", "[INFO]");

                vm.ReceiveScanResult(e.Results[0].Value);
            }
        });
    }
}
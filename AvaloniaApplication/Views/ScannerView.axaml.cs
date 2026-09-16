using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Maui.Controls;
using Avalonia.Threading;
using AvaloniaApplication.ViewModels;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

// ========================================================
// 类型别名（MAUI 悬浮相机控件用）
// --------------------------------------------------------
// Microsoft.Maui.Controls 和 Avalonia.Controls 里都有 Button / Grid 这些同名类型，
// 两个命名空间全量 using 会冲突，所以给 MAUI 侧的类型起别名。
// 下面代码里 MauiButton / MauiGrid / MauiColor / MauiFontAttributes / MauiLayoutOptions
// / MauiHorizontalStackLayout / MauiThickness 都明确表示"这是 MAUI 控件"，
// 避免和 Avalonia 的同名控件混淆。
// ========================================================
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiFontAttributes = Microsoft.Maui.Controls.FontAttributes;
using MauiGrid = Microsoft.Maui.Controls.Grid;
using MauiHorizontalStackLayout = Microsoft.Maui.Controls.HorizontalStackLayout;
using MauiLayoutOptions = Microsoft.Maui.Controls.LayoutOptions;
using MauiThickness = Microsoft.Maui.Thickness;

namespace AvaloniaApplication;

/// <summary>
/// 扫码页面视图。
///
/// 相机预览与右上角悬浮按钮组由代码组装成一棵 MAUI 视图树，作为
/// MauiControlHost.Content 交给原生层渲染。
///
/// 背景知识（决定本文件机制的关键约束）：
/// - Avalonia 自己画的任何内容（包括按钮）都画在同一个 SurfaceView 里，这个表面位于
///   原生视图（MauiControlHost 承载的 MAUI 视图）的下面一层，所以 Avalonia 按钮永远
///   盖不住相机预览。
/// - 悬浮在预览上的控件必须和相机待在同一个 MAUI 视图树里：MAUI Grid 在 Android 上的
///   平台视图是 LayoutViewGroup，子控件按 Children 下标顺序绘制，下标大的在上层。
/// 详细说明与验证数据见 docs/maui-overlay-verification.md 与 docs/adr/0001-camera-overlay-must-be-maui.md。
/// </summary>
public partial class ScannerView : UserControl
{
    /// <summary>
    /// 当前使用的 ZXing 相机条码识别视图（MAUI 控件）。
    /// 在 OnLoaded 里由 BuildCameraContent 构建；Torch / CameraLocation / StartDetecting / BarcodesDetected
    /// 都通过它操作相机。
    /// </summary>
    private CameraBarcodeReaderView? cameraBarcodeReaderView;

    /// <summary>
    /// 缓存 BuildCameraContent 里创建好的相机实例。
    /// OnLoaded 可能被多次调用（详见 OnLoaded），用它做幂等保护，避免重复构建视图、
    /// 重复订阅事件，也避免悬浮按钮被重复添加。
    /// </summary>
    private CameraBarcodeReaderView? cameraContent;

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
    /// 1) 构建"相机 + 右上角悬浮按钮组"的 MAUI 视图树并赋给 MauiControlHost.Content；
    /// 2) 设置条码识别参数（只识别一维条码、自动旋转、一次只出一个结果）；
    /// 3) 把 ViewModel 的事件桥接到本视图（MAUI 控件无法用 Avalonia 绑定，走事件机制）；
    /// 4) 启动识别。
    /// </summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"{nameof(ScannerView)}.{nameof(OnLoaded)}", "[TRACE]");

        // 相机与悬浮按钮在代码中组装成整棵 MAUI 视图树，赋给 MauiControlHost.Content。
        this.cameraBarcodeReaderView = BuildCameraContent(this.Get<MauiControlHost>("cameraBarcodeReaderHost"));

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
    /// 构建"相机 + 右上角悬浮按钮组"的 MAUI 视图树并赋给 MauiControlHost.Content。
    /// 这是悬浮按钮能显示在相机预览之上的核心机制。
    /// </summary>
    /// <remarks>
    /// 机制说明：
    /// - MauiControlHost.Content 的类型是 Microsoft.Maui.Controls.View，MAUI 的 Grid 也继承自 View，
    ///   所以 Content 可以是一棵包含相机和按钮的 MAUI 视图树，而不仅仅是一个叶子控件。
    /// - MAUI Grid 在 Android 上的平台视图是 LayoutViewGroup：子控件按 Children 下标顺序绘制，
    ///   下标小的在下层，下标大的在上层。
    /// - 因此相机放 index 0、按钮面板放 index 1，按钮就会渲染在相机预览之上，且点击事件可以直接
    ///   命中悬浮按钮。此能力是 Avalonia 自绘按钮不具备的（背景见 docs/adr/0001-camera-overlay-must-be-maui.md）。
    /// </remarks>
    private CameraBarcodeReaderView BuildCameraContent(MauiControlHost host)
    {
        // ---------- 幂等保护 ----------
        // OnLoaded 会被多次调用（视图再次进入可视化树时），第一次构建后把相机实例缓存起来，
        // 之后直接复用：既避免重复创建视图、重复订阅 BarcodesDetected，也避免重复添加悬浮按钮组。
        if (this.cameraContent is not null)
        {
            return this.cameraContent;
        }

        // ---------- 相机（下层，index 0） ----------
        // 创建 ZXing 的相机识别视图，检出事件接到本类的 BarcodesDetected 处理器。
        var camera = new CameraBarcodeReaderView();
        camera.BarcodesDetected += BarcodesDetected;

        // 组装 MAUI Grid：相机先放入 index 0 作为背景预览。
        var grid = new MauiGrid();
        grid.Children.Add(camera);

        // ---------- 右上角悬浮按钮组（上层，index 1） ----------
        // 与相机同处一个 MAUI Grid 才能压住相机预览（见 docs/adr/0001-camera-overlay-must-be-maui.md）。
        // 三个按钮是底部命令栏三个操作（开关闪光灯 / 关闭扫描页 / 切换前后摄像头）的第二组入口：
        // 点击调用同一个 ViewModel 操作，效果与命令栏一致；配色沿用命令栏，方便两处一一对应。
        var panel = new MauiHorizontalStackLayout
        {
            // 深色半透明底板：相机画面是亮画面，垫高按钮文字与边界的对比度。
            // 顶部留 28 dip 内边距，让按钮避开系统状态栏（模拟器实测按钮贴在 y=0 起会被压住）。
            BackgroundColor = MauiColor.FromArgb("#80000000"),
            Spacing = 8,
            Padding = new MauiThickness(8, 28, 8, 8),
            HorizontalOptions = MauiLayoutOptions.End,
            VerticalOptions = MauiLayoutOptions.Start,
        };

        panel.Children.Add(MakeOverlayButton("灯控", "#F0E68C", "#FFFF00", vm => vm.ToggleTorch()));
        panel.Children.Add(MakeOverlayButton("取消", "#FFE4C4", "#FFA500", vm => vm.CancelCommand()));
        panel.Children.Add(MakeOverlayButton("切换摄像头", "#F0F8FF", "#0000FF", vm => vm.ToggleCameraLocation()));

        // 面板后加入，落在 index 1 → 绘制在相机（index 0）之上、贴在预览右上角。
        grid.Children.Add(panel);

        // ---------- 交给宿主 ----------
        // 把整棵 MAUI 视图树赋给 MauiControlHost.Content：宿主经 MAUI handler 管线
        //（IViewHandler.CreatePlatformView）生成平台视图，再由 NativeControlHost
        // 挂进 Avalonia 视图层（该原生视图位于 Avalonia 渲染表面之上）。
        host.Content = grid;
        this.cameraContent = camera;

        return camera;
    }

    /// <summary>
    /// 构造右上角悬浮按钮：紧凑尺寸、粗体，文案与配色按调用方给定。
    /// MAUI 按钮用不上 Avalonia 命令绑定，点击直接调用 ScannerViewModel 的操作；
    /// Torch / Camera 的相机动作仍由现有事件桥接（TorchToggled / CameraLocationToggled）落到底层控件。
    /// </summary>
    private MauiButton MakeOverlayButton(string text, string backgroundHex, string foregroundHex, System.Action<ScannerViewModel> action)
    {
        var button = new MauiButton
        {
            Text = text,
            WidthRequest = 78,
            HeightRequest = 42,
            CornerRadius = 8,
            FontSize = 14,
            FontAttributes = MauiFontAttributes.Bold,
            BackgroundColor = MauiColor.FromArgb(backgroundHex),
            TextColor = MauiColor.FromArgb(foregroundHex),
        };
        button.Clicked += (_, _) =>
        {
            if (DataContext is ScannerViewModel vm)
            {
                action(vm);
            }
        };
        return button;
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
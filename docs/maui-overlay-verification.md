# MAUI 按钮悬浮相机预览：验证记录与实现说明

- 日期：2026-09-16
- 范围：从「把刚才的代码再写入文件中，人工再验证一下」之后的所有问答与验证工作
- 状态：验证已完成，正式实现待定（见 §10）
- 相关代码：`AvaloniaApplication/Views/ScannerView.axaml`、`ScannerView.axaml.cs`（当前保留验证用的探针代码，见 §6）

---

## 1. 结论摘要

| 命题 | 结论 |
|---|---|
| Avalonia 按钮能否显示在 `MauiControlHost` 之上 | **不能**（平台合成模型决定，见 §2） |
| MAUI 按钮放进 `MauiControlHost.Content`（一个 MAUI `Grid`）能否浮在相机预览之上 | **能**，已模拟器像素级验证（见 §4） |
| 早期观察到「按钮不显示」 | **错误结论**，是部署假象，已纠正（见 §4 后半、§5） |

---

## 2. 为什么 Avalonia 内容盖不上去（合成模型）

- `controls:MauiControlHost` 继承自 Avalonia 的 `NativeControlHost`（包：`Avalonia.Maui` 11.1.0，仓库 `github.com/AvaloniaUI/AvaloniaMauiHybrid`）。
- Android 上，Avalonia 的**整套 UI（包括所有按钮）画在同一个 `InvalidationAwareSurfaceView` 里**；`AndroidNativeControlHostImpl` 通过 `AddView` 把 MAUI 视图的原生 View 挂进同一 `ViewGroup`。
- Android `ViewGroup` 按子视图索引顺序绘制 → 后加入的原生视图画在 Avalonia 表面之上。
- 因此无论怎么调 Avalonia 的 `ZIndex`/层，Avalonia 像素都在相机原生视图**下面**；MAUI 侧按钮与相机是同一个原生 `ViewGroup` 里的兄弟，靠下标定先后，所以能压在预览之上。

**证据**（`uiautomator dump`）：

```
FrameLayout [0,0][1080,2400]
  SurfaceView [0,0][1080,2400]     ← Avalonia 渲染表面（整个 UI）
  ViewGroup   [0,0][1080,2216]     ← MauiControlHost 承载的 MAUI Grid
    FrameLayout ...                 ← CameraX PreviewView
      SurfaceView ...               ← 相机预览表面
```

---

## 3. MAUI 方案：机制

- `MauiControlHost.Content` 的类型是 `Microsoft.Maui.Controls.View`；MAUI 的 `Grid` 也是 `View`，所以 `Content` 可以是一棵包含相机和按钮的 MAUI 视图树。
- 经 MAUI handler 管线（`IViewHandler.CreatePlatformView()` → `PlatformView`）生成原生视图：`Grid` → `LayoutViewGroup`，子控件按 `Children` 顺序绘制，**后加的在上层**。
- 当前代码结构（`ScannerView.axaml.cs` 的 `BuildSpikeContent`）：

```
MauiControlHost.Content              （MAUI Grid → LayoutViewGroup）
├── [0] CameraBarcodeReaderView      ← 相机预览（下层）
└── [1] SPIKE 按钮                   ← 探针（上层，可压住预览）
```

---

## 4. 验证过程与结果

### 4.1 流程

1. 把验证代码写回 `ScannerView.axaml`（`MauiControlHost` 留空）与 `ScannerView.axaml.cs`（C# 组装 MAUI Grid）。
2. 重启模拟器 `pixel_7_-_api_36_0`。
3. 编译 Android APK：`dotnet build AvaloniaApplication.Android\AvaloniaApplication.Android.csproj -f net9.0-android -p:EmbedAssembliesIntoApk=true`（先删输出 APK 避免文件锁）。
4. `adb install -r` → 授权相机 → 启动 → 点中间“Scan”进入扫描页。
5. 用 `adb exec-out screencap -p` 截图 + Python/PIL 做像素分析。

### 4.2 结果数据

状态一（初始）：相机 + 品红按钮同屏

```
magenta = 26376   bbox x[278..802] y[1004..1212]   ← 按钮在预览上方
camera (540,400) = (122,92,64)                      ← 相机虚拟场景正常渲染
```

状态二（点击按钮后）：

```
magenta = 0  cyan = 26377   probe(350,1108)=(0,255,255)   ← 点击到达叠加层按钮，变青色
camera (540,400) = (122,92,64)                             ← 相机全程保持实时
```

运行时日志（logcat，tag 为进程名，出现两次是 DumpSpikeTree 的重复输出）：

```
[SPIKE]: built children=2
[SPIKE]: probePv=MauiMaterialButton(parent=LayoutViewGroup) probePvParentIsGridPv=True
         camPv=PreviewView(parent=LayoutViewGroup, children=1) camPvParentIsGridPv=True
```

截图与像素分析文件：`C:\Users\admin\AppData\Local\Temp\spike\I_initial.png`、`J_tapped.png`。

### 4.3 早期错误结论的纠正

第一轮验证报「按钮不显示」，真实原因是**部署假象**：该工程 Debug 构建默认走 **Fast Deployment**，managed 程序集不在 APK 里，而当时用 `adb install` 手动装 APK，装进去的一直是修改前的旧程序集。

确认方法：早期截图 `scanner.png` 里有 Khaki(240,230,140)/Bisque(255,228,196)/AliceBlue(240,248,255) 三色（原始三个 Avalonia 底部按钮），却没有品红——跑的是原版代码。改用 `-p:EmbedAssembliesIntoApk=true` 重新打包后，新代码才真正部署。

---

## 5. 踩坑记录（重要，避免重蹈）

1. **Fast Deployment**：Debug 构建默认程序集不进 APK。改代码后要么 `-p:EmbedAssembliesIntoApk=true` 打自包含 APK 再 `adb install`，要么 `dotnet build -t:Install` 让它自己推送。直接 `adb install` 会碰到「改了不生效」甚至启动崩溃（`No assemblies found ... MUST be STORED`）。
2. **XABLD7000 文件锁**：打包 APK 时报 `Renaming temporary file failed: Permission denied`。删掉 `bin/Debug/net9.0-android/*.apk` 与 `obj/.../android/bin/*.apk` 再编译即可。
3. **相机视图不可摘除再挂回**：运行时从 MAUI `Grid` 移除 `CameraBarcodeReaderView` 再重新 `Children.Insert` 回来，预览不会恢复（ZXing 相机会话不会自动重建）。**相机视图应永远作为静态子元素留在 Grid 里。**
4. **`MainView.axaml` 黑幕 `Opacity="90"` 超出 Avalonia 范围 0–1**：实际渲染为完全不透明（本意大概是 0.9 半透明遮罩）。属顺带发现的小 bug，未修复。

---

## 6. SPIKE 按钮说明

- 性质：**临时验证探针**，不是产品功能；验证完成后会被真实的 Torch / Cancel / Camera 三个 MAUI 按钮替换。
- 结构：`MauiButton`（`Microsoft.Maui.Controls.Button`），200×80 dip 居中，品红 `#FF00FF`（与相机画面的棕色反差最大，便于像素级客观验证），黑字 `SPIKE`。
- 点击行为（**非破坏性**，特意如此）：品红 ⇄ 青色切换 + Toast + `[SPIKE]` 日志，证明点击到达叠加层按钮；相机始终挂在 Grid 里不动。
- 最初版本做成「点一下把相机摘掉/加回」，因暴露第 5.3 条的问题而改为非破坏性。
- 命名冲突处理：文件顶部用别名 `MauiButton`/`MauiGrid`/`MauiColor` 等，避免与 `Avalonia.Controls` 的 `Button`/`Grid` 冲突。

---

## 7. Torch（现状）与 SPIKE（MAUI 方案）机制对比

| 维度 | Avalonia `Button`（Torch，现状） | MAUI `Button`（SPIKE/目标方案） |
|---|---|---|
| 声明位置 | `ScannerView.axaml:31-43`（XAML） | `ScannerView.axaml.cs`（C# 构建） |
| 原生形态 | 无（Skia 画进 Avalonia surface） | `MauiMaterialButton`（真实 Android View） |
| 能盖住相机预览 | 不能（所有 Avalonia 像素在原生视图下层） | 能（与相机同一 `LayoutViewGroup`，index 1 在上） |
| 布局 | Avalonia `Grid.Row=1` 底部栏，不重叠 | MAUI `Grid` 居中，压住预览 |
| 事件/绑定 | `Command="{Binding ToggleTorch}"`，编译绑定 + `MethodToCommandConverter` 包装方法 | `Clicked +=` 直接调 VM 方法 |
| 样式 | Fluent 主题 + XAML 属性 | MAUI 主题 + `BackgroundColor`/`TextColor`/`CornerRadius` |
| 无障碍树 | 不可见 | 可见 |

---

## 8. 与「标准原生 View 嵌入」的对比

「标准实现」含义有二：

- **纯 Android 应用**：原生控件直接进 `ViewGroup`（相机 App 全屏预览 + 悬浮按钮的标准写法）。
- **Avalonia 应用**：`NativeControlHost` + 原始 Android `View`（Avalonia 官方规定的嵌入方式）。

| 维度 | 纯 Avalonia（Torch） | `NativeControlHost` + 原生 View | `MauiControlHost` + MAUI（SPIKE） |
|---|---|---|---|
| 能否盖住预览 | 不能 | 能 | 能 |
| 代码写在哪 | 共享工程 XAML | **必须写在 Android 平台工程**（共享层是 `net9.0` 引用不了 `Android.Views`） | 共享工程 C# |
| 布局单位 | Avalonia Grid/边距 | dp、`FrameLayout`/`gravity`/`weight` | MAUI `Grid`/`Alignment` |
| 样式 | Fluent | Android `styles.xml`/Material | MAUI 主题 |
| MVVM/绑定 | 编译绑定 | 无，手写接线 | 事件回调直调 VM |
| 跨平台 | 全部 Avalonia 平台 | 仅 Android | Android/iOS 同一份 MAUI 代码 |

**结论**：三个按钮（Torch/Cancel/Camera）没有需要 Android 专属能力的理由，标准原生做法要多承担「代码进平台工程 + 手写布局 + 放弃绑定」三笔成本而无收益。推荐走 MAUI 路线；仅当将来覆盖层需要取景框/扫描线/复杂动画等 Android 底层精细控制时，才值得考虑标准做法。

---

## 9. 当前扫码结果展示链路

1. `BarcodesDetected`（ScannerView）：`IsDetecting=false` 停扫 → `Length==1` 时传 `e.Results[0].Value` 给 `ScannerViewModel.ReceiveScanResult`。
2. `ReceiveScanResult` → `MainViewModel.ScanResult = 值` → setter 里 `ShowScanner=false; ShowResult=true`。
3. `MainView.axaml`：全屏黑幕 + 中央白色卡片（`LightGreen` 边框），三行：标题 `Scan Result:` / 内容 `{Binding ScanResult}` / `Scan Again` 按钮。
4. `Scan Again` → `ScanCommand`：重置 `ScanResult` → 权限检查 → `ShowScanner=true; Scanner.StartDetecting()`（Scanner 实例缓存复用，相机不重建）。

注意点：
- 只接受单个结果（`Multiple=false` + `Length==1`），多码/空结果静默无提示。
- 结果只有条码原文，无复制/历史/格式识别。
- 结果展示逻辑在纯 Avalonia 层（`MainView`），不受 §3 按钮改造影响。

---

## 10. 当前代码状态与后续

- **代码状态**：SPIKE 探针按钮与 `[SPIKE]` 诊断日志已**移除**；相机与右上角悬浮按钮组由
  `ScannerView.axaml.cs` 的 `BuildCameraContent` 在代码中组装（见 §11）。**底部 Avalonia
  按钮栏保留**（决策：作为命令栏，与右上角悬浮按钮组互为第二入口；不是"移除/替换"）。
- **遗留待定决策**：
  - Q2：桌面端是否要保持可用？（MAUI 侧按钮在桌面无宿主，不会显示；当前桌面端只有底部命令栏）
  - Q3：除三个按钮外，是否还有别的要浮在预览上的元素（取景框、扫描线等）？

---

## 11. 右上角悬浮按钮组（正式实现）验证

探针按钮与诊断脚手架已移除，替换为右上角 Torch / Cancel / Camera 三个真实按钮
（MAUI `MauiButton`，与相机同处一个 `LayoutViewGroup`：相机 index 0、按钮面板 index 1，
面板 `HorizontalOptions=End, VerticalOptions=Start`，`#80000000` 半透明底板，顶部留 28 dip
内边距避开状态栏）。架构约束记录于 `docs/adr/0001-camera-overlay-must-be-maui.md`。

### 验证结果

| 检查项 | 结果 |
|---|---|
| 右上角水平一行三按钮、右对齐、尺寸紧凑 | ✅ 模拟器实测节点：灯控 `[402,74][607,185]`、取消 `[628,74][833,185]`、切换摄像头 `[854,74][1059,185]`（1080px 宽屏，右缘距屏边约 21px = 8dip 内边距 × 2.625 密度） |
| 显示在相机实时画面之上 | ✅ `dumpsys activity top`：按钮位于相机 `PreviewView` 之后的兄弟布局内，绘制于其上 |
| 状态栏遮挡 | ✅ 顶部 28dip 内边距后按钮 y 起点 74px，位于状态栏交互区域之下；⚠️ 模拟器实测该区域（y≈130 以下）部分输入会被系统顶部区域吞掉，点击按钮中下部（y≥150）事件正常 |
| 点击接线（Torch） | ✅ logcat `[TRACE]: TorchToggled` → `TorchControl: Unable to enableTorch due to there is no flash unit`（模拟器相机无闪光灯单元，视觉效果无法在模拟器验证，接线本身正确） |
| 点击接线（Cancel / Camera） | ✅ 用户手动验证确认（点击关闭扫描页 / 切换前后摄像头与命令栏一致） |
| 扫码成功→结果页不遮挡、Scan Again 恢复、桌面端启动无异常 | ✅ 用户手动验证确认 |
| 幂等性（重复进出扫描页） | ✅ 按钮节点每次只出现一组；构建逻辑在首次后缓存复用 |
| Debug APK 快速部署坑 | 见 `docs/build-apk.md`：直接 `adb install` Debug APK 会跑设备上残留的旧程序集；安装需 `-p:EmbedAssembliesIntoApk=true` 或 Release |

### 验证环境

- 模拟器：`pixel_7_-_api_36_0`（API 36，1080×2400，软件渲染 `swiftshader_indirect`）。
- 自动化手段：`uiautomator dump` 取按钮节点与坐标、`dumpsys activity top` 取视图层级、
  `logcat` 取事件日志（`System.Diagnostics.Debug.WriteLine` 落在 `app_process64` 标签）；
  按钮点击行为部分由用户手动验证确认。
- 模拟器相机无闪光灯单元，Torch 的"开/关可见效果"需在带闪光灯的真机/可模拟闪光的设备上确认。
- 按钮文案在手动验证阶段由用户改为中文（灯控 / 取消 / 切换摄像头）；按钮为定宽
  （78dip），文案不影响位置，上表坐标在改动前后一致。

---

*截图与运行日志的原始素材位于 `C:\Users\admin\AppData\Local\Temp\spike\`（本机临时目录，可清理）。*
# 编译 Android APK 指南（Avalonia-scanner）

本文说明怎么把这个仓库编译成可安装 / 可分发的 Android APK，以及在部署、运行验证时会遇到的坑。

## 环境要求

- **.NET SDK 9.0+**，带 Android 工作负载。命令行方式：
  `dotnet workload install android`；用 Visual Studio 的话在安装器里勾选 ".NET Android 开发"。
- **Android SDK + JDK**：构建时需要。模拟器运行还需 system-images（用 Visual Studio 的 Android 模拟器管理器或 `avdmanager` 建 AVD）。
- **adb**：`<Android SDK>/platform-tools/adb.exe`。若 adb 不在 PATH，直接引用完整路径即可。

## 构建 APK

### 常规 Debug 构建

```bash
dotnet build AvaloniaApplication.Android/AvaloniaApplication.Android.csproj -c Debug
```

产物在 `AvaloniaApplication.Android/bin/Debug/net9.0-android/`：
- `com.CompanyName.AvaloniaTest-Signed.apk`（已签名，用于安装）
- `com.CompanyName.AvaloniaTest.apk`（未签名中间产物）

### 直接可分发 / 可靠安装的 APK（重要）

**Debug 构建默认启用"快速部署"（fast deployment）：程序集不打包进 APK**，而是在 IDE 或
MSBuild 部署管线（`-t:Install` / `-t:Run`）运行时才单独推到设备的应用私有目录。

后果：**只执行 `adb install` 安装这个 Debug APK，程序集会用设备上残留的旧版本**——
界面和源码对不上（改完代码后界面还是老样子），排查起来非常迷惑（本仓库实测遇到：安装后仍显示
已被删除的探针按钮）。只有当设备上从未装过旧版本、或借助完整部署管线时，Debug APK 才是新的。

要得到一个"装完即跑当前代码"的 APK，强制把程序集内嵌进 APK：

```bash
dotnet build AvaloniaApplication.Android/AvaloniaApplication.Android.csproj -c Debug -p:EmbedAssembliesIntoApk=true
```

**Release 构建默认内嵌程序集**，同样适合做可分发包：

```bash
dotnet build AvaloniaApplication.Android/AvaloniaApplication.Android.csproj -c Release
```

## 部署与启动

```bash
# 安装（-r 覆盖安装保留数据）
adb install -r AvaloniaApplication.Android/bin/Debug/net9.0-android/com.CompanyName.AvaloniaTest-Signed.apk

# 启动：启动 Activity 的名称是混淆过的（crc647167d0bf37611f10.MainActivity），
# 用 monkey 按默认启动 Intent 起应用最省事
adb shell monkey -p com.CompanyName.AvaloniaTest -c android.intent.category.LAUNCHER 1
```

- 首次运行会弹相机权限对话框；测试时直接在命令行预授权，省得点对话框：
  `adb shell pm grant com.CompanyName.AvaloniaTest android.permission.CAMERA`
- 若打开失败/行为异常，先 `adb uninstall com.CompanyName.AvaloniaTest` 清掉旧数据和旧程序集再重装。

## 运行验证小抄

- **日志**：`adb logcat -d -s app_process64`。应用里 `System.Diagnostics.Debug.WriteLine` 的
  `[TRACE]` / `[INFO]` 前缀会出现在该标签下，可粗查流程是否走通。
- **UI 节点**：`adb shell uiautomator dump /sdcard/ui.xml` 后 `adb shell cat /sdcard/ui.xml`。
  注意：**只有 MAUI / 原生控件可见**（如相机预览上叠加的按钮）；Avalonia 自绘的内容整个画在
  一张 SurfaceView 里，uiautomator 看不到（如主页的 Scan 按钮，得按屏幕坐标点）。
- **截屏**：`adb exec-out screencap -p > shot.png`。Git Bash 里注意 MSYS 路径转换：
  设备侧路径（`/sdcard/...`）要加 `export MSYS_NO_PATHCONV=1`，否则会被改写成 Windows 路径。
- **模拟器冷启动**：`emulator -avd <名字> -no-snapshot-load -no-audio -no-boot-anim -gpu swiftshader_indirect`，
  等 `adb shell getprop sys.boot_completed` 返回 1 再操作。

## 平台与警告说明

- 共享工程 `AvaloniaApplication/` 目标是纯 `net9.0`；`net9.0-android` 只在 Android head 里。
  Desktop 入口独立构建：`dotnet build AvaloniaApplication.Desktop/AvaloniaApplication.Desktop.csproj`。
- 构建时常见的与代码无关的警告可忽略：
  - `XA0141`：第三方 native 库（SkiaSharp 等）未按 Android 16 的 16 KB 页大小打包；
  - `NETSDK1202`：net9.0-android 工作负载已过支持期（可考虑升级目标框架，属另一项工作）。
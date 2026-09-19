using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AvaloniaApplication;

/// <summary>
/// 嵌入原生 Android 视图的 Avalonia 控件。
///
/// 【核心原理】
/// NativeControlHost 是 Avalonia 提供的"桥梁控件"，用于在 Avalonia 的视觉树中嵌入原生平台控件。
/// 它的工作流程：
///   1. 当这个控件被添加到 Avalonia 视觉树时，Avalonia 会调用 CreateNativeControlCore()
///   2. 你在 CreateNativeControlCore() 中创建原生 Android 控件（如 Button、WebView）
///   3. 将原生控件包装在 AndroidViewControlHandle 中返回
///   4. Avalonia 会自动将原生控件添加到 Android 视图层级，并管理其位置和大小
///
/// 【跨平台设计】
/// 这个类放在共享项目中（AvaloniaApplication），它不包含任何 Android 特定代码。
/// 实际创建 Android 控件的逻辑由平台实现类（EmbedSampleAndroid）提供。
/// 这样同一份共享代码可以同时支持 Android、iOS、Desktop 等多个平台。
///
/// 【静态属性注入】
/// Implementation 属性在应用启动时由平台项目设置（如 MainActivity）。
/// 这是一种简单的依赖注入模式：共享代码定义接口，平台代码提供实现。
/// </summary>
public class EmbedSample : NativeControlHost
{
    /// <summary>
    /// 平台实现的注入点。
    /// 在 Android 项目中，MainActivity 会设置：
    ///   EmbedSample.Implementation = new EmbedSampleAndroid();
    /// </summary>
    public static INativeDemoControl? Implementation { get; set; }

    /// <summary>
    /// 控制显示哪个原生控件（false = Button, true = WebView）。
    /// </summary>
    public bool IsSecond { get; set; }

    /// <summary>
    /// 当控件需要创建原生视图时，Avalonia 会调用此方法。
    ///
    /// 【参数 parent】
    /// 父控件的平台句柄（IPlatformHandle）。在 Android 上，它可以被转换为
    /// AndroidViewControlHandle，从中获取 Android Context（用于创建原生控件）。
    ///
    /// 【返回值】
    /// 返回 IPlatformHandle，包装了你创建的原生控件。
    /// Avalonia 会用这个句柄来定位和显示原生控件。
    ///
    /// 【调用时机】
    /// 当控件被添加到视觉树并完成首次布局时调用一次。
    /// 之后如果控件大小变化，Avalonia 会自动调整原生控件的大小。
    /// </summary>
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        // 委托给平台实现。如果实现为空，回退到基类默认行为（不显示任何内容）
        return Implementation?.CreateControl(IsSecond, parent, () => base.CreateNativeControlCore(parent))
            ?? base.CreateNativeControlCore(parent);
    }

    /// <summary>
    /// 当控件从视觉树移除或被销毁时，Avalonia 会调用此方法。
    /// 用于清理原生控件持有的资源。
    ///
    /// 【重要】
    /// 即使你不需要额外清理，也应该调用 base.DestroyNativeControlCore()，
    /// 让 Avalonia 框架正确释放原生控件的引用。
    /// </summary>
    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}

/// <summary>
/// 平台实现接口。定义了如何创建原生控件。
///
/// 【为什么要用接口？】
/// 共享项目无法引用 Android 特定的类型（如 Android.Widget.Button）。
/// 通过接口，共享代码只关心"给我一个 IPlatformHandle"，
/// 而不关心这个句柄里面是 Button 还是 WebView —— 这由平台实现决定。
///
/// 【跨平台支持】
/// 如果要支持 iOS，只需创建 iOS 版本的实现类：
///   public class EmbedSampleIos : INativeDemoControl { ... }
/// 共享代码完全不需要修改。
/// </summary>
public interface INativeDemoControl
{
    /// <summary>
    /// 创建原生控件并返回其句柄。
    /// </summary>
    /// <param name="isSecond">false = 显示 Button, true = 显示 WebView</param>
    /// <param name="parent">父控件的平台句柄，用于获取 Android Context</param>
    /// <param name="createDefault">创建默认原生控件的回调（备用方案）</param>
    IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault);
}

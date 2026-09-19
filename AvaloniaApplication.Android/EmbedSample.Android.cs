using System;
using Avalonia.Platform;
using Avalonia.Android;
using AvaloniaApplication;

namespace AvaloniaApplication.Android;

/// <summary>
/// EmbedSample 的 Android 平台实现。
///
/// 【职责】
/// 这个类负责创建真正的 Android 原生控件（Button、WebView），
/// 并将它们包装在 AndroidViewControlHandle 中返回给 Avalonia。
///
/// 【AndroidViewControlHandle 是什么？】
/// 它是 Avalonia 提供的包装器，将 Android 的 View 对象包装成 IPlatformHandle 接口。
/// IPlatformHandle 是 Avalonia 框架能理解的"通用句柄"，不管底层是 Android Button 还是 iOS UIButton。
///
/// 【parent 参数的秘密】
/// CreateControl 的 parent 参数是父控件的平台句柄。在 Android 上，它通常是 AndroidViewControlHandle。
/// 我们可以从中获取 Android Context —— 这是 Android 系统中最重要的对象之一，
/// 几乎所有 Android 控件的创建都需要 Context（它提供系统服务、资源、主题等）。
/// </summary>
public class EmbedSampleAndroid : INativeDemoControl
{
    /// <summary>
    /// 创建原生 Android 控件。
    /// </summary>
    /// <param name="isSecond">
    /// false = 创建一个可点击计数的 Button（演示基本交互）
    /// true = 创建一个加载网页的 WebView（演示复杂控件嵌入）
    /// </param>
    /// <param name="parent">
    /// 父控件的平台句柄。通过它可以获取 Android Context。
    /// </param>
    /// <param name="createDefault">
    /// 创建默认原生控件的回调。如果不需要特殊处理，可以调用它。
    /// </param>
    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        // 【获取 Android Context 的两种方式】
        // 方式 1：从父控件获取（推荐）
        //   parent 是 IPlatformHandle，在 Android 上实际是 AndroidViewControlHandle。
        //   它的 View.Context 属性提供了父控件所在的 Activity Context。
        //   这个 Context 与父控件在同一窗口中，共享主题和资源配置。
        //
        // 方式 2：使用全局 Application Context（备用）
        //   global::Android.App.Application.Context 是应用级别的 Context。
        //   注意：Application Context 没有 Activity 引用，某些需要 Activity 的操作会失败。
        //   但在创建普通控件时通常没问题。
        //
        // 【为什么用 global:: 前缀？】
        // 因为项目的命名空间是 AvaloniaApplication.Android，编译器会把 "Android"
        // 解析为 AvaloniaApplication.Android，而不是全局的 Android 命名空间。
        // global:: 强制从全局命名空间开始查找。
        var parentContext = (parent as AndroidViewControlHandle)?.View.Context
            ?? global::Android.App.Application.Context;

        if (isSecond)
        {
            // 【嵌入 WebView】
            // WebView 是 Android 系统的网页浏览器控件。
            // 这里演示如何将一个完整的网页嵌入到 Avalonia 应用中。
            var webView = new global::Android.Webkit.WebView(parentContext);
            webView.LoadUrl("https://www.android.com/");

            // 将 WebView 包装在 AndroidViewControlHandle 中返回。
            // Avalonia 会将这个 WebView 添加到 Android 视图层级，
            // 并自动管理它的位置（与 EmbedSample 控件在 Avalonia 中的位置对齐）。
            return new AndroidViewControlHandle(webView);
        }
        else
        {
            // 【嵌入 Button】
            // 创建一个原生 Android 按钮，演示基本的交互。
            // 这个按钮不是 Avalonia 的 Button，而是 Android 原生的 Button。
            // 它的外观和行为完全由 Android 系统控制。
            var button = new global::Android.Widget.Button(parentContext)
            {
                Text = "Hello from Android!"
            };

            // 【原生事件处理】
            // 这里直接订阅 Android 原生的 Click 事件。
            // 注意：这不是 Avalonia 的事件系统，而是 Android 的。
            // 事件处理代码在 Android 的 UI 线程上执行。
            var clickCount = 0;
            button.Click += (sender, args) =>
            {
                clickCount++;
                // 更新按钮文本，显示点击次数
                // 这个更新会立即反映在界面上，因为 Android 的 UI 更新
                // 在 Android 线程上直接执行（不需要像 Avalonia 那样 Dispatcher.Post）
                button.Text = $"Clicked {clickCount} times!";
            };

            // 包装并返回。Avalonia 会：
            // 1. 将 Button 添加到 Android 视图层级
            // 2. 根据 EmbedSample 控件的大小调整 Button 的大小
            // 3. 根据 EmbedSample 控件的位置调整 Button 的位置
            // 4. 当 EmbedSample 移动或调整大小时，自动同步 Button
            return new AndroidViewControlHandle(button);
        }
    }
}

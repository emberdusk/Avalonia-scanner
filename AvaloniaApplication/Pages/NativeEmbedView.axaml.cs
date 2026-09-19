using Avalonia.Controls;

namespace AvaloniaApplication.Pages;

/// <summary>
/// 原生 Android 视图嵌入演示页面。
///
/// 这个页面本身很简单，核心逻辑都在 AXAML 中：
/// - EmbedSample 控件在 XAML 中声明
/// - 它们会自动创建原生 Android 控件
/// - 无需额外的代码来管理原生控件的生命周期
///
/// 这正是 NativeControlHost 的设计目标：让嵌入原生控件变得尽可能简单。
/// </summary>
public partial class NativeEmbedView : UserControl
{
    public NativeEmbedView()
    {
        InitializeComponent();
    }
}

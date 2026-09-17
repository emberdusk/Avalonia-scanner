using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AvaloniaApplication;

public class NativeScannerButtonHost : NativeControlHost
{
    public static INativeScannerButtonHostImpl? Implementation { get; set; }

    public event Action? TorchClicked;
    public event Action? CancelClicked;
    public event Action? CameraClicked;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        return Implementation?.CreateControl(
            parent,
            () => TorchClicked?.Invoke(),
            () => CancelClicked?.Invoke(),
            () => CameraClicked?.Invoke())
            ?? base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }
}

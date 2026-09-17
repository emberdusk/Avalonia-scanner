using System;
using Avalonia.Platform;

namespace AvaloniaApplication;

public interface INativeScannerButtonHostImpl
{
    IPlatformHandle CreateControl(
        IPlatformHandle parent,
        Action onTorchClicked,
        Action onCancelClicked,
        Action onCameraClicked);
}

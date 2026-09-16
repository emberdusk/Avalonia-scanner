using System;
using Avalonia.Controls;

namespace AvaloniaApplication;

public interface INativeScannerHost
{
    INativeButtonHost CreateButtonHost();
}

public interface INativeButtonHost : IDisposable
{
    NativeControlHost Host { get; }
    event Action? TorchClicked;
    event Action? CancelClicked;
    event Action? CameraClicked;
}

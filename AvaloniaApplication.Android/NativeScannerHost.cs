using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Avalonia.Android.Platform;
using Avalonia.Controls;
using AvaloniaApplication;
using AvaloniaApplication.Services;

namespace AvaloniaApplication.Android;

public class NativeScannerHost : INativeScannerHost
{
    public INativeButtonHost CreateButtonHost()
    {
        return new AndroidButtonHost();
    }
}

public class AndroidButtonHost : INativeButtonHost
{
    private AndroidButtonHostControl? control;

    public NativeControlHost Host
    {
        get
        {
            if (control is null)
            {
                control = new AndroidButtonHostControl();
                control.TorchClicked += () => TorchClicked?.Invoke();
                control.CancelClicked += () => CancelClicked?.Invoke();
                control.CameraClicked += () => CameraClicked?.Invoke();
            }
            return control;
        }
    }

    public event Action? TorchClicked;
    public event Action? CancelClicked;
    public event Action? CameraClicked;

    public void Dispose()
    {
        control?.Dispose();
        control = null;
    }
}

public class AndroidButtonHostControl : NativeControlHost
{
    public event Action? TorchClicked;
    public event Action? CancelClicked;
    public event Action? CameraClicked;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var context = (parent as AndroidViewControlHandle)?.View.Context
            ?? Application.Context;

        var layout = new LinearLayout(context)
        {
            Orientation = Orientation.Horizontal,
        };

        var torchButton = new Button(context)
        {
            Text = "Torch",
        };
        torchButton.SetTextColor(Android.Graphics.Color.Yellow);
        torchButton.SetBackgroundColor(Android.Graphics.Color.Khaki);
        torchButton.Click += (s, e) => TorchClicked?.Invoke();

        var cancelButton = new Button(context)
        {
            Text = "Cancel",
        };
        cancelButton.SetTextColor(Android.Graphics.Color.Orange);
        cancelButton.SetBackgroundColor(Android.Graphics.Color.Bisque);
        cancelButton.Click += (s, e) => CancelClicked?.Invoke();

        var cameraButton = new Button(context)
        {
            Text = "Camera",
        };
        cameraButton.SetTextColor(Android.Graphics.Color.Blue);
        cameraButton.SetBackgroundColor(Android.Graphics.Color.AliceBlue);
        cameraButton.Click += (s, e) => CameraClicked?.Invoke();

        layout.AddView(torchButton);
        layout.AddView(cancelButton);
        layout.AddView(cameraButton);

        return new AndroidViewControlHandle(layout);
    }
}

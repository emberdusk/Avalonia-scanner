using System;
using Android.App;
using Android.Widget;
using Avalonia.Android;
using Avalonia.Platform;
using AvaloniaApplication;

namespace AvaloniaApplication.Android;

public class NativeScannerButtonHostImpl : INativeScannerButtonHostImpl
{
    public IPlatformHandle CreateControl(
        IPlatformHandle parent,
        Action onTorchClicked,
        Action onCancelClicked,
        Action onCameraClicked)
    {
        var context = (parent as AndroidViewControlHandle)?.View.Context
            ?? global::Android.App.Application.Context;

        var layout = new LinearLayout(context)
        {
            Orientation = Orientation.Horizontal,
        };

        var torchButton = new global::Android.Widget.Button(context)
        {
            Text = "Torch",
        };
        torchButton.SetTextColor(global::Android.Graphics.Color.Yellow);
        torchButton.SetBackgroundColor(global::Android.Graphics.Color.Khaki);
        torchButton.Click += (s, e) => onTorchClicked();

        var cancelButton = new global::Android.Widget.Button(context)
        {
            Text = "Cancel",
        };
        cancelButton.SetTextColor(global::Android.Graphics.Color.Orange);
        cancelButton.SetBackgroundColor(global::Android.Graphics.Color.Bisque);
        cancelButton.Click += (s, e) => onCancelClicked();

        var cameraButton = new global::Android.Widget.Button(context)
        {
            Text = "Camera",
        };
        cameraButton.SetTextColor(global::Android.Graphics.Color.Blue);
        cameraButton.SetBackgroundColor(global::Android.Graphics.Color.AliceBlue);
        cameraButton.Click += (s, e) => onCameraClicked();

        layout.AddView(torchButton);
        layout.AddView(cancelButton);
        layout.AddView(cameraButton);

        return new AndroidViewControlHandle(layout);
    }
}

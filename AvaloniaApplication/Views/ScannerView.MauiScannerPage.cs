using System;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using ZXing.Net.Maui.Controls;

namespace AvaloniaApplication.Views.ScannerView;

public class MauiScannerPage : ContentPage
{
    private readonly CameraBarcodeReaderView cameraBarcodeReaderView;
    private readonly Button torchButton;
    private readonly Button cancelButton;
    private readonly Button cameraButton;
    private readonly StackLayout buttonPanel;

    public event Action? TorchToggled;
    public event Action? CameraLocationToggled;
    public event Action? CancelRequested;
    public event Action<string>? BarcodeDetected;

    public MauiScannerPage()
    {
        cameraBarcodeReaderView = new CameraBarcodeReaderView
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
        };

        torchButton = new Button
        {
            Text = "Torch",
            TextColor = Microsoft.Maui.Graphics.Colors.Yellow,
            BackgroundColor = Microsoft.Maui.Graphics.Colors.Khaki,
            BorderColor = Microsoft.Maui.Graphics.Colors.Yellow,
            FontAttributes = FontAttributes.Bold,
            Padding = new Microsoft.Maui.Thickness(15, 8),
            HorizontalOptions = LayoutOptions.Start,
        };
        torchButton.Clicked += (_, _) => TorchToggled?.Invoke();

        cancelButton = new Button
        {
            Text = "Cancel",
            TextColor = Microsoft.Maui.Graphics.Colors.Orange,
            BackgroundColor = Microsoft.Maui.Graphics.Colors.Bisque,
            BorderColor = Microsoft.Maui.Graphics.Colors.Orange,
            FontAttributes = FontAttributes.Bold,
            Padding = new Microsoft.Maui.Thickness(15, 8),
            HorizontalOptions = LayoutOptions.Center,
        };
        cancelButton.Clicked += (_, _) => CancelRequested?.Invoke();

        cameraButton = new Button
        {
            Text = "Camera",
            TextColor = Microsoft.Maui.Graphics.Colors.Blue,
            BackgroundColor = Microsoft.Maui.Graphics.Colors.AliceBlue,
            BorderColor = Microsoft.Maui.Graphics.Colors.Blue,
            FontAttributes = FontAttributes.Bold,
            Padding = new Microsoft.Maui.Thickness(15, 8),
            HorizontalOptions = LayoutOptions.End,
        };
        cameraButton.Clicked += (_, _) => CameraLocationToggled?.Invoke();

        buttonPanel = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Microsoft.Maui.Thickness(0, 0, 0, 20),
            Children = { torchButton, cancelButton, cameraButton },
        };

        var grid = new Grid
        {
            Children = { cameraBarcodeReaderView, buttonPanel },
        };

        Content = grid;
    }

    public void ConfigureReader()
    {
        cameraBarcodeReaderView.Options = new ZXing.Net.Maui.BarcodeReaderOptions
        {
            Formats = ZXing.Net.Maui.BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false,
            TryHarder = false,
            TryInverted = false,
        };
        cameraBarcodeReaderView.BarcodesDetected += OnBarcodesDetected;
    }

    public void StartDetecting()
    {
        cameraBarcodeReaderView.IsDetecting = true;
    }

    public void SetButtonVisibility(bool visible)
    {
        buttonPanel.IsVisible = visible;
    }

    private void OnBarcodesDetected(object? sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (cameraBarcodeReaderView.IsDetecting)
        {
            cameraBarcodeReaderView.IsDetecting = false;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.Results.Length == 1)
            {
                BarcodeDetected?.Invoke(e.Results[0].Value);
            }
        });
    }
}

using Avalonia.Controls;
using AvaloniaApplication.ViewModels;

namespace AvaloniaApplication.Views.ScannerView;

public partial class NativeScannerView : Avalonia.Controls.UserControl
{
    private ZXing.Net.Maui.Controls.CameraBarcodeReaderView? cameraBarcodeReaderView;

    public NativeScannerView()
    {
        InitializeComponent();
    }

    public NativeScannerView(ScannerViewNativeViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.cameraBarcodeReaderView = (ZXing.Net.Maui.Controls.CameraBarcodeReaderView)
            this.Get<Avalonia.Maui.Controls.MauiControlHost>("cameraBarcodeReaderHost").Content!;
        this.cameraBarcodeReaderView.Options = new ZXing.Net.Maui.BarcodeReaderOptions
        {
            Formats = ZXing.Net.Maui.BarcodeFormats.OneDimensional,
            AutoRotate = true,
            Multiple = false,
            TryHarder = false,
            TryInverted = false,
        };

        if (DataContext is ScannerViewNativeViewModel vm)
        {
            vm.TorchToggled += TorchToggled;
            vm.CameraLocationToggled += CameraLocationToggled;
        }

        this.cameraBarcodeReaderView.IsDetecting = true;
        base.OnLoaded(e);
    }

    bool torchState = false;
    private void TorchToggled()
    {
        torchState = !torchState;
        if (this.cameraBarcodeReaderView is not null)
            this.cameraBarcodeReaderView.IsTorchOn = torchState;
    }

    ZXing.Net.Maui.CameraLocation cameraLocation = ZXing.Net.Maui.CameraLocation.Rear;
    private void CameraLocationToggled()
    {
        this.cameraLocation = this.cameraLocation switch
        {
            ZXing.Net.Maui.CameraLocation.Rear => ZXing.Net.Maui.CameraLocation.Front,
            _ => ZXing.Net.Maui.CameraLocation.Rear,
        };
        if (this.cameraBarcodeReaderView is not null)
            this.cameraBarcodeReaderView.CameraLocation = this.cameraLocation;
    }

    public void StartDetecting()
    {
        if (this.cameraBarcodeReaderView is not null)
            this.cameraBarcodeReaderView.IsDetecting = true;
    }

    private void BarcodesDetected(object? sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (this.cameraBarcodeReaderView is not null)
        {
            this.cameraBarcodeReaderView.IsDetecting = false;
            if (torchState) TorchToggled();
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (e.Results.Length == 1 && DataContext is ScannerViewNativeViewModel vm)
                vm.ReceiveScanResult(e.Results[0].Value);
        });
    }
}

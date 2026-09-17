using Avalonia.Controls;
using Avalonia.Maui.Controls;
using AvaloniaApplication.ViewModels;

namespace AvaloniaApplication.Views.ScannerView;

public partial class MauiScannerView : UserControl
{
    private MauiScannerPage? mauiPage;

    public MauiScannerView()
    {
        InitializeComponent();
    }

    public MauiScannerView(ScannerViewMauiViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is ScannerViewMauiViewModel vm)
        {
            mauiPage = new MauiScannerPage();
            mauiPage.ConfigureReader();
            mauiPage.TorchToggled += () => vm.ToggleTorch();
            mauiPage.CameraLocationToggled += () => vm.ToggleCameraLocation();
            mauiPage.CancelRequested += () => vm.CancelCommand();
            mauiPage.BarcodeDetected += value => vm.ReceiveScanResult(value);

            vm.TorchToggled += () => mauiPage.ToggleTorch();
            vm.CameraLocationToggled += () => mauiPage.ToggleCameraLocation();

            var host = this.Get<MauiControlHost>("mauiHost");
            host.Content = mauiPage.Content;
        }

        base.OnLoaded(e);
    }

    public void StartDetecting()
    {
        mauiPage?.StartDetecting();
    }
}

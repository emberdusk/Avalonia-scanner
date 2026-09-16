using ReactiveUI;
using System;

namespace AvaloniaApplication.ViewModels;

public class ScannerViewNativeViewModel : ViewModelBase
{
    private readonly MainViewModel mainViewModel;

    public ScannerViewNativeViewModel(MainViewModel mainViewModel)
    {
        this.mainViewModel = mainViewModel;
    }

    private bool showButtons = true;
    public bool ShowButtons
    {
        get => showButtons;
        set => showButtons = this.RaiseAndSetIfChanged(ref showButtons, value);
    }

    public event Action? TorchToggled;
    public void ToggleTorch()
    {
        TorchToggled?.Invoke();
    }

    public event Action? CameraLocationToggled;
    public void ToggleCameraLocation()
    {
        CameraLocationToggled?.Invoke();
    }

    public void CancelCommand()
    {
        this.mainViewModel.ShowNativeScanner = false;
    }

    public void ReceiveScanResult(string scanResult)
    {
        ShowButtons = false;
        this.mainViewModel.ScanResult = scanResult;
    }
}

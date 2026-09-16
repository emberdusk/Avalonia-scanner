using ReactiveUI;
using System;

namespace AvaloniaApplication.ViewModels;

public class ScannerViewAvaloniaViewModel : ViewModelBase
{
    private readonly MainViewModel mainViewModel;

    public ScannerViewAvaloniaViewModel(MainViewModel mainViewModel)
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
        this.mainViewModel.ShowAvaloniaScanner = false;
    }

    public void ReceiveScanResult(string scanResult)
    {
        ShowButtons = false;
        this.mainViewModel.ScanResult = scanResult;
    }
}

using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Maui.ApplicationModel;
using ReactiveUI;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaApplication.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {

    }

    // --- Original Scanner ---
    private ScannerView? originalScanner;
    public ScannerView? OriginalScanner
    {
        get => originalScanner;
        set => originalScanner = this.RaiseAndSetIfChanged(ref originalScanner, value);
    }

    private bool showOriginalScanner;
    public bool ShowOriginalScanner
    {
        get => showOriginalScanner;
        set
        {
            if (value)
            {
                HideAllScanners();
            }
            showOriginalScanner = this.RaiseAndSetIfChanged(ref showOriginalScanner, value);
        }
    }

    // --- Avalonia Scanner ---
    private Views.ScannerView.AvaloniaScannerView? avaloniaScanner;
    public Views.ScannerView.AvaloniaScannerView? AvaloniaScanner
    {
        get => avaloniaScanner;
        set => avaloniaScanner = this.RaiseAndSetIfChanged(ref avaloniaScanner, value);
    }

    private bool showAvaloniaScanner;
    public bool ShowAvaloniaScanner
    {
        get => showAvaloniaScanner;
        set
        {
            if (value)
            {
                HideAllScanners();
            }
            showAvaloniaScanner = this.RaiseAndSetIfChanged(ref showAvaloniaScanner, value);
        }
    }

    // --- MAUI Scanner ---
    private Views.ScannerView.MauiScannerView? mauiScanner;
    public Views.ScannerView.MauiScannerView? MauiScanner
    {
        get => mauiScanner;
        set => mauiScanner = this.RaiseAndSetIfChanged(ref mauiScanner, value);
    }

    private bool showMauiScanner;
    public bool ShowMauiScanner
    {
        get => showMauiScanner;
        set
        {
            if (value)
            {
                HideAllScanners();
            }
            showMauiScanner = this.RaiseAndSetIfChanged(ref showMauiScanner, value);
        }
    }

    // --- Native Scanner ---
    private Views.ScannerView.NativeScannerView? nativeScanner;
    public Views.ScannerView.NativeScannerView? NativeScanner
    {
        get => nativeScanner;
        set => nativeScanner = this.RaiseAndSetIfChanged(ref nativeScanner, value);
    }

    private bool showNativeScanner;
    public bool ShowNativeScanner
    {
        get => showNativeScanner;
        set
        {
            if (value)
            {
                HideAllScanners();
            }
            showNativeScanner = this.RaiseAndSetIfChanged(ref showNativeScanner, value);
        }
    }

    // --- Shared Result State ---
    private string scanResult = "";
    public string ScanResult
    {
        get => scanResult;
        set
        {
            if (value != "")
            {
                HideAllScanners();
                ShowResult = true;
            }
            else
            {
                ShowResult = false;
            }

            scanResult = this.RaiseAndSetIfChanged(ref scanResult, value);
        }
    }

    private bool showResult;
    public bool ShowResult
    {
        get => showResult;
        set => showResult = this.RaiseAndSetIfChanged(ref showResult, value);
    }

    // --- Any Scanner Active (for overlay visibility) ---
    public bool ShowAnyScanner => ShowOriginalScanner || ShowAvaloniaScanner || ShowMauiScanner || ShowNativeScanner;

    private void HideAllScanners()
    {
        ShowOriginalScanner = false;
        ShowAvaloniaScanner = false;
        ShowMauiScanner = false;
        ShowNativeScanner = false;
        this.RaisePropertyChanged(nameof(ShowAnyScanner));
    }

    // --- Permission Check (shared) ---
    private async Task<bool> EnsureCameraPermission()
    {
        bool hasPermission = await Services.PermissionService.CheckPermission<Permissions.Camera>();

        if (!hasPermission)
        {
            try
            {
                using var ctsRequest = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var token = ctsRequest.Token;

                Task<bool> res = Services.PermissionService.RequestPermission<Permissions.Camera>();

                bool gotPermission = await res.WaitAsync(token);

                if (gotPermission)
                {
                    hasPermission = true;
                }
            }
            catch (OperationCanceledException oce)
            {
                System.Diagnostics.Debug.WriteLine(oce, "[ERROR]");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex, "[ERROR]");
                throw;
            }

            using var ctsPollTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            while (!ctsPollTimeout.IsCancellationRequested)
            {
                bool result = await Services.PermissionService.CheckPermission<Permissions.Camera>();

                if (result)
                {
                    hasPermission = true;
                    break;
                }
            }
        }

        return hasPermission;
    }

    // --- Original Scan Command ---
    public async Task OriginalScanCommand()
    {
        System.Diagnostics.Debug.WriteLine(nameof(OriginalScanCommand), "[TRACE]");

        ScanResult = "";
        ShowResult = false;

        if (await EnsureCameraPermission())
        {
            OriginalScanner ??= new ScannerView(new ScannerViewModel(this));
            ShowOriginalScanner = true;
            this.RaisePropertyChanged(nameof(ShowAnyScanner));
            OriginalScanner.StartDetecting();
        }
        else
        {
            Services.ToastService.ShowToastLong("ERROR: Camera permission not granted");
        }
    }

    // --- Avalonia Scan Command ---
    public async Task AvaloniaScanCommand()
    {
        System.Diagnostics.Debug.WriteLine(nameof(AvaloniaScanCommand), "[TRACE]");

        ScanResult = "";
        ShowResult = false;

        if (await EnsureCameraPermission())
        {
            var vm = new ScannerViewAvaloniaViewModel(this);
            AvaloniaScanner ??= new Views.ScannerView.AvaloniaScannerView(vm);
            ShowAvaloniaScanner = true;
            this.RaisePropertyChanged(nameof(ShowAnyScanner));
            AvaloniaScanner.StartDetecting();
        }
        else
        {
            Services.ToastService.ShowToastLong("ERROR: Camera permission not granted");
        }
    }

    // --- MAUI Scan Command ---
    public async Task MauiScanCommand()
    {
        System.Diagnostics.Debug.WriteLine(nameof(MauiScanCommand), "[TRACE]");

        ScanResult = "";
        ShowResult = false;

        if (await EnsureCameraPermission())
        {
            var vm = new ScannerViewMauiViewModel(this);
            MauiScanner ??= new Views.ScannerView.MauiScannerView(vm);
            ShowMauiScanner = true;
            this.RaisePropertyChanged(nameof(ShowAnyScanner));
            MauiScanner.StartDetecting();
        }
        else
        {
            Services.ToastService.ShowToastLong("ERROR: Camera permission not granted");
        }
    }

    // --- Native Scan Command ---
    public async Task NativeScanCommand()
    {
        System.Diagnostics.Debug.WriteLine(nameof(NativeScanCommand), "[TRACE]");

        ScanResult = "";
        ShowResult = false;

        if (await EnsureCameraPermission())
        {
            var vm = new ScannerViewNativeViewModel(this);
            NativeScanner ??= new Views.ScannerView.NativeScannerView(vm);
            ShowNativeScanner = true;
            this.RaisePropertyChanged(nameof(ShowAnyScanner));
            NativeScanner.StartDetecting();
        }
        else
        {
            Services.ToastService.ShowToastLong("ERROR: Camera permission not granted");
        }
    }

    // --- Native Embed Demo ---
    private Pages.NativeEmbedView? nativeEmbedView;
    public Pages.NativeEmbedView? NativeEmbedView
    {
        get => nativeEmbedView;
        set => nativeEmbedView = this.RaiseAndSetIfChanged(ref nativeEmbedView, value);
    }

    private bool showNativeEmbed;
    public bool ShowNativeEmbed
    {
        get => showNativeEmbed;
        set
        {
            if (value)
            {
                HideAllScanners();
                ShowResult = false;
            }
            showNativeEmbed = this.RaiseAndSetIfChanged(ref showNativeEmbed, value);
        }
    }

    public void NativeEmbedCommand()
    {
        System.Diagnostics.Debug.WriteLine(nameof(NativeEmbedCommand), "[TRACE]");
        NativeEmbedView ??= new Pages.NativeEmbedView();
        ShowNativeEmbed = true;
    }

    public void GoBackCommand()
    {
        ShowNativeEmbed = false;
    }
}

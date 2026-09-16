# Avalonia-scanner

Avalonia.Maui Hybrid Android barcode scanning application using ZXing.Net.MAUI.

## Language

**ScannerView**:
The barcode scanning view that embeds MAUI's `CameraBarcodeReaderView` via `MauiControlHost`. Displays camera feed with overlay buttons for torch, cancel, and camera switch.
_Avoid_: ScanPage, BarcodeView, CameraView

**Variant**:
One of three alternative UI implementations for overlaying buttons on the scanner camera feed. Each variant uses a different framework approach: Avalonia, MAUI, or Android Native.
_Avoid_: Mode, Version, Type

**NativeControlHost**:
Avalonia control that hosts native Android views by subclassing and overriding `CreateNativeControlCore`. Returns `AndroidViewControlHandle` wrapping the native view.
_Avoid_: NativeHost, PlatformHost

**MauiControlHost**:
Avalonia.Maui control that embeds MAUI controls (like `CameraBarcodeReaderView`) into the Avalonia visual tree. Bridges the two UI frameworks.
_Aavoid_: MauiBridge, HybridHost

**ScannerViewModel**:
ViewModel managing scanner UI logic: torch toggle, camera switch, cancel, and scan result handling. Each variant implementation has its own independent ScannerViewModel instance.
_Avoid_: ScanLogic, CameraViewModel

# 0001: Three scanner button overlay variants

The project implements three alternative approaches for overlaying control buttons on the barcode scanner camera feed, kept alongside the original implementation for comparison:

1. **Avalonia** — pure Avalonia `Button` controls in an overlay panel
2. **MAUI** — MAUI `Button` controls embedded via `MauiControlHost`
3. **Avalonia + Native** — Android native `Button` via `NativeControlHost`

Each variant has its own `ScannerView.{Variant}.axaml` and independent `ScannerView{Variant}ViewModel`. The original `ScannerView` (buttons below camera) is preserved as baseline.

## Considered Options

- **Single implementation**: only pick the best approach → rejected because the goal is to compare all three approaches side-by-side
- **Three variants with shared ViewModel**: share one ViewModel across all → rejected because each variant needs independent lifecycle and state management
- **Three variants with independent ViewModels**: each variant fully self-contained → chosen for clean separation and independent experimentation

## Consequences

- Homepage has 4 entry buttons (original + 3 variants) in a 2×2 grid
- Each variant is a separate `.axaml` + `.cs` + `ViewModel` file set
- Buttons float as overlay on camera feed (auto-hide on scan success)
- Both Cancel button and system back key supported for navigation

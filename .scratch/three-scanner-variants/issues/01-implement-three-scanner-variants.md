Status: ready-for-agent

# Three Scanner Button Overlay Variants

Implement three alternative approaches for overlaying control buttons on the barcode scanner camera feed, keeping the original implementation as baseline for comparison.

## Summary

- **Original**: buttons below camera (existing, preserved as-is)
- **Avalonia**: Avalonia buttons in overlay panel
- **MAUI**: MAUI buttons embedded via MauiControlHost
- **Native**: Android native buttons via NativeControlHost

## Scope

- 4 entry buttons on homepage (2×2 grid)
- 3 new ScannerView variants + 3 independent ViewModels
- Auto-hide buttons on scan result
- Both Cancel button + system back key

## Spec

See `spec.md` in this directory.

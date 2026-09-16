# Spec: Three Scanner Button Overlay Variants

## Problem Statement

The current barcode scanner implementation places control buttons (Torch, Cancel, Camera switch) **below** the camera feed in a bottom row. The user wants to compare three different approaches for overlaying these buttons **on top of** the camera feed, while keeping the original implementation as a baseline for comparison. This is a learning/demonstration project to evaluate how Avalonia, MAUI, and Android Native approaches differ when embedding interactive controls over a MAUI camera surface.

## Solution

Add three new scanner view variants, each implementing button overlay on the camera feed using a different framework approach. Preserve the original `ScannerView` (buttons below camera) as baseline. The homepage gains a 2×2 grid of entry buttons leading to all four implementations (original + 3 variants). Each variant has its own independent ViewModel and shares identical functional behavior (torch toggle, cancel, camera switch, scan result handling).

## User Stories

1. As a developer, I want to see the original scanner implementation with buttons below the camera, so that I have a baseline for comparison
2. As a developer, I want to tap an "Avalonia" button on the homepage, so that I can see Avalonia-rendered buttons overlaid on the camera feed
3. As a developer, I want to tap a "MAUI" button on the homepage, so that I can see MAUI-rendered buttons overlaid on the camera feed
4. As a developer, I want to tap a "Native" button on the homepage, so that I can see Android native buttons overlaid on the camera feed
5. As a developer, I want to tap the original "Scan" button on the homepage, so that I can access the baseline implementation
6. As a user, I want buttons to float on top of the camera feed (not below), so that the scanner UI feels more like a real camera app
7. As a user, I want the overlay buttons to auto-hide when a barcode is scanned, so that the result card is clearly visible
8. As a user, I want the overlay buttons to reappear when I tap "Scan Again", so that I can control the scanner again
9. As a user, I want to toggle the torch/flashlight from the overlay buttons, so that I can scan in low-light conditions
10. As a user, I want to cancel scanning and return to the homepage from the overlay buttons, so that I can exit without scanning
11. As a user, I want to switch between front and rear cameras from the overlay buttons, so that I can scan barcodes on different surfaces
12. As a user, I want the system back button to return me to the homepage, so that I have a familiar navigation pattern
13. As a user, I want all four implementations to produce the same scan result behavior, so that I can focus on comparing the UI approaches
14. As a developer, I want each variant to have its own independent ViewModel, so that changes to one variant don't affect others
15. As a developer, I want the homepage to display four buttons in a 2×2 grid, so that all entry points are equally visible and accessible
16. As a developer, I want to see how Avalonia handles Z-ordering when placing Avalonia controls over a MAUI-embedded camera surface
17. As a developer, I want to see how MAUI buttons render when placed inside the same MauiControlHost as the camera
18. As a developer, I want to see how Android native buttons via NativeControlHost render on top of the Avalonia+MAUI stack
19. As a user, I want each variant's scan result to display the barcode value in a card overlay, so that I can confirm the scan succeeded
20. As a user, I want each variant's "Scan Again" button to restart detection, so that I can perform multiple scans in sequence

## Implementation Decisions

### Architecture
- Each variant is a self-contained set: `ScannerView.{Variant}.axaml` + `.cs` + `ScannerView{Variant}ViewModel.cs`
- All ViewModels inherit from `ReactiveObject` (existing `ViewModelBase` pattern)
- Each ViewModel holds a reference to `MainViewModel` for navigation state (`ShowScanner`, `ShowResult`, `ScanResult`)
- Navigation remains visibility-based (no Router/PageStack), consistent with existing architecture

### UI Layout
- Scanner variants: full-screen camera feed with button overlay panel (using `Panel` or `Grid` with higher z-index)
- Homepage: 2×2 `Grid` with four buttons: Original, Avalonia, MAUI, Native
- Original "Scan" button label renamed to "Original" for clarity in the grid

### Button Overlay Behavior
- Overlay buttons are positioned at the bottom of the camera feed, floating above it
- `IsVisible` bound to a `ShowButtons` property in each ViewModel
- `ShowButtons = true` by default, set to `false` when barcode detected, set back to `true` on "Scan Again"
- All three variants implement identical button layout and functionality

### Framework-Specific Approaches
1. **Avalonia variant**: Avalonia `Button` controls in an overlay `Panel` above `MauiControlHost`. Tests whether Avalonia controls can render above MAUI content.
2. **MAUI variant**: MAUI `Button` controls placed inside the same `MauiControlHost` as the camera, or in a separate `MauiControlHost` overlay.
3. **Native variant**: Android native `Button` controls via `NativeControlHost` subclass, using `AndroidViewControlHandle`. Native views render on top of all Avalonia content per documentation.

### Navigation
- Each variant supports both Cancel button and Android system back key
- System back key handled via `OnKeyDown` or platform-specific back button interception
- Cancel sets `MainViewModel.ShowScanner = false`

## Testing Decisions

- **What to test**: ViewModel command behavior (torch toggle, cancel, camera switch, scan result propagation), visibility state transitions, navigation back to homepage
- **Seams**: ViewModel layer is the primary test seam — each ViewModel is independent and testable in isolation
- **Prior art**: No existing tests in the codebase; this will establish the testing pattern
- **Framework comparison**: Manual visual testing for button rendering differences across variants (Avalonia vs MAUI vs Native)

## Out of Scope

- Redesigning the camera feed or ZXing integration (existing `CameraBarcodeReaderView` usage is preserved)
- Adding new barcode formats (QR codes, etc.) — remains 1D only
- Desktop platform support for the native variant (Android-specific `NativeControlHost`)
- Performance benchmarking between the three approaches
- Modifying the `ScannerViewModel` (original) — it stays as-is

## Further Notes

- The third variant (Avalonia + Native) references the Avalonia docs: https://docs.avaloniaui.net/docs/platform-specific-guides/android/embed-native-views
- ADR-0001 records the decision to implement three comparison variants
- The project uses a static service locator pattern (`Services` class) — no DI changes needed
- Camera permission flow remains unchanged (MAUI Essentials with polling fallback)

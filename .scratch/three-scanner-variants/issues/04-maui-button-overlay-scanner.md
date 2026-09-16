Status: ready-for-agent

# 04: MAUI 按钮叠加扫码实现

**What to build:** 实现 MAUI 变体的扫码界面，使用 MAUI Button 控件浮在相机画面上方，功能与 Avalonia 变体一致。

**Blocked by:** 02-homepage-grid-buttons

- [ ] 创建 `ScannerViewMauiViewModel`，实现与 Avalonia 变体相同的业务逻辑
- [ ] 创建 `ScannerView.Maui.axaml`，MAUI Button 放在 `MauiControlHost` 内或独立 `MauiControlHost` overlay
- [ ] 按钮布局和行为与 Avalonia 变体一致
- [ ] 扫码成功后按钮自动隐藏，"Scan Again" 后重新显示
- [ ] 支持 Cancel 按钮返回首页

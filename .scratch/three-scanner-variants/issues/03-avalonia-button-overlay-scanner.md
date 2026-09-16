Status: ready-for-agent

# 03: Avalonia 按钮叠加扫码实现

**What to build:** 实现 Avalonia 变体的扫码界面，使用 Avalonia Button 控件浮在相机画面上方作为 overlay，功能与原始实现一致（torch/cancel/camera switch），扫码成功后按钮自动隐藏。

**Blocked by:** 02-homepage-grid-buttons

- [ ] 创建 `ScannerViewAvaloniaViewModel`，实现 torch toggle、cancel、camera switch 逻辑
- [ ] 创建 `ScannerView.Avalonia.axaml`，使用 `MauiControlHost` 嵌入相机 + Avalonia `Panel` overlay 叠加按钮
- [ ] 按钮浮在相机画面底部，半透明背景
- [ ] 扫码成功后 `ShowButtons = false`，按钮自动隐藏
- [ ] 点击 "Scan Again" 后 `ShowButtons = true`，按钮重新显示
- [ ] 支持 Cancel 按钮返回首页
- [ ] 处理 Avalonia 控件与 MAUI 内容的 Z-order 叠加问题

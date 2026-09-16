Status: ready-for-agent

# 05: Native 按钮叠加扫码实现

**What to build:** 实现 Avalonia + Android Native 变体的扫码界面，使用 `NativeControlHost` 嵌入 Android 原生 `Button` 控件浮在相机画面上方，功能与其他变体一致。

**Blocked by:** 02-homepage-grid-buttons, 06-extract-common-scanner-interface

- [ ] 创建 `ScannerViewNativeViewModel`，通过 `IScannerService` 调用 torch/camera 逻辑
- [ ] 创建 `NativeButtonHost`（`NativeControlHost` 子类），在 `CreateNativeControlCore` 中创建 Android `Button`
- [ ] 创建 `ScannerView.Native.axaml`，组合 `MauiControlHost`（相机）+ `NativeButtonHost`（按钮 overlay）
- [ ] Android 原生按钮实现 torch/cancel/camera switch 功能
- [ ] 扫码成功后按钮自动隐藏，"Scan Again" 后重新显示
- [ ] 支持 Cancel 按钮和系统返回键返回首页
- [ ] 处理原生按钮的点击事件与 ViewModel 的通信

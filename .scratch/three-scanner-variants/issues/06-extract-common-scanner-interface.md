Status: ready-for-agent

# 06: 提取公共 Scanner 接口供 Native 变体复用

**What to build:** 抽取 `IScannerService` 接口，将 torch toggle 和 camera switch 逻辑从 code-behind 提取到可复用的服务接口，让原始实现和 Native 变体共享相同的功能实现。

**Blocked by:** 01-refactor-mainviewmodel-multi-navigation

- [ ] 定义 `IScannerService` 接口，包含 `ToggleTorch()`、`SwitchCamera()`、`StartDetecting()` 方法
- [ ] 原始 `ScannerView.axaml.cs` 实现该接口，将 torch/camera 逻辑从事件驱动改为接口调用
- [ ] 接口保持向后兼容，原有功能不受影响
- [ ] 为 Native 变体的 ViewModel 提供调用入口

Status: ready-for-agent

# 01: 重构 MainViewModel 支持多入口导航

**What to build:** 将 MainViewModel 从单一 `ShowScanner` 状态改为 4 个独立的 Show 状态，支持 Original / Avalonia / MAUI / Native 四个扫码入口同时存在，每个入口独立控制显示/隐藏。

**Blocked by:** None (can start immediately)

- [ ] MainViewModel 新增 `ShowOriginalScanner`、`ShowAvaloniaScanner`、`ShowMauiScanner`、`ShowNativeScanner` 四个 bool 属性
- [ ] 每个入口有独立的 Scanner 实例和 ScanCommand
- [ ] 任一 Show 状态为 true 时，其他 Show 状态自动为 false（互斥）
- [ ] `ShowResult` 和 `ScanResult` 保持通用，四个入口共享结果展示逻辑
- [ ] 原有 `ShowScanner` 和 `ScanCommand` 重命名为 `ShowOriginalScanner` 和 `OriginalScanCommand`
- [ ] 原有 `Scanner` 属性重命名为 `OriginalScanner`

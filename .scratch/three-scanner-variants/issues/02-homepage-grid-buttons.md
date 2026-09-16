Status: ready-for-agent

# 02: 首页 2×2 网格入口按钮

**What to build:** 首页从单一 "Scan" 按钮改为 2×2 网格布局，显示四个入口按钮（Original / Avalonia / MAUI / Native），点击进入对应扫码实现。

**Blocked by:** 01-refactor-mainviewmodel-multi-navigation

- [ ] MainView 使用 `Grid` 布局（2 行 × 2 列）展示四个按钮
- [ ] 四个按钮分别绑定到对应的 ScanCommand
- [ ] 每个按钮有清晰的标题标签（Original / Avalonia / MAUI / Native）
- [ ] 按钮样式保持一致，视觉上平衡
- [ ] 原始 "Scan" 按钮的样式和行为作为 "Original" 按钮保留

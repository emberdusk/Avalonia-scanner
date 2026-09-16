# 领域文档（Domain Docs）

本文件规定：各工程类 skill 在探索本仓库代码时，应如何消费这里的领域文档。

## 探索之前先读这些

- 仓库根目录的 **`CONTEXT.md`**；或
- 若根目录存在 **`CONTEXT-MAP.md`**，则由它指向每个上下文各自的 `CONTEXT.md`，按需读取与当前话题相关的那几份。
- **`docs/adr/`**：读取与你要动的区域相关的 ADR。

如果上述文件不存在，**静默继续**。不要指出它们缺失，也不要在一开始就建议创建它们。`/domain-modeling` skill（经 `/grill-with-docs` 与 `/improve-codebase-architecture` 到达）会在术语或决策真正被敲定时按需惰性创建。

## 文件结构

本仓库是单上下文（single-context）仓库，无 monorepo 信号，因此没有 `CONTEXT-MAP.md`，也没有 `src/` 分层：

```
/
├── AGENTS.md                              ← agent 约定（含 ## Agent skills 段落）
├── CONTEXT.md                             ← 术语表 / 领域词汇，按需惰性创建
├── AvaloniaScanner.sln
├── Directory.Build.props
├── README.md
├── AvaloniaApplication/                   ← 共享应用：视图、ViewModel、扫描逻辑
├── AvaloniaApplication.Android/           ← Android 平台 head（相机、MAUI 权限）
├── AvaloniaApplication.Desktop/           ← 桌面平台 head
└── docs/
    ├── adr/                               ← 决策记录，例如 0001-xxx.md
    ├── agents/                            ← 本文件，以及 issue-tracker.md、triage-labels.md
    └── maui-overlay-verification.md
```

若日后引入 `CONTEXT-MAP.md`（即出现多上下文），则改为：根目录 `docs/adr/` 存系统级决策，每个上下文各自有 `CONTEXT.md` 与自己的 `docs/adr/` 存上下文特有决策。

## 使用术语表里的词汇

当你的产出提到某个领域概念时（issue 标题、重构提案、假设、测试名），使用 `CONTEXT.md` 中定义的术语。不要漂移到术语表明确回避的同义词。

如果你需要的概念还没进术语表，这本身是个信号：要么你在发明项目并不使用的语言（请重新考虑），要么这里存在真实空缺（记下来交给 `/domain-modeling`）。

## 与 ADR 冲突要显式标注

如果你的产出与既有 ADR 相悖，要明确提出来，而不是悄悄覆盖：

> _与 ADR-0007（event-sourced orders）相冲突，但值得重开讨论，因为……_

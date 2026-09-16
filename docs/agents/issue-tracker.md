# Issue tracker：本地 Markdown

本仓库的 issue 与 spec 以 markdown 文件形式存放在 `.scratch/` 下。

## 约定

- 一个 feature 一个目录：`.scratch/<feature-slug>/`
- spec 文件为 `.scratch/<feature-slug>/spec.md`
- 实现类 issue 一个 ticket 一个文件，位于 `.scratch/<feature-slug>/issues/<NN>-<slug>.md`，从 `01` 起编号；不要把所有 ticket 合并进单个文件
- triage 状态记录为每个 issue 文件靠顶部的 `Status:` 行（角色字符串见 `triage-labels.md`）
- 评论与讨论历史追加到文件末尾的 `## Comments` 标题之下

## 当某个 skill 说「publish to the issue tracker」

在 `.scratch/<feature-slug>/` 下新建一个文件（目录不存在就先创建）。

## 当某个 skill 说「fetch the relevant ticket」

读取所引用路径的文件。通常由用户直接给出路径或 issue 编号。

## Wayfinding 操作

供 `/wayfinder` 使用。**map** 是一个文件，每个 ticket 对应一个 **child** 文件。

- **Map**：`.scratch/<effort>/map.md`，正文包含 Notes / Decisions-so-far / Fog 三部分。
- **Child ticket**：`.scratch/<effort>/issues/NN-<slug>.md`，从 `01` 起编号，正文写要解决的问题。`Type:` 行记录 ticket 类型（`research`/`prototype`/`grilling`/`task`）；`Status:` 行记录 `claimed`/`resolved`。
- **Blocking**：靠顶部一行 `Blocked by: NN, NN`。当它列出的每个文件都处于 `resolved` 时，该 ticket 即解除阻塞。
- **Frontier**：扫描 `.scratch/<effort>/issues/`，找出未关闭、无阻塞、未被认领的文件；编号最小者优先。
- **Claim**：动工之前先写 `Status: claimed` 并保存。
- **Resolve**：在 `## Answer` 标题下追加答案，写 `Status: resolved`，然后把上下文指针（要点 + 链接）追加到 `map.md` 的 Decisions-so-far 中。

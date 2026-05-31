# prompts/init.md — AI セッション開始手順

Codex / Claude Code は、本リポジトリで作業を始める前に以下を順に読み込むこと。

1. `AGENTS.md` — 行動規範・作業フロー（テストファースト・セキュリティ監査・承認フロー）
2. `CLAUDE.md` — 実装規約（命名・レイヤー依存・XML コメント・PII/AI 境界・テナント分離）
3. `docs/architecture/design.md` — 設計方針（抽象モデル・モジュール・スコアリング・効果測定）
4. `docs/architecture/adr/` — 関連 ADR（特に 0003〜0007）
5. `TASKS.md` と `docs/codex-instructions/` — 着手するタスクの仕様・完了条件
6. `docs/work-records/` — 関連する過去の作業記録

参照階層: `AGENTS.md` は `CLAUDE.md` を参照する。`CLAUDE.md` は末端ルールで他ルールを参照しない。
設計・仕様の詳細は `docs/` を正とする。

着手前チェック:
- 該当タスクファイルの「完了条件」を把握したか
- 触れる領域に関わる ADR（承認フロー / PII・AI 境界 / テナント分離 / スコアリング）を読んだか
- テストファーストで進められるか

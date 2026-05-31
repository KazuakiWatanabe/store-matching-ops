# src/ — .NET 実装

Phase 0（Stage 0.1 / `docs/codex-instructions/000_initial_skeleton.md`）で以下を作成する。

```
src/
├── MatchOps.Domain/          # ドメイン層（外部依存なし。BCL のみ）
├── MatchOps.Application/     # ユースケース層（Domain のみ参照）
├── MatchOps.Infrastructure/  # 永続化/AI/配信/取込（Domain + Application 参照）
├── MatchOps.Api/             # 管理画面向け API（Application + Infrastructure）
└── MatchOps.Worker/          # 非同期処理（取込/スコア/配信/分析）
```

依存方向の絶対ルールは `CLAUDE.md` §3.2。現時点ではプレースホルダ（コード未作成）。

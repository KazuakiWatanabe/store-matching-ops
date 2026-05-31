# 2026-05-31 リポジトリ初期化

## 背景
店舗マッチング・オペレーション基盤の開発に向け、リポジトリ骨格・規約・設計方針・タスクを整備する。
既存社内基盤 `chargehub` の CLAUDE.md / AGENTS.md を参考に、本プロダクト向けへ調整した。

## 実施内容
- リポジトリ名 `store-matching-ops` / 名前空間ルート `MatchOps` を決定（ADR-0001）。
- 設計方針 `docs/architecture/design.md` を作成（抽象モデル・モジュラーモノリス・スコアリング・AI 境界・効果測定）。
- `CLAUDE.md`（実装規約）/ `AGENTS.md`（行動規範）を本プロダクト向けに作成。
- ADR 0001〜0007 を起票（命名・モジュラーモノリス・ルールベース・承認フロー・PII/AI 境界・テナント分離・リフト測定）。
- `TASKS.md` と `docs/codex-instructions/000〜011` を作成。
- ビルド設定（Directory.Build.props）・.editorconfig・.gitignore・docker-compose・prompts/init.md を配置。

## 設計判断
- 完全自動化せず Human-in-the-loop（ADR-0004）。
- PII を LLM に渡さない（ADR-0005）。
- Phase 0/1 はルールベース・スコア v0=3要素（ADR-0003）。
- マルチテナントを最初から（ADR-0006）。
- 効果は増分（リフト）で測る（ADR-0007）。

## 検証結果
- ドキュメントのみ（実装コードなし）。`src/` の .NET ソリューションは Stage 0.1（000）で作成予定。

## 未解決事項・次アクション
- リポジトリ名/コードネームの最終確定（暫定: store-matching-ops / MatchOps）。
- 初期 PoC 対象業種（美容院 or 飲食店）の決定。
- Stage 0.1（000_initial_skeleton）に着手。

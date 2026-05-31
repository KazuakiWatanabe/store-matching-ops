# ADR-0001: リポジトリ名・名前空間ルート

- ステータス: 採用（暫定）
- 日付: 2026-05-31

## コンテキスト
店舗マッチング基盤を独立リポジトリとして新設する。命名は `docs/` 全体と .NET 名前空間に波及するため早期に固定する。

## 決定
- リポジトリ名: `store-matching-ops`（プロダクト名 Store Matching Ops Platform に対応）
- .NET ソリューション/名前空間ルート（コードネーム）: `MatchOps`
- レイヤー名前空間: `MatchOps.Domain` / `MatchOps.Application` / `MatchOps.Infrastructure` / `MatchOps.Api` / `MatchOps.Worker`

## 影響
- 既存社内基盤 `chargehub` と並ぶ命名（短いコードネーム + 説明的リポジトリ名）。
- 変更時は本 ADR を更新し、`docs/` と csproj/名前空間を一括置換する。

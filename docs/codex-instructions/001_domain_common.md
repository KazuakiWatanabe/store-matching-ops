# 001 — Domain 共通部品 (Stage 0.2)

## 前提
- `CLAUDE.md` §6（命名・ID 型・Value Object）, §10.4（Time Source）
- ADR-0003（ルールベース・スコアリング）

## 対象ディレクトリ
- `src/MatchOps.Domain/Common`
- `tests/MatchOps.Domain.Tests/Common`

## 作成物
- `DomainException`（ドメイン不変条件違反）
- ID 型: `TenantId` / `StoreId` / `CustomerId` / `TimeSlotId` / `OfferId` / `CampaignId`（いずれも `readonly record struct`）
- `Money`（JPY は整数のみ、通貨混在禁止、Factory `Money.Jpy`）
- `MatchScore` / `ScoreBreakdown`（0〜1 正規化スコアと内訳。後続タスクで使用）
- `IClock`（`Application` 側に置く場合は本タスク対象外。Domain には時刻をパラメータで渡す方針）

## 仕様
- すべて不変。等価性は値ベース。invariant が崩れうる ctor は private。
- `Money` は通貨コード 3 文字検証、JPY 小数禁止。異通貨演算は `DomainException`。
- `ScoreBreakdown` は項目名→寄与値の辞書を保持し、合計が `MatchScore` と整合する。

## テスト要件
- `Money`: JPY 小数で例外 / 異通貨加算で例外 / 同通貨加算が正しい。
- ID 型: `New()` が一意 / 値等価。
- `MatchScore`: 範囲外（<0 or >1）で例外。

## 制約
- NuGet を追加しない（BCL のみ）。`DateTime.UtcNow` を使わない。

## 完了条件
- `dotnet build -warnaserror` / `dotnet test` 通過。Domain カバレッジ 95%+。

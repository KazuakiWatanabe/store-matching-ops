# 2026-05-31 Stage 0.5 Catalog: Offer（メニュー/クーポン）

## 背景
`004_offer_and_catalog.md` に従い、Catalog モジュール（Offer の統一表現・値引き上限）を実装する。
値引き上限は配信制御の中核のため ADR-0010 を起票。テストファースト。

## 実施内容
`src/MatchOps.Domain/Catalog/`（名前空間 `MatchOps.Domain.Catalog`）に作成。
- `OfferType` enum（Menu / Course / Coupon）。
- `DiscountKind` enum（Amount / Rate）。
- `Discount`（値オブジェクト）: 金額 or 率の提案値引き。
- `DiscountCap`（値オブジェクト）: 金額上限 or 率上限。`EnsureWithin(Discount)` で上限超過を拒否。
- `OfferConditions`（値オブジェクト）: 適用曜日・対象セグメントタグ。`AppliesOn` / `TargetsSegment`。
- `Offer`（Aggregate Root）: メニュー/コース/クーポンを統一表現。`CreateCoupon`（上限必須）/ `CreateItem`、`Activate`/`Deactivate`、`EnsureDiscountWithinCap`、`IsAvailableOn`。

ADR: `docs/architecture/adr/0010-discount-cap-and-delivery-control.md` を新規起票。
テストは `tests/MatchOps.Domain.Tests/Catalog/` に 30 件。

## 設計判断
- **値引き上限（ADR-0010）**: 値引き・上限は金額 (`Money`) または率 (`decimal` 0〜1) のいずれか。`DiscountCap.EnsureWithin` が**種別不一致・通貨不一致・上限超過を `DomainException`** で拒否（CLAUDE.md §4.3「値引きは上限を超えない」を Domain で強制）。
- **同種比較に限定**: 金額上限 vs 率提案の換算は v0 では行わない（換算が必要なら ADR 改訂）。
- **クーポンのみ上限を必須保持**: `CreateCoupon` は `DiscountCap` 必須、`CreateItem`(Menu/Course) は上限なし。`CreateItem` に Coupon 種別を渡すと拒否。上限なし Offer に値引き検証を呼ぶと `DomainException`。
- **OfferId は Common を再利用**（Stage 0.2 で定義済み）。Catalog 固有 ID を増やさない。
- **対象セグメントはタグ（文字列）**: Customers のセグメント方針（タグベース）に整合。TimeSlot の対応 Offer 種別タグとも将来突き合わせ。
- **有効状態 + 適用条件**: `IsAvailableOn(date)` = 有効 ∧ 曜日条件。「無効 Offer は候補に出ない」の基礎（結合検証は Stage 0.6）。
- **時刻はパラメータ渡し**: `IsAvailableOn(DateOnly)` 等は引数で日付を受け取り、`IClock` を Domain に持ち込まない（CLAUDE.md §10.4）。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 146/146 Green（Domain 142 + 各骨格 1）。
- Domain カバレッジ: **line 99.78% / branch 100%**（Catalog 型は全て 100%。目標 95%+）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable` → 全 10 プロジェクトでクリーン（NuGet 追加なし）。

## 未解決事項・次アクション
- 値引きの金額/率 混在換算（必要時に ADR-0010 改訂）。
- TimeSlot 対応 Offer 種別タグ × Offer の整合（Matching 結合時）。
- Stage 0.6（`005_matching_engine_scoring`）: Matching（候補抽出・v0 スコア・MatchingCampaign 状態遷移）。スコア中核は Claude Code 担当。

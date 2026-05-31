# 2026-05-31 Stage 0.2 Domain 共通部品

## 背景
`001_domain_common.md` に従い、Domain 層の共通値オブジェクト・ID 型・例外を実装する。
後続の Customers/Scheduling/Catalog/Matching が依存する基礎部品。テストファーストで作成。

## 実施内容
`src/MatchOps.Domain/Common/` に以下を作成（名前空間 `MatchOps.Domain.Common`、いずれも不変・値等価）。
- `DomainException`: ドメイン不変条件違反の例外。標準3コンストラクタ（既定／メッセージ／メッセージ+内部例外）。
- ID 型 6 種（`readonly record struct`、`New()` + `ToString()`）: `TenantId` / `StoreId` / `CustomerId` / `TimeSlotId` / `OfferId` / `CampaignId`。
- `Money`（`readonly record struct`）: 金額 + ISO 4217 通貨コード（英大文字3文字検証）。JPY 小数禁止、異通貨演算で `DomainException`。Factory `Of` / `Jpy`、`+` / `-` 演算子。
- `MatchScore`（`readonly record struct`）: 0〜1 正規化スコア。NaN/∞/範囲外で `DomainException`。`Zero` / `From`。
- `ScoreBreakdown`（`sealed class`）: 項目名→寄与値の不変スナップショット。`Total` を寄与値総和から導出し常に整合（ADR-0003 段階的スコアリングの説明性の中核）。

テストは `tests/MatchOps.Domain.Tests/Common/` に 42 件（Money/MatchScore/ScoreBreakdown/IdTypes/DomainException）。

## 設計判断
- **配置**: ID 型を含め全て `Common` に集約（指示文の対象ディレクトリどおり）。モジュール跨ぎの Domain 直接参照（CLAUDE.md §4.1）を避けられる。
- **`IClock` は本 Stage 対象外**: 001 指示文の明記に従い Domain には時刻型を入れない。`IClock` は Application 層着手時（Stage 0.7）に作成し、Domain には時刻をパラメータで渡す（CLAUDE.md §10.4）。
- **`MatchScore` に `IComparable` を入れない**: 本 Stage では不要。ランキングが必要になる Matching（Stage 0.5）で必要なら追加（アナライザ表面積の抑制）。
- **`ScoreBreakdown` は `record` ではなく `sealed class`**: 辞書フィールドの record 値等価は参照比較となり誤解を招くため。等価性は本 Stage の要件外。
- **`ScoreBreakdown` の下限クランプを削除**: 寄与値を 0〜1 に検証済みで総和は常に 0 以上のため、下限分岐は到達不能（デッドコード）。浮動小数誤差対策の上限クランプ（`1 + 1e-9` まで）のみ残置。
- **負の `Money` を許容**: 値引き・返金を表現するため符号制約を設けない。
- **テストファーストと §4.6 の両立**: テスト先行で設計しつつ、ビルド/テストが緑になる単一コミットで投入（broken commit を作らない）。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 46/46 Green（Domain 42 + 各骨格 1）。
- Domain カバレッジ: **line 97.41% / branch 97.82%**（目標 95%+ 達成）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable` → 脆弱性ゼロ（新規 NuGet 追加なし。Domain は BCL のみ）。
- PII/シークレット: 例外メッセージは数値・通貨のみで PII・秘密情報なし。

## レビュー反映 (post-review, code-review --effort high)
高 effort のレビューで挙がった修正推奨を反映。
- **`Money.ToString()` を `InvariantCulture` に固定**（`Fix 1`）。`$"{Amount}"` 補間が `CurrentCulture` 依存で、de-DE 等で `"1234,5"` になる不整合を解消（`MatchScore` と整合）。カルチャ切替テストを追加。
- **浮動小数許容を `MatchScore.From` に集約**（`Fix 2`/altitude）。`ScoreBreakdown` 独自の上限 ε クランプを廃し、±`1e-9` の境界吸収を `MatchScore.From` の単一責務へ。Stage 0.5 のスコアエンジンも同じ許容を再利用できる。両側クランプのテストを追加。
- **`IsValidCurrencyCode` を `char.IsAsciiLetterUpper` で簡素化**（`Fix 4`）。手書きの `'A'..'Z'` ループを BCL 述語に置換。
- **`ScoreBreakdown.Contributions` を自動プロパティ化**（`Fix 5`）。冗長なバッキングフィールドを除去し `Total` と一貫。
- 反映後: build -warnaserror 0/0、全 46 テスト緑、**Domain カバレッジ line/branch ともに 100%**、format 差分なし。

### レビューで「対応不要」と判断した項目
- `ScoreBreakdown` の値等価なし（`sealed class` は辞書 record の参照比較回避のため意図的）。
- `Money` は JPY のみ小数禁止（v0 は JPY 前提の意図的絞り込み）。
- ID 型6種の重複は CLAUDE.md §6.2/§6.3 が要求する規約（§4.2 の無断抽象化禁止に抵触するため統合しない）。
- `default(Money)` の null 通貨は record struct の宿命。今回は据え置き（必要なら将来 EnsureSameCurrency に null ガード）。

## 未解決事項・次アクション
- Stage 0.3（`002_customer_and_activity`）: Customers / Activity の Aggregate とセグメント方針。
- `IClock` は Stage 0.7（Application UseCase）で定義予定。

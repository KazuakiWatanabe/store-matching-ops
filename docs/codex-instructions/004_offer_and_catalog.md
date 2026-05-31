# 004 — Catalog: Offer（メニュー/クーポン） (Stage 0.5)

## 前提
- `docs/architecture/design.md` §4（Offer）, §6.3（値引き上限）
- ADR-0010（値引き上限・配信制御）※必要なら起票

## 対象ディレクトリ
- `src/MatchOps.Domain/Catalog`, `tests/MatchOps.Domain.Tests/Catalog`

## 作成物
- `Offer` Aggregate（メニュー/コース/クーポンを統一表現。`TenantId`, `StoreId`, 種別, 値引き上限, 配信条件, 有効状態）
- `OfferType` enum（menu / course / coupon）

## 仕様
- クーポンは値引き上限（金額 or 率）を保持し、提案時にこれを超えないことを保証できる。
- 有効/無効、適用条件（曜日・時間帯・対象セグメント等）を保持。

## テスト要件
- 値引き上限超過の提案を作れない（不変条件 or バリデーション）。
- 無効 Offer は提案候補に出ない（後続 005 で結合検証）。

## 制約
- 業種固有メニュー体系を作り込みすぎない（汎用 Offer + 種別/属性で吸収）。

## 完了条件
- build/test 通過。

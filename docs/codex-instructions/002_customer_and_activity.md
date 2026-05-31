# 002 — Customers / Activity (Stage 0.3)

## 前提
- `docs/architecture/design.md` §4（抽象モデル: Requester=顧客, Activity）, §9（PII）
- ADR-0005（PII/AI 境界）, ADR-0006（テナント分離）

## 対象ディレクトリ
- `src/MatchOps.Domain/Customers`, `tests/MatchOps.Domain.Tests/Customers`

## 作成物
- `Customer` Aggregate（`TenantId`, `StoreId`, 表示名, `phone_hash`/`email_hash`, 来店統計, オプトイン状態, 通知頻度メタ）
- `CustomerActivity` Entity（来店/注文/施術/予約/キャンセル。append-only）
- `OptInStatus` enum（opted_in / opted_out / unknown）

## 仕様
- 連絡先は平文で保持しない（ハッシュ値のみ）。
- `last_visit_at` / `visit_count` 等の統計は Activity から導出 or 集計列として保持（設計判断は work-record に残す）。
- オプトアウト顧客は配信対象から除外できるフラグ/メソッドを持つ。

## テスト要件
- 平文連絡先を受け取らない（ハッシュのみ受理）。
- オプトアウト状態の判定メソッドが正しい。
- 異なる `TenantId` の Activity を Customer に紐付けようとすると拒否（不変条件）。

## 制約
- 連絡先平文・氏名フルネームをログ/例外に出さない。EF/外部依存を Domain に入れない。

## 完了条件
- build/test 通過。テナント整合・PII 最小化のテストが緑。

# 003 — Scheduling: TimeSlot / Resource (Stage 0.4)

## 前提
- `docs/architecture/design.md` §4-5（TimeSlot / Resource / Provider）
- ADR-0009（空き枠の状態遷移）※必要なら本タスクで起票

## 対象ディレクトリ
- `src/MatchOps.Domain/Scheduling`, `tests/MatchOps.Domain.Tests/Scheduling`

## 作成物
- `Resource`（席・個室・スタッフ/施術台。業種で意味が変わる抽象）
- `TimeSlot` Aggregate（`TenantId`, `StoreId`, 日時範囲, `ResourceId`, 対応 Offer 種別, 状態）
- `SlotStatus` enum（open / held / booked / closed）

## 仕様
- 状態遷移: open → held（仮押さえ）→ booked（確定）/ open。closed は手動/期限切れ。
- 不正遷移（booked → held 等）は `DomainException`。
- 同一リソース・同一時間帯の重複 open を作らない（不変条件 or サービスで検証）。

## テスト要件
- 正常遷移 / 不正遷移拒否 / 重複枠検出 / テナント整合。

## 制約
- 予約確定の副作用（顧客通知等）を Domain に持ち込まない（Application で調整）。

## 完了条件
- build/test 通過。状態遷移網羅テストが緑。

# ADR-0009: 空き枠 (TimeSlot) の状態遷移と重複枠の防止

- ステータス: 採用
- 日付: 2026-05-31
- 関連: ADR-0006（テナント分離）, 設計 §4-5, タスク `003_timeslot_and_resource`

## コンテキスト
空き枠は「提案 → 仮押さえ → 確定」または「解放」というライフサイクルを持つ。
不正な状態遷移（確定済みを仮押さえに戻す等）や、同一リソース・同一時間帯の重複枠は
供給の二重提案・ダブルブッキングを招くため、Domain の不変条件として機械的に防ぐ必要がある。

## 決定
### 状態と遷移
`SlotStatus` = `Open` / `Held` / `Booked` / `Closed`。許可する遷移のみを `TimeSlot` のメソッドで提供し、それ以外は `DomainException`。

```
Open  --Hold-->    Held
Held  --Book-->    Booked
Held  --Release--> Open
(Open|Held|Booked) --Close--> Closed   // 手動 / 期限切れ
```

- `Book`（確定）は必ず `Held`（仮押さえ）を経る。`Open → Booked` の直接遷移は許可しない。
- `Closed` は終端。`Closed → *` は不可（再クローズも `DomainException`）。
- 予約確定の副作用（顧客通知・在庫連携等）は **Domain に持ち込まない**。状態遷移のみを表現し、副作用は Application 層で調整する（Outbox 等）。

### 重複枠の防止
「同一リソース・同一時間帯に重複枠を作らない」は単一 Aggregate では判定できない横断不変条件のため、
ドメインサービス `SlotScheduler.OpenSlot` で、対象リソースの既存枠（非 `Closed`）との重複を検証してから開設する。

- 時間範囲は半開区間 `[Start, End)`（`TimeRange.Overlaps`）。隣接（前枠の終了＝次枠の開始）は重複としない。
- `Closed` 枠はリソースを占有しないため重複判定の対象から除外する。
- テナント整合: `TimeSlot.Open` はリソースのテナント・店舗が引数と一致しない場合に拒否する。

## 影響
- 状態遷移は網羅的にテストする（正常遷移・不正遷移拒否・重複検出・テナント整合）。
- 重複検証は既存枠の集合を必要とするため、永続化を伴う最終的な強制は Application/Infrastructure（リポジトリ）と組み合わせる。`SlotScheduler` はその中核ロジックを Domain 内に保持する。
- 「対応 Offer 種別」は Catalog（次タスク）への Domain 結合を避けるため、当面はカテゴリタグ（文字列集合）で保持し、Catalog 実装時に整合させる。

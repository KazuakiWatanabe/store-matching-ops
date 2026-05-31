# 2026-05-31 Stage 0.4 Scheduling: TimeSlot / Resource

## 背景
`003_timeslot_and_resource.md` に従い、Scheduling モジュール（空き枠・リソース・状態遷移）を実装する。
状態遷移は設計の中核のため ADR-0009 を起票。テストファースト。

## 実施内容
`src/MatchOps.Domain/Scheduling/`（名前空間 `MatchOps.Domain.Scheduling`）に作成。
- `SlotStatus` enum（Open / Held / Booked / Closed）。
- `ResourceKind` enum（Seat / Room / Staff / Equipment）。
- `ResourceId`（モジュール固有の強い型 ID）。
- `TimeRange`（値オブジェクト）: 半開区間 [Start, End)、終了>開始の検証、`Overlaps`、ISO8601 ラウンドトリップ ToString。
- `Resource`（Entity）: テナント・店舗・種別・名称。
- `TimeSlot`（Aggregate Root）: 状態機械（Hold/Book/Release/Close）、`OverlapsWith`、対応 Offer 種別タグ。
- `SlotScheduler`（ドメインサービス）: 重複枠を作らずに開設。

ADR: `docs/architecture/adr/0009-slot-state-transitions.md` を新規起票。
テストは `tests/MatchOps.Domain.Tests/Scheduling/` に 34 件。

## 設計判断
- **状態遷移（ADR-0009）**: `Open→Held→Booked` / `Held→Open`（解放）/ 非Closed→`Closed`。`Book` は必ず `Held` を経る（`Open→Booked` 直接遷移は不可）。`Closed` は終端。不正遷移・再クローズは `DomainException`。
- **重複枠防止はドメインサービス**: 単一 Aggregate では判定不能な横断不変条件のため `SlotScheduler.OpenSlot` で既存枠（非Closed）との重複を検証（CLAUDE.md §4.2 が許容する Domain Service）。半開区間で隣接は非重複。Closed 枠はリソース非占有として除外。
- **テナント整合**: `TimeSlot.Open` は `Resource` を受け取り、リソースのテナント・店舗が引数と不一致なら拒否（ADR-0006、Stage 0.3 と同型）。
- **予約確定の副作用を Domain に持ち込まない**: `Book()` は状態遷移のみ。通知等は Application 層（Outbox）で調整（指示文の制約）。
- **対応 Offer 種別はカテゴリタグ（文字列集合）で保持**: Catalog（次タスク）への Domain 結合を避けるため。Catalog 実装時に整合させる。
- **時刻はパラメータ渡し**: `TimeRange` は `DateTimeOffset` を引数で受け取り、`IClock` を Domain に持ち込まない（CLAUDE.md §10.4）。ToString は `:o`（不変）で揺れない。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 112/112 Green（Domain 108 + 各骨格 1）。
- Domain カバレッジ: **line 99.69% / branch 100%**（目標 95%+）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable` → 全 10 プロジェクトでクリーン（NuGet 追加なし）。

## 未解決事項・次アクション
- 「対応 Offer 種別」タグと Catalog の Offer カテゴリの整合（Stage 0.5 で対応）。
- 重複枠の最終強制は Application/Infrastructure（リポジトリ）と `SlotScheduler` の組合せで実現（後続）。
- Stage 0.5（`004_offer_and_catalog`）: Catalog（Offer：メニュー/クーポン・値引き上限方針）。

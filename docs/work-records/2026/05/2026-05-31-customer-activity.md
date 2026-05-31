# 2026-05-31 Stage 0.3 Customers / Activity

## 背景
`002_customer_and_activity.md` に従い、Customers モジュールの顧客・行動履歴モデルを実装する。
PII 最小化（ADR-0005）とテナント分離（ADR-0006）を Domain の不変条件として作り込む。テストファースト。

## 実施内容
`src/MatchOps.Domain/Customers/`（名前空間 `MatchOps.Domain.Customers`）に以下を作成。
- `OptInStatus` enum（Unknown / OptedIn / OptedOut）。
- `ActivityType` enum（Visit / Order / Treatment / Reservation / Cancellation）。
- `CustomerActivityId`（モジュール固有の強い型 ID）。
- `ContactHash`（値オブジェクト）: SHA-256 を表す 64 桁 16 進のみ受理。平文連絡先を拒否。
- `CustomerActivity`（append-only な不変 Entity）: Id/TenantId/CustomerId/Type/OccurredOn/Amount(任意)。Factory `Record`。
- `Customer`（Aggregate Root）: 連絡先はハッシュのみ、来店統計を集計列で保持、配信可否・休眠日数を提供。

テストは `tests/MatchOps.Domain.Tests/Customers/` に 27 件（ContactHash/CustomerActivity/Customer）。

## 設計判断
- **連絡先 PII 境界（`ContactHash`）**: 平文の電話/メールは長さ・文字種で機械的に拒否し、ハッシュ（SHA-256, 64 桁 16 進）のみ受理。**例外メッセージに入力値を含めない**（平文 PII のログ流出防止。指示文「平文を例外に出さない」）。ハッシュ方式は v0 で SHA-256 を前提（Infrastructure 側で算出）。
- **来店統計は集計列で保持**（指示文の二択のうち集計列を採用）: 全 Activity をメモリ保持せず、`Customer.VisitCount` / `LastVisitOn` を `RecordActivity` で更新。大量履歴のロードを避ける。`CustomerActivity` は独立した append-only Entity。
- **テナント整合の不変条件**: `Customer.RecordActivity` は `activity.TenantId` / `CustomerId` の不一致を `DomainException` で拒否（ADR-0006）。
- **来店判定**: `VisitCount` / `LastVisitOn` は `ActivityType.Visit` のみで更新（注文/施術/予約/キャンセルは来店回数に含めない）。業種差（注文≒来店等）は将来テンプレートで吸収。
- **配信可否は要オプトイン**: `CanReceiveNotifications()` は `OptedIn` のみ true。単なる「非オプトアウト」より保守的（日本の特定電子メール法等の同意原則を考慮）。`IsOptedOut` も提供。Unknown 顧客の扱いは運用で要再検討。
- **`DaysSinceLastVisit(today)`**: v0 スコアの休眠日数要素（ADR-0003）に備えて Customer に用意。時刻は `IClock` ではなくパラメータ渡し（CLAUDE.md §10.4）。
- **`Amount` を `CustomerActivity` に任意追加**: 注文/会計の金額。将来の客単価期待スコアに備える（v0 必須ではないが自然なモデル）。

## セグメント方針（設計のみ・コード化は Phase 1）
セグメントは `Customer` の集計値から派生規則で導出する想定（専用集計エンジンは Matching/Phase 1）。
- 休眠: `DaysSinceLastVisit(today)` が閾値（例 45 日）以上。
- 常連: `VisitCount` が閾値以上。
- 新規: `VisitCount` が 0〜1。
- 平日利用者 等: 行動履歴の曜日分布から（履歴蓄積後）。
セグメントは Domain に固定実装せず、設定可能な規則として Matching 層に置く（業種テンプレートで吸収）。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 78/78 Green（Domain 74 + 各骨格 1）。
- Domain カバレッジ: **line 100% / branch 100%**（目標 95%+）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable` → 全 10 プロジェクトでクリーン（NuGet 追加なし）。
- PII/テナント: 平文拒否・例外非漏えい・テナント整合の各テストが緑。

## 未解決事項・次アクション
- Unknown オプトイン顧客の配信ポリシー最終確認（現状は配信不可）。
- Stage 0.4（`003_timeslot_and_resource`）: Scheduling（TimeSlot/Resource）の状態遷移。
- `IClock` は Stage 0.7（Application UseCase）で定義予定。

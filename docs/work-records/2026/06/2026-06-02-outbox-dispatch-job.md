# 2026-06-02 Stage 0.11 残り Outbox 実送信ジョブ

## 背景
`010_notification_outbox.md`。配線 PR で `outbox_messages` / `EfOutboxWriter`（積み込み）は実装済み。本作業は残りの **実送信（ディスパッチ）**：配信スタブ・配信ログ・OutboxDispatchJob（リトライ/バックオフ・配信制御スキップ）・Worker 配線。Phase 0 は実 LINE/メール API を叩かず、ログ出力スタブ。

## 実施内容
- **Application/Notifications（ポート）**: `INotificationSender` / `NotificationMessage`（PII 非含）/ `NotificationSendResult`、`INotificationEligibility`（配信直前の送信可否）、`IOutboxDispatcher` / `OutboxDispatchSummary`。
- **Infrastructure/Notifications**
  - `LoggingNotificationSender`（Phase 0 スタブ。識別子と本文長のみログ、連絡先・本文平文は出さない）。
  - `CustomerNotificationEligibility`（オプトイン状態を `IgnoreQueryFilters`＋テナント/ID 照合で再確認。頻度の送信直前再判定は Phase 1）。
  - `OutboxDispatcher`（背景処理のため `IgnoreQueryFilters` で全テナント横断。queued かつ再試行時刻到来を処理 → 配信可否でスキップ / 送信 → notification_logs 記録 → 状態更新。失敗は指数バックオフ、最大試行回数で恒久失敗）。
  - `OutboxOptions`（MaxAttempts/BatchSize/BaseBackoffSeconds/PollIntervalSeconds）。
- **Infrastructure/Persistence**
  - `OutboxMessageEntity` に `AttemptCount` / `NextAttemptAt` 追加。
  - `NotificationLogEntry` ＋ Configuration（notification_logs、PII 非含、CustomerId 参照）。DbContext に DbSet＋テナントフィルタ。
  - Migration `AddNotificationLogsAndOutboxRetry`（notification_logs 作成＋outbox 2 列追加）。
- **Worker**: `OutboxDispatchJob`（`BackgroundService`、ポーリングでスコープ毎に `IOutboxDispatcher` 実行。1 回の失敗で停止せず次回再試行）。`Program` に `AddInfrastructure` ＋ `AddHostedService`。`appsettings` に接続/Outbox セクション。
- **共通整理**: `SystemClock` を Api から Infrastructure へ移設し `AddInfrastructure` で `IClock` を登録（Api・Worker 共通化）。Api の重複登録を削除。
- **DI 配線**: `AddInfrastructure` に sender/eligibility/dispatcher/OutboxOptions を登録。

## 実機検証で検出・修正した不具合（重要）
- **Worker の DI 解決失敗**：`AddInfrastructure` が `MatchOpsDbContext`（`ITenantContext` 依存）を登録する一方、`ITenantContext` 自体を登録していなかった。Api は `RequestContext` を別途登録するため顕在化せず、Worker 起動（実 DB）で初めて発覚。
  - 修正：`NullTenantContext`（背景処理＝テナント未解決）を `AddInfrastructure` に `TryAddScoped` で既定登録（Api の `RequestContext` を上書きしない）。
  - 回帰防止：`AddInfrastructureTests`（外部登録なしで `IOutboxDispatcher` を `validateScopes:true` 構築・解決できる）を追加。

## 設計判断
- **背景処理は IgnoreQueryFilters でテナント横断**：Worker はテナント未解決のため、Global Query Filter を回避して全テナントの Outbox を処理する（ディスパッチャ・配信可否の双方）。
- **配信制御スキップ**：送信直前に `INotificationEligibility` で再判定（承認〜配信の時間差でオプトアウトが変わりうるため）。スキップは notification_logs に skipped 記録、送信しない。
- **リトライ**：失敗で AttemptCount++ し `NextAttemptAt = now + Base×2^(n-1)`。最大到達で status=failed（恒久）。queued かつ NextAttemptAt 到来のみ処理対象。
- **PII**：NotificationMessage / OutboxMessageEntity / NotificationLogEntry とも連絡先を持たず CustomerId 参照。スタブ送信は本文平文・連絡先を出さない。
- **Phase 1 への残し**：頻度の送信直前再判定（最終通知日）、実 LINE/メール 送信、Hangfire/cron 化、notification_logs の運用閲覧。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 244/244 Green（Domain 196 + Application 10 + Api 8 + Infra 29〔+10: ディスパッチ3・配信可否3・PII境界3・AddInfrastructure1〕 + Integration 1）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable / --deprecated` → クリーン（新規 NuGet 追加なし）。
- **実機 E2E（PostgreSQL 18・マイグレーション適用）**：オプトイン顧客＋queued メッセージを seed → Worker 起動 → outbox status=sent / notification_logs=sent を確認。ディスパッチャ統合テストでスキップ・リトライ/バックオフ・恒久失敗も検証。

## 未解決事項・次アクション
- `ICampaignCandidateSource` 本実装（Phase 1）→ run が実候補生成、send が実メッセージを Outbox に積み、本ジョブが実配信ログを生成。
- 実 LINE/メール 送信実装（INotificationSender 差し替え）、Hangfire/cron 化、頻度の送信直前再判定。
- Stage 0.12（Experiments：ホールドアウト割当・リフト）。

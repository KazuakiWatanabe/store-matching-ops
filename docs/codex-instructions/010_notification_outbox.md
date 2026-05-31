# 010 — Notifications: Outbox + 配信 (Stage 0.11)

## 前提
- `CLAUDE.md` §10.3（Outbox）, §4.3（配信制御）, §9.2（ログ）

## 対象ディレクトリ
- `src/MatchOps.Infrastructure/Notifications`, `src/MatchOps.Worker`, tests 各所

## 作成物
- `outbox_messages` テーブル + 書き込み（Application の send と同一 Tx）
- `OutboxDispatchJob`（Phase 0 は cron/手動トリガ、Phase 1 で Hangfire）
- `INotificationSender`（Phase 0 は CSV 出力 or ログ出力のスタブ実装）

## 仕様
- 配信は承認済み施策のみ。オプトアウト顧客・頻度上限超過は送信前にスキップ。
- 配信ログ（成功/失敗）を `notification_logs` に記録。PII を平文で残さない。
- 失敗は指数バックオフでリトライ（最大回数を設定）。

## テスト要件
- 承認→send で Outbox に積まれ、Job が処理して `notification_logs` に記録。
- オプトアウト顧客・頻度超過がスキップされる。
- ログに連絡先平文が出ない。

## 制約
- Phase 0 で実 LINE/メール API を叩かない（スタブ/モック）。

## 完了条件
- build/test 通過。配信制御・ログ非 PII のテストが緑。

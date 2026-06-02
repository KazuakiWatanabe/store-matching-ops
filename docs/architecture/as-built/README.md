# 実装ドキュメント（as-built / Phase 0 時点）

本ディレクトリは、Phase 0（Stage 0.1〜0.12）完了時点で**実際に実装されたコード**に基づくドキュメントである。
将来を見据えた設計方針は `docs/architecture/design.md`、設計判断の記録は `docs/architecture/adr/`、各 Stage の作業記録は `docs/work-records/` を参照。

> 本書は実装の現況を反映する（コード・EF Core モデル・マイグレーション・コントローラ由来）。仕様の正本はコードであり、乖離を見つけた場合はコードを優先し本書を更新すること。

## 目次

| ドキュメント | 内容 |
|---|---|
| [architecture.md](architecture.md) | レイヤー構成・モジュール・依存方向・プロジェクト構成・技術スタック・横断的関心事 |
| [database.md](database.md) | テーブル仕様（全 11 テーブル）と ER 図 |
| [sequences.md](sequences.md) | 主要フローのシーケンス図（run / propose / approve / send / Outbox 配信 / リフト） |

## Phase 0 で実装済みの範囲（要約）

- **縦串**: 施策の `run`（候補抽出＋スコア）→ `propose`（AI 文面）→ `approve`（人手承認）→ `send`（Outbox 積み）→ Worker による配信 → 効果測定（リフト）の枠組み。
- **API**: `MatchingCampaignsController`（run/propose/approve/send/results）。Idempotency-Key・テナントスコープ・認証スタブ。OpenAPI＋Scalar UI。
- **永続化**: EF Core / PostgreSQL 18。11 テーブル。テナント分離は Global Query Filter で機械的に強制。
- **AI**: `IAiProposalService`（OpenAI 互換）。プロンプトは匿名化・集約データのみ（PII 非送出）。障害時フォールバック。
- **配信**: Outbox パターン。`OutboxDispatchJob`（Worker）が配信制御スキップ・指数バックオフ付きで処理（Phase 0 はログ出力スタブ）。
- **効果測定**: ホールドアウト割当（決定的ハッシュ）＋リフト算出（処置群CVR − 対照群CVR）。

## Phase 0 で未実装（Phase 1 以降）

- `ICampaignCandidateSource` の本実装（セグメント抽出・スコア特徴量）。現状は暫定（空候補）。
- 実 LINE/メール送信（現状はログ出力スタブ）、`conversion_events` の取込。
- 本認証（OIDC/JWT・ロール）。現状は `X-Tenant-Id` / `X-User-Id` ヘッダのスタブ。
- 冪等ストアの Redis 化（現状はインメモリ）、管理画面（Next.js）、Metabase ダッシュボード構築。
- `audit_logs` への操作記録連携（テーブルは存在）。

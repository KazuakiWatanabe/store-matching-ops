---
file: docs/architecture/design.md
version: 0.1.0
last-updated: 2026-05-31
status: draft (Phase 0)
---

# 設計方針 — Store Matching Ops Platform

本ドキュメントは `store-matching-ops`（コードネーム `MatchOps`）の**設計方針**を定義する。
実装規約は `CLAUDE.md`、作業フローは `AGENTS.md`、個別タスクは `docs/codex-instructions/` を参照。

---

## 1. プロダクトの位置付けと狙い

店舗（飲食・美容・小売／サービス）の **空き枠（席・スタッフ・時間帯）** を起点に、来店可能性が高い顧客を逆算抽出し、最適なメニュー／クーポンと配信文面を AI が提案する。提案は管理者の承認を経て配信し、効果を測定して次回に反映する。

配車サービスのマッチング（乗客 × ドライバー × 時間 …）を店舗向けに転用したもので、本質は **供給（空き枠）と需要（来店可能性）の最適マッチング** にある。

差別化は、単発の予約・配信ツールではなく、
**空き枠検出 × 顧客スコア × AI提案 × 承認配信 × 効果測定（リフト）** を一体で回すクローズドループにある。

## 2. 設計原則（Guiding Principles）

1. **Human-in-the-loop を前提にする。** AI は意思決定主体ではない。提案 → 承認 → 配信。
2. **説明可能性を最優先する。** Phase 0/1 は機械学習を使わず、ルール＋重み付けスコア。なぜその顧客に提案するかを常に説明できる状態を保つ。
3. **汎用マッチング基盤として抽象設計する。** 業種専用に作り込まず、抽象モデル（§4）に業種テンプレートを乗せる。
4. **小さく縦に1本通す。** モジュールを横に広げる前に、取込→抽出→スコア→提案→記録→可視化の縦串を最短で動かす。
5. **個人情報を最小化し、AI から隔離する。** AI に渡すのは匿名化・集約済みデータのみ。
6. **マルチテナントを最初から。** すべてのデータは `TenantId` でスコープし、店舗・本部の権限を分離する。
7. **将来の分離を見据えた境界を引く。** マッチング・AI・配信は、後でサービス分離しやすいモジュール境界にする（が、初期は分離しない）。

## 3. アーキテクチャ概要：モジュラーモノリス + 非同期ワーカー

PoC/MVP では開発速度が重要であり、サービス境界も固まっていないため、**モジュラーモノリス**を採用する（ADR-0002）。

```
[管理画面 / Next.js (apps/admin-web)]
        │  HTTP/JSON
        ▼
[MatchOps.Api]  ── 認証・認可・テナントスコープ・Idempotency
        │
        ▼
[MatchOps.Application]  ── ユースケース調整・トランザクション境界
        │   （モジュール: 顧客 / 空き枠 / メニュー・クーポン / マッチング / 配信 / 効果測定）
        ▼
[MatchOps.Domain]  ── 抽象モデル・スコアリングルール・状態遷移（外部依存なし）

[MatchOps.Infrastructure]  ── EF Core(PostgreSQL) / Redis / AI(LLM) / 配信(LINE・メール) / 取込
[MatchOps.Worker]          ── 取込・バッチスコアリング・配信・分析更新（cron→Hangfire）
[Metabase]                 ── KPI 可視化・効果測定
```

### 3.1 レイヤーと依存方向（クリーンアーキテクチャ）

| レイヤー | 責務 | 例 |
|---|---|---|
| Domain | ビジネスルール・スコアリング・状態遷移・不変条件 | `MatchScore`, `MatchingCampaign.Approve`, `TimeSlot` |
| Application | ユースケース調整・トランザクション境界・インターフェース定義 | `MatchingCampaignService`, `IAiProposalService` |
| Infrastructure | 永続化・LLM・配信・外部連携 | `MatchOpsDbContext`, `OpenAiProposalService`, `LineNotificationSender` |
| Api | HTTP・認証・テナントスコープ・OpenAPI | `MatchingCampaignsController` |
| Worker | バックグラウンド処理 | `NightlyScoringJob`, `OutboxDispatchJob` |

依存方向の絶対ルール（CLAUDE.md §4 と一致）:

- `Domain` は他レイヤーに依存しない（BCL のみ）。
- `Application` は `Domain` のみ参照。
- `Infrastructure` は `Domain` と `Application` を参照。
- `Api` / `Worker` は `Application` と `Infrastructure` を参照（DI 登録のみ）。
- 逆方向参照を作らない。

### 3.2 モジュール（境界づけられたコンテキスト）

Application/Domain は以下のモジュールに分割し、フォルダ・名前空間で境界を表現する。

| モジュール | 役割 | 将来の分離容易性 |
|---|---|---|
| Tenancy | テナント・店舗・権限・設定 | 低（共通基盤） |
| Customers | 顧客・セグメント・来店/施術/注文履歴（Activity） | 中 |
| Scheduling | 空き枠（TimeSlot）・予約・リソース（席/スタッフ） | 中 |
| Catalog | メニュー・コース・クーポン（Offer） | 中 |
| Matching | 候補抽出・スコアリング・施策（Campaign）・マッチング結果 | **高（将来分離候補）** |
| Ai | 施策要約・文面生成・分析コメント（LLM 隔離） | **高（将来分離候補）** |
| Notifications | 配信文面・配信ログ・Outbox（LINE/メール） | **高（将来分離候補）** |
| Experiments | ホールドアウト割当・リフト測定（効果実証） | 中 |
| Analytics | KPI 集計・効果測定（Metabase 連携） | 中 |

## 4. 抽象モデル（ユビキタス言語）

業種横断のため、業務概念を抽象モデルにマッピングする。Domain はこの語彙で実装する。

| 抽象概念 | 意味 | 飲食店 | 美容院 |
|---|---|---|---|
| Requester | 提案を受ける主体 | 顧客 | 顧客 |
| Provider | 提供する主体 | 店舗 / 席 | スタッフ |
| Resource | 割り当て対象 | 席・個室・テーブル | スタッフ・施術台 |
| TimeSlot | 提供可能な時間枠 | 予約可能枠 | 施術可能枠 |
| Offer | 提案内容 | クーポン・コース | メニュー・指名枠 |
| Match | マッチング結果 | 席提案・予約提案 | スタッフ/メニュー提案 |
| Activity | 行動履歴 | 来店・注文 | 来店・施術 |

業種差は「業種テンプレート（設定）」で吸収し、Domain のコア語彙は共通に保つ（ADR-0008 予定）。

## 5. 主要 Aggregate と状態

| Aggregate | 主な責務 | 主な状態 / 不変条件 |
|---|---|---|
| `Tenant` / `Store` | テナント・店舗・業種・設定（値引き上限・通知頻度上限） | — |
| `Customer` | 顧客属性・連絡先（ハッシュ）・オプトイン状態 | opt-in/opt-out、識別子は最小化 |
| `CustomerActivity` | 来店・注文・施術・予約・キャンセルの履歴（append-only） | 不変 |
| `TimeSlot` | 空き枠（日時・リソース・対応メニュー） | open / held / booked / closed |
| `Offer` | メニュー・クーポン（配信条件・値引き上限） | active / inactive |
| `MatchingCampaign` | 施策（対象空き枠・候補・スコア・提案） | draft → scored → proposed → **approved**（人手）→ sent → measured |
| `MatchingResult` | 顧客 × 空き枠 × Offer のスコアと提案理由・文面 | スコア内訳を保持 |
| `Notification` | 配信文面・配信ログ（Outbox 経由） | queued → sent / failed |
| `Experiment` | ホールドアウト割当（処置群/対照群） | リフト測定の基礎（ADR-0007） |
| `ConversionEvent` | 予約・来店・購入などの成果 | append-only |
| `AuditLog` | AI提案・承認・配信・編集の操作履歴 | append-only（UPDATE/DELETE 禁止） |

`MatchingCampaign` の `proposed → approved` は **必ず人手**（Human-in-the-loop, ADR-0004）。

## 6. マッチング・スコアリング設計

### 6.1 基本フロー

```
1. 空き枠を検出（Scheduling）
2. 合致する顧客候補を抽出（Matching）
3. 顧客ごとにスコアリング（Matching / Domain）
4. 提案する Offer を選定
5. AI が提案理由と配信文面を生成（Ai） ※承認前
6. 管理者が確認・編集・承認（人手）
7. 配信（Notifications, Outbox 経由）
8. 成果を記録（ConversionEvent）
9. 次回スコアリングへ反映（Experiments / Analytics）
```

### 6.2 スコアは v0（3要素）から始める（ADR-0003）

Phase 0/1 は機械学習を使わず、説明可能なルール＋重み付け。
v0 は次の 3 要素のみで開始する（識別子と来店日時さえあれば動く）。

```
match_score(v0) =
    休眠日数スコア          × w1
  + 来店周期との乖離スコア   × w2
  + 空き枠の曜日/時間帯一致  × w3
```

データ蓄積に応じて、メニュー相性・クーポン反応・客単価期待・通知疲れペナルティ等を順次追加する。
**段階的スコアリング（progressive scoring）**：欠損項目は重みから除外し、残り項目で動的に再正規化する。

スコアの重みは Phase 0 では設定（`appsettings`/設定ファイル）で持ち、管理画面 UI 化は後回し。

### 6.3 AI の役割と境界（ADR-0005）

AI は補助に限定する：施策要約・提案理由の文章化・配信文面生成・結果コメント・改善案提示。

- **AI に PII を渡さない。** プロンプトには匿名化・集約済みデータのみ（例：「45日以上未来店の顧客 12名」）。識別子・氏名・連絡先・カード情報を渡さない。
- **顧客ごとに生成しない。** 生成は施策（キャンペーン）単位・セグメント単位。文面は AI 生成テンプレ＋差し込み。これによりコストを抑える（1施策あたりの生成コストはサブスク比 1% 未満〜数%を目安）。
- 値引き提案は店舗設定の上限を超えない。通知頻度制限を尊重する。
- AI 連携は `Infrastructure/Ai` に隔離し、`Application` は `IAiProposalService` インターフェース越しに使う（モデル差し替え・モック容易）。

## 7. 配信・冪等性・監査（横断的関心事）

chargehub と同じパターンを踏襲する。

- **Idempotency-Key**：施策の `run`/`approve`/`send` 等の POST/PATCH は冪等キー必須（多重実行防止）。
- **Outbox パターン**：配信は DB 状態変更と同一トランザクションで Outbox に積み、`OutboxDispatchJob`（Worker）が後で実送信（LINE/メール）。失敗は指数バックオフでリトライ。
- **監査ログ**：AI提案・承認・編集・配信を `AuditLog` に append-only 記録（アプリロールから UPDATE/DELETE を REVOKE）。
- **Time Source**：本番コードで `DateTime.UtcNow` を直呼びしない。`IClock` 経由。Domain には `DateOnly today` 等をパラメータで渡す。

## 8. データ設計（Phase 0/1 は軽量版）

MVP は約 8〜9 テーブルから開始する（企画の 18 本を統合・JSONB/内包列で簡素化）。

主要テーブル（PostgreSQL 18）:

```
tenants / stores
customers
customer_activities      -- 来店・注文・施術・予約・キャンセル（append-only）
time_slots               -- 空席・空きスタッフ枠
offers                   -- メニュー・クーポン（条件・値引き上限を内包）
matching_results         -- 候補・スコア内訳・提案理由・文面・状態（campaign は文字列キーで内包）
experiment_assignments   -- arm = control | treatment
notification_logs        -- 配信ログ（Outbox は別テーブル outbox_messages）
conversion_events        -- 成果
audit_logs               -- 操作履歴（append-only）
```

- 個人情報は最小列。電話・メールは **ハッシュ化／暗号化**して保持（`phone_hash` / `email_hash`）。
- すべての業務テーブルに `tenant_id`。クエリは必ず `tenant_id` でスコープ（行レベルのテナント分離、ADR-0006）。
- `segments` / `menus` / `coupons` / `campaigns` 等はビュー・JSONB・内包列で代替（Phase 1 で必要に応じ昇格）。

## 9. セキュリティ・個人情報

- **テナント分離**：`tenant_id` スコープを Application/Infrastructure で機械的に強制。
- **権限**：本部管理者 / 店舗管理者 / 閲覧者 を分離。
- **PII**：暗号化・ハッシュ化。ログ・例外・AI プロンプト・Webhook ペイロードに平文 PII を出さない。
- **シークレット**：`appsettings.json` に書かない。環境変数 / Secrets Manager / KMS。
- **配信制御**：オプトアウト・頻度上限・値引き上限を尊重。
- **監査**：AI提案・承認・配信・編集・データ削除要求への対応を記録。

詳細な実装ルールは `CLAUDE.md` §9 を参照。

## 10. 効果測定と KPI（ADR-0007）

「施策をしなくても来た顧客」と区別するため、**ホールドアウト（対照群）**を必ず置く。

- 各施策（またはセグメント）で対象顧客を処置群（配信, 80〜90%）／対照群（非配信, 10〜20%）にランダム分割。
- リフト = 処置群CVR − 対照群CVR（主要CV=来店、サブ=予約・クーポン利用）。
- 増分売上 = リフト × 対象人数 × 客単価 − 配信・値引原価。増分ROI = 増分粗利 ÷ 施策コスト。
- 1施策では検出力不足になりやすい → 同種施策をプールし、信頼区間つきで判断。
- 事業KPI：空き枠充足率・再来店率・休眠復帰率・配信予約率・配信来店率・クーポン利用率・客単価・LTV・通知停止率。
- 運用KPI：AI提案採用率・管理者修正率・配信成功率・ノーショー率。

## 11. 開発フェーズ

| フェーズ | 期間目安 | 内容 |
|---|---|---|
| Phase 0 | 1〜2週 | リポジトリ骨格・サンプルCSV取込・候補抽出・v0スコア・AI要約・Metabase 可視化 |
| Phase 1 | 1〜2か月 | 管理画面・各種管理・空き枠検出・スコア・AI提案・承認・配信・効果測定 |
| Phase 2 | 2〜3か月 | 1〜3店舗で実データ検証・配信結果分析・ルール改善・運用負荷確認 |
| Phase 3 | 継続 | 複数業種・本部管理・複数店舗送客・POS/予約台帳API連携・課金基盤連携 |

初期 PoC は美容院または飲食店の一方に絞る（設計は汎用のまま）。
タスクの粒度と順序は `TASKS.md` / `docs/codex-instructions/` を参照。

## 12. 用語集

- **空き枠（TimeSlot）**：席・スタッフ・時間帯で表現される、埋めたい供給。
- **施策（Campaign）**：1つの空き枠群に対する、候補抽出〜提案〜配信の単位。
- **来店可能性スコア**：顧客がこの空き枠に来店しそうかの優先度。
- **リフト（Lift）**：施策の純粋な効果（処置群と対照群の差）。
- **Human-in-the-loop**：AI 提案を人が承認してから実行する運用形態。

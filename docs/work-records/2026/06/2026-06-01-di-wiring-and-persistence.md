# 2026-06-01 合成ルート配線・施策永続化・Outbox（Phase 0 完結）

## 背景
PR #9（Stage 0.9 API）・#10（Stage 0.10 Ai）マージ後、`/api/campaigns` を実際に動かすため、`MatchingCampaignService` が依存するデータ系ポートを実装し合成ルート（DI）を配線する。ユーザー判断: **「Phase 0 完結版」**（候補ソースの本実装＝セグメント抽出/スコア特徴量は Phase 1 のため暫定）。

## 実施内容
- **施策永続化（Stage 0.7 繰り延べ分）**
  - `MatchingCampaign` を EF Core でマッピング（`MatchingCampaignConfiguration`）。状態・承認メタはスカラ列、対象枠と候補（スコア内訳・提案理由）は jsonb 列（`MatchingCampaignConverters`）。
  - Domain 集約に **ORM 再構成専用の private 既定コンストラクタ**を追加（不変条件・public 表面は不変、挙動非破壊）。`_targetSlots`/`_candidates` の readonly を外し EF バッキングフィールド経由で再構成。候補の可変（提案理由付与）を検知するため比較子はディープコピーでスナップショット。
  - `EfMatchingCampaignRepository`（`IMatchingCampaignRepository`）、`EfUnitOfWork`（`IUnitOfWork`）。
- **Outbox（Stage 0.11 領域）**
  - `OutboxMessageEntity` ＋ `OutboxMessageEntityConfiguration`（outbox_messages、PII 非保持・顧客は CustomerId 参照）、`EfOutboxWriter`（`IOutboxWriter`、CreatedAt は IClock）。
- **ポリシー・候補ソース**
  - `ConfigurationMatchingPolicyProvider`（`IMatchingPolicyProvider`、`MatchingOptions` から重み/頻度。重み未設定は v0 既定）。
  - `PlaceholderCampaignCandidateSource`（`ICampaignCandidateSource`、対象枠を空候補で返す暫定。セグメント抽出/スコア特徴量は Phase 1）。
- **DI 配線**
  - `AddInfrastructure(IConfiguration)`（永続化・UoW・リポジトリ・Outbox・ポリシー・候補ソース・AI〔OpenAI typed client＋OpenAiOptions バインド〕・Matching 委譲アダプタ）。
  - `Program.cs`：`AddInfrastructure` ＋ `IMatchingCampaignService`/`IMatchingCampaignQueries` を登録。
  - `appsettings.json`：`ConnectionStrings:MatchOps`（空＝環境変数で注入）、`OpenAi`（ApiKey なし）、`Matching` セクション。**シークレットは書かない**（接続文字列・API キーは環境変数/Secrets）。
  - 接続文字列未設定でも起動できるよう `UseNpgsql()`（遅延）にフォールバック。
- **マイグレーション**：`AddMatchingCampaignAndOutbox`（matching_campaigns・outbox_messages、jsonb 列・tenant_id/status インデックス）。既存テーブルへの変更なし。
- **API 補完（フロー成立に必須）**：コントローラに `POST {id}/propose` を追加（指示文 008 は run/approve/send/results のみで propose が欠落しており、run→approve が常に 409 で人手承認フローを完走できなかったため）。既存 `ProposeAsync` を公開、冪等・Result 写像は他アクションと一貫。
- **不具合修正**：`IdempotencyFilter` の再生レスポンス本文を Web 既定（camelCase）でシリアライズするよう修正。実機検証で初回 `campaignId`／再生 `CampaignId` の表記不一致を検出（既定 PascalCase と MVC camelCase の差）。回帰テスト追加。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 234/234 Green（Domain 196 + Application 10 + Api 8〔+2: 合成ルート解決・完全フロー〕 + Infra 19〔+3: 施策往復・テナント分離・Outbox〕 + Integration 1）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable / --deprecated` → クリーン（追加: `Microsoft.Extensions.Http` 10.0.0、`Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.0。脆弱性・非推奨なし）。
- **実機 E2E（PostgreSQL 18 / Testcontainers 外の docker・マイグレーション適用）**：
  - run→200 / propose→204 / approve→204 / send→204 / results→200(status=Sent) を実データで完走。
  - 別テナント results→404（Global Query Filter）、Idempotency-Key 欠如→400、同一キー二重 run→同一施策（副作用1回・再生本文の表記一致を確認）。
  - DB に施策2件（Scored/Sent）永続化、Outbox 0件（候補0＝暫定候補ソース）。
  - AI 障害時（OpenAi エンドポイント到達不可）も propose がフォールバックで継続（例外を投げない）。

## 設計判断
- **Domain への private 既定コンストラクタ追加**：EF 再構成のための最小・挙動非破壊の変更。新規生成はファクトリ (Open) のみで、private ctor は ORM 専用。不変条件は状態遷移メソッドが担保。
- **候補ソースは暫定（空候補）**：セグメント抽出（休眠/常連/新規 等）とスコア特徴量算出（休眠日数・来店周期乖離・曜日時間帯一致）は Phase 1 本実装（TASKS §3）。本暫定で人手承認フローは完走可能（候補0）。
- **propose エンドポイント追加**：指示文 008 の欠落補完。AI 提案（scored→proposed）を経て承認に進む人手フローを API で成立させるために必須。
- **接続文字列/API キーは設定（環境変数/Secrets）**：appsettings に書かない（CLAUDE.md §9.1）。未設定でも起動（クエリ時に解決）。

## 未解決事項・次アクション
- `ICampaignCandidateSource` の本実装（セグメント抽出・スコア特徴量）= Phase 1。これにより run が実候補を生成し、send が Outbox に実メッセージを積む。
- Outbox の実送信（`OutboxDispatchJob`・Worker、指数バックオフ）= Stage 0.11 残り。
- audit_logs への AI 提案・承認・配信の記録連携。認証スタブ→本認証（Phase 1）。冪等ストア→Redis（Phase 1）。
- 監査: `infra/db/audit_logs_revoke.sql` の本番/ステージング適用（既存事項）。

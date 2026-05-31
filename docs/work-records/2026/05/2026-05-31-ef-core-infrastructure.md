# 2026-05-31 Stage 0.7 EF Core Infrastructure（永続化）

## 背景
`006_ef_core_infrastructure.md`。EF Core / PostgreSQL 18 による永続化層。テナント分離（ADR-0006）を Global Query Filter で機械的に強制する。

## スコープ判断（指示文からの差分・要確認事項）
- **MatchingCampaign の永続化は Stage 0.8（Application UseCase: run/approve/send）へ繰り延べ**。理由: ①候補リストの読み書きパターン（jsonb 内包 vs 子テーブル／承認 UI 向け取得）は Application のユースケースで決まる設計判断で、単独マッピングは 0.8 で作り直しのリスク ②ユースケース不在では意味ある統合テストが不可。
- 本 Stage は永続化形状が明確な **5 Aggregate（Customer / CustomerActivity / Resource / TimeSlot / Offer）＋ audit_logs** を確実にマッピング・統合テスト。

## 実施内容
- NuGet 追加（Infrastructure）: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2、`Microsoft.EntityFrameworkCore(.Relational)` 10.0.8、`Microsoft.EntityFrameworkCore.Design` 10.0.8（PrivateAssets=all）。テスト: `Testcontainers.PostgreSql`。
- `ITenantContext`（Application/Tenancy）: 現在テナントの抽象。
- `MatchOpsDbContext`: DbSet 6種、`ConfigureConventions` で強い型 ID×8 を GUID 変換一括登録、`OnModelCreating` で全業務エンティティに Global Query Filter（`TenantId == CurrentTenant`、未解決時は既定値＝該当なし）。
- 値オブジェクト変換（`ValueObjectConverters`）: ContactHash↔text、Money↔"金額 通貨"text、TimeRange/対応Offer種別/DiscountCap/OfferConditions↔jsonb（factory 再構築・ValueComparer 付き）。
- 各 Aggregate の `IEntityTypeConfiguration`（5種）＋ `AuditLogEntry`（Infrastructure エンティティ）＋ Configuration。
- 初版 Migration `InitialCreate`（PostgreSQL 18 / Npgsql、6テーブル・jsonb列・tenant_id index）。
- `MatchOpsDbContextFactory`（設計時）。
- audit_logs の UPDATE/DELETE REVOKE は運用スクリプト `infra/db/audit_logs_revoke.sql` に分離（理由は下記）。

## 設計判断
- **テナント強制 = Global Query Filter**（ADR-0006 の二択のうち採用）。`ITenantContext` から現在テナントを解決し、未解決時は既定値で照合 → 何も返さない安全側。インサートはフィルタ対象外（エンティティが保持する TenantId をそのまま保存）。
- **強い型 ID は `ConfigureConventions` で型単位に一括変換**（プロパティ個別指定不要）。
- **複合値オブジェクトは jsonb**（設計 §8）。Domain は private ctor + factory 検証を持つため、変換は「素朴表現 ↔ factory 再構築」とし、Domain を一切変更しない（制約遵守）。EF は private ctor へのコンストラクタバインディングで各 Aggregate を復元。
- **audit_logs の REVOKE はマイグレーションに含めず運用スクリプト化**。対象ロールが環境依存で Testcontainers 等の一時 DB に存在せず、マイグレーションの可搬性（空 DB 適用）を壊すため（指示文「Migration or 初期化スクリプト」の後者を採用）。テーブル作成はマイグレーション、権限剥奪は運用適用。
- **EF Core バージョン統一**: Npgsql 提供の 10.0.4 と Design の 10.0.8 の混在が `warnaserror` で MSB3277 化したため、`Microsoft.EntityFrameworkCore(.Relational)` を 10.0.8 に明示ピンして統一。
- **生成 Migration を生成コード扱い**: `.editorconfig` に `[**/Migrations/*.cs] generated_code = true` を追加し、IDE0161（file-scoped namespace）等のスタイル規則・CS1591 の対象外に（CLAUDE.md §5.4 と整合）。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 205/205 Green（Domain 196 + Infra 統合 6 + 骨格 3）。
- 統合テスト（Testcontainers PostgreSQL 18）: マイグレーション空 DB 適用／強い型 ID・ContactHash 往復／TimeRange・カテゴリ jsonb 往復／DiscountCap jsonb 往復／**別テナントのデータが取得されない**／テナント未解決で空。
- `dotnet format --verify-no-changes` → 差分なし（生成 Migration は除外）。
- `dotnet list package --vulnerable / --deprecated` → 全 10 プロジェクトでクリーン。Design は PrivateAssets=all で下流非伝播。

## 未解決事項・次アクション
- MatchingCampaign の永続化（候補/状態/承認メタ）を Stage 0.8 で実装。
- audit_logs REVOKE を本番/ステージングのプロビジョニングで適用。
- TimeRange を jsonb で保持（時間範囲クエリは jsonb 式）。timestamptz 列への昇格は将来最適化。
- Stage 0.8（`007_application_usecase`）: Application UseCase（run/approve/send 骨格・IUnitOfWork・承認境界）。

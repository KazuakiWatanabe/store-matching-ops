# 006 — EF Core Infrastructure (Stage 0.7)

## 前提
- `CLAUDE.md` §3, §9, §10。`docs/architecture/design.md` §8（データ設計）
- ADR-0006（テナント分離）

## 対象ディレクトリ
- `src/MatchOps.Infrastructure/Persistence`, `tests/MatchOps.Infrastructure.Tests`

## 作成物
- `MatchOpsDbContext`
- 各 Aggregate の `IEntityTypeConfiguration`
- 強い型 ID の ValueConverter、`phone_hash`/`email_hash` のマッピング
- 初版 Migration（PostgreSQL 18 / Npgsql）
- テナントスコープを強制する仕組み（Global Query Filter or リポジトリ層での必須引数）

## 仕様
- すべての業務テーブルに `tenant_id`。読み書きは `TenantId` でスコープ。
- `audit_logs` はアプリロールから UPDATE/DELETE を REVOKE（Migration or 初期化スクリプト）。
- JSONB を使う列（スコア内訳・提案メタ等）は明示マッピング。

## テスト要件（Testcontainers PostgreSQL）
- 別テナントのデータが取得されない（Query Filter 検証）。
- 強い型 ID のラウンドトリップ。
- Migration が空 DB に適用できる。

## 制約
- Domain を変更しない（永続化都合の漏れを Domain に逆流させない）。
- Migration を down で破壊しない。

## 完了条件
- build/test（統合）通過。テナント分離テストが緑。

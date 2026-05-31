-- audit_logs を append-only にするための権限スクリプト（運用適用）。
-- 監査ログの改ざん防止のため、アプリケーションロールから UPDATE / DELETE を剥奪する（CLAUDE.md §10.2, 設計 §9）。
--
-- EF マイグレーションには含めない理由:
--   - 対象ロールは環境ごとに異なり、Testcontainers 等の一時 DB には存在しないため。
--   - マイグレーションの可搬性（空 DB への適用）を保つため。
-- 本番/ステージングのプロビジョニング時に、アプリロールを指定して適用する。
--
-- 使い方（例）: psql -v app_role=matchops_app -f audit_logs_revoke.sql

\set app_role :app_role

REVOKE UPDATE, DELETE ON TABLE audit_logs FROM :"app_role";

-- INSERT / SELECT は許可（append-only と参照のため）。
GRANT INSERT, SELECT ON TABLE audit_logs TO :"app_role";

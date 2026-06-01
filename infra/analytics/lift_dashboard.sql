-- =============================================================================
-- MatchOps 効果測定（リフト）ダッシュボード用クエリ（ADR-0007 / design.md §10）
-- experiment_assignments（arm = control / treatment）と conversion_events を突合し、
-- 施策別の 処置群CVR・対照群CVR・リフト・増分件数・増分売上 を算出する。
--
-- 注意（検出力）:
--   1 施策では対照群が小さく検出力が不足しやすい。同種施策をプールして信頼区間つきで判断すること。
--   下部の「同種施策プール集計」を参照。サンプル数の目安（簡易）も併記する。
--   Metabase ではテナントでフィルタして表示する（tenant_id）。
-- =============================================================================

-- 顧客単位に CV を集約（重複 CV の二重計上を避ける）。
WITH conv AS (
    SELECT campaign_id, customer_id, SUM(revenue) AS revenue
    FROM conversion_events
    GROUP BY campaign_id, customer_id
),
-- arm 別の人数・CV 件数・売上。
tally AS (
    SELECT
        a.tenant_id,
        a.experiment_id,
        a.campaign_id,
        a.arm,
        COUNT(*)                       AS n,
        COUNT(c.customer_id)           AS conversions,
        COALESCE(SUM(c.revenue), 0)    AS revenue
    FROM experiment_assignments a
    LEFT JOIN conv c
        ON c.campaign_id = a.campaign_id
       AND c.customer_id = a.customer_id
    GROUP BY a.tenant_id, a.experiment_id, a.campaign_id, a.arm
),
-- 実験（施策）単位に処置群／対照群を 1 行へピボット。
arms AS (
    SELECT
        tenant_id,
        experiment_id,
        campaign_id,
        COALESCE(MAX(n)           FILTER (WHERE arm = 'Treatment'), 0) AS t_n,
        COALESCE(MAX(conversions) FILTER (WHERE arm = 'Treatment'), 0) AS t_cv,
        COALESCE(MAX(revenue)     FILTER (WHERE arm = 'Treatment'), 0) AS t_rev,
        COALESCE(MAX(n)           FILTER (WHERE arm = 'Control'), 0)   AS c_n,
        COALESCE(MAX(conversions) FILTER (WHERE arm = 'Control'), 0)   AS c_cv
    FROM tally
    GROUP BY tenant_id, experiment_id, campaign_id
)
-- 施策別リフト。
SELECT
    tenant_id,
    experiment_id,
    campaign_id,
    t_n  AS treatment_count,
    c_n  AS control_count,
    CASE WHEN t_n > 0 THEN t_cv::numeric / t_n ELSE 0 END AS treatment_cvr,
    CASE WHEN c_n > 0 THEN c_cv::numeric / c_n ELSE 0 END AS control_cvr,
    (CASE WHEN t_n > 0 THEN t_cv::numeric / t_n ELSE 0 END)
        - (CASE WHEN c_n > 0 THEN c_cv::numeric / c_n ELSE 0 END) AS lift,
    ((CASE WHEN t_n > 0 THEN t_cv::numeric / t_n ELSE 0 END)
        - (CASE WHEN c_n > 0 THEN c_cv::numeric / c_n ELSE 0 END)) * t_n AS incremental_conversions,
    -- 増分売上 = リフト × 処置群人数 × 客単価（処置群 CV あたり売上）。
    (((CASE WHEN t_n > 0 THEN t_cv::numeric / t_n ELSE 0 END)
        - (CASE WHEN c_n > 0 THEN c_cv::numeric / c_n ELSE 0 END)) * t_n)
        * (CASE WHEN t_cv > 0 THEN t_rev::numeric / t_cv ELSE 0 END) AS incremental_revenue
FROM arms
ORDER BY tenant_id, experiment_id;

-- =============================================================================
-- 累積増分売上（テナント別の合計）。Metabase の単一値/時系列に。
-- 上記 SELECT を sub-query 化して SUM すること（BI ツール側で集約しても可）。
-- =============================================================================

-- =============================================================================
-- 同種施策プール集計（検出力確保）: 複数施策を束ねて処置群／対照群を合算し、
-- プール全体のリフトを見る。簡易なサンプル数目安として、対照群 n が小さい行は
-- 信頼できない旨をダッシュボードに注記する（例: control_count < 100 は参考値）。
-- =============================================================================

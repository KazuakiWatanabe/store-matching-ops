# 2026-06-02 Stage 0.12 Experiments（ホールドアウト・リフト測定）

## 背景
`011_experiment_lift.md` / ADR-0007。配信由来の見かけ CV から「施策がなくても来た顧客」を差し引くため、対象顧客を control（非配信・追跡）/ treatment（配信）に分割し、リフト（増分）で効果を測る。Phase 0 最終 Stage。

## 実施内容
- **Domain/Experiments（中核）**
  - `ExperimentId`（Common, 強い型）、`ExperimentArm`（Control/Treatment）。
  - `HoldoutAssignmentPolicy`：実験 ID×顧客 ID の **SHA-256 安定ハッシュ**で [0,1) の一様値を導き control 比率と比較。シード/乱数なしで再現可能（同一入力→同一 arm）。
  - `ExperimentAssignment`（実験×顧客で一意）。`ArmOutcome`（人数・CV・売上、検証付き）。
  - `LiftResult.Calculate`：リフト = 処置群CVR − 対照群CVR、増分件数 = リフト×処置群人数、増分売上 = 増分件数×客単価（処置群 CV あたり売上）。
- **Application/Experiments**
  - ポート: `IExperimentAssignmentRepository`、`IConversionReadStore`（+`ConversionRecord`）。
  - `ExperimentService.AssignAsync`：対象顧客を決定的割当・永続化し、**配信対象（treatment）集合**を返す（control は含めない＝非配信）。比率は既定 0.1（運用 10〜20%）。想定失敗は Result。
  - `ExperimentQueries.GetLiftAsync`：割当を arm 別集計し conversion_events と顧客単位で突合（重複 CV は二重計上しない）→ `LiftResult`。
- **Infrastructure**
  - `experiment_assignments`（複合主キー 実験×顧客・arm は文字列）、`conversion_events`（成果。記録＝取込は Phase 1、本 Stage は読み取り）。Entity/Configuration、`ExperimentId` 変換登録、DbSet＋テナントフィルタ。
  - `EfExperimentAssignmentRepository`、`EfConversionReadStore`。Migration `AddExperimentsAndConversions`。`AddInfrastructure` に登録。
- **分析**: `infra/analytics/lift_dashboard.sql`（施策別リフト・増分売上・対照群差、累積増分売上、同種施策プール集計の雛形と検出力注記）= Metabase 用最小ダッシュボード。

## 設計判断
- **決定的ハッシュ割当**：再現可能（シード不要）・顧客単位で安定（再実行/再集計で割当が揺れない）。比率 0/1 の境界も網羅。Domain で SHA-256（BCL のみ・NuGet 非追加）。
- **control 非配信の表現**：`AssignAsync` が treatment 集合（配信対象）を返す契約とし、送信はこの集合のみを対象とする（control 除外をテストで保証）。実際の send 連携（実候補での treatment のみ Outbox 積み）は Phase 1（候補ソース本実装）で結線。
- **二重計上の回避**：リフト集計は顧客単位で CV 有無・売上合計を取る（重複 CV を二重計上しない）。
- **検出力**：1 施策では対照群が小さく不足しがちなため、SQL に同種施策プール集計の雛形とサンプル数目安の注記を用意（ADR-0007）。
- **API 露出なし**：指示文の作成物は Domain/Application/Infrastructure＋分析 SQL のため、Experiments の API エンドポイントは作成しない（必要時 Phase 1）。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 269/269 Green（Domain 212〔+16〕 + Application 16〔+6〕 + Api 8 + Infra 32〔+4〕 + Integration 1）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable / --deprecated` → クリーン（新規 NuGet 追加なし）。
- 受け入れ観点（011）: 指定比率で control/treatment 分割（10000 件・誤差内）/ 同一入力・同一実験で割当再現 / control は配信対象集合に含まれない / リフト算出が既知データで正値（Domain・Application・Infra 統合の各層）。

## 未解決事項・次アクション
- `conversion_events` の取込（成果記録）と `ICampaignCandidateSource` 本実装（Phase 1）→ 実データでリフトが回る。
- send 連携で treatment のみ配信（候補ソース本実装と同時に結線）。
- リフトの信頼区間・サンプル数の本格計算、プール集計の本実装、Metabase ダッシュボード構築。
- Phase 0 はこれで一巡（骨格〜縦串〜効果測定の枠組み）。次は Phase 1（管理画面・セグメント抽出・実配信・効果測定運用）。

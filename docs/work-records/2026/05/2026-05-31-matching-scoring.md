# 2026-05-31 Stage 0.6 Matching: 候補抽出・v0 スコア・Campaign

## 背景
`005_matching_engine_scoring.md`（プロダクト中核）。スコアリングと状態遷移は Claude Code 主担当。
ADR-0003（スコアリング）・ADR-0004（承認フロー）に準拠。テストファースト。

## 重要な設計判断: 候補抽出の置き場所（ユーザー確認済み）
指示文は Matching に候補抽出を置くが、CLAUDE.md §4.1（別モジュールの Domain 型を直接参照しない／Matching は将来分離候補・高）と衝突する。
**ユーザー判断により「Matching 内に抽象入力で実装」を採用**:
- Matching Domain は他モジュール（Customers/Scheduling/Catalog）の Domain 型に依存しない。
- 候補抽出は Matching ローカルの入力抽象（`CustomerCandidacy` / `SlotCandidacy` / `OfferOption` / `CandidateInput`。Common の ID＋真偽値/日付のみ）に対して実装。
- 実 Aggregate（Customer.CanReceiveNotifications()・Offer.EnsureDiscountWithinCap()・TimeSlot 等）→ 入力抽象への写像は Application（Stage 0.7）が担う。
- 値引き上限/適用条件/オプトインの「判定」は各モジュールに残り、Matching は写像済みの真偽値で「除外」する。各判定ロジック自体は各モジュールのテストで担保済み。

## 実施内容
`src/MatchOps.Domain/Matching/`（名前空間 `MatchOps.Domain.Matching`）に作成。
- `CampaignStatus` enum（Draft/Scored/Proposed/Approved/Sent/Measured）。
- `ScoringFactors`（項目キー定数）、`ScoreInputs`（0〜1 特徴量・欠損は不含）、`ScoringPolicy`（重み正規化＋動的再正規化、`Score→ScoreBreakdown`）。
- `MatchingCandidate`（Score は ScoreBreakdown.Total から導出＝常に整合、提案理由の格納先）。
- `NotificationFrequencyPolicy`（最小間隔日数）。
- 入力抽象: `CustomerCandidacy` / `SlotCandidacy` / `OfferOption`（IsUsable）/ `CandidateInput`。
- `MatchingEngine`（ドメインサービス・候補抽出＋採点・スコア降順）。
- `MatchingCampaign`（Aggregate・状態機械・ドメインイベント保持）。
- Domain Event: `MatchScored` / `CampaignApproved` / `CampaignSent`（`IDomainEvent` を Common に新設）。
ADR-0003 に v0 実装詳細（既定重み・再正規化・全欠損時 0・設定注入）を追記。

## 設計判断（重み・欠損時方針）
- **既定重み 0.40 / 0.35 / 0.25**（ADR-0003）。`ScoringPolicy.Create` は与えた重みを合計 1 に正規化（相対値可・各重み正）。コード直書きを避け設定注入前提。`CreateV0Default` は既定値の便宜ファクトリ。
- **動的再正規化**: 入力にある項目のみで `weight_i / Σ available` に再正規化。寄与 = 値 × 再正規化重み。合計は常に 0〜1（境界誤差は `MatchScore.From` の許容で吸収）。
- **全項目欠損時は合計 0**（`MatchScore.Zero`・空内訳）。候補からの除外は上位（エンジン/Application）の判断とし、ポリシー自体は 0 を返す。
- **承認フロー（ADR-0004）**: `Send` は `Approved` 必須（`RequireStatus(Approved)`）。`proposed` のままの `Send` は `DomainException`。全不正遷移を状態チェックで拒否。
- **MatchingCandidate のスコア整合**: `Score => Breakdown.Total` で構造的に常に整合。
- **副作用なし**: `Send`/`Approve` は状態遷移＋イベント記録のみ。実送信・AI 文面生成・永続化は持ち込まない。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 200/200 Green（Domain 196 + 各骨格 1）。
- Domain カバレッジ: **line 99.57% / branch 100%**（Matching の主要型は概ね 100%。目標 95%+）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable` → 全 10 プロジェクトでクリーン（NuGet 追加なし）。
- 必須観点テスト緑: 承認前 Send 拒否／不正遷移網羅／全項目あり重み通り／欠損再正規化（合計1）／全欠損 0／スコア 0〜1／値引き上限超過 Offer 不採用／オプトアウト・頻度超過除外／内訳合計＝スコア／テナント混在なし。

## 未解決事項・次アクション
- Application（Stage 0.7）で実 Aggregate → Matching 入力抽象への写像を実装（候補抽出の結線）。
- Stage 0.7（`006_ef_core_infrastructure` ではなく順序は `006` = EF Core）: 次は EF Core Infrastructure。
  （注: codex-instructions の順序では 006 = EF Core Infrastructure。Application UseCase は 007。）

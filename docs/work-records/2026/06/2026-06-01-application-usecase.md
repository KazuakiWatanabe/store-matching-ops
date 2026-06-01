# 2026-06-01 Stage 0.8 Application UseCase（run/approve/send 骨格）

## 背景
`007_application_usecase.md`。施策ユースケースの調整層（Application Service）を実装する。承認境界（ADR-0004 Human-in-the-loop）と PII/AI 境界（ADR-0005）を Application のオーケストレーションで強制し、配信は Outbox に積むのみ（実送信は Worker, Stage 0.11）とする。

## 実施内容
- **Application/Common**
  - `IClock`（`Now` / `Today`）: 時刻源の抽象（CLAUDE.md §10.4。Domain には時刻をパラメータで渡す）。
  - `IUnitOfWork`（`SaveChangesAsync`）: トランザクション境界。状態変更と Outbox 積み込みを同一確定。
  - `Result` / `Result<T>`: 想定済み失敗を例外でなく返す結果型（CLAUDE.md §7.3）。`ErrorCode` + 日本語 `ErrorMessage`（PII/シークレット非含）。
- **Application/Matching**
  - `IMatchingCampaignService` / `MatchingCampaignService`: `RunAsync`（候補抽出＋スコア, draft→scored）/ `ProposeAsync`（AI 提案依頼, scored→proposed）/ `ApproveAsync`（人手承認, proposed→approved）/ `SendAsync`（配信＝Outbox 積み込み, approved→sent）。
  - ポート: `IMatchingCampaignRepository`（施策永続化）、`ICampaignCandidateSource`（各モジュール横断で枠ごとの `SlotCandidates` を組立）、`IMatchingPolicyProvider`（`ScoringPolicy`/`NotificationFrequencyPolicy` を設定から）、`IAiProposalService`（`AiProposalRequest`/`AiProposalDraft`）。
  - Command: `RunCampaignCommand` / `ProposeCampaignCommand` / `ApproveCampaignCommand` / `SendCampaignCommand`。
- **Application/Notifications**
  - `IOutboxWriter` / `OutboxMessage`: 配信メッセージの積み込み抽象（PII を保持せず顧客は `CustomerId` 参照、文面 `Body` は差し込み済みテキスト）。
- **テスト（手書きテストダブル, NuGet 追加なし）**: `MatchingCampaignServiceTests` 9 件 + `TestDoubles`（FakeClock/InMemoryCampaignRepository/FakeCandidateSource/FakePolicyProvider/SpyAiProposalService/SpyOutboxWriter/CountingUnitOfWork）。

## 設計判断
- **承認境界は Application でも二重に強制**: `SendAsync` は `Status != Approved` を Result.Failure(`not_approved`) で返し、Domain の `Send`（DomainException）を制御フローに使わない（CLAUDE.md §7.3）。状態不正・未検出・承認者空白・空提案も Result で表現。
- **PII/AI 境界を型で表現**: `IAiProposalService` は個別顧客データを受け取らず、`AiProposalRequest` は施策の候補集合から集約した値（件数・枠数・Offer 種類数・平均スコア・日本語サマリ）と `StoreId` のみ保持。`BuildProposalRequest` は候補の `CustomerId`/連絡先を一切渡さない。AI 呼び出しは施策単位で 1 回（顧客ごとに呼ばない・CLAUDE.md §4.3）。テストで `AiProposalRequest` をシリアライズし候補 `CustomerId` が含まれないことを検証。
- **AI 入力の絞り込み（指示文からの差分）**: 指示文の `AiProposalRequest` 例から、業種別テンプレ選択や値引き上限の「プロンプト制約文」は実 AI 実装（Stage 0.10 / 009）で構築する責務とし、Application は施策集約から導出可能な PII フリー値のみを渡す形に簡素化（追加 lookup を持ち込まない）。値引き上限の遵守は候補抽出時に `OfferOption.DiscountWithinCap` で既に担保。
- **Outbox は 1 候補 = 1 メッセージ**: `SendAsync` は承認済み施策の各候補について `OutboxMessage` を積み、`IUnitOfWork` で状態変更と同一確定。実送信・リトライは Worker（Stage 0.11）。
- **`MatchingEngine` はステートレスなので `static readonly` フィールドで保持**（Domain サービス・依存なし）。
- **DI 登録は本 Stage では未実施（意図的）**: ポート実装（リポジトリ/候補ソース/AI/Outbox/ポリシー）が未実装で合成ルート（Api/Infrastructure 配線）が骨格のため。実装が揃う Stage 0.9（API）/Infrastructure で `AddApplication` 等として配線する。`MatchOps.Application` への DI パッケージ追加も同時に行う。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 213/213 Green（Domain 196 + Application 9〔新規〕 + Infra 統合 6 + Api 1 + Integration 1）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable --include-transitive` → 全 10 プロジェクトでクリーン（新規 NuGet 追加なし）。
- 受け入れ観点（AGENTS §2）: 未承認 `SendAsync` 拒否（Outbox 0 件）/ 承認後 `SendAsync` 成功（Outbox 1 件）/ AI 入力に `CustomerId` 非含・施策単位 1 回呼び出し、を緑で確認。

## 未解決事項・次アクション
- ポート実装と DI 配線: `IMatchingCampaignRepository`（Stage 0.7 で繰り延べた MatchingCampaign 永続化＝候補/状態/承認メタ）、`ICampaignCandidateSource`（Customers/Scheduling/Catalog 横断読み取り）、`IMatchingPolicyProvider`（設定）を Infrastructure に実装。
- `IAiProposalService` 実装と `AiProposalRequest` のプロンプト制約（業種テンプレ・値引き上限文・オプトアウト）強化は Stage 0.10（009）。
- `IOutboxWriter`/`OutboxMessage` の EF Core 実装と `OutboxDispatchJob` は Stage 0.11（010）。
- Idempotency-Key（run/approve/send の二重実行防止）は API 層 `IdempotencyFilter`（Stage 0.9）。
- 提案理由（説明文）と配信文面の分離保持: 現状は `MatchingCandidate.ProposalReason` に配信文面を格納。理由の別保持が必要なら Domain 拡張を検討（要設計判断）。

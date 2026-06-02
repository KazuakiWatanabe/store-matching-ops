# シーケンス図（as-built）

主要ユースケースの実装フロー。参加者はクラス／コンポーネント名（実装どおり）。

## 0. 横断：テナント解決と冪等性（/api 配下の共通前処理）

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant MW as TenantResolutionMiddleware
    participant RC as RequestContext
    participant F as IdempotencyFilter
    participant Ctl as MatchingCampaignsController

    C->>MW: HTTP（X-Tenant-Id, X-User-Id, Idempotency-Key）
    alt X-Tenant-Id 欠如/不正
        MW-->>C: 401 tenant_required
    else 解決成功
        MW->>RC: Resolve(tenantId, userId)
        MW->>F: next()
        alt Idempotency-Key 欠如
            F-->>C: 400 idempotency_key_required
        else 保存済み（同一キー＋同一本文）
            F-->>C: 保存済みレスポンスを再生（再実行しない）
        else 同一キー＋異なる本文
            F-->>C: 409 idempotency_key_conflict
        else 初回
            F->>Ctl: アクション実行
            Ctl-->>F: 結果
            F->>F: レスポンスを保存（5xx・例外は除く）
            F-->>C: レスポンス
        end
    end
```

## 1. run（候補抽出＋スコアリング, draft→scored）

```mermaid
sequenceDiagram
    autonumber
    participant Ctl as MatchingCampaignsController
    participant Svc as MatchingCampaignService
    participant Src as ICampaignCandidateSource
    participant Eng as MatchingEngine
    participant Pol as IMatchingPolicyProvider
    participant Repo as IMatchingCampaignRepository
    participant UoW as IUnitOfWork

    Ctl->>Svc: RunAsync(tenant, store, targetSlots)
    Svc->>Pol: GetScoringPolicy / GetFrequencyPolicy
    Svc->>Src: GetAsync(tenant, store, targetSlots)
    Note right of Src: Phase 0 は暫定（空候補）。<br/>本実装＝セグメント抽出/スコア特徴量は Phase 1
    Src-->>Svc: SlotCandidates[]
    loop 各空き枠
        Svc->>Eng: BuildCandidates(slot, inputs, policy, frequency, today)
        Eng-->>Svc: MatchingCandidate[]（スコア降順）
    end
    Svc->>Svc: MatchingCampaign.Open + RecordScoring
    Svc->>Repo: AddAsync(campaign)
    Svc->>UoW: SaveChangesAsync
    Svc-->>Ctl: Result<CampaignId>
```

## 2. propose（AI 提案生成, scored→proposed・PII 非送出）

```mermaid
sequenceDiagram
    autonumber
    participant Svc as MatchingCampaignService
    participant Repo as IMatchingCampaignRepository
    participant Ad as AiProposalServiceAdapter
    participant Ai as OpenAiProposalService
    participant LLM as OpenAI 互換 API
    participant UoW as IUnitOfWork

    Svc->>Repo: GetAsync(campaignId)
    alt 見つからない / scored でない
        Svc-->>Svc: Result.Failure（404 / 409）
    else
        Svc->>Svc: BuildProposalRequest（集約・匿名化／PII 非含）
        Svc->>Ad: GenerateProposalAsync(request)
        Ad->>Ai: GenerateMessageAsync(AiMessageContext)
        Ai->>LLM: POST /chat/completions（匿名化プロンプト・値引き上限明示）
        alt 成功
            LLM-->>Ai: 文面
            Ai->>Ai: 値引き上限超過を検証（超過なら既定文面へ置換）
        else 障害（非2xx/例外）
            Ai-->>Ad: フォールバック既定文面＋注意フラグ（例外を投げない）
        end
        Ad-->>Svc: AiProposalDraft（理由＋文面）
        Svc->>Svc: 各候補へ文面付与 + campaign.Propose()
        Svc->>UoW: SaveChangesAsync
    end
```

## 3. approve（人手承認, proposed→approved）

```mermaid
sequenceDiagram
    autonumber
    participant Ctl as MatchingCampaignsController
    participant Svc as MatchingCampaignService
    participant Repo as IMatchingCampaignRepository
    participant Clk as IClock
    participant UoW as IUnitOfWork

    Ctl->>Svc: ApproveAsync(campaignId, approvedBy=X-User-Id)
    Svc->>Repo: GetAsync(campaignId)
    alt approver 空 / 未検出 / proposed でない
        Svc-->>Ctl: Result.Failure（422 / 404 / 409）
    else
        Svc->>Clk: Now
        Svc->>Svc: campaign.Approve(approvedBy, now)
        Svc->>UoW: SaveChangesAsync
        Svc-->>Ctl: Result.Success → 204
    end
```

## 4. send（配信＝Outbox 積み, approved→sent）

```mermaid
sequenceDiagram
    autonumber
    participant Svc as MatchingCampaignService
    participant Repo as IMatchingCampaignRepository
    participant Box as IOutboxWriter
    participant UoW as IUnitOfWork

    Svc->>Repo: GetAsync(campaignId)
    alt approved でない
        Svc-->>Svc: Result.Failure("not_approved") → 409
    else
        loop 各候補（treatment 想定）
            Svc->>Box: EnqueueAsync(OutboxMessage)（実送信しない）
        end
        Svc->>Svc: campaign.Send(now)
        Svc->>UoW: SaveChangesAsync（状態変更＋Outbox を同一Tx）
    end
```

## 5. Outbox 配信（Worker・リトライ／配信制御）

```mermaid
sequenceDiagram
    autonumber
    participant Job as OutboxDispatchJob
    participant Disp as OutboxDispatcher
    participant DB as outbox_messages
    participant Elig as INotificationEligibility
    participant Snd as INotificationSender
    participant Log as notification_logs

    loop ポーリング間隔
        Job->>Disp: DispatchPendingAsync(batchSize)
        Disp->>DB: queued かつ next_attempt_at 到来（IgnoreQueryFilters・全テナント）
        loop 各メッセージ
            Disp->>Elig: IsAllowedAsync（オプトアウト等を再判定）
            alt 不許可
                Disp->>DB: status=skipped
                Disp->>Log: skipped
            else 許可
                Disp->>Snd: SendAsync（Phase 0 はログ出力スタブ）
                alt 成功
                    Disp->>DB: status=sent
                    Disp->>Log: sent
                else 失敗
                    Disp->>DB: attempt_count++、指数バックオフ／最大到達で failed
                    Disp->>Log: failed
                end
            end
        end
        Disp-->>Job: Summary(sent, failed, skipped)
    end
```

## 6. 効果測定（ホールドアウト割当 → リフト集計）

```mermaid
sequenceDiagram
    autonumber
    participant ES as ExperimentService
    participant Pol as HoldoutAssignmentPolicy
    participant AR as IExperimentAssignmentRepository
    participant UoW as IUnitOfWork
    participant EQ as ExperimentQueries
    participant CR as IConversionReadStore

    Note over ES: 割当（決定的）
    ES->>ES: AssignAsync(experiment, campaign, customers, controlRatio)
    loop 各顧客
        ES->>Pol: Assign(experimentId, customerId, controlRatio)
        Pol-->>ES: arm（SHA-256 安定ハッシュ／再現可能）
        ES->>AR: AddAsync(ExperimentAssignment)
    end
    ES->>UoW: SaveChangesAsync
    ES-->>ES: 配信対象（treatment 集合・control 除外）

    Note over EQ: リフト集計
    EQ->>AR: GetByExperimentAsync(experimentId)
    EQ->>CR: GetByCampaignAsync(campaignId)
    EQ->>EQ: arm 別集計（顧客単位・重複CV非二重計上）
    EQ->>EQ: LiftResult.Calculate（処置CVR − 対照CVR、増分件数・増分売上）
    EQ-->>EQ: LiftResult
```

> リフト集計は Metabase でも実行できる（`infra/analytics/lift_dashboard.sql`）。

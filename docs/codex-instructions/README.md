# docs/codex-instructions/

実装タスクの詳細指示。番号順に実施し、前タスクの完了条件を満たしてから次へ進む（`AGENTS.md` §6）。

各タスクファイルは次の節を必ず含む:

- **前提**：参照する設計書・ADR
- **対象ディレクトリ**
- **作成物**（ファイル単位）
- **仕様**（詳細）
- **テスト要件**（必須テストケース）
- **制約**（やってはいけないこと）
- **完了条件**

## 順序

```
000_initial_skeleton
 → 001_domain_common
   → 002_customer_and_activity
     → 003_timeslot_and_resource
       → 004_offer_and_catalog
         → 005_matching_engine_scoring
           → 006_ef_core_infrastructure
             → 007_application_usecase
               → 008_api_admin
                 → 009_ai_agent_service
                   → 010_notification_outbox
                     → 011_experiment_lift
```

## Aggregate 実装系タスク共通チェックリスト

新規 Aggregate / Value Object を追加するとき、以下を満たすこと。

- [ ] `TenantId` を保持し、不変条件で必須化している
- [ ] public メンバに日本語 XML コメント、`.cs` 先頭にファイルヘッダー
- [ ] ID は強い型（`<Aggregate>Id` record struct）。`Guid` を直接公開しない
- [ ] 不正な状態遷移を `DomainException` で拒否するテストがある
- [ ] 時刻はパラメータ（`DateOnly today` / `DateTimeOffset now`）で受け、`DateTime.UtcNow` を直呼びしない
- [ ] EF Core materialization 用に必要なら private parameterless ctor を追加（理由をコメント）
- [ ] テストファースト（Red → Green → Refactor）で実装している

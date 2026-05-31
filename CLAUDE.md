---
file: CLAUDE.md
version: 0.1.0
last-updated: 2026-05-31
review-required: 2 humans
---

# CLAUDE.md — 実装規約とコーディング指針

このファイルは、Codex / Claude Code を含む AI エージェントが本リポジトリ `store-matching-ops`（コードネーム `MatchOps`）で**コードを書く際の実装規約**を定義する。「何をどう書くか」（命名・XML コメント・レイヤー依存・テスト・セキュリティ・PII/AI 境界・冪等性）を定める。

> 本ファイルは実装規約の末端ルールである。作業フロー（新規実装の進め方・セキュリティ監査・承認フロー）は `AGENTS.md` を参照。AI セッション開始時は `prompts/init.md` の手順で全ルールファイルを読み込むこと。

---

## 目次

1. [リポジトリの位置付け](#1-リポジトリの位置付け)
2. [Codex と Claude Code の役割分担](#2-codex-と-claude-code-の役割分担)
3. [アーキテクチャ方針](#3-アーキテクチャ方針)
4. [禁止事項 (Hard Rules)](#4-禁止事項-hard-rules)
5. [XML ドキュメントコメント規約](#5-xml-ドキュメントコメント規約)
6. [命名規則](#6-命名規則)
7. [コーディングスタイル](#7-コーディングスタイル)
8. [テスト規約](#8-テスト規約)
9. [セキュリティ・個人情報・AI 境界](#9-セキュリティ個人情報ai-境界)
10. [冪等性・監査・正確性](#10-冪等性監査正確性)
11. [フロントエンド (Next.js) 規約](#11-フロントエンド-nextjs-規約)
12. [作業の進め方 (Phase 0 → 1)](#12-作業の進め方-phase-0--1)
13. [困ったときの参照先](#13-困ったときの参照先)

---

## 1. リポジトリの位置付け

`store-matching-ops` は、店舗の空き枠と来店可能性が高い顧客をつなぐ AI マッチング基盤である。独立リポジトリ・独立プロダクトとして構築する **モジュラーモノリス + 非同期ワーカー** 構成（ADR-0002）。

詳細は以下を参照:

- `README.md` — 概要・起動手順
- `docs/architecture/design.md` — 設計方針（抽象モデル・モジュール・マッチングロジック・AI 境界・効果測定）
- `docs/architecture/adr/*.md` — 設計判断記録

## 2. Codex と Claude Code の役割分担

| ツール | 性格 | 担当 |
|---|---|---|
| **Codex** | 速度・量産が得意 | **実装担当**（定型実装・テスト・EF Core 設定・Controller・Migration・フォーマット） |
| **Claude Code** | 文脈保持・複雑判断が得意 | **設計・レビュー担当**（ADR・複雑な状態遷移/スコアリングの中核・Codex 出力レビュー・指示文作成） |

### 2.1 Codex に投げるタスク
- DTO / Command / Query クラス、単純な CRUD コントローラ
- `IEntityTypeConfiguration` の実装、Migration 生成
- ボイラープレート（例外クラス・空のサービス）、単純な単体テスト
- パッケージのインストール、フォーマット・整形

### 2.2 Claude Code が直接書くタスク
- ADR、README / 設計ドキュメント
- スコアリングの中核（`MatchScore` / `ScoringPolicy` の動的再正規化）
- `MatchingCampaign` の状態遷移コア（特に `proposed → approved → sent`）
- `IAiProposalService` / `INotificationSender` 等のインターフェース設計
- `Idempotency` / `Outbox` / テナントスコープ等の横断的関心事
- Codex 用の詳細指示文（`docs/codex-instructions/NNN-<topic>.md`）

### 2.3 Claude Code が必ず実施するタスク
- Codex 出力のレビュー（規約違反・設計整合・**PII/AI 境界・テナント分離**チェック）
- 設計判断が必要な領域の ADR 化
- 複雑仕様の Codex 用詳細化

## 3. アーキテクチャ方針

DDD ライト + クリーンアーキテクチャを、**モジュラーモノリス**で採用する。

### 3.1 レイヤー構成と依存方向

```
┌──────────────────────────────────────────────┐
│ Api / Worker（エントリポイント層）             │
│  - Controller / Middleware / Filter           │
│  - cron（Phase 0）→ Hangfire（Phase 1+）       │
└───────────────┬──────────────────────────────┘
                ▼ 依存
┌──────────────────────────────────────────────┐
│ Application（ユースケース層）                  │
│  - Service / Command / Query                  │
│  - インターフェース定義（IAiProposalService 等）│
│  - モジュール: Customers / Scheduling /        │
│    Catalog / Matching / Notifications /        │
│    Experiments / Analytics / Tenancy           │
└───────────────┬──────────────────────────────┘
                ▼ 依存
┌──────────────────────────────────────────────┐
│ Domain（ドメイン層）                           │
│  - Aggregate / Entity / ValueObject            │
│  - スコアリングルール・状態遷移・DomainEvent     │
│  - 外部依存なし                                │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Infrastructure（実装層）                       │
│  - EF Core(PostgreSQL) / Redis / AI(LLM) /     │
│    配信(LINE・メール) / 取込(CSV)               │
│  → Domain と Application を参照                 │
└──────────────────────────────────────────────┘
```

### 3.2 依存方向の絶対ルール
- `Domain` は他レイヤーに依存しない（BCL のみ）。
- `Application` は `Domain` のみ参照。
- `Infrastructure` は `Domain` と `Application` を参照。
- `Api` / `Worker` は `Application` と `Infrastructure` を参照（DI 登録のみ）。
- 逆方向参照を作らない。
- **モジュール間は Application のインターフェース経由**で連携し、別モジュールの Domain 内部に直接依存しない（将来の分離容易性のため）。

### 3.3 各レイヤーの責務
| レイヤー | 責務 | 例 |
|---|---|---|
| Domain | ビジネスルール・スコアリング・状態遷移 | `MatchScore`, `MatchingCampaign.Approve`, `TimeSlot.Hold` |
| Application | ユースケース調整・トランザクション境界 | `MatchingCampaignService.RunAsync` |
| Infrastructure | 永続化・LLM・配信・取込 | `MatchOpsDbContext`, `OpenAiProposalService` |
| Api | HTTP・認証・テナントスコープ・OpenAPI | `MatchingCampaignsController` |
| Worker | バックグラウンド処理 | `NightlyScoringJob`, `OutboxDispatchJob` |

---

## 4. 禁止事項 (Hard Rules)

PR レビューで機械的にリジェクトされる。

### 4.1 依存性関連
- `Domain` に `EntityFrameworkCore` / `Microsoft.AspNetCore.*` / `Hangfire` / `FluentValidation` を参照しない。
- `Domain` に `DataAnnotations` 属性を書かない。
- `Domain` に NuGet パッケージを追加しない（BCL のみ）。
- 別モジュールの `Domain` 型を跨いで直接参照しない（Application インターフェース経由）。

### 4.2 設計関連
- `Controller` にビジネスロジックを書かない（Application Service を呼ぶだけ）。`Controller` から `DbContext` を直接触らない。
- ビジネスロジックを `Extension Method` / `static` に逃がさない（Aggregate / Domain Service のメソッドにする）。
- `MediatR` / `AutoMapper` を独断で導入しない（必要時は ADR）。

### 4.3 AI / Human-in-the-loop 関連（本プロダクト固有・最重要）
- **承認なしに配信しない。** `MatchingCampaign` は `approved`（人手）を経ずに `sent` へ遷移できない実装にする（ADR-0004）。
- **LLM プロンプトに PII を渡さない。** 識別子・氏名・電話・メール・住所・カード情報をプロンプトに含めない。匿名化・集約済みデータのみ（ADR-0005）。
- **顧客ごとに LLM を呼ばない。** 生成は施策／セグメント単位。文面はテンプレ＋差し込み。
- 値引き提案は店舗設定の**上限を超えない**。通知**頻度上限・オプトアウト**を無視しない。
- Phase 0/1 で**機械学習を導入しない**（ルール＋重み付け。導入時は ADR 必須、ADR-0003）。
- 法的・医療的・差別的属性を判定に使わない。

### 4.4 マルチテナント関連
- `tenant_id` スコープなしで業務データを read/write しない（ADR-0006）。
- 他テナントのデータに到達しうるクエリ・API を作らない。

### 4.5 セキュリティ関連
- `appsettings.json` にシークレットを書かない（環境変数 / Secrets Manager / KMS）。
- PII（氏名・メール・電話・住所）をログ・例外・プロンプトに平文で出さない。
- `Authorization` ヘッダ・トークンをログに出さない。
- 外部 Webhook（POS/予約台帳等）を署名検証なしで受け付けない。

### 4.6 コミット・ブランチ関連
- ビルド/テストが通らない状態でコミットしない。
- マイグレーションを `down` で破壊しない（新規マイグレーションで上書き）。
- 直接 `main` への push 禁止（PR 経由）。

---

## 5. XML ドキュメントコメント規約

### 5.1 原則
**すべての public 型・メソッド・プロパティ・enum メンバ・パラメータ・例外に XML ドキュメントコメントを日本語で記述する。** IntelliSense と Swagger/OpenAPI に反映される。

### 5.2 ビルド設定（`Directory.Build.props` で強制）
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <WarningsAsErrors>$(WarningsAsErrors);CS1591</WarningsAsErrors>
  <NoWarn>$(NoWarn);CS1573</NoWarn>
</PropertyGroup>
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

### 5.3 ファイルヘッダーコメント（.cs 先頭・必須）
```csharp
// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaign.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策 (MatchingCampaign) Aggregate Root。
// 1つの空き枠群に対する候補抽出〜スコア〜提案〜承認〜配信の単位を表現する。
// 状態遷移 (draft / scored / proposed / approved / sent / measured)。
// proposed → approved は必ず人手 (Human-in-the-loop)。
// 関連 ADR: ADR-0004 (承認フロー), ADR-0003 (スコアリング)
// </summary>
// -----------------------------------------------------------------------------
```

### 5.4 省略可能なケース
private/internal メンバ・テストコード・自動生成（Migration）・`Program.cs` トップレベル文・record の自動実装メンバ（`Equals` 等）。それ以外は CS1591 でビルドが通らない。

---

## 6. 命名規則

### 6.1 名前空間ルート
`MatchOps`。レイヤー別に `MatchOps.Domain`, `MatchOps.Application`, `MatchOps.Infrastructure`, `MatchOps.Api`, `MatchOps.Worker`。
モジュールはさらに下位名前空間で表す（例: `MatchOps.Domain.Matching`, `MatchOps.Application.Scheduling`）。

### 6.2 命名規則表
| 種別 | 規約 | 例 |
|---|---|---|
| Aggregate / Entity | 単数名詞 | `Customer`, `TimeSlot`, `MatchingCampaign` |
| Value Object | 単数名詞 + 役割 | `MatchScore`, `ScoreBreakdown`, `Money`, `PhoneHash` |
| ID 型 | `<Aggregate>Id` 強い型（record struct） | `CustomerId`, `TimeSlotId`, `CampaignId` |
| Enum | 単数名詞 | `CampaignStatus`, `SlotStatus`, `ExperimentArm` |
| Domain Event | 過去形 | `CampaignApproved`, `NotificationSent`, `MatchScored` |
| Service (Application) | `<Aggregate>Service` | `MatchingCampaignService`, `CustomerService` |
| Command / Query | `<Verb><Object>Command/Query` | `RunCampaignCommand`, `ApproveCampaignCommand`, `ListSlotsQuery` |
| Controller | `<Resource>Controller` | `MatchingCampaignsController` |
| Configuration | `<Entity>Configuration` | `CustomerConfiguration` |
| Test Class | `<TestTarget>Tests` | `MatchScoreTests` |

### 6.3 ID 型（`Guid` を直接ばら撒かない）
```csharp
/// <summary>顧客の一意識別子。</summary>
public readonly record struct CustomerId(Guid Value)
{
    /// <summary>新しい顧客 ID を生成する。</summary>
    public static CustomerId New() => new(Guid.NewGuid());
    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
```

### 6.4 Value Object 方針
- 不変。`readonly record struct` または `record class`。
- 等価性は値で判定。Factory メソッドで生成（`MatchScore.From(...)`, `Money.Jpy(5000)`）。
- invariant が崩れうるコンストラクタは `private`。

### 6.5 テナント識別
すべての業務 Aggregate は `TenantId` を持つ。リポジトリ/クエリは `TenantId` を必須引数で受け、スコープを機械的に強制する。

---

## 7. コーディングスタイル

### 7.1 .editorconfig 準拠
インデント4スペース・UTF-8(BOM なし)・LF・末尾改行あり・`System` を最上段の using 並び。

### 7.2 C# スタイル
- `var` は型が明白なときのみ。File-scoped namespace（`namespace MatchOps.Domain.Matching;`）。
- nullable 参照型有効。async は `Async` サフィックス、`CancellationToken` を最後の引数に。
- 1 ファイル 1 トップレベル型。`record` の primary constructor を活用（C# 12）。

### 7.3 例外設計
- ドメイン例外: `DomainException`（`MatchOps.Domain.Common`）。
- ユースケース層は `Result<T>` で成功/失敗を表現（例外を制御フローに使わない）。
- 例外メッセージは日本語。シークレット・PII を含めない。

---

## 8. テスト規約

### 8.1 フレームワーク
xUnit / AwesomeAssertions / Testcontainers(PostgreSQL) / WireMock.Net(LLM・配信 HTTP モック) / `WebApplicationFactory`(API)。

### 8.2 テスト構造
```
tests/
├── MatchOps.Domain.Tests/         # Domain 単体（DB 不要）
├── MatchOps.Application.Tests/    # Application 単体（Mock）
├── MatchOps.Infrastructure.Tests/ # EF Core 統合（Testcontainers）
├── MatchOps.Api.Tests/            # HTTP（WebApplicationFactory）
└── MatchOps.IntegrationTests/     # E2E（Testcontainers + WireMock）
```

### 8.3 命名・要件
- メソッド名: `<対象>_<条件>_<期待結果>`（例: `Approve_WithoutProposal_Throws`, `Score_MissingFields_RenormalizesWeights`）。
- テストファースト（Domain/Application/Infrastructure）。1テスト1観点（AAA 分離）。
- 時刻は `IClock` 経由で固定。ランダム値禁止・`Thread.Sleep` 禁止。
- **本プロダクト固有の必須観点**:
  - 承認なしで `sent` に遷移しようとすると拒否される。
  - スコア欠損項目があるとき重みが再正規化される。
  - LLM 連携テストで**プロンプトに PII が含まれない**ことを検証する。
  - 異なる `TenantId` のデータが取得されない（テナント分離）。
  - 同一 `Idempotency-Key` の二重 `run/approve/send` で副作用が一度きり。

### 8.4 カバレッジ目標
| レイヤー | 目標 |
|---|---|
| Domain | 95%+ |
| Application | 90%+ |
| Infrastructure | 70%+ |
| Api | 80%+ |
| 全体 | 85%+ |

---

## 9. セキュリティ・個人情報・AI 境界

### 9.1 シークレット管理
| シークレット | 保管先 |
|---|---|
| LLM API キー（OpenAI/Azure） | Secrets Manager（本番）/ .env（開発） |
| LINE / メール送信の資格情報 | 同上 |
| DB 接続文字列 | Secrets Manager |
| 暗号鍵（PII 暗号化） | KMS（本番）/ .env（開発） |

`appsettings.json` にシークレットを書かない。

### 9.2 ログ
- Serilog + JSON 構造化ログ。PII（email/phone/住所/氏名）は `Destructure` で mask。
- トークン・`Authorization` ヘッダ・LLM プロンプト本文はログに出さない。

### 9.3 個人情報（PII）
- 連絡先は `phone_hash` / `email_hash`（ハッシュ化）または暗号化で保持。最小列。
- 顧客削除・匿名化要求に対応できる設計。

### 9.4 AI 境界（最重要・ADR-0005）
- LLM へ渡すのは**匿名化・集約済み**データのみ（件数・セグメント名・統計）。
- 個別顧客の識別子・氏名・連絡先・購買明細を渡さない。
- 値引き上限・通知頻度上限・オプトアウトをプロンプトの制約として明示し、出力もこれを超えないことを検証する。
- AI 連携は `Infrastructure/Ai` に隔離し、`Application` は `IAiProposalService` 越しに使う。

### 9.5 外部連携（POS/予約台帳/Webhook）
- Inbound Webhook は署名検証必須・タイムスタンプ検証（リプレイ防止）。失敗時 401。
- 連携は CSV 取込から開始し、API 連携は段階対応（design.md §11）。

---

## 10. 冪等性・監査・正確性

### 10.1 Idempotency-Key
- 施策の `run` / `approve` / `send` 等の POST/PATCH で `Idempotency-Key` ヘッダ必須。
- 24時間以内の同一キー＋同一リクエスト → 保存済みレスポンス。異なるボディ → 409。
- 実装: `IdempotencyFilter`（`MatchOps.Api/Filters`）。

### 10.2 監査ログ（`audit_logs`）
- AI 提案・承認・編集・配信・データ削除要求を append-only 記録。
- アプリロールから `UPDATE`/`DELETE` を REVOKE。

### 10.3 Outbox パターン
- DB 状態変更と配信は同一トランザクションで `outbox_messages` に積み、`OutboxDispatchJob`（Worker）が実送信。失敗は指数バックオフでリトライ。

### 10.4 Time Source
- 本番コードで `DateTime.UtcNow` / `DateTimeOffset.UtcNow` を直呼びしない。`IClock` 経由。
- Domain メソッドには `DateOnly today` / `DateTimeOffset now` をパラメータで渡す（Domain は `IClock` を知らない）。

---

## 11. フロントエンド (Next.js) 規約

管理画面は `apps/admin-web`（Next.js App Router + TypeScript）。

- API 通信は型付きクライアント（OpenAPI から生成 or 手書き型）。`any` 禁止。
- 施策の承認/編集/却下 UI は、サーバ側の状態遷移（§4.3）に従う。承認前の文面編集を許容する。
- PII はクライアントに不要なものを送らない（一覧はマスク表示）。
- 状態管理は最小限（過度なグローバル状態を作らない）。フォームはサーバ検証を信頼源にする。
- 詳細なフロント規約は Phase 1 着手時に `docs/frontend/` として追補する（本ファイルはバックエンド中心）。

---

## 12. 作業の進め方 (Phase 0 → 1)

Phase 0（リポジトリ骨格〜縦串デモ）から着手する。Stage と担当は以下（詳細は `docs/codex-instructions/`、ロードマップは `TASKS.md`）。

| Stage | 内容 | Claude Code | Codex |
|---|---|---|---|
| 0.1 | リポジトリ骨格・ADR・docker-compose | ADR 0001-0007、設計配置、レビュー | csproj、ディレクトリ、compose |
| 0.2 | Domain 共通部品（`MatchScore` / `Money` / `DomainException` / ID 型 / `IClock`） | スコア設計レビュー | 値オブジェクト・テスト |
| 0.3 | Customers / Activity | セグメント方針 | Aggregate・テスト |
| 0.4 | Scheduling（TimeSlot / Resource） | 状態遷移 ADR | Aggregate・テスト |
| 0.5 | Matching（候補抽出・v0 スコア・Campaign） | スコアリング中核・状態遷移 | 値オブジェクト・残テスト |
| 0.6 | EF Core Infrastructure（PostgreSQL） | DbContext 設計・Migration 戦略 | Configuration・Migration |
| 0.7 | Application UseCase（run/approve/send 骨格） | `IUnitOfWork`・承認境界 | Service・Command/Query |
| 0.8 | Ai（`IAiProposalService` + 要約/文面、PII 境界） | インターフェース・境界実装 | DTO・モック |
| 0.9 | Notifications（Outbox + CSV/手動配信） | Outbox 統合方針 | 定型実装 |
| 0.10 | Experiments（ホールドアウト割当・リフト） | 実験設計 | 割当・集計 |

### 12.1 作業記録
主要タスク・規約変更・構成変更・依存追加では `docs/work-records/YYYY/MM/YYYY-MM-DD-<topic>.md` に記録（背景・実施内容・設計判断・検証結果・次アクション）。シークレット・PII・未マスクトークンは書かない。恒久的な設計判断は ADR に分離する。

---

## 13. 困ったときの参照先

| 何を知りたいか | 参照先 |
|---|---|
| 実装規約・XML コメント・命名・PII/AI 境界 | 本ファイル |
| 作業フロー・セキュリティ監査・承認フロー | `AGENTS.md` |
| 設計方針・抽象モデル・モジュール・スコアリング | `docs/architecture/design.md` |
| 過去の設計判断 | `docs/architecture/adr/*.md` |
| タスクの順序・完了条件 | `TASKS.md` / `docs/codex-instructions/*.md` |
| 外部連携（LINE/POS/予約台帳） | `docs/integration/*.md` |
| 過去の作業経緯・検証結果 | `docs/work-records/` |
| API 仕様 | OpenAPI(`/swagger`) |

---

*このファイルは Codex / Claude Code が作業前に必ず読み込まれる前提で書かれている。改訂は人間判断（PR レビュー必須）で行うこと。*

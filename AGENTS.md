---
file: AGENTS.md
version: 0.1.0
last-updated: 2026-05-31
review-required: 2 humans
---

# AGENTS.md — AI エージェント行動規範

このファイルは、Codex / Claude Code を含む AI エージェントが本リポジトリ `store-matching-ops`（コードネーム `MatchOps`）で作業する際の **行動規範とワークフロー** を定義する。「新規実装の進め方」を定める。実装規約・アーキテクチャ・XML コメントの詳細は `CLAUDE.md`、設計方針は `docs/architecture/design.md` を参照。

> AI セッション開始時は `prompts/init.md` の手順で全ルールファイルを読み込むこと。

---

## 1. 作業開始前の必須手順

**コードの生成・修正を行う前に、以下を必ず実施する。**

1. `CLAUDE.md` を読み、コーディング規約・レイヤー依存・XML コメント・**PII/AI 境界・Human-in-the-loop・テナント分離**・役割分担を把握する。
2. 本ファイル（`AGENTS.md`）を読み、作業フロー（テストファースト・セキュリティ監査・承認フロー）を把握する。
3. `docs/architecture/design.md` で該当領域（抽象モデル・モジュール・スコアリング）の設計を把握する。
4. `docs/architecture/adr/` の関連 ADR を読む（特に ADR-0003〜0007）。
5. `docs/codex-instructions/` の該当タスクファイルを読み、仕様と完了条件を確認する。
6. `docs/work-records/` の関連作業記録を読み、過去の判断・未解決事項を確認する。
7. 既存コードの構造を確認する。

**上記を省略して実装に着手してはならない。**

---

## 2. テストファースト開発フロー（新規実装・厳守）

### 原則
**すべての Domain / Application / Infrastructure 層の新規実装は、テストを先に書いてから行う。** テストなしで本体コードをコミットしない。

例外: API Controller の薄い実装・DTO・Command・Query 等のボイラープレートはテストファースト対象外だが、Application Service 経由（`WebApplicationFactory`）で振る舞いをカバーする。

### 1 タスクの作業フロー
```
1. タスクファイルを読む（仕様・完了条件・関連 ADR）
2. テストを書く (Red)        ── 正常系・異常系・境界値・本プロダクト固有観点(§下)
3. インターフェースとモデルを定義（XML コメント付与）
4. 実装を書く (Green)        ── テストが通る最小限
5. リファクタリング (Refactor)
6. 静的解析の確認            ── dotnet build 警告ゼロ / CS1591 もエラー扱いで通過 / dotnet format
7. セキュリティ監査（該当時・§3）── パッケージ追加時 / PII・シークレット混入チェック
8. コミット（build + test + format 通過、§4 のメッセージ規約）
```

### テスト作成時のチェックリスト
- [ ] 正常系 / 異常系 / 境界値（空き枠の同時刻重複、来店履歴ゼロ、スコア項目全欠損 等）
- [ ] **承認フロー**: `proposed` を経ずに `sent` へ遷移しようとすると拒否される
- [ ] **スコアリング**: 欠損項目があると重みが再正規化される / 重み合計が正規化される
- [ ] **PII/AI 境界**: LLM へ渡すプロンプトに識別子・氏名・連絡先が含まれない
- [ ] **テナント分離**: 異なる `TenantId` のデータが取得・更新されない
- [ ] **冪等性**: 同一 `Idempotency-Key` の二重 `run/approve/send` で副作用が一度きり
- [ ] **配信制御**: 値引き上限・通知頻度上限・オプトアウトを超えない
- [ ] テストメソッド名が仕様を表現 / AAA 分離 / テスト間に依存なし
- [ ] `IClock` で時刻固定（`DateTime.UtcNow` 直呼びゼロ）
- [ ] DB 統合テストは Testcontainers PostgreSQL、LLM/配信は WireMock.Net でモック

### カバレッジ目標
Domain 95%+ / Application 90%+ / Infrastructure 70%+ / Api 80%+ / 全体 85%+。下回る PR はリジェクト。

---

## 3. セキュリティ・プライバシー監査

### 3.1 パッケージ追加・更新時（必須）
NuGet / npm を新規追加・更新する PR では監査を実施し、結果を PR 本文に記載する。

```bash
# .NET
dotnet list package --vulnerable --include-transitive
dotnet list package --deprecated
dotnet list package --outdated
# Node (apps/admin-web)
npm audit
```

`Directory.Build.props` で NuGet Audit を強制（NU1901-1904 をエラー扱い）。低レベル以上の脆弱性でビルドエラー。

### 3.2 シークレット混入チェック（毎コミット）
- [ ] `appsettings*.json` / フロントの `.env*` にシークレットが入っていない
- [ ] テスト・サンプルにハードコードされたシークレット・実 API キーがない
- [ ] `.env` をコミットしていない（`.gitignore` 確認）
- [ ] ログ・例外・コミットメッセージにシークレットが含まれない

```bash
git diff --cached | grep -iE 'api[_-]?key|secret|password|token|bearer' \
  | grep -v '_PLACEHOLDER\|PLEASE_SET_IN_ENV\|<value>\|example'
```

### 3.3 PII・AI 境界チェック（本プロダクト固有・毎コミット）
- [ ] ログ・例外・配信ペイロードに氏名・メール・電話・住所を平文出力していない
- [ ] **LLM へ渡すプロンプト構築箇所に、個別顧客の識別子・氏名・連絡先・購買明細が入っていない**（集約・匿名化のみ）
- [ ] 顧客一覧などの API レスポンスで、画面に不要な PII を返していない（マスク）
- [ ] 新規テーブル/列で、連絡先を平文保存していない（ハッシュ/暗号化）

`grep -rniE 'prompt|messages\s*=' src/MatchOps.Infrastructure/Ai` 等で AI 連携箇所を確認し、PII を渡していないか目視する。

---

## 4. コミットメッセージ規約

### 形式
```
type(scope): 日本語で簡潔に説明

# 例:
test(domain/matching): スコア欠損時の重み再正規化テストを追加
feat(domain/matching): MatchScore と ScoringPolicy(v0) を実装
feat(domain/scheduling): TimeSlot の Hold/Book 状態遷移を実装
test(domain/matching): 承認なし送信が拒否されるテストを追加
feat(domain/matching): MatchingCampaign.Approve / Send を実装
feat(infrastructure/persistence): EF Core Migration 初版を追加
feat(application/matching): RunCampaign / ApproveCampaign を実装
feat(api/campaigns): MatchingCampaignsController を実装
feat(infrastructure/ai): IAiProposalService の OpenAI 実装（PII 非送出）
feat(infrastructure/notifications): Outbox 配信ジョブを実装
fix(domain/matching): 通知疲れペナルティの符号を修正
docs(adr): ADR-0004 承認フロー仕様を追加
chore(deps): Hangfire を追加（セキュリティ監査済み）
```

### type 一覧
| type | 用途 |
|---|---|
| `test` | テストの追加・修正（テストファーストの最初のコミット） |
| `feat` | 機能追加 |
| `fix` | バグ修正 |
| `refactor` | 挙動を変えない構造改善 |
| `docs` | ドキュメント・ADR・XML コメント |
| `chore` | 依存・解析・CI・設定 |

scope はモジュール/レイヤー（`domain/matching`, `application/scheduling`, `api/campaigns`, `infrastructure/ai` 等）。

---

## 5. XML ドキュメントコメント（要点）

- すべての public 型・メソッド・プロパティ・enum メンバに日本語 XML コメント。
- すべての `.cs` 先頭にファイルヘッダーコメント（`CLAUDE.md` §5.3）。
- 省略可: private/internal・テスト・自動生成・`Program.cs`・record 自動実装メンバ。
- 実装後、Hover / `/swagger` で表示されることを確認する。

---

## 6. タスクの進め方

### 順序
`docs/codex-instructions/` のタスクは番号順に実施する。前タスクの完了条件を満たしてから次へ。

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

各タスクファイルには 前提 / 対象ディレクトリ / 作成物 / 仕様 / テスト要件 / 制約 / 完了条件 を必ず含める。

### タスクごとの完了条件チェック
```bash
dotnet build --no-restore                 # 警告ゼロ（CS1591 エラー扱い通過）
dotnet build --no-restore -warnaserror     # 静的解析もエラー扱いで通過
dotnet test  --no-build                    # 全テスト Green
dotnet format --verify-no-changes          # 書式統一
dotnet list package --vulnerable --include-transitive   # 脆弱性ゼロ
# タスクファイルの完了条件を満たす / 必要なら work-records を更新
```

### 作業記録
主要タスク・規約変更・構成変更・依存追加では `docs/work-records/YYYY/MM/YYYY-MM-DD-<topic>.md` に記録。判断理由・検証結果を補足し、恒久的判断は ADR に分離。

---

## 7. コード生成時のルール

### 必ず守ること
- `CLAUDE.md` の規約に従う（命名・レイヤー依存・XML コメント・PII/AI 境界・テナント分離）。
- 既存コードのスタイルに合わせる（新パターンを勝手に導入しない）。
- 新クラスは DI 登録を忘れない。Application Service は必ずインターフェースを定義して注入する。
- POST/PATCH の `run/approve/send` は `IdempotencyFilter` を経由する。
- 配信は Outbox 経由。`MatchingCampaign` は承認後にのみ送信可能にする。
- LLM へは匿名化・集約データのみ渡す。

### 避けること
- 既存挙動を壊す変更（リグレッション）。タスク外の機能追加（スコープ外）。
- NuGet/npm の無断追加（§3 の監査と PR 確認）。
- 過度な抽象化（MediatR/AutoMapper の独断導入）。XML コメント・テストの省略。
- `appsettings.json` へのシークレット書き込み。ログ/プロンプトへの PII 出力。
- Domain への EF Core / ASP.NET Core / FluentValidation 参照追加。
- **承認を経ない自動配信。Phase 0/1 での機械学習導入。**

---

## 8. 判断に迷った場合

優先順:
1. タスクファイルの記載
2. `docs/architecture/design.md`
3. `docs/architecture/adr/`
4. `CLAUDE.md` の規約
5. 本ファイルの規約
6. 既存コードの慣例
7. Microsoft .NET コーディング規約
8. 一般的な C# / DDD のベストプラクティス

判断がつかない場合は実装を進めずユーザーに確認を求める。特に以下は必ず人間判断を仰ぐ:

- 規約から外れた実装が必要と判断したとき
- 新規パッケージ追加が必要なとき
- API/DB スキーマに破壊的変更が必要なとき
- **承認フロー・PII/AI 境界・テナント分離に関わる設計を変えたいとき**
- 機械学習の導入が妥当と判断したとき（ADR 必須）
- ルールファイル（`AGENTS.md`/`CLAUDE.md`/ADR）自体の改訂が必要なとき

---

## 9. エラー・問題発生時

| 状況 | 対応 |
|---|---|
| ビルドエラーが解消できない | 内容を報告し対処案を提示 |
| テストが Red のまま Green にできない | 仕様解釈の確認を求める |
| 静的解析の警告が解消困難 | 理由を説明し抑制可否を確認（個別 `#pragma` は ADR 必須） |
| 仕様の矛盾に気づいた | 実装を止めて指摘し確認を求める |
| 脆弱性のあるパッケージが必要 | 代替検討。不可なら ADR で人間判断 |
| LLM へ PII を渡さないと実現困難に見える | 実装を止めて確認（設計の見直しが必要な可能性） |
| 役割境界が不明 | `CLAUDE.md` §2 を再確認。なお不明なら確認を求める |

---

## 10. Codex と Claude Code の使い分け

| ツール | 担当 | 典型タスク |
|---|---|---|
| **Codex** | 実装担当 | Aggregate/Service の量産、テスト生成、EF Core Configuration、Controller、Migration |
| **Claude Code** | 設計・レビュー担当 | ADR、スコアリング/状態遷移の中核、PII/AI 境界・テナント分離レビュー、指示文作成 |

役割が曖昧なタスク、特に **承認フロー・PII/AI 境界・テナント分離・スコアリング中核** は Claude Code が判断する。

---

*このファイルは Codex / Claude Code が作業前に必ず読み込まれる前提で書かれている。改訂は人間判断（PR レビュー必須）で行うこと。*

---
file: TASKS.md
version: 0.1.0
last-updated: 2026-05-31
---

# TASKS.md — ロードマップとタスク一覧

タスクの入口。フェーズ／Stage と、各 Stage に対応する詳細タスク（`docs/codex-instructions/NNN-*.md`）への索引を示す。
作業フローは `AGENTS.md`、実装規約は `CLAUDE.md`、設計方針は `docs/architecture/design.md`。

---

## 1. フェーズ・ロードマップ

| フェーズ | 期間目安 | ゴール |
|---|---|---|
| **Phase 0** | 1〜2週 | 縦串デモ：サンプルCSV取込 → 候補抽出 → v0スコア → AI要約 → Metabase 可視化 |
| **Phase 1** | 1〜2か月 | MVP：管理画面・各種管理・空き枠検出・スコア・AI提案・承認・配信・効果測定 |
| **Phase 2** | 2〜3か月 | 実店舗 PoC（1〜3店舗）・実データ連携・リフト検証・ルール改善 |
| **Phase 3** | 継続 | 汎用基盤化（複数業種・本部管理・複数店舗送客・POS/予約台帳連携・課金連携） |

初期 PoC は美容院または飲食店の一方に絞る（設計は汎用のまま）。

---

## 2. Phase 0 の Stage と担当

| Stage | 内容 | タスクファイル | Claude Code | Codex |
|---|---|---|---|---|
| 0.1 | リポジトリ骨格・ソリューション・compose | `000_initial_skeleton.md` | ADR・設計配置・レビュー | csproj/ディレクトリ/compose |
| 0.2 | Domain 共通部品 | `001_domain_common.md` | `MatchScore`/`IClock` 設計 | `Money`/ID 型/`DomainException`/テスト |
| 0.3 | Customers / Activity | `002_customer_and_activity.md` | セグメント方針 | Aggregate・テスト |
| 0.4 | Scheduling（TimeSlot/Resource） | `003_timeslot_and_resource.md` | 状態遷移 ADR | Aggregate・テスト |
| 0.5 | Catalog（Offer：メニュー/クーポン） | `004_offer_and_catalog.md` | 値引き上限方針 | Aggregate・テスト |
| 0.6 | Matching（候補抽出・v0 スコア・Campaign） | `005_matching_engine_scoring.md` | スコア中核・状態遷移 | 値オブジェクト・残テスト |
| 0.7 | EF Core Infrastructure（PostgreSQL） | `006_ef_core_infrastructure.md` | DbContext/Migration 戦略 | Configuration・Migration |
| 0.8 | Application UseCase（run/approve/send 骨格） | `007_application_usecase.md` | `IUnitOfWork`/承認境界 | Service・Command/Query |
| 0.9 | API（管理画面向け） | `008_api_admin.md` | Idempotency/テナントスコープ | Controller・DTO |
| 0.10 | Ai（要約・文面、PII 境界） | `009_ai_agent_service.md` | `IAiProposalService`・境界 | DTO・モック |
| 0.11 | Notifications（Outbox + CSV/手動配信） | `010_notification_outbox.md` | Outbox 統合 | 定型実装 |
| 0.12 | Experiments（ホールドアウト・リフト） | `011_experiment_lift.md` | 実験設計 | 割当・集計 |

> Phase 0 の到達点は「1業種・1店舗・サンプルデータで縦串が通り、AI 要約と Metabase での可視化までできる」こと。配信は CSV 出力 or 手動で可。自動配信（LINE/メール API）は Phase 1。

---

## 3. Phase 1 で着手する主なタスク（概要・詳細は別途指示文化）

- 管理画面（Next.js）：ダッシュボード／空き枠一覧／顧客一覧／施策／AI提案／配信履歴／効果測定
- 自動配信連携（LINE Messaging API / メール）と Outbox 実送信
- セグメント抽出の本実装（休眠・常連・新規・平日利用者 等）
- スコアの段階的拡張（メニュー相性・クーポン反応・客単価期待・通知疲れ）
- 効果測定ダッシュボード（リフト・増分ROI・信頼区間）
- 認証・認可（本部管理者／店舗管理者／閲覧者）とテナント管理
- 監査ログ画面、オプトアウト・頻度制御の運用 UI

---

## 4. 横断タスク（フェーズ非依存・随時）

- ADR の追加・更新（設計判断が生じるたび）
- セキュリティ／プライバシー監査（パッケージ追加・PII 経路変更時、`AGENTS.md` §3）
- 作業記録（`docs/work-records/`）の更新
- 外部連携指示文（`docs/integration/`：LINE / POS / 予約台帳）の整備

---

## 5. タスク完了の共通条件（Definition of Done）

各タスクは、タスクファイル固有の完了条件に加え、以下を満たすこと。

```bash
dotnet build --no-restore -warnaserror   # 警告ゼロ（CS1591 含む）
dotnet test  --no-build                  # 全テスト Green、カバレッジ目標達成
dotnet format --verify-no-changes        # 書式統一
dotnet list package --vulnerable --include-transitive   # 脆弱性ゼロ
```

加えて、本プロダクト固有の受け入れ観点（`AGENTS.md` §2 チェックリスト）:

- 承認なし送信が拒否される
- スコア欠損時に重みが再正規化される
- LLM プロンプトに PII が含まれない
- 異なるテナントのデータに到達しない
- 同一 Idempotency-Key の二重操作で副作用が一度きり
- 値引き上限・通知頻度上限・オプトアウトを超えない

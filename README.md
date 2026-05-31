# store-matching-ops

**Store Matching Ops Platform / 店舗マッチング・オペレーション基盤**

店舗の空き時間・空席・スタッフ枠と、来店可能性が高い顧客をつなぐ AI マッチング基盤。
飲食店・美容院・小売／サービス店舗向けに、「誰に・いつ・何を提案するか」を判断し、現場の承認を挟んで配信・効果測定までを一体で回す。

> リポジトリ名 `store-matching-ops` / .NET ソリューション・名前空間ルート `MatchOps`（コードネーム）。
> 名前は確定ではない。変更する場合は ADR-0001 を更新し、`docs/` 内の参照を一括置換すること。

---

## 1. このリポジトリは何か

- 独立リポジトリ・独立プロダクトとして構築する **モジュラーモノリス + 非同期ワーカー** 構成。
- バックエンドは .NET 10 / ASP.NET Core、管理画面は Next.js、データストアは PostgreSQL 18。
- マッチング・AI・配信は、将来サービス分離しやすい **モジュール境界** として設計する（初期は分離しない）。
- AI は意思決定主体にしない。**AI提案 → 管理者が確認 → 承認 → 配信** の Human-in-the-loop を必須とする。

詳細な背景・狙いは社内企画書「店舗マッチング・オペレーション基盤 企画・設計書」および本リポジトリの `docs/architecture/design.md` を参照。

## 2. 技術スタック

| 領域 | 採用 | 備考 |
|---|---|---|
| バックエンド | .NET 10 / ASP.NET Core | LTS（2028年11月までサポート）・型安全・長期保守 |
| 管理画面（フロント） | Next.js + TypeScript | 施策確認・ダッシュボード（`apps/admin-web`） |
| データベース | PostgreSQL 18 | JSONB・分析・時系列への拡張性 |
| キャッシュ / キュー | Redis + Worker | ジョブキュー・バッチ・配信の非同期化 |
| バッチ / ジョブ | Hangfire（Phase 1+）/ cron（Phase 0） | スコアリング・配信・取込 |
| AI 連携 | OpenAI / Azure OpenAI | 施策要約・文面生成・分析コメント（Infrastructure 層に隔離） |
| 分析 | Metabase | KPI 可視化・効果測定 |
| 認証 | ASP.NET Identity / Keycloak（検討） | テナント・権限管理 |
| インフラ | Docker + Cloud Run / ECS / Azure Container Apps | 小さく始めてスケール |

技術選定の根拠は ADR-0002（モジュラーモノリス採用）ほかを参照。

## 3. ディレクトリ構成

```
store-matching-ops/
├── README.md                     # 本ファイル
├── CLAUDE.md                     # 実装規約・コーディング指針（AI/人間共通）
├── AGENTS.md                     # AI エージェント行動規範・作業フロー
├── TASKS.md                      # ロードマップとタスク一覧（タスクの入口）
├── Directory.Build.props         # 全 .NET プロジェクト共通のビルド設定
├── .editorconfig / .gitignore
├── prompts/
│   └── init.md                   # AI セッション開始時のルール読込手順
├── docs/
│   ├── architecture/
│   │   ├── design.md             # 設計方針（中核ドキュメント）
│   │   └── adr/                  # Architecture Decision Records
│   ├── codex-instructions/       # 実装タスクの詳細指示（番号順）
│   ├── integration/              # 外部連携（LINE / POS / 予約台帳）指示
│   └── work-records/             # 作業記録（YYYY/MM/...）
├── src/                          # .NET 実装（Phase 0 で骨格作成）
│   ├── MatchOps.Domain/          # ドメイン層（外部依存なし）
│   ├── MatchOps.Application/     # ユースケース層
│   ├── MatchOps.Infrastructure/  # 永続化・AI・配信・外部連携
│   ├── MatchOps.Api/             # 管理画面向け API
│   └── MatchOps.Worker/          # 非同期処理（取込・スコア・配信・分析）
├── tests/                        # テストプロジェクト群
├── apps/
│   └── admin-web/                # Next.js 管理画面
└── infra/
    └── docker-compose.yml        # PostgreSQL / Redis / Metabase（ローカル）
```

`src/` 配下の .NET プロジェクトは Phase 0（リポジトリ骨格）で作成する。現時点ではプレースホルダ。

## 4. ローカル起動（予定）

```bash
# 1. ミドルウェア（PostgreSQL 18 / Redis / Metabase）を起動
docker compose -f infra/docker-compose.yml up -d

# 2. バックエンド API（Phase 0 で骨格作成後）
dotnet run --project src/MatchOps.Api

# 3. 管理画面（Phase 1 で着手）
cd apps/admin-web && npm install && npm run dev
```

`appsettings.json` にシークレットを書かない。接続文字列・API キーは環境変数（開発は `.env`、本番は Secrets Manager / KMS）経由で渡す（CLAUDE.md §9 参照）。

## 5. 開発の進め方

- 作業フロー（テストファースト・セキュリティ監査・承認フロー）は `AGENTS.md`。
- 実装規約（命名・レイヤー依存・XML コメント・PII/AI 境界）は `CLAUDE.md`。
- 設計方針（抽象モデル・モジュール・マッチングロジック）は `docs/architecture/design.md`。
- タスクの順序と完了条件は `TASKS.md` と `docs/codex-instructions/`。
- AI セッション開始時は `prompts/init.md` の手順で全ルールファイルを読み込む。

## 6. 重要な原則（要約）

1. **Human-in-the-loop**：AI が自動配信しない。提案は必ず管理者承認を経て配信する。
2. **PII を LLM に渡さない**：プロンプトには匿名化・集約済みデータのみ。識別子・氏名・連絡先を直接渡さない。
3. **テナント分離**：すべてのデータアクセスは `TenantId` でスコープする。
4. **ルールベースから開始**：Phase 0/1 は機械学習を使わず、説明可能なルール＋重み付けスコア。
5. **配信制御**：頻度上限・オプトアウト・値引き上限を必ず尊重する。
6. **効果は増分で測る**：ホールドアウト（対照群）でリフトを測定する（ADR-0007）。

詳細は `CLAUDE.md` §4（禁止事項）と `docs/architecture/design.md` を参照。

# 2026-05-31 リポジトリ初期化

## 背景
店舗マッチング・オペレーション基盤の開発に向け、リポジトリ骨格・規約・設計方針・タスクを整備する。
既存社内基盤 `chargehub` の CLAUDE.md / AGENTS.md を参考に、本プロダクト向けへ調整した。

## 実施内容
- リポジトリ名 `store-matching-ops` / 名前空間ルート `MatchOps` を決定（ADR-0001）。
- 設計方針 `docs/architecture/design.md` を作成（抽象モデル・モジュラーモノリス・スコアリング・AI 境界・効果測定）。
- `CLAUDE.md`（実装規約）/ `AGENTS.md`（行動規範）を本プロダクト向けに作成。
- ADR 0001〜0007 を起票（命名・モジュラーモノリス・ルールベース・承認フロー・PII/AI 境界・テナント分離・リフト測定）。
- `TASKS.md` と `docs/codex-instructions/000〜011` を作成。
- ビルド設定（Directory.Build.props）・.editorconfig・.gitignore・docker-compose・prompts/init.md を配置。

## 設計判断
- 完全自動化せず Human-in-the-loop（ADR-0004）。
- PII を LLM に渡さない（ADR-0005）。
- Phase 0/1 はルールベース・スコア v0=3要素（ADR-0003）。
- マルチテナントを最初から（ADR-0006）。
- 効果は増分（リフト）で測る（ADR-0007）。

## 検証結果
- ドキュメントのみ（実装コードなし）。`src/` の .NET ソリューションは Stage 0.1（000）で作成予定。

## 未解決事項・次アクション
- リポジトリ名/コードネームの最終確定（暫定: store-matching-ops / MatchOps）。
- 初期 PoC 対象業種（美容院 or 飲食店）の決定。
- ~~Stage 0.1（000_initial_skeleton）に着手。~~ → 完了（下記参照）。
- 次は Stage 0.2（001_domain_common）: `MatchScore` / `Money` / ID 型 / `IClock` / `DomainException`。

---

# 2026-05-31 Stage 0.1 リポジトリ骨格（追記）

## 背景
`000_initial_skeleton.md` に従い、.NET 10 ソリューションとレイヤー別プロジェクト・テスト雛形を作成する。

## 実施内容
- ソリューション `MatchOps.sln`（従来 .sln 形式）を作成し、10 プロジェクトを登録。
  - src: `MatchOps.Domain` / `.Application` / `.Infrastructure`（classlib）, `.Api`（Web）, `.Worker`（Worker Service）。
  - tests: `*.Domain.Tests` / `.Application.Tests` / `.Infrastructure.Tests` / `.Api.Tests` / `MatchOps.IntegrationTests`（各スモークテスト1件）。
- プロジェクト参照を依存方向（CLAUDE.md §3.2）どおりに設定（逆参照なしを確認）。
  - Application→Domain / Infrastructure→Domain+Application / Api・Worker→Application+Infrastructure。
- `MatchOps.Api/Program.cs` をヘルスエンドポイント（`GET /health`）のみに簡素化。テンプレートの WeatherForecast を削除。統合テスト用に `public partial class Program` を公開。
- `MatchOps.Worker` はテンプレートの `Worker.cs`（`DateTimeOffset.Now` 直呼びが §10.4 違反・public 型）を削除し、`Program.cs` を最小ホストに（ホステッドサービスは後続 Stage で追加）。
- Domain/Application 各層にモジュール用空フォルダ（Customers/Scheduling/Catalog/Matching/Notifications/Experiments/Tenancy）を `.gitkeep` で用意。
- テンプレート生成の `Class1.cs`×3 を削除（骨格にロジックを持たせない）。

## 設計判断
- **SDK ピン留め**: `global.json` で `10.0.204`（`rollForward: latestPatch`）に固定。安定 band(2xx) に留め、併存する preview band(3xx, 10.0.300) のアナライザ差異を避け再現性を確保。
- **ソリューション形式**: .NET 10 既定の `.slnx` ではなく、指示文記載の従来 `.sln` を採用（`dotnet new sln --format sln`）。
- **`Directory.Build.props` のテスト判定修正**: `EndsWith('.Tests')` → `EndsWith('Tests')`。`MatchOps.IntegrationTests` が `.Tests` で終わらず CS1591 緩和が効かなかったため。§5.4「テストコードは XML コメント省略可」の意図を正しく実装する修正（業務プロジェクトはいずれも `Tests` 終端でないため誤適用なし）。
- モジュール用フォルダは DDD モジュールが宿る Domain/Application のみに作成（Infrastructure は Persistence/Ai 等の別軸構成のため対象外）。

## 検証結果
- `dotnet build MatchOps.sln -warnaserror` → 警告0・エラー0。
- `dotnet test MatchOps.sln --no-build` → 5/5 プロジェクト Green（各1件）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable --include-transitive` → 全プロジェクトで脆弱性なし。
- `docker compose -f infra/docker-compose.yml config` → postgres:18 / redis:7 / metabase が正しく解決（構文妥当）。実コンテナの `up`（イメージ取得）は本記録では未実施。
- 依存方向: Domain は参照ゼロ、逆参照なしを csproj で確認。

## 未解決事項・次アクション
- `docker compose up -d` による実起動確認（イメージ取得を伴うためローカルで実施）。
- Stage 0.2（`001_domain_common`）に着手。

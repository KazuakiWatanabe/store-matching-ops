# 000 — リポジトリ骨格 (Stage 0.1)

## 前提
- `README.md`, `docs/architecture/design.md`, `CLAUDE.md` §3（アーキテクチャ）
- ADR-0001（リポジトリ名・名前空間）, ADR-0002（モジュラーモノリス）

## 対象ディレクトリ
- リポジトリ直下, `src/`, `tests/`, `infra/`

## 作成物
- .NET ソリューション `MatchOps.sln`
- プロジェクト（クラスライブラリ / Web / Worker）:
  - `src/MatchOps.Domain`（classlib, netstandard 不要・net10.0）
  - `src/MatchOps.Application`（classlib）→ Domain 参照
  - `src/MatchOps.Infrastructure`（classlib）→ Domain + Application 参照
  - `src/MatchOps.Api`（ASP.NET Core Web API）→ Application + Infrastructure 参照
  - `src/MatchOps.Worker`（Worker Service）→ Application + Infrastructure 参照
- テストプロジェクト雛形（§テスト構造, `CLAUDE.md` §8.2）
- `Directory.Build.props`（XML ドキュメント生成・CS1591 エラー化・NuGet Audit・nullable・LangVersion）
- `.editorconfig`, `.gitignore`
- `infra/docker-compose.yml`（PostgreSQL 18 / Redis / Metabase）

## 仕様
- 全プロジェクト `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors`（最低 CS1591 / NU190x）。
- プロジェクト参照は `CLAUDE.md` §3.2 の依存方向に厳密に従う（逆参照を作らない）。
- `MatchOps.Api` は最小の `Program.cs`（health endpoint のみ）でビルド可能にする。
- フォルダ構成は将来のモジュール分割を見据え、各レイヤー内にモジュール用フォルダ（`Customers/`, `Scheduling/`, `Catalog/`, `Matching/`, `Notifications/`, `Experiments/`, `Tenancy/`）を空で用意してよい。

## テスト要件
- 各テストプロジェクトが空でもビルド・実行できる（ダミーテスト 1 件可）。
- `dotnet build` が警告ゼロで通る。

## 制約
- Domain プロジェクトに NuGet を一切追加しない。
- この Stage ではビジネスロジックを書かない（骨格のみ）。
- シークレットを compose / appsettings に直書きしない（`.env` 参照）。

## 完了条件
- `dotnet build -warnaserror` が通る。
- `docker compose -f infra/docker-compose.yml up -d` で PostgreSQL/Redis/Metabase が起動する。
- 依存方向が設計どおり（逆参照なし）であることをレビューで確認。
- `docs/work-records/2026/05/2026-05-31-initial-repo.md` を更新。

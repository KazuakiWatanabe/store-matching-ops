# 008 — API（管理画面向け） (Stage 0.9)

## 前提
- `CLAUDE.md` §4.2（Controller 規約）, §10.1（Idempotency）, §4.4（テナント）

## 対象ディレクトリ
- `src/MatchOps.Api`, `tests/MatchOps.Api.Tests`

## 作成物
- `MatchingCampaignsController`（`POST run` / `POST approve` / `POST send` / `GET results`）
- `IdempotencyFilter`、テナントスコープ middleware、認証スタブ
- DTO（リクエスト/レスポンス）、OpenAPI 注釈（XML コメント由来）

## 仕様
- run/approve/send は `Idempotency-Key` 必須。
- レスポンスに不要な PII を含めない（候補は件数・セグメント・スコアのみ。連絡先を返さない）。
- テナント外リソースへのアクセスは 404/403。

## テスト要件（WebApplicationFactory）
- Idempotency-Key 欠如で 400。二重リクエストで副作用 1 回。
- 未承認 send で 409/422。
- 他テナント campaign 取得で 404。

## 制約
- Controller にビジネスロジックを書かない。DbContext を直接触らない。

## 完了条件
- build/test 通過。冪等・承認・テナントの API テストが緑。`/swagger` に日本語説明が出る。

# 007 — Application UseCase: run / approve / send 骨格 (Stage 0.8)

## 前提
- `CLAUDE.md` §3, §4.3（承認フロー）, §10（冪等性/Outbox）
- ADR-0004（Human-in-the-loop）

## 対象ディレクトリ
- `src/MatchOps.Application/Matching`, `tests/MatchOps.Application.Tests`

## 作成物
- `IUnitOfWork`, `IClock`（Application/Common）
- `MatchingCampaignService`（`RunAsync` 候補抽出+スコア, `ProposeAsync` AI 提案生成依頼, `ApproveAsync` 人手承認, `SendAsync` 配信積み）
- Command/Query（`RunCampaignCommand` 等）、`Result<T>`

## 仕様
- `SendAsync` は `approved` 状態でのみ成功する（未承認は `Result.Failure`）。
- 配信は Outbox に積むだけ（実送信は Worker）。
- `IAiProposalService` 越しに提案生成（実装は 009）。Application は PII を集約してから渡す。

## テスト要件（Mock 利用）
- 未承認 `SendAsync` が失敗する。
- `ApproveAsync` 後に `SendAsync` が成功し Outbox に 1 件積まれる。
- AI 呼び出しに渡す引数に PII が含まれない（モックで引数検証）。

## 制約
- Controller を作らない（008）。実 LLM/配信を呼ばない（モック）。

## 完了条件
- build/test 通過。承認境界・PII 引数のテストが緑。

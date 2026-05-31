# 009 — Ai: 施策要約・文面生成（PII 境界） (Stage 0.10)

> PII/AI 境界の設計は Claude Code が主担当（`CLAUDE.md` §2.2, §9.4）。

## 前提
- `docs/architecture/design.md` §6.3（AI の役割と境界）
- ADR-0005（PII/AI 境界）

## 対象ディレクトリ
- `src/MatchOps.Application/Ai`（インターフェース・入出力 DTO）
- `src/MatchOps.Infrastructure/Ai`（LLM 実装）
- tests 各所

## 作成物
- `IAiProposalService`（Application）
  - `SummarizeCampaignAsync(AiCampaignContext, ct)` — 施策の要約
  - `GenerateMessageAsync(AiMessageContext, ct)` — 配信文面（テンプレ＋差し込み用）
  - `CommentResultsAsync(AiResultContext, ct)` — 結果コメント
- 入力 DTO（**匿名化・集約済み**フィールドのみ）
  - 例: 店舗業種, 空き枠サマリ（日時・対応メニュー種別）, 候補サマリ（件数, セグメント名配列, 主要理由配列）, 許容 Offer（種別・値引き上限）, トーン
- 出力 DTO（要約テキスト, 文面テンプレ, 注意フラグ）
- `OpenAiProposalService`（Infrastructure）— OpenAI / Azure OpenAI 実装。プロンプト構築をここに隔離。

## 仕様
- **プロンプトに PII を一切含めない。** 識別子・氏名・電話・メール・住所・購買明細を渡さない。件数・セグメント名・統計のみ。
- 生成は**施策／セグメント単位**。顧客ごとに呼ばない。
- 値引き上限・通知頻度・トーン制約をプロンプトに明示し、出力がこれを逸脱しないことを後段で検証。
- モデル・エンドポイント・キーは設定/Secrets 経由。`appsettings.json` に書かない。
- 失敗時はリトライ/フォールバック（要約なしでも施策フローが止まらない設計）。

## テスト要件
- **プロンプト非 PII 検証**：`OpenAiProposalService` が組み立てるリクエストに PII が含まれないことを WireMock.Net で捕捉・アサート。
- 入力 DTO に連絡先/識別子フィールドが存在しない（型レベルで保証）。
- 値引き上限を超える文面が生成された場合に検証で弾く（後処理 or 再生成）。
- LLM 障害時に `Result.Failure` ではなく「要約なし」で施策が継続できる（運用継続性）。

## 制約
- Application は LLM SDK に直接依存しない（`IAiProposalService` 越し）。
- 顧客単位ループでの生成を実装しない。

## 完了条件
- build/test 通過。PII 非送出テスト（WireMock 捕捉）が緑。
- プロンプト構築箇所が `Infrastructure/Ai` に隔離され、PII 経路がないことをレビューで確認。

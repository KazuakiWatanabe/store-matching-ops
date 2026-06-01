# 2026-06-01 Stage 0.10 Ai（施策要約・文面生成・PII 境界）

## 背景
`009_ai_agent_service.md` / ADR-0005（PII/AI 境界）。施策要約・配信文面・結果コメントを生成する AI 連携を、PII を一切渡さない境界で実装する。プロンプト構築は Infrastructure に隔離し、Application は `IAiProposalService` 越しに使う。

## 前提・分岐の判断（ユーザー確認済み）
- **ブランチ基点**: PR #9（Stage 0.9）は未マージ。Ai モジュールは API 層と独立で Matching シームも非破壊のため、**main（Stage 0.8）から分岐**（0.9 と並行マージ可能）。
- **`IAiProposalService` の整理**: Stage 0.8 で `Application/Matching/IAiProposalService`（`GenerateProposalAsync`）を作成済みで `MatchingCampaignService`（merged）が依存。009 は `Application/Ai` に 3 メソッド版を指定。**「Ai モジュール新設・Matching は委譲」を採用**：Ai に本格 IF を新設し、既存 Matching シームはアダプタ経由で Ai へ委譲（既存シグネチャ非破壊）。

## 実施内容
- **Application/Ai（新モジュール）**
  - `IAiProposalService`（3 メソッド）: `SummarizeCampaignAsync` / `GenerateMessageAsync` / `CommentResultsAsync`。
  - 入力 DTO（**匿名化・集約済み**のみ）: `AiCampaignContext` / `AiMessageContext` / `AiResultContext` / `AllowedOffer`（種別＋最大値引き率）/ `AiTone`。識別子・連絡先・購買明細を型として持たない。
  - 出力 DTO: `AiCampaignSummary` / `AiMessageDraft` / `AiResultComment`（テキスト＋注意フラグ `Cautions`）。
- **Application/Matching（委譲アダプタ）**
  - `AiProposalServiceAdapter`：`Matching.IAiProposalService.GenerateProposalAsync` を `Ai.IAiProposalService.GenerateMessageAsync` へ委譲。集約入力（`AiProposalRequest`, PII なし）を `AiMessageContext` へ写像。LLM 呼び出しは施策単位 1 回、提案理由は集約値から決定的に構築（追加 LLM 呼び出しなし）。
- **Infrastructure/Ai**
  - `OpenAiOptions`（Endpoint/Path/Model/ApiKey/Timeout。API キーは Secrets/環境変数、appsettings に書かない）。
  - `OpenAiProposalService`：OpenAI 互換 Chat Completions を `HttpClient` で呼ぶ。プロンプト構築をここに隔離し集約情報のみ載せる。値引き上限超過（正規表現で `n%` 抽出し上限比較）は安全側の既定文面へ置換し注意フラグ。LLM 障害（非 2xx・例外）は投げずフォールバック出力（注意フラグ）を返し施策フローを継続。API キー・プロンプト本文をログに出さない。
- **テスト**
  - Infrastructure.Tests（WireMock.Net 追加）: 成功時の内容反映、**プロンプト非 PII 検証**（送信 JSON を復号しセグメント名が含まれ・PII サンプルが含まれないこと）、値引き上限超過の置換、上限内の保持、LLM 障害時のフォールバック（要約=空＋注意/文面=既定＋注意）。型レベル保証（`AiContextPiiBoundaryTests`：Ai 入力 DTO が Domain 型 ID や連絡先名のプロパティを持たない）。
  - Application.Tests: `AiProposalServiceAdapterTests`（委譲・写像・1 回呼び出し）。

## 設計判断
- **PII 境界を型と隔離の二重で担保**: Ai 入力 DTO は集約値・文字列・enum のみ（識別子/連絡先フィールドなし）。プロンプト構築は Infrastructure のみ。WireMock で送信プロンプトを捕捉し非 PII を自動検証（ADR-0005 の要求）。送信 JSON は日本語を `\u` エスケープするため、テストは本文を JSON 復号してプロンプト内容で検証（生文字列一致より堅牢）。
- **運用継続性**: AI 失敗は `Result.Failure` ではなくフォールバック出力（注意フラグ）。施策フロー（propose）は既定文面で継続可能。
- **値引き上限の後段検証**: 出力に上限超過の `n%` を検出したら既定文面へ置換し注意フラグ（ADR-0010。再生成でなく置換を採用＝決定的・低コスト）。
- **2 つの `IAiProposalService`（Matching/Ai）**: 名前は同一だが名前空間で分離。Matching は「提案生成シーム」、Ai は「汎用 AI 能力」。アダプタが橋渡し（エイリアスで曖昧性回避）。
- **LLM SDK 非依存**: Infrastructure は生 `HttpClient`＋System.Text.Json で OpenAI 互換 API を呼ぶ（重い SDK を持ち込まず、プロンプトの可視性と WireMock 検証性を確保）。Application は SDK に一切依存しない。
- **DI 配線は繰り延べ（合成ルート組み立て時）**: `AddHttpClient`/Options `Bind` には `Microsoft.Extensions.Http` 等の追加パッケージが必要で、合成ルート（Api Program）は未マージ 0.9 にあり本ブランチで配線を実行できない。0.8/0.9 と同様、ポート実装の DI 登録（OpenAiProposalService の typed client・Options バインド・Matching アダプタ）は合成ルート組み立て時にまとめて行う。本 Stage はサービスを直接構築してテスト検証。

## 検証結果
- `dotnet build -warnaserror` → 警告0・エラー0。
- `dotnet test`（全体）→ 224/224 Green（Domain 196 + Application 10〔+1〕 + Infra 16〔+10〕 + Api 1 + Integration 1）。
- `dotnet format --verify-no-changes` → 差分なし。
- `dotnet list package --vulnerable / --deprecated` → クリーン（追加: WireMock.Net 2.8.0〔テスト〕。脆弱性・非推奨なし。xunit の Legacy 表示は全テスト共通の既存事項）。
- 受け入れ観点（009）: プロンプト非 PII（WireMock 捕捉）/ 入力 DTO に連絡先・識別子フィールドなし（型レベル）/ 値引き上限超過の弾き / LLM 障害でも施策継続。

## 未解決事項・次アクション
- AI の DI 配線（`AddHttpClient<IAiProposalService, OpenAiProposalService>`・`OpenAiOptions` バインド・Matching アダプタ登録）を合成ルート組み立て時に実施。`Microsoft.Extensions.Http` / `Microsoft.Extensions.Options.ConfigurationExtensions` の追加要否を併せて判断。
- `SummarizeCampaignAsync` / `CommentResultsAsync` を管理 API / 効果測定（Stage 0.12）から利用する導線。
- プロンプト/トーンの業種テンプレート化（design.md の業種テンプレ）。
- 通知頻度・オプトアウトの制約もプロンプトへ明示（現状は値引き上限・トーン・PII 非含を明示）。

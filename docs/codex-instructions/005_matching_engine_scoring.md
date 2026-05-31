# 005 — Matching: 候補抽出・v0 スコア・Campaign (Stage 0.6)

> 本タスクはプロダクトの中核。状態遷移とスコアリングの設計は Claude Code が主担当（`CLAUDE.md` §2.2）。

## 前提
- `docs/architecture/design.md` §5（Aggregate）, §6（スコアリング）
- ADR-0003（ルールベース・スコアリング）, ADR-0004（承認フロー）

## 対象ディレクトリ
- `src/MatchOps.Domain/Matching`, `tests/MatchOps.Domain.Tests/Matching`

## 作成物
- `MatchingCampaign` Aggregate（`TenantId`, `StoreId`, 対象 TimeSlot 群, 候補, 状態, 提案メタ）
- `CampaignStatus` enum（draft / scored / proposed / approved / sent / measured）
- `MatchingCandidate`（`CustomerId`, `TimeSlotId`, `OfferId`, `MatchScore`, `ScoreBreakdown`, 提案理由・文面の格納先）
- `ScoringPolicy`（v0：3要素＋重み、欠損項目の動的再正規化）
- `ScoreInputs`（顧客・空き枠から計算済みの特徴量を受ける入力 DTO。Domain 内で完結）
- Domain Event: `MatchScored`, `CampaignApproved`, `CampaignSent`

## 仕様

### 状態遷移（厳守）
```
draft → scored → proposed → approved → sent → measured
```
- `proposed → approved` は人手のみ（`Approve(approvedBy, now)`）。
- `approved` を経ずに `Send` できない（`DomainException` or `Result.Failure`）。
- 不正遷移（例: `sent` から `Approve`）は拒否。

### v0 スコア（ADR-0003）
```
match_score(v0) =
    休眠日数スコア          × w1   (w1 = 0.40)
  + 来店周期との乖離スコア   × w2   (w2 = 0.35)
  + 空き枠の曜日/時間帯一致  × w3   (w3 = 0.25)
```
- 各項目は 0〜1 に正規化。重みは設定から注入（ハードコードしない。既定値は上記）。
- **段階的スコアリング**：ある項目の入力が欠損（来店履歴なし等）の場合、その項目を除外し、**残り項目の重みを再正規化**して合計が 1 になるようにする。
- `ScoreBreakdown` に各項目の寄与を保持し、提案理由生成（009）と説明性に使う。
- 通知疲れペナルティ等の追加項目は将来拡張（v0 では持たない or 0 重み）。

### 候補抽出
- 対象 TimeSlot に適合する顧客（同テナント・同店舗、オプトイン、頻度上限内）を候補化。
- 候補ごとに最適 Offer を選定（値引き上限内・適用条件一致）。

## テスト要件（テストファースト）
- `Approve` 前の `Send` が拒否される。
- 不正な状態遷移が拒否される（網羅）。
- スコア: 全項目ありで重み通り / 1項目欠損で残り2項目が再正規化（合計1）/ 全欠損時の扱い（既定スコア or 候補外）。
- スコアが 0〜1 に収まる。
- 値引き上限を超える Offer が候補に選ばれない。
- オプトアウト・頻度超過の顧客が候補に入らない。
- `ScoreBreakdown` の寄与合計が `MatchScore` と整合。
- 異なる `TenantId` の顧客/枠が混在しない。

## 制約
- 機械学習を導入しない（ルール＋重みのみ。ADR-0003）。
- 重みをコードに直書きしない（設定注入）。
- AI/配信/永続化を Domain に持ち込まない（候補とスコアの算出までが Domain の責務）。
- 顧客ごとの文面生成をここで行わない（009 が施策単位で行う）。

## 完了条件
- `dotnet build -warnaserror` / `dotnet test` 通過、Domain カバレッジ 95%+。
- 状態遷移・再正規化・配信制御の各テストが緑。
- スコアリング設計の根拠を ADR-0003 に反映（必要なら更新）。
- `docs/work-records/` に設計判断（重み既定値・欠損時方針）を記録。

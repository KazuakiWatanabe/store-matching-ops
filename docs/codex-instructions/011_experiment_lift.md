# 011 — Experiments: ホールドアウト・リフト測定 (Stage 0.12)

## 前提
- `docs/architecture/design.md` §10（効果測定）
- ADR-0007（ホールドアウトによるリフト測定）

## 対象ディレクトリ
- `src/MatchOps.Domain/Experiments`, `src/MatchOps.Application/Experiments`,
  `src/MatchOps.Infrastructure/Persistence`, tests 各所

## 作成物
- `ExperimentArm` enum（control / treatment）
- `experiment_assignments` テーブルとマッピング（`experiment_id`, `campaign_id`, `customer_id`, `arm`, `assigned_at`）
- 割当ロジック（施策の候補を control/treatment にランダム分割。control 比率は設定、既定 10〜20%）
- リフト集計（Application/Analytics 側、または Metabase クエリ）：
  - 処置群CVR・対照群CVR・リフト・増分件数・増分売上の算出

## 仕様
- 配信は treatment のみ。control は非配信で追跡。
- 割当は再現可能（シード固定 or 決定的ハッシュ）でテスト可能にする。`IClock` で時刻固定。
- `conversion_events` と arm を突合してリフトを算出。
- 1施策で検出力不足になりやすい旨をダッシュボードに明記し、同種施策のプール集計を用意。
- control 比率・対象人数から、最低限の信頼区間/サンプル数の目安を表示（簡易でよい）。

## テスト要件
- 指定比率で control/treatment に分割される（誤差範囲内）。
- control 顧客には配信されない（010 と結合）。
- 同一入力・同一シードで割当が再現する。
- リフト算出が既知データで正しい値を返す。

## 制約
- ランダム値を固定せずにテストしない（再現性必須）。
- 値引き・配信の二重計上を避ける（増分の定義に忠実に）。

## 完了条件
- build/test 通過。割当再現性・リフト算出のテストが緑。
- Metabase に「施策別リフト／累積増分売上／対照群差」の最小ダッシュボードを用意。

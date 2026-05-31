# ADR-0002: モジュラーモノリス + 非同期ワーカーを採用する

- ステータス: 採用
- 日付: 2026-05-31

## コンテキスト
PoC/MVP では開発速度が重要で、サービス境界もまだ固まっていない。一方で、マッチング・AI・配信は将来独立しやすい領域である。

## 決定
- 初期はモジュラーモノリス（単一デプロイ）+ 非同期ワーカーを採用する。
- クリーンアーキテクチャのレイヤー（Domain/Application/Infrastructure/Api/Worker）に加え、Application/Domain をモジュール（Customers/Scheduling/Catalog/Matching/Ai/Notifications/Experiments/Analytics/Tenancy）で分割する。
- モジュール間連携は Application のインターフェース越しに行い、別モジュールの Domain 内部に直接依存しない。

## 影響
- マイクロサービス化のオーバーヘッドを避けつつ、将来 Matching/Ai/Notifications を切り出せる境界を維持する。
- 結合度の監視（モジュール間の直接依存禁止）をレビューで担保する。

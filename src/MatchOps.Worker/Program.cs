// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 非同期ワーカー（取込・スコアリング・配信・分析）のエントリポイント。
// Phase 0 骨格段階ではホステッドサービスを登録しない（後続 Stage で追加）。
// 時刻は IClock 経由で扱い、DateTime.UtcNow 等は直呼びしない（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);

// Phase 0 骨格: バックグラウンドジョブ（NightlyScoringJob / OutboxDispatchJob 等）は
// 後続 Stage で AddHostedService により登録する。

var host = builder.Build();
host.Run();

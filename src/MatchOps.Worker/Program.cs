// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 非同期ワーカー（取込・スコアリング・配信・分析）のエントリポイント。
// Stage 0.11: Outbox 配信ジョブ（OutboxDispatchJob）を登録する。Infrastructure 実装を DI 登録する。
// 時刻は IClock 経由で扱い、DateTime.UtcNow 等は直呼びしない（CLAUDE.md §10.4）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Infrastructure 実装（永続化・Outbox・配信・ポリシー・AI 等）を登録する。
builder.Services.AddInfrastructure(builder.Configuration);

// Outbox 配信ジョブ（Phase 0 はポーリング。Phase 1 で Hangfire/cron）。
builder.Services.AddHostedService<OutboxDispatchJob>();

var host = builder.Build();
host.Run();

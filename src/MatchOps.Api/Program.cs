// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 管理画面向け API のエントリポイント。
// 施策の run/approve/send/results を公開し、Idempotency-Key・テナントスコープ・認証スタブを横断適用する。
// Application ユースケースと Infrastructure 実装（永続化・Outbox・ポリシー・候補ソース・AI）を DI 登録する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Api.Idempotency;
using MatchOps.Api.Tenancy;
using MatchOps.Application.Matching;
using MatchOps.Application.Tenancy;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenAPI ドキュメント生成（/openapi/v1.json）。XML コメント由来の日本語説明を含める。
builder.Services.AddOpenApi();

// リクエストスコープのテナント・操作者コンテキスト。ITenantContext として DbContext のクエリフィルタにも供給する（ADR-0006）。
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<RequestContext>());

// 冪等性（CLAUDE.md §10.1）。Phase 0 はインメモリ。
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddScoped<IdempotencyFilter>();

// Infrastructure 実装（永続化・Outbox・ポリシー・候補ソース・AI＋Matching 委譲アダプタ）を登録する。
builder.Services.AddInfrastructure(builder.Configuration);

// 施策ユースケース（Application）。依存ポートは Infrastructure が提供する。
builder.Services.AddScoped<IMatchingCampaignService, MatchingCampaignService>();
builder.Services.AddScoped<IMatchingCampaignQueries, MatchingCampaignQueries>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // API リファレンス UI（Scalar）。/scalar で OpenAPI ドキュメントを閲覧できる。
    app.MapScalarApiReference();
}

// 認証スタブ兼テナント解決（/api 配下で X-Tenant-Id を要求, ADR-0006）。
app.UseMiddleware<TenantResolutionMiddleware>();

// ヘルスチェック: 骨格段階の死活確認用エンドポイント。
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

/// <summary>
/// アプリケーションのエントリポイント型。
/// 統合テスト（<c>WebApplicationFactory&lt;Program&gt;</c>）から参照するため公開する。
/// </summary>
public partial class Program;

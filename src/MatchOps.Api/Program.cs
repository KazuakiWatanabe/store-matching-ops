// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 管理画面向け API のエントリポイント。
// 施策の run/approve/send/results を公開し、Idempotency-Key・テナントスコープ・認証スタブを横断適用する。
// データ系ポート（リポジトリ/候補ソース/ポリシー/AI/Outbox/UnitOfWork）の実装は後続 Stage（Infrastructure / 0.10 / 0.11）で
// DI 登録する。本 Stage では API 表層と横断的関心事（冪等性・テナント・認証スタブ・OpenAPI）を配線する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Api.Idempotency;
using MatchOps.Api.Tenancy;
using MatchOps.Api.Time;
using MatchOps.Application.Common;
using MatchOps.Application.Tenancy;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenAPI ドキュメント生成（/openapi/v1.json）。XML コメント由来の日本語説明を含める。
builder.Services.AddOpenApi();

// 時刻源（CLAUDE.md §10.4）。
builder.Services.AddSingleton<IClock, SystemClock>();

// リクエストスコープのテナント・操作者コンテキスト。ITenantContext として DbContext のクエリフィルタにも供給する（ADR-0006）。
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<RequestContext>());

// 冪等性（CLAUDE.md §10.1）。Phase 0 はインメモリ。
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
builder.Services.AddScoped<IdempotencyFilter>();

// 施策ユースケース（IMatchingCampaignService / IMatchingCampaignQueries）と、その依存データ系ポート
// （リポジトリ/候補ソース/ポリシー/AI/Outbox/UnitOfWork）の実装はまだ存在しないため、本 Stage では登録しない。
// 実装が揃う後続 Stage（Infrastructure / 0.10 / 0.11）でまとめて DI 登録する。それまで /api/campaigns は 500 を返す。
// 本 Stage の検証は WebApplicationFactory のテスト host にインメモリ実装を差し込んで行う。

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

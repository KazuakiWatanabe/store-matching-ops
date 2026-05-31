// -----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 管理画面向け API のエントリポイント。
// Phase 0 骨格段階では死活確認用のヘルスエンドポイントのみを公開する。
// ビジネスロジックは持たず、後続 Stage で Application / Infrastructure を DI 登録する。
// </summary>
// -----------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// OpenAPI ドキュメント生成（/openapi/v1.json）。Swagger UI は Phase 1 で追補。
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ヘルスチェック: 骨格段階の死活確認用エンドポイント。
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

/// <summary>
/// アプリケーションのエントリポイント型。
/// 統合テスト（<c>WebApplicationFactory&lt;Program&gt;</c>）から参照するため公開する。
/// </summary>
public partial class Program;

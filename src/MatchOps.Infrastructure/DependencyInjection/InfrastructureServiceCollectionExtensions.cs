// -----------------------------------------------------------------------------
// <copyright file="InfrastructureServiceCollectionExtensions.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Infrastructure の DI 登録（合成ルート組み立て）。EF Core 永続化・リポジトリ・UnitOfWork・Outbox・
// マッチングポリシー・候補ソース・AI（OpenAI 実装＋Matching 委譲アダプタ）を一括登録する。
// 接続文字列・API キー等のシークレットは設定（環境変数/Secrets）から注入し、appsettings に書かない（CLAUDE.md §9.1）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Application.Notifications;
using MatchOps.Infrastructure.Ai;
using MatchOps.Infrastructure.Matching;
using MatchOps.Infrastructure.Notifications;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AiApp = MatchOps.Application.Ai;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Infrastructure サービスの DI 登録拡張。</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Infrastructure の実装（永続化・Outbox・ポリシー・候補ソース・AI）を DI に登録する。
    /// </summary>
    /// <param name="services">サービスコレクション。</param>
    /// <param name="configuration">設定（接続文字列・<c>OpenAi</c>・<c>Matching</c> セクション）。</param>
    /// <returns>連鎖呼び出し用の <paramref name="services"/>。</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 永続化（PostgreSQL）。接続文字列は設定（環境変数/Secrets）から注入する。
        // 未設定でもアプリが起動できるよう、その場合は接続文字列を遅延（クエリ実行時に解決）する。
        string? connectionString = configuration.GetConnectionString("MatchOps");
        services.AddDbContext<MatchOpsDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql();
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IMatchingCampaignRepository, EfMatchingCampaignRepository>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();

        // マッチングのスコアリング重み・頻度ポリシー（設定）と候補ソース（Phase 1 まで暫定）。
        services.Configure<MatchingOptions>(configuration.GetSection(MatchingOptions.SectionName));
        services.AddSingleton<IMatchingPolicyProvider, ConfigurationMatchingPolicyProvider>();
        services.AddScoped<ICampaignCandidateSource, PlaceholderCampaignCandidateSource>();

        // AI（OpenAI 実装）。プロンプト構築は Infrastructure に隔離（PII 非送出, ADR-0005）。
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddHttpClient<AiApp.IAiProposalService, OpenAiProposalService>((provider, client) =>
        {
            OpenAiOptions options = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.Endpoint))
            {
                client.BaseAddress = new Uri(options.Endpoint, UriKind.Absolute);
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        // Matching の提案生成シームを AI モジュールへ委譲する（ユーザー判断: Matching は委譲）。
        services.AddScoped<IAiProposalService, AiProposalServiceAdapter>();

        return services;
    }
}

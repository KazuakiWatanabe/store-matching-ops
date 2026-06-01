using MatchOps.Application.Common;
using MatchOps.Application.Matching;
using MatchOps.Application.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MatchOps.Api.Tests;

/// <summary>
/// データ系ポートをインメモリのテストダブルで差し込む WebApplicationFactory。
/// 本番未配線のポート（リポジトリ/候補ソース/ポリシー/AI/Outbox/UnitOfWork）をテスト host に登録する。
/// </summary>
internal sealed class CampaignApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 本番未配線の施策ユースケースと、その依存データ系ポートをテスト用に登録する。
            services.AddScoped<IMatchingCampaignService, MatchingCampaignService>();
            services.AddScoped<IMatchingCampaignQueries, MatchingCampaignQueries>();

            services.AddSingleton<InMemoryCampaignStore>();
            services.AddScoped<IMatchingCampaignRepository, TenantScopedCampaignRepository>();
            services.AddSingleton<ICampaignCandidateSource, TestCandidateSource>();
            services.AddSingleton<IMatchingPolicyProvider, TestPolicyProvider>();
            services.AddSingleton<IAiProposalService, TestAiProposalService>();
            services.AddSingleton<IOutboxWriter, TestOutboxWriter>();
            services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
        });
    }
}

using MatchOps.Application.Matching;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MatchOps.Api.Tests;

/// <summary>
/// 本番の合成ルート（Program）が、テスト用フェイクを差し込まずに DI グラフを構築・解決できることを検証する。
/// データベース接続は伴わない（EF Core の DbContext 構築は接続を要しない）。
/// </summary>
public sealed class ApiCompositionRootTests
{
    [Fact]
    public void ProductionDiGraph_ResolvesCampaignServiceAndDependencies()
    {
        using var factory = new WebApplicationFactory<Program>();
        using IServiceScope scope = factory.Services.CreateScope();
        IServiceProvider provider = scope.ServiceProvider;

        // 施策ユースケースとその依存ポート（リポジトリ/Outbox/AI 委譲アダプタ/参照）が実装込みで解決できる。
        Assert.NotNull(provider.GetRequiredService<IMatchingCampaignService>());
        Assert.NotNull(provider.GetRequiredService<IMatchingCampaignQueries>());
        Assert.NotNull(provider.GetRequiredService<IAiProposalService>());
    }
}

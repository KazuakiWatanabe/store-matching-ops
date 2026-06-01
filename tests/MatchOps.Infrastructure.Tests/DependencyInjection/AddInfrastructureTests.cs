using MatchOps.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchOps.Infrastructure.Tests.DependencyInjection;

/// <summary>
/// AddInfrastructure が外部登録（Api 固有のテナントコンテキスト等）なしで自己完結し、
/// 背景処理（Worker）から Outbox ディスパッチャを解決できることを検証する。
/// </summary>
public sealed class AddInfrastructureTests
{
    [Fact]
    public void AddInfrastructure_ResolvesOutboxDispatcher_WithoutExternalRegistrations()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        // Worker 相当（Api のテナントコンテキストを登録しない）でも検証込みで構築できる。
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOutboxDispatcher>());
    }
}

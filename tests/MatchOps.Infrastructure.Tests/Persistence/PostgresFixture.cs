using MatchOps.Application.Tenancy;
using MatchOps.Domain.Common;
using MatchOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace MatchOps.Infrastructure.Tests.Persistence;

/// <summary>
/// Testcontainers の PostgreSQL 18 を起動し、初期マイグレーションを適用する統合テスト用フィクスチャ。
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    private string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using MatchOpsDbContext context = CreateContext(tenant: null);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>指定テナントのコンテキストを生成する。</summary>
    public MatchOpsDbContext CreateContext(TenantId? tenant)
    {
        DbContextOptions<MatchOpsDbContext> options = new DbContextOptionsBuilder<MatchOpsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new MatchOpsDbContext(options, new TestTenantContext(tenant));
    }

    private sealed class TestTenantContext(TenantId? tenantId) : ITenantContext
    {
        public TenantId? CurrentTenantId { get; } = tenantId;
    }
}

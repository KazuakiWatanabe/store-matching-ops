// -----------------------------------------------------------------------------
// <copyright file="MatchOpsDbContextFactory.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 設計時（dotnet ef）に DbContext を生成するファクトリ。
// マイグレーション生成は DB へ接続しないため、接続文字列はプレースホルダで足りる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Tenancy;
using MatchOps.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MatchOps.Infrastructure.Persistence;

/// <summary>設計時用の <see cref="MatchOpsDbContext"/> ファクトリ。</summary>
public sealed class MatchOpsDbContextFactory : IDesignTimeDbContextFactory<MatchOpsDbContext>
{
    /// <inheritdoc />
    public MatchOpsDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<MatchOpsDbContext> options = new DbContextOptionsBuilder<MatchOpsDbContext>()
            .UseNpgsql("Host=localhost;Database=matchops_design;Username=design;Password=design")
            .Options;

        return new MatchOpsDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public TenantId? CurrentTenantId => null;
    }
}

// -----------------------------------------------------------------------------
// <copyright file="ConfigurationMatchingPolicyProvider.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// IMatchingPolicyProvider の設定ベース実装。スコアリング重み・通知頻度上限を設定（MatchingOptions）から提供する。
// 重みが未設定なら v0 既定（ScoringPolicy.CreateV0Default）を用いる。テナント/店舗別設定は将来拡張。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Matching;
using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;
using Microsoft.Extensions.Options;

namespace MatchOps.Infrastructure.Matching;

/// <summary>設定からスコアリング・頻度ポリシーを提供する実装。</summary>
public sealed class ConfigurationMatchingPolicyProvider : IMatchingPolicyProvider
{
    private readonly MatchingOptions _options;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="options">マッチング設定。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> が <c>null</c> の場合。</exception>
    public ConfigurationMatchingPolicyProvider(IOptions<MatchingOptions> options)
        => _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public ScoringPolicy GetScoringPolicy(TenantId tenantId, StoreId storeId)
        => _options.ScoringWeights.Count > 0
            ? ScoringPolicy.Create(_options.ScoringWeights)
            : ScoringPolicy.CreateV0Default();

    /// <inheritdoc />
    public NotificationFrequencyPolicy GetFrequencyPolicy(TenantId tenantId, StoreId storeId)
        => NotificationFrequencyPolicy.OfMinIntervalDays(_options.NotificationMinIntervalDays);
}

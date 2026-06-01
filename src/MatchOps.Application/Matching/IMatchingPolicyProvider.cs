// -----------------------------------------------------------------------------
// <copyright file="IMatchingPolicyProvider.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// スコアリング重みと通知頻度上限を設定から提供する抽象（CLAUDE.md §4.3, ScoringPolicy はコード直書き禁止）。
// テナント・店舗ごとに異なる設定を許容する。実装は Infrastructure（設定/DB）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Matching;

/// <summary>スコアリング・頻度ポリシーを提供する抽象。</summary>
public interface IMatchingPolicyProvider
{
    /// <summary>対象テナント・店舗のスコアリングポリシー（重みは設定から注入）を取得する。</summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <returns>スコアリングポリシー。</returns>
    ScoringPolicy GetScoringPolicy(TenantId tenantId, StoreId storeId);

    /// <summary>対象テナント・店舗の通知頻度上限ポリシーを取得する。</summary>
    /// <param name="tenantId">所属テナント。</param>
    /// <param name="storeId">所属店舗。</param>
    /// <returns>通知頻度ポリシー。</returns>
    NotificationFrequencyPolicy GetFrequencyPolicy(TenantId tenantId, StoreId storeId);
}

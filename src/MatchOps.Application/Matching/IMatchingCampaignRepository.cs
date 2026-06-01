// -----------------------------------------------------------------------------
// <copyright file="IMatchingCampaignRepository.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// MatchingCampaign の永続化抽象。実装は Infrastructure（EF Core, Stage 0.7 で繰り延べた施策永続化）。
// テナントスコープは Infrastructure の Global Query Filter で機械的に強制する（ADR-0006）。
// 取得は現在テナント外の施策に到達しない（解決不能時は null）。確定は IUnitOfWork に委ねる。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Matching;

/// <summary>施策 (MatchingCampaign) の永続化を担う抽象。</summary>
public interface IMatchingCampaignRepository
{
    /// <summary>新規の施策を追加する（確定は <see cref="Common.IUnitOfWork"/>）。</summary>
    /// <param name="campaign">追加する施策。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>追加処理を表すタスク。</returns>
    Task AddAsync(MatchingCampaign campaign, CancellationToken cancellationToken = default);

    /// <summary>
    /// 施策を ID で取得する。現在テナント外・存在しない場合は <c>null</c>。
    /// </summary>
    /// <param name="id">施策 ID。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>該当する施策。なければ <c>null</c>。</returns>
    Task<MatchingCampaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default);
}

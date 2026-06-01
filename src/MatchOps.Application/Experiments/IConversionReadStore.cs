// -----------------------------------------------------------------------------
// <copyright file="IConversionReadStore.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// コンバージョン（conversion_events）の読み取り抽象。リフト算出で割当 (arm) と突合する（ADR-0007）。
// テナントスコープは Infrastructure で強制する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Experiments;

/// <summary>コンバージョンの読み取りを担う抽象。</summary>
public interface IConversionReadStore
{
    /// <summary>指定施策に紐づくコンバージョンを取得する。</summary>
    /// <param name="campaignId">対象施策。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>コンバージョンレコードの一覧。</returns>
    Task<IReadOnlyList<ConversionRecord>> GetByCampaignAsync(
        CampaignId campaignId, CancellationToken cancellationToken = default);
}

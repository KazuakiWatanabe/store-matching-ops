// -----------------------------------------------------------------------------
// <copyright file="MatchingCampaignQueries.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策の参照ユースケース実装。リポジトリ（テナントスコープ強制）から施策を取得し、PII を含まないビューに射影する。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Application.Matching;

/// <summary>施策の参照ユースケースの実装。</summary>
public sealed class MatchingCampaignQueries : IMatchingCampaignQueries
{
    private readonly IMatchingCampaignRepository _repository;

    /// <summary>依存を注入して参照サービスを構築する。</summary>
    /// <param name="repository">施策リポジトリ。</param>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> が <c>null</c> の場合。</exception>
    public MatchingCampaignQueries(IMatchingCampaignRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task<CampaignResultsView?> GetResultsAsync(
        CampaignId campaignId, CancellationToken cancellationToken = default)
    {
        MatchingCampaign? campaign = await _repository.GetAsync(campaignId, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null)
        {
            return null;
        }

        var candidates = campaign.Candidates
            .Select(c => new CandidateScoreView(c.Score.Value, c.OfferId, c.TimeSlotId))
            .ToList();

        return new CampaignResultsView(
            campaign.Id, campaign.Status.ToString(), candidates.Count, candidates);
    }
}

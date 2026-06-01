// -----------------------------------------------------------------------------
// <copyright file="IMatchingCampaignQueries.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策の参照（読み取り）ユースケース。テナントスコープは下層（リポジトリ/Global Query Filter）で強制する（ADR-0006）。
// 返却モデルは PII（連絡先）を含まない（CLAUDE.md §9.3, §11）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Matching;

/// <summary>施策の参照ユースケースを提供する抽象。</summary>
public interface IMatchingCampaignQueries
{
    /// <summary>
    /// 施策の結果概況を取得する。現在テナント外・存在しない場合は <c>null</c>。
    /// </summary>
    /// <param name="campaignId">施策 ID。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>結果ビュー。なければ <c>null</c>。</returns>
    Task<CampaignResultsView?> GetResultsAsync(CampaignId campaignId, CancellationToken cancellationToken = default);
}

// -----------------------------------------------------------------------------
// <copyright file="CampaignId.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 施策 (MatchingCampaign) の一意識別子（強い型）。
// 1つの空き枠群に対する候補抽出〜提案〜承認〜配信の単位を表す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>施策 (MatchingCampaign) の一意識別子。</summary>
/// <param name="Value">基となる GUID 値。</param>
public readonly record struct CampaignId(Guid Value)
{
    /// <summary>新しい施策 ID を生成する。</summary>
    /// <returns>一意な <see cref="CampaignId"/>。</returns>
    public static CampaignId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}

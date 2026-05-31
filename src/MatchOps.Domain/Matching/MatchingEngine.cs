// -----------------------------------------------------------------------------
// <copyright file="MatchingEngine.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 候補抽出を担うドメインサービス。対象枠に対し、同テナント・同店舗／オプトイン／頻度上限内の
// 顧客を、利用可能な Offer（有効・適用可・値引き上限内）とともに候補化し、v0 スコアで採点する。
// 他モジュールの Domain 型に依存せず、Matching ローカルの入力抽象に対して動作する（CLAUDE.md §4.1）。
// 機械学習・AI・配信・永続化は行わない（候補とスコアの算出までが責務）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Matching;

/// <summary>候補抽出と採点を担うドメインサービス。</summary>
public sealed class MatchingEngine
{
    /// <summary>
    /// 対象枠に対する候補を抽出し、スコア降順で返す。
    /// テナント/店舗不一致・配信不可（オプトアウト等）・頻度超過・利用可能 Offer なしの顧客は除外する。
    /// </summary>
    /// <param name="slot">対象の空き枠。</param>
    /// <param name="inputs">候補抽出入力（顧客ごと）。</param>
    /// <param name="policy">スコアリングポリシー。</param>
    /// <param name="frequency">通知頻度ポリシー。</param>
    /// <param name="today">基準日（頻度判定に使用）。</param>
    /// <returns>スコア降順のマッチング候補。</returns>
    /// <exception cref="ArgumentNullException">引数が <c>null</c> の場合。</exception>
    public IReadOnlyList<MatchingCandidate> BuildCandidates(
        SlotCandidacy slot,
        IReadOnlyCollection<CandidateInput> inputs,
        ScoringPolicy policy,
        NotificationFrequencyPolicy frequency,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(policy);

        var candidates = new List<MatchingCandidate>();
        foreach (CandidateInput input in inputs)
        {
            CustomerCandidacy customer = input.Customer;

            // テナント・店舗が一致しない顧客は混在させない（ADR-0006）。
            if (customer.TenantId != slot.TenantId || customer.StoreId != slot.StoreId)
            {
                continue;
            }

            // オプトアウト等で配信不可、または頻度上限超過の顧客は除外（ADR-0010）。
            if (!customer.CanReceiveNotifications || !frequency.Allows(customer.LastNotifiedOn, today))
            {
                continue;
            }

            // 利用可能な Offer（有効・適用可・値引き上限内）がなければ候補にしない。
            OfferOption? offer = SelectOffer(input.OfferOptions);
            if (offer is null)
            {
                continue;
            }

            candidates.Add(MatchingCandidate.Create(
                customer.CustomerId, slot.TimeSlotId, offer.OfferId, policy.Score(input.ScoreInputs)));
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score.Value)
            .ToList();
    }

    private static OfferOption? SelectOffer(IReadOnlyList<OfferOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        foreach (OfferOption option in options)
        {
            if (option.IsUsable)
            {
                return option;
            }
        }

        return null;
    }
}

// -----------------------------------------------------------------------------
// <copyright file="ExperimentQueries.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 実験のリフト集計実装。割当を群別に集計し、conversion_events を顧客で突合して CV 件数・売上を求め、
// LiftResult.Calculate でリフト・増分を算出する（ADR-0007 / design.md §10）。
// 1 顧客の重複 CV は二重計上しない（顧客単位で CV 有無・売上合計を取る）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Experiments;

/// <summary>実験のリフト集計の実装。</summary>
public sealed class ExperimentQueries : IExperimentQueries
{
    private readonly IExperimentAssignmentRepository _assignments;
    private readonly IConversionReadStore _conversions;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="assignments">割当リポジトリ。</param>
    /// <param name="conversions">コンバージョン読み取り。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public ExperimentQueries(IExperimentAssignmentRepository assignments, IConversionReadStore conversions)
    {
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _conversions = conversions ?? throw new ArgumentNullException(nameof(conversions));
    }

    /// <inheritdoc />
    public async Task<LiftResult?> GetLiftAsync(
        ExperimentId experimentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExperimentAssignment> assignments = await _assignments
            .GetByExperimentAsync(experimentId, cancellationToken).ConfigureAwait(false);
        if (assignments.Count == 0)
        {
            return null;
        }

        // 関係する施策のコンバージョンを顧客単位で集約（重複 CV は二重計上しない）。
        var revenueByCustomer = new Dictionary<CustomerId, decimal>();
        foreach (CampaignId campaignId in assignments.Select(a => a.CampaignId).Distinct())
        {
            IReadOnlyList<ConversionRecord> records = await _conversions
                .GetByCampaignAsync(campaignId, cancellationToken).ConfigureAwait(false);
            foreach (ConversionRecord record in records)
            {
                revenueByCustomer[record.CustomerId] =
                    revenueByCustomer.GetValueOrDefault(record.CustomerId) + record.Revenue;
            }
        }

        ArmOutcome treatment = Tally(assignments, ExperimentArm.Treatment, revenueByCustomer);
        ArmOutcome control = Tally(assignments, ExperimentArm.Control, revenueByCustomer);

        return LiftResult.Calculate(treatment, control);
    }

    private static ArmOutcome Tally(
        IReadOnlyList<ExperimentAssignment> assignments,
        ExperimentArm arm,
        IReadOnlyDictionary<CustomerId, decimal> revenueByCustomer)
    {
        int count = 0;
        int conversions = 0;
        decimal revenue = 0m;

        foreach (ExperimentAssignment assignment in assignments.Where(a => a.Arm == arm))
        {
            count++;
            if (revenueByCustomer.TryGetValue(assignment.CustomerId, out decimal customerRevenue))
            {
                conversions++;
                revenue += customerRevenue;
            }
        }

        return ArmOutcome.Of(count, conversions, revenue);
    }
}

// -----------------------------------------------------------------------------
// <copyright file="ExperimentService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト実験ユースケースの実装。HoldoutAssignmentPolicy で決定的に arm を割り当て、永続化する（ADR-0007）。
// 配信は treatment のみのため、結果として treatment 顧客集合（配信対象）を返す。時刻は IClock 経由。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;
using MatchOps.Domain.Common;
using MatchOps.Domain.Experiments;

namespace MatchOps.Application.Experiments;

/// <summary>ホールドアウト実験ユースケースの実装。</summary>
public sealed class ExperimentService : IExperimentService
{
    private static readonly HoldoutAssignmentPolicy Policy = new();

    private readonly IExperimentAssignmentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>依存を注入して構築する。</summary>
    /// <param name="repository">割当リポジトリ。</param>
    /// <param name="unitOfWork">トランザクション境界。</param>
    /// <param name="clock">時刻源。</param>
    /// <exception cref="ArgumentNullException">いずれかの依存が <c>null</c> の場合。</exception>
    public ExperimentService(
        IExperimentAssignmentRepository repository, IUnitOfWork unitOfWork, IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public async Task<Result<ExperimentAssignmentResult>> AssignAsync(
        AssignHoldoutCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Customers is null || command.Customers.Count == 0)
        {
            return Result<ExperimentAssignmentResult>.Failure("no_customers", "割当対象の顧客が指定されていません。");
        }

        if (double.IsNaN(command.ControlRatio) || command.ControlRatio < 0d || command.ControlRatio > 1d)
        {
            return Result<ExperimentAssignmentResult>.Failure("invalid_control_ratio", "対照群比率は 0〜1 で指定してください。");
        }

        DateTimeOffset now = _clock.Now;
        var treatmentCustomers = new List<CustomerId>();
        int controlCount = 0;

        foreach (CustomerId customerId in command.Customers.Distinct())
        {
            ExperimentArm arm = Policy.Assign(command.ExperimentId, customerId, command.ControlRatio);
            ExperimentAssignment assignment = ExperimentAssignment.Create(
                command.ExperimentId, command.CampaignId, customerId, command.TenantId, arm, now);
            await _repository.AddAsync(assignment, cancellationToken).ConfigureAwait(false);

            if (arm == ExperimentArm.Treatment)
            {
                treatmentCustomers.Add(customerId);
            }
            else
            {
                controlCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ExperimentAssignmentResult>.Success(
            new ExperimentAssignmentResult(treatmentCustomers.Count, controlCount, treatmentCustomers));
    }
}

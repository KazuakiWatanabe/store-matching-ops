// -----------------------------------------------------------------------------
// <copyright file="IExperimentService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ホールドアウト実験のユースケース抽象。対象顧客を control/treatment に決定的分割して永続化し、
// 配信対象（treatment）集合を返す（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Application.Common;

namespace MatchOps.Application.Experiments;

/// <summary>ホールドアウト実験のユースケースを提供する抽象。</summary>
public interface IExperimentService
{
    /// <summary>
    /// 対象顧客を control/treatment に割り当て、永続化し、配信対象（treatment）集合を返す。
    /// </summary>
    /// <param name="command">割当コマンド。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>成功時は割当結果、失敗時はエラー。</returns>
    Task<Result<ExperimentAssignmentResult>> AssignAsync(
        AssignHoldoutCommand command, CancellationToken cancellationToken = default);
}

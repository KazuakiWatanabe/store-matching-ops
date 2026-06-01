// -----------------------------------------------------------------------------
// <copyright file="IUnitOfWork.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// トランザクション境界（Unit of Work）の抽象。ユースケース 1 回の状態変更をまとめて確定する。
// DB 状態変更と Outbox への積み込みを同一トランザクションで確定するために用いる（design.md §10, Outbox パターン）。
// 実装は Infrastructure（EF Core の SaveChanges）。Application はこの抽象越しに確定する。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Common;

/// <summary>ユースケースの状態変更を確定するトランザクション境界。</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 保留中の状態変更（Aggregate の更新・Outbox への積み込み等）を 1 トランザクションで確定する。
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>確定処理を表すタスク。</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

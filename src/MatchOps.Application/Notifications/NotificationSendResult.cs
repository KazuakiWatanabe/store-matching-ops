// -----------------------------------------------------------------------------
// <copyright file="NotificationSendResult.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 実送信の結果。成功/失敗と失敗理由（PII・シークレットを含めない）を持つ。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Notifications;

/// <summary>実送信の結果。</summary>
/// <param name="Succeeded">送信に成功したか。</param>
/// <param name="Error">失敗理由（成功時は <c>null</c>。PII・シークレットを含めない）。</param>
public sealed record NotificationSendResult(bool Succeeded, string? Error)
{
    /// <summary>成功結果を生成する。</summary>
    /// <returns>成功を表す <see cref="NotificationSendResult"/>。</returns>
    public static NotificationSendResult Success() => new(true, null);

    /// <summary>失敗結果を生成する。</summary>
    /// <param name="error">失敗理由（PII・シークレットを含めない）。</param>
    /// <returns>失敗を表す <see cref="NotificationSendResult"/>。</returns>
    public static NotificationSendResult Failure(string error) => new(false, error);
}

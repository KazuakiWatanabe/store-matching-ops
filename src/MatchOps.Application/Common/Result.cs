// -----------------------------------------------------------------------------
// <copyright file="Result.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ユースケースの成功/失敗を表す結果型。例外を制御フローに使わず、想定済みの失敗は Result で返す（CLAUDE.md §7.3）。
// 失敗にはエラーコードと日本語メッセージを持たせ、上位（API）が HTTP ステータスへ写像する。
// PII・シークレットをエラーメッセージに含めない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Common;

/// <summary>値を伴わないユースケース結果。</summary>
public class Result
{
    /// <summary>結果を構築する。</summary>
    /// <param name="isSuccess">成功なら <c>true</c>。</param>
    /// <param name="errorCode">失敗時のエラーコード（成功時は <c>null</c>）。</param>
    /// <param name="errorMessage">失敗時のメッセージ（成功時は <c>null</c>）。</param>
    protected Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>成功したか。</summary>
    public bool IsSuccess { get; }

    /// <summary>失敗したか。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>失敗時のエラーコード（成功時は <c>null</c>）。</summary>
    public string? ErrorCode { get; }

    /// <summary>失敗時のメッセージ（成功時は <c>null</c>）。PII・シークレットを含めない。</summary>
    public string? ErrorMessage { get; }

    /// <summary>成功結果を生成する。</summary>
    /// <returns>成功を表す <see cref="Result"/>。</returns>
    public static Result Success() => new(true, null, null);

    /// <summary>失敗結果を生成する。</summary>
    /// <param name="errorCode">エラーコード。</param>
    /// <param name="errorMessage">日本語メッセージ（PII・シークレットを含めない）。</param>
    /// <returns>失敗を表す <see cref="Result"/>。</returns>
    public static Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}

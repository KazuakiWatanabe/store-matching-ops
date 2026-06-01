// -----------------------------------------------------------------------------
// <copyright file="ResultOfT.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 値を伴うユースケース結果。成功時に値を、失敗時にエラーコードとメッセージを保持する（CLAUDE.md §7.3）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Common;

/// <summary>値を伴うユースケース結果。</summary>
/// <typeparam name="TValue">成功時の値の型。</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, string? errorCode, string? errorMessage)
        : base(isSuccess, errorCode, errorMessage)
        => _value = value;

    /// <summary>成功時の値。失敗時にアクセスすると例外を送出する。</summary>
    /// <exception cref="InvalidOperationException">失敗結果の値にアクセスした場合。</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("失敗結果から値を取得することはできません。");

    /// <summary>値を伴う成功結果を生成する。</summary>
    /// <param name="value">成功時の値。</param>
    /// <returns>成功を表す <see cref="Result{TValue}"/>。</returns>
    public static Result<TValue> Success(TValue value) => new(true, value, null, null);

    /// <summary>失敗結果を生成する。</summary>
    /// <param name="errorCode">エラーコード。</param>
    /// <param name="errorMessage">日本語メッセージ（PII・シークレットを含めない）。</param>
    /// <returns>失敗を表す <see cref="Result{TValue}"/>。</returns>
    public static new Result<TValue> Failure(string errorCode, string errorMessage)
        => new(false, default, errorCode, errorMessage);
}

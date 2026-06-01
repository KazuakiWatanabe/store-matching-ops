// -----------------------------------------------------------------------------
// <copyright file="ApiError.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// API の標準エラーレスポンス。エラーコードと日本語メッセージを返す。PII・シークレットを含めない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Api.Contracts;

/// <summary>API の標準エラーレスポンス。</summary>
/// <param name="ErrorCode">機械可読のエラーコード。</param>
/// <param name="ErrorMessage">日本語のエラーメッセージ（PII・シークレットを含めない）。</param>
public sealed record ApiError(string ErrorCode, string ErrorMessage);

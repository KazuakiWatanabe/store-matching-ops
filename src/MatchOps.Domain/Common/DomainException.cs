// -----------------------------------------------------------------------------
// <copyright file="DomainException.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// ドメインの不変条件 (invariant) 違反を表す例外。
// 値オブジェクト・Aggregate の生成/操作で業務ルールに反した場合に送出する。
// メッセージは日本語。シークレット・PII を含めてはならない（CLAUDE.md §7.3）。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Common;

/// <summary>
/// ドメインの不変条件 (invariant) 違反を表す例外。
/// </summary>
public class DomainException : Exception
{
    /// <summary>既定のメッセージで <see cref="DomainException"/> を生成する。</summary>
    public DomainException()
        : base("ドメインの不変条件に違反しました。")
    {
    }

    /// <summary>指定したメッセージで <see cref="DomainException"/> を生成する。</summary>
    /// <param name="message">違反内容を説明する日本語メッセージ。</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>メッセージと内部例外を指定して <see cref="DomainException"/> を生成する。</summary>
    /// <param name="message">違反内容を説明する日本語メッセージ。</param>
    /// <param name="innerException">原因となった内部例外。</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

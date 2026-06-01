// -----------------------------------------------------------------------------
// <copyright file="ConversionRecord.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// リフト算出に用いるコンバージョン（成果）レコード。顧客と売上のみを持つ（ADR-0007）。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Common;

namespace MatchOps.Application.Experiments;

/// <summary>コンバージョン（成果）レコード。</summary>
/// <param name="CustomerId">CV した顧客。</param>
/// <param name="Revenue">CV の売上。</param>
public sealed record ConversionRecord(CustomerId CustomerId, decimal Revenue);

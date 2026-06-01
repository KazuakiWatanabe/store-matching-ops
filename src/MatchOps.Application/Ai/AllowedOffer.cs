// -----------------------------------------------------------------------------
// <copyright file="AllowedOffer.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI に提示してよい Offer の制約（種別と値引き上限）。プロンプトに制約として明示し、出力がこれを逸脱しないことを後段で検証する（ADR-0005, ADR-0010）。
// 識別子・金額の生データではなく、種別名と上限率のみを持つ集約表現。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>AI に提示してよい Offer の制約。</summary>
/// <param name="OfferKind">Offer の種別名（例: "クーポン" / "コース" / "メニュー"）。</param>
/// <param name="MaxDiscountPercent">許容する最大値引き率（％）。値引きなしは <c>null</c>。</param>
public sealed record AllowedOffer(string OfferKind, int? MaxDiscountPercent);

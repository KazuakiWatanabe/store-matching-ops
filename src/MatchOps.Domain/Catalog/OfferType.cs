// -----------------------------------------------------------------------------
// <copyright file="OfferType.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 提案内容 (Offer) の種別。メニュー・コース・クーポンを統一表現する（設計 §4）。
// 業種固有体系は作り込まず、種別＋属性で吸収する。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Catalog;

/// <summary>提案内容 (Offer) の種別。</summary>
public enum OfferType
{
    /// <summary>単品メニュー。</summary>
    Menu = 0,

    /// <summary>コース（セットメニュー等）。</summary>
    Course = 1,

    /// <summary>クーポン（値引きを伴う）。</summary>
    Coupon = 2,
}

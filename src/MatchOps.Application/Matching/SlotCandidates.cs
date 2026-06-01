// -----------------------------------------------------------------------------
// <copyright file="SlotCandidates.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 1 つの空き枠と、その枠に対する候補抽出入力（顧客ごと）の束。
// ICampaignCandidateSource が各モジュール（Customers / Scheduling / Catalog）から組み立てて返す。
// </summary>
// -----------------------------------------------------------------------------

using MatchOps.Domain.Matching;

namespace MatchOps.Application.Matching;

/// <summary>空き枠 1 件分の候補抽出入力の束。</summary>
/// <param name="Slot">対象の空き枠（Matching ローカル抽象）。</param>
/// <param name="Inputs">その枠に対する顧客ごとの候補抽出入力。</param>
public sealed record SlotCandidates(SlotCandidacy Slot, IReadOnlyList<CandidateInput> Inputs);

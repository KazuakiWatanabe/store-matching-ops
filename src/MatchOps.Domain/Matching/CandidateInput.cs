// -----------------------------------------------------------------------------
// <copyright file="CandidateInput.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// 1 顧客分の候補抽出入力。顧客情報・スコア特徴量・提案しうる Offer 選択肢を束ねる。
// Application が各モジュールの Aggregate から組み立てて MatchingEngine に渡す。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Domain.Matching;

/// <summary>1 顧客分の候補抽出入力。</summary>
/// <param name="Customer">顧客の最小情報。</param>
/// <param name="ScoreInputs">スコア計算の入力特徴量。</param>
/// <param name="OfferOptions">提案しうる Offer 選択肢。</param>
public sealed record CandidateInput(
    CustomerCandidacy Customer,
    ScoreInputs ScoreInputs,
    IReadOnlyList<OfferOption> OfferOptions);

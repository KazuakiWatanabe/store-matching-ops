// -----------------------------------------------------------------------------
// <copyright file="AiProposalServiceAdapter.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// Matching の提案生成シーム（Matching.IAiProposalService）を、汎用 AI モジュール（Ai.IAiProposalService）へ委譲する
// アダプタ（ユーザー判断: Ai モジュール新設・Matching は委譲）。
// 施策単位の集約入力（AiProposalRequest, PII なし）を Ai 用コンテキストへ写像し、配信文面テンプレートを得る。
// LLM 呼び出しは施策単位で 1 回（GenerateMessageAsync）。提案理由は集約値から決定的に構築し、追加の LLM 呼び出しをしない。
// </summary>
// -----------------------------------------------------------------------------

using System.Globalization;
using AiModule = MatchOps.Application.Ai;

namespace MatchOps.Application.Matching;

/// <summary>Matching の提案生成シームを汎用 AI モジュールへ委譲するアダプタ。</summary>
public sealed class AiProposalServiceAdapter : IAiProposalService
{
    private readonly AiModule.IAiProposalService _ai;

    /// <summary>依存を注入してアダプタを構築する。</summary>
    /// <param name="ai">汎用 AI 提案サービス。</param>
    /// <exception cref="ArgumentNullException"><paramref name="ai"/> が <c>null</c> の場合。</exception>
    public AiProposalServiceAdapter(AiModule.IAiProposalService ai)
        => _ai = ai ?? throw new ArgumentNullException(nameof(ai));

    /// <inheritdoc />
    public async Task<AiProposalDraft> GenerateProposalAsync(
        AiProposalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 施策単位の集約入力（PII なし）を Ai 用コンテキストへ写像する。値引き情報は本シームには無いため上限なし扱い。
        var context = new AiModule.AiMessageContext(
            StoreCategory: string.Empty,
            SegmentName: request.SegmentSummary,
            SlotSummary: $"空き枠 {request.SlotCount} 件",
            Offer: new AiModule.AllowedOffer(OfferKind: string.Empty, MaxDiscountPercent: null),
            Tone: AiModule.AiTone.Friendly);

        AiModule.AiMessageDraft draft = await _ai.GenerateMessageAsync(context, cancellationToken)
            .ConfigureAwait(false);

        string averageScore = request.AverageScore.ToString("0.00", CultureInfo.InvariantCulture);
        string reasonTemplate = $"{request.SegmentSummary}（平均スコア {averageScore}）に基づく提案。";

        return new AiProposalDraft(reasonTemplate, draft.MessageTemplate);
    }
}

// -----------------------------------------------------------------------------
// <copyright file="IAiProposalService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI による施策要約・配信文面・結果コメント生成の抽象（ADR-0005）。実装は Infrastructure/Ai に隔離する。
// 入力はすべて匿名化・集約済み DTO（PII を型として持たない）。生成は施策／セグメント単位で、顧客ごとに呼ばない。
// LLM 障害時は例外でなくフォールバック出力（注意フラグ付き）を返し、施策フローを止めない（運用継続性）。
// 0.8 の Matching.IAiProposalService（提案生成シーム）は、本サービスへの委譲アダプタ経由で実装する。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Ai;

/// <summary>施策要約・配信文面・結果コメントを生成する抽象（LLM 隔離）。</summary>
public interface IAiProposalService
{
    /// <summary>施策の要約を生成する。</summary>
    /// <param name="context">匿名化・集約済みの施策コンテキスト。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>要約テキストと注意フラグ。</returns>
    Task<AiCampaignSummary> SummarizeCampaignAsync(
        AiCampaignContext context, CancellationToken cancellationToken = default);

    /// <summary>配信文面テンプレートを生成する（テンプレ＋差し込み用）。</summary>
    /// <param name="context">匿名化・集約済みの文面コンテキスト。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>文面テンプレートと注意フラグ。</returns>
    Task<AiMessageDraft> GenerateMessageAsync(
        AiMessageContext context, CancellationToken cancellationToken = default);

    /// <summary>施策結果のコメントを生成する。</summary>
    /// <param name="context">匿名化・集約済みの結果コンテキスト。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>結果コメントと注意フラグ。</returns>
    Task<AiResultComment> CommentResultsAsync(
        AiResultContext context, CancellationToken cancellationToken = default);
}

// -----------------------------------------------------------------------------
// <copyright file="IAiProposalService.cs" company="MatchOps">
// Copyright (c) MatchOps. All rights reserved.
// </copyright>
// <summary>
// AI 提案生成の抽象（ADR-0005）。実装は Infrastructure/Ai に隔離する（Stage 0.10, 009）。
// 入力 (AiProposalRequest) は匿名化・集約済みデータのみ。Application はここへ渡す前に PII を集約する。
// LLM への PII 送出を型レベルで防ぐため、本インターフェースは個別顧客データを受け取らない。
// </summary>
// -----------------------------------------------------------------------------

namespace MatchOps.Application.Matching;

/// <summary>施策単位の提案ドラフトを生成する抽象（LLM 隔離）。</summary>
public interface IAiProposalService
{
    /// <summary>
    /// 集約・匿名化済みの入力から、施策単位の提案理由・配信文面テンプレートを生成する。
    /// </summary>
    /// <param name="request">匿名化・集約済みの入力（PII を含まない）。</param>
    /// <param name="cancellationToken">キャンセルトークン。</param>
    /// <returns>生成された提案ドラフト。</returns>
    Task<AiProposalDraft> GenerateProposalAsync(AiProposalRequest request, CancellationToken cancellationToken = default);
}

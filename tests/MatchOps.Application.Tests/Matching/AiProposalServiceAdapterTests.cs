using MatchOps.Application.Ai;
using MatchOps.Application.Matching;
using MatchOps.Domain.Common;
using MatchingSeam = MatchOps.Application.Matching;

namespace MatchOps.Application.Tests.Matching;

public class AiProposalServiceAdapterTests
{
    /// <summary>渡された文面コンテキストを記録し、固定ドラフトを返す Ai サービス（テスト用）。</summary>
    private sealed class SpyAiService : MatchOps.Application.Ai.IAiProposalService
    {
        public AiMessageContext? LastMessageContext { get; private set; }

        public int MessageCallCount { get; private set; }

        public Task<AiCampaignSummary> SummarizeCampaignAsync(
            AiCampaignContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new AiCampaignSummary("要約", []));

        public Task<AiMessageDraft> GenerateMessageAsync(
            AiMessageContext context, CancellationToken cancellationToken = default)
        {
            LastMessageContext = context;
            MessageCallCount++;
            return Task.FromResult(new AiMessageDraft("生成された配信文面。", []));
        }

        public Task<AiResultComment> CommentResultsAsync(
            AiResultContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new AiResultComment("コメント", []));
    }

    [Fact]
    public async Task GenerateProposalAsync_DelegatesToAi_AndMapsMessageTemplate()
    {
        var ai = new SpyAiService();
        var adapter = new AiProposalServiceAdapter(ai);
        var request = new AiProposalRequest(
            StoreId.New(), CandidateCount: 3, SlotCount: 2, OfferVariety: 1,
            AverageScore: 0.5d, SegmentSummary: "対象顧客 3 名");

        MatchingSeam.AiProposalDraft draft = await adapter.GenerateProposalAsync(request);

        // 委譲は施策単位で 1 回（顧客ごとに呼ばない）。
        Assert.Equal(1, ai.MessageCallCount);
        Assert.Equal("生成された配信文面。", draft.MessageTemplate);
        Assert.Contains("対象顧客 3 名", draft.ReasonTemplate);

        // 集約サマリがセグメント名へ写像され、値引き情報はシームに無いため上限なし扱い。
        Assert.NotNull(ai.LastMessageContext);
        Assert.Equal("対象顧客 3 名", ai.LastMessageContext!.SegmentName);
        Assert.Null(ai.LastMessageContext.Offer.MaxDiscountPercent);
    }
}

using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class MatchingCandidateTests
{
    private static ScoreBreakdown Breakdown(double total)
        => ScoreBreakdown.From(new Dictionary<string, double> { ["x"] = total });

    [Fact]
    public void Create_ScoreEqualsBreakdownTotal()
    {
        var breakdown = Breakdown(0.6d);

        var candidate = MatchingCandidate.Create(CustomerId.New(), TimeSlotId.New(), OfferId.New(), breakdown);

        Assert.Equal(breakdown.Total, candidate.Score);
        Assert.Equal(0.6d, candidate.Score.Value, precision: 9);
        Assert.Null(candidate.ProposalReason);
    }

    [Fact]
    public void Create_NullBreakdown_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchingCandidate.Create(CustomerId.New(), TimeSlotId.New(), OfferId.New(), null!));
    }

    [Fact]
    public void AttachProposalReason_SetsTrimmedReason()
    {
        var candidate = MatchingCandidate.Create(CustomerId.New(), TimeSlotId.New(), OfferId.New(), Breakdown(0.5d));

        candidate.AttachProposalReason("  45日ご無沙汰のお客様へ  ");

        Assert.Equal("45日ご無沙汰のお客様へ", candidate.ProposalReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AttachProposalReason_Blank_Throws(string reason)
    {
        var candidate = MatchingCandidate.Create(CustomerId.New(), TimeSlotId.New(), OfferId.New(), Breakdown(0.5d));

        Assert.Throws<DomainException>(() => candidate.AttachProposalReason(reason));
    }
}

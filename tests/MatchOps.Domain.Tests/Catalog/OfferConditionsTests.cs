using MatchOps.Domain.Catalog;
using MatchOps.Domain.Common;

namespace MatchOps.Domain.Tests.Catalog;

public class OfferConditionsTests
{
    [Fact]
    public void Unrestricted_AppliesToAnyDayAndSegment()
    {
        var conditions = OfferConditions.Unrestricted;

        Assert.True(conditions.AppliesOn(new DateOnly(2026, 5, 31)));   // 日曜
        Assert.True(conditions.AppliesOn(new DateOnly(2026, 6, 1)));    // 月曜
        Assert.True(conditions.TargetsSegment("dormant"));
    }

    [Fact]
    public void AppliesOn_RestrictedDays_OnlyMatchingDays()
    {
        var conditions = OfferConditions.Create(applicableDays: new[] { DayOfWeek.Monday, DayOfWeek.Tuesday });

        Assert.True(conditions.AppliesOn(new DateOnly(2026, 6, 1)));   // 月曜
        Assert.False(conditions.AppliesOn(new DateOnly(2026, 5, 31))); // 日曜
    }

    [Fact]
    public void TargetsSegment_RestrictedSegments_OnlyMatching()
    {
        var conditions = OfferConditions.Create(targetSegments: new[] { "dormant", "regular" });

        Assert.True(conditions.TargetsSegment("dormant"));
        Assert.False(conditions.TargetsSegment("new"));
    }

    [Fact]
    public void Create_BlankSegment_Throws()
    {
        Assert.Throws<DomainException>(() => OfferConditions.Create(targetSegments: new[] { "ok", "  " }));
    }

    [Fact]
    public void Create_TrimsSegments()
    {
        var conditions = OfferConditions.Create(targetSegments: new[] { "  dormant  " });

        Assert.True(conditions.TargetsSegment("dormant"));
    }
}

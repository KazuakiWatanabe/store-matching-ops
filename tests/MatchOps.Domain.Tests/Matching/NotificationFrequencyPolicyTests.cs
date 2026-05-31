using MatchOps.Domain.Common;
using MatchOps.Domain.Matching;

namespace MatchOps.Domain.Tests.Matching;

public class NotificationFrequencyPolicyTests
{
    private static readonly DateOnly Today = new(2026, 5, 31);

    [Fact]
    public void Allows_NeverNotified_ReturnsTrue()
    {
        var policy = NotificationFrequencyPolicy.OfMinIntervalDays(7);

        Assert.True(policy.Allows(lastNotifiedOn: null, Today));
    }

    [Fact]
    public void Allows_WithinInterval_ReturnsFalse()
    {
        var policy = NotificationFrequencyPolicy.OfMinIntervalDays(7);

        Assert.False(policy.Allows(Today.AddDays(-3), Today));
    }

    [Fact]
    public void Allows_AtIntervalBoundary_ReturnsTrue()
    {
        var policy = NotificationFrequencyPolicy.OfMinIntervalDays(7);

        Assert.True(policy.Allows(Today.AddDays(-7), Today));
    }

    [Fact]
    public void Unlimited_AlwaysAllows()
    {
        Assert.True(NotificationFrequencyPolicy.Unlimited.Allows(Today, Today));
    }

    [Fact]
    public void OfMinIntervalDays_Negative_Throws()
    {
        Assert.Throws<DomainException>(() => NotificationFrequencyPolicy.OfMinIntervalDays(-1));
    }
}

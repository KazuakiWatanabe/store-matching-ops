using MatchOps.Domain.Common;
using MatchOps.Domain.Scheduling;

namespace MatchOps.Domain.Tests.Scheduling;

public class TimeRangeTests
{
    private static DateTimeOffset At(int hour) => new(2026, 5, 31, hour, 0, 0, TimeSpan.FromHours(9));

    [Fact]
    public void Create_ValidRange_SetsStartAndEnd()
    {
        var range = TimeRange.Create(At(10), At(11));

        Assert.Equal(At(10), range.Start);
        Assert.Equal(At(11), range.End);
    }

    [Theory]
    [InlineData(11, 10)] // 終了が開始より前
    [InlineData(10, 10)] // 同一（長さゼロ）
    public void Create_EndNotAfterStart_Throws(int startHour, int endHour)
    {
        Assert.Throws<DomainException>(() => TimeRange.Create(At(startHour), At(endHour)));
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var a = TimeRange.Create(At(10), At(12));
        var b = TimeRange.Create(At(11), At(13));

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_AdjacentRanges_ReturnsFalse()
    {
        var a = TimeRange.Create(At(10), At(11));
        var b = TimeRange.Create(At(11), At(12));

        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_DisjointRanges_ReturnsFalse()
    {
        var a = TimeRange.Create(At(10), At(11));
        var b = TimeRange.Create(At(12), At(13));

        Assert.False(a.Overlaps(b));
    }

    [Fact]
    public void ToString_UsesRoundTripFormat()
    {
        var range = TimeRange.Create(At(10), At(11));

        Assert.Equal($"{At(10):o}/{At(11):o}", range.ToString());
    }
}

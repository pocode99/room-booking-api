using RoomBooking.Tests.Helpers;
using Xunit;

namespace RoomBooking.Tests.Helpers;

public class TestClockTests
{
    private static readonly DateTimeOffset Start =
        new(2027, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetUtcNow_ReturnsTheConfiguredTime()
    {
        var clock = new TestClock(Start);

        Assert.Equal(Start, clock.GetUtcNow());
    }

    [Fact]
    public void Advance_MovesTheTimeForward()
    {
        var clock = new TestClock(Start);

        clock.Advance(TimeSpan.FromHours(2));

        Assert.Equal(Start.AddHours(2), clock.GetUtcNow());
    }
}
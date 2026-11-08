using PROG2026_part3.Helpers;

namespace PROG2026_part3.Tests;

/// <summary>
/// Tests for the helper that turns raw data into what the user sees:
/// "47th out of 312", status colours and finish times.
/// </summary>
public class DisplayHelperTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 29, 12, 0, 0);

    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(101, "101st")]
    [InlineData(112, "112th")]
    public void Ordinal_FormatsPositionsCorrectly(int number, string expected)
    {
        Assert.Equal(expected, DisplayHelper.Ordinal(number));
    }

    [Fact]
    public void EventStatus_FutureEvent_IsUpcoming()
    {
        Assert.Equal("Upcoming", DisplayHelper.EventStatus(Now.AddDays(10), Now));
    }

    [Fact]
    public void EventStatus_SameDay_IsToday()
    {
        Assert.Equal("Today", DisplayHelper.EventStatus(Now.AddHours(3), Now));
    }

    [Fact]
    public void EventStatus_PastEvent_IsCompleted()
    {
        Assert.Equal("Completed", DisplayHelper.EventStatus(Now.AddDays(-3), Now));
    }

    [Fact]
    public void EnrolmentStatus_WithResult_IsCompleted()
    {
        Assert.Equal("Completed", DisplayHelper.EnrolmentStatus(Now.AddDays(-3), true, Now));
    }

    [Fact]
    public void EnrolmentStatus_PastEventWithoutResult_IsAwaitingResult()
    {
        Assert.Equal("Awaiting result", DisplayHelper.EnrolmentStatus(Now.AddDays(-3), false, Now));
    }

    [Fact]
    public void EnrolmentStatus_FutureEvent_IsConfirmed()
    {
        Assert.Equal("Confirmed", DisplayHelper.EnrolmentStatus(Now.AddDays(5), false, Now));
    }

    [Theory]
    [InlineData("Upcoming", "badge-upcoming")]
    [InlineData("Confirmed", "badge-upcoming")]
    [InlineData("Today", "badge-today")]
    [InlineData("Awaiting result", "badge-awaiting")]
    [InlineData("Completed", "badge-completed")]
    [InlineData("Something else", "badge-default")]
    public void StatusBadgeClass_MapsEachStatusToItsColour(string status, string expected)
    {
        Assert.Equal(expected, DisplayHelper.StatusBadgeClass(status));
    }

    [Theory]
    [InlineData("Run", "type-run")]
    [InlineData("Walk", "type-walk")]
    [InlineData("Cycle", "type-cycle")]
    public void EventTypeBadgeClass_MapsEachTypeToItsColour(string type, string expected)
    {
        Assert.Equal(expected, DisplayHelper.EventTypeBadgeClass(type));
    }

    [Fact]
    public void FormatFinishTime_ShowsHoursMinutesSeconds()
    {
        Assert.Equal("00:45:30", DisplayHelper.FormatFinishTime(new TimeSpan(0, 45, 30)));
        Assert.Equal("02:05:09", DisplayHelper.FormatFinishTime(new TimeSpan(2, 5, 9)));
    }
}

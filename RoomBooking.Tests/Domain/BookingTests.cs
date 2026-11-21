using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using Xunit;

namespace RoomBooking.Tests.Domain;

public class BookingTests
{
    // Builds a UTC time on 2027-03-01 (or another day in March) from a "HH:mm" string.
    private static DateTime At(string time, int day = 1)
    {
        var parts = time.Split(':');
        return new DateTime(2027, 3, day, int.Parse(parts[0]), int.Parse(parts[1]), 0, DateTimeKind.Utc);
    }

    // The reference booking used by every overlap test: 09:00 to 11:00.
    private static Booking ExistingBooking() => new()
    {
        StartUtc = At("09:00"),
        EndUtc = At("11:00")
    };

    [Theory]
    [InlineData("10:00", "12:00")]   // overlaps the end of the existing booking
    [InlineData("08:00", "10:00")]   // overlaps the start of the existing booking
    [InlineData("09:30", "10:30")]   // fully inside the existing booking
    [InlineData("08:00", "12:00")]   // fully encloses the existing booking
    [InlineData("09:00", "11:00")]   // identical time range
    [InlineData("10:59", "11:30")]   // overlaps by a single minute at the end
    [InlineData("08:00", "09:01")]   // overlaps by a single minute at the start
    public void Overlaps_WhenRangesShareSomeTime_ReturnsTrue(string start, string end)
    {
        var existing = ExistingBooking();

        var result = existing.Overlaps(At(start), At(end));

        Assert.True(result);
    }

    [Theory]
    [InlineData("11:00", "12:00")]   // starts exactly when the existing booking ends
    [InlineData("08:00", "09:00")]   // ends exactly when the existing booking starts
    [InlineData("12:00", "13:00")]   // completely after, with a gap
    [InlineData("07:00", "08:00")]   // completely before, with a gap
    [InlineData("11:01", "12:00")]   // starts one minute after the existing booking ends
    public void Overlaps_WhenRangesOnlyTouchOrAreApart_ReturnsFalse(string start, string end)
    {
        var existing = ExistingBooking();

        var result = existing.Overlaps(At(start), At(end));

        Assert.False(result);
    }

    [Fact]
    public void Overlaps_WhenSameHoursOnADifferentDay_ReturnsFalse()
    {
        var existing = ExistingBooking();

        var result = existing.Overlaps(At("09:00", day: 2), At("11:00", day: 2));

        Assert.False(result);
    }

    [Fact]
    public void NewBooking_IsActiveByDefault()
    {
        var booking = new Booking();

        Assert.Equal(BookingStatus.Active, booking.Status);
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled()
    {
        var booking = new Booking();

        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }
}
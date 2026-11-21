using Moq;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Exceptions;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Tests.Helpers;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace RoomBooking.Tests.Application;

public class BookingServiceCreateTests
{
    private const int UserId = 42;

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Room _orion = new() { Id = 1, Name = "Orion", Capacity = 4 };

    // "Now" is fixed at 08:00 UTC on 2027-01-01 for every test.
    private readonly TestClock _clock = new(Utc(8, 0));

    // Captures the booking that the service hands to the repository.
    private Booking? _savedBooking;

    private static DateTimeOffset Utc(int hour, int minute) =>
        new(2027, 1, 1, hour, minute, 0, TimeSpan.Zero);

    private BookingService CreateService() =>
        new(_bookings.Object, _rooms.Object, _clock, NullLogger<BookingService>.Instance);

    private static CreateBookingRequest Request(
        DateTimeOffset start, DateTimeOffset end, string title = "Team sync") =>
        new() { RoomId = 1, Title = title, Start = start, End = end };

    // Room 1 exists, and the repository reports whether the slot is free.
    private void SetupRoom(bool slotIsFree)
    {
        _rooms.Setup(r => r.GetByIdAsync(_orion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_orion);

        _bookings.Setup(b => b.TryAddIfAvailableAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((booking, _) => _savedBooking = booking)
            .ReturnsAsync(slotIsFree);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ReturnsAnActiveBookingWithTheRoomName()
    {
        SetupRoom(slotIsFree: true);
        var service = CreateService();

        var response = await service.CreateAsync(
            UserId, Request(Utc(9, 0), Utc(10, 0)), CancellationToken.None);

        Assert.Equal(1, response.RoomId);
        Assert.Equal("Orion", response.RoomName);
        Assert.Equal(UserId, response.UserId);
        Assert.Equal("Team sync", response.Title);
        Assert.Equal("Active", response.Status);
        Assert.Equal(Utc(9, 0), response.StartUtc);
        Assert.Equal(Utc(10, 0), response.EndUtc);
        Assert.Equal(Utc(8, 0), response.CreatedAtUtc);   // taken from the clock
    }

    [Fact]
    public async Task CreateAsync_WhenTimesHaveAnOffset_StoresThemAsUtcAndTrimsTheTitle()
    {
        SetupRoom(slotIsFree: true);
        var service = CreateService();

        // 16:00 and 17:00 at UTC+7 are 09:00 and 10:00 UTC.
        var request = Request(
            new DateTimeOffset(2027, 1, 1, 16, 0, 0, TimeSpan.FromHours(7)),
            new DateTimeOffset(2027, 1, 1, 17, 0, 0, TimeSpan.FromHours(7)),
            title: "  Team sync  ");

        await service.CreateAsync(UserId, request, CancellationToken.None);

        Assert.NotNull(_savedBooking);
        Assert.Equal(new DateTime(2027, 1, 1, 9, 0, 0), _savedBooking!.StartUtc);
        Assert.Equal(new DateTime(2027, 1, 1, 10, 0, 0), _savedBooking.EndUtc);
        Assert.Equal(DateTimeKind.Utc, _savedBooking.StartUtc.Kind);
        Assert.Equal("Team sync", _savedBooking.Title);
        Assert.Equal(UserId, _savedBooking.UserId);
        Assert.Equal(BookingStatus.Active, _savedBooking.Status);
    }

    [Theory]
    [InlineData(10, 0, 9, 0)]    // ends before it starts
    [InlineData(9, 0, 9, 0)]     // zero length
    [InlineData(7, 0, 8, 30)]    // starts in the past
    [InlineData(7, 59, 9, 0)]    // starts one minute in the past
    [InlineData(9, 0, 13, 1)]    // 4 hours and 1 minute, one minute too long
    public async Task CreateAsync_WhenTimesBreakARule_ThrowsAndNeverTouchesTheRepositories(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        var service = CreateService();
        var request = Request(Utc(startHour, startMinute), Utc(endHour, endMinute));

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CreateAsync(UserId, request, CancellationToken.None));

        // Simple rules are checked before any database work happens.
        _rooms.Verify(
            r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _bookings.Verify(
            b => b.TryAddIfAvailableAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenStartingExactlyNow_Succeeds()
    {
        SetupRoom(slotIsFree: true);
        var service = CreateService();

        var response = await service.CreateAsync(
            UserId, Request(Utc(8, 0), Utc(9, 0)), CancellationToken.None);

        Assert.Equal("Active", response.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenLastingExactlyFourHours_Succeeds()
    {
        SetupRoom(slotIsFree: true);
        var service = CreateService();

        var response = await service.CreateAsync(
            UserId, Request(Utc(9, 0), Utc(13, 0)), CancellationToken.None);

        Assert.Equal("Active", response.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenTheRoomDoesNotExist_ThrowsNotFoundAndSavesNothing()
    {
        _rooms.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateAsync(UserId, Request(Utc(9, 0), Utc(10, 0)), CancellationToken.None));

        _bookings.Verify(
            b => b.TryAddIfAvailableAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTheSlotIsAlreadyTaken_ThrowsConflict()
    {
        SetupRoom(slotIsFree: false);
        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(UserId, Request(Utc(9, 0), Utc(10, 0)), CancellationToken.None));
    }
}
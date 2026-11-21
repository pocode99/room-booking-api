using Moq;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Exceptions;
using RoomBooking.Application.Rooms;
using RoomBooking.Domain.Entities;
using RoomBooking.Domain.Enums;
using RoomBooking.Tests.Helpers;
using Xunit;

namespace RoomBooking.Tests.Application;

public class BookingServiceCancelTests
{
    private const int OwnerId = 42;
    private const int StrangerId = 99;
    private const int BookingId = 7;

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly TestClock _clock = new(new DateTimeOffset(2027, 1, 1, 8, 0, 0, TimeSpan.Zero));

    private BookingService CreateService() =>
        new(_bookings.Object, _rooms.Object, _clock);

    // A booking owned by OwnerId. The repository returns it for BookingId.
    private Booking SetupBooking(BookingStatus status = BookingStatus.Active)
    {
        var booking = new Booking { Id = BookingId, UserId = OwnerId, Status = status };

        _bookings.Setup(b => b.GetByIdAsync(BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        return booking;
    }

    private void VerifySavedTimes(Times times) =>
        _bookings.Verify(b => b.SaveChangesAsync(It.IsAny<CancellationToken>()), times);

    [Theory]
    [InlineData(OwnerId, false)]      // the owner cancels their own booking
    [InlineData(StrangerId, true)]    // an admin cancels someone else's booking
    [InlineData(OwnerId, true)]       // an admin cancels their own booking
    public async Task CancelAsync_WhenCallerIsTheOwnerOrAnAdmin_CancelsAndSavesOnce(
        int requesterId, bool isAdmin)
    {
        var booking = SetupBooking();
        var service = CreateService();

        await service.CancelAsync(BookingId, requesterId, isAdmin, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        VerifySavedTimes(Times.Once());
    }

    [Fact]
    public async Task CancelAsync_WhenCallerIsAnotherNonAdminUser_ThrowsForbiddenAndChangesNothing()
    {
        var booking = SetupBooking();
        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CancelAsync(BookingId, StrangerId, isAdmin: false, CancellationToken.None));

        Assert.Equal(BookingStatus.Active, booking.Status);
        VerifySavedTimes(Times.Never());
    }

    [Fact]
    public async Task CancelAsync_WhenTheBookingDoesNotExist_ThrowsNotFound()
    {
        _bookings.Setup(b => b.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);
        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.CancelAsync(BookingId, OwnerId, isAdmin: false, CancellationToken.None));

        VerifySavedTimes(Times.Never());
    }

    [Fact]
    public async Task CancelAsync_WhenTheBookingIsAlreadyCancelled_DoesNothingAndDoesNotThrow()
    {
        var booking = SetupBooking(BookingStatus.Cancelled);
        var service = CreateService();

        await service.CancelAsync(BookingId, OwnerId, isAdmin: false, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        VerifySavedTimes(Times.Never());   // nothing changed, so there is nothing to save
    }

    [Fact]
    public async Task CancelAsync_WhenAStrangerCancelsAnAlreadyCancelledBooking_StillThrowsForbidden()
    {
        // Permissions are checked before the "already cancelled" shortcut,
        // so a stranger can never learn anything from a silent success.
        SetupBooking(BookingStatus.Cancelled);
        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CancelAsync(BookingId, StrangerId, isAdmin: false, CancellationToken.None));
    }
}
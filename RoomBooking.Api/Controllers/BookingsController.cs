using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBooking.Application.Bookings;
using RoomBooking.Application.Exceptions;
using RoomBooking.Domain.Enums;
using RoomBooking.Application.Common;

namespace RoomBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]   // any logged-in user (User or Admin) can use these endpoints
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings)
    {
        _bookings = bookings;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request, CancellationToken ct)
    {
        // The booking owner always comes from the token, never from the request body.
        var booking = await _bookings.CreateAsync(GetCurrentUserId(), request, ct);

        // Returns 201 Created with a Location header pointing to GET api/bookings/{id}.
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingResponse>>> Search(
    [FromQuery] BookingSearchRequest request, CancellationToken ct)
    => Ok(await _bookings.SearchAsync(request, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(int id, CancellationToken ct)
        => Ok(await _bookings.GetByIdAsync(id, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var isAdmin = User.IsInRole(nameof(UserRole.Admin));
        await _bookings.CancelAsync(id, GetCurrentUserId(), isAdmin, ct);
        return NoContent();
    }

    // Reads the user id from the "sub" claim written by TokenService.
    private int GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;

        return int.TryParse(sub, out var id)
            ? id
            : throw new InvalidCredentialsException("The token does not contain a valid user id.");
    }
}
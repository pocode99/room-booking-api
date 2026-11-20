namespace RoomBooking.Application.Exceptions;

// Thrown when the caller is authenticated but not allowed to do this (maps to 403).
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
namespace RoomBooking.Application.Exceptions;

// Thrown when a request is well-formed but breaks a business rule (maps to 400).
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
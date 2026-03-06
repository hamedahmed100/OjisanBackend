namespace OjisanBackend.Domain.Exceptions;

public class BadgeCountValidationException : Exception
{
    public BadgeCountValidationException(string message) : base(message) { }
}

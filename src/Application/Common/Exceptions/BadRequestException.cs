namespace OjisanBackend.Application.Common.Exceptions;

/// <summary>
/// Exception for invalid request data (400 Bad Request).
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message)
        : base(message)
    {
    }

    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

namespace OjisanBackend.Domain.Exceptions;

public class BadgeCommentValidationException : Exception
{
    public BadgeCommentValidationException(string message) : base(message) { }
}

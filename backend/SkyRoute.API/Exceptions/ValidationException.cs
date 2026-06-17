namespace SkyRoute.API.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors, string message = "Validation Failed")
        : base(message)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public IDictionary<string, string[]> Errors { get; }
}

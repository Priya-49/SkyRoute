namespace SkyRoute.API.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message = "The requested resource was not found.")
        : base(message)
    {
    }
}

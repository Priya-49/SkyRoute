namespace SkyRoute.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(id));
        }

        Id = id;
        Email = RequireValue(email, nameof(email));
        PasswordHash = RequireValue(passwordHash, nameof(passwordHash));
        FirstName = RequireValue(firstName, nameof(firstName));
        LastName = RequireValue(lastName, nameof(lastName));
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    private static string RequireValue(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        }

        return value.Trim();
    }
}

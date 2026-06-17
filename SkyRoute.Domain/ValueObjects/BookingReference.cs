using System.Text.RegularExpressions;

namespace SkyRoute.Domain.ValueObjects;

public sealed class BookingReference : IEquatable<BookingReference>
{
    private static readonly Regex FormatRegex = new("^SKY-[A-Z0-9]{7}$", RegexOptions.Compiled);

    public BookingReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Booking reference cannot be null or whitespace.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!FormatRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Booking reference must match format SKY-XXXXXXX using uppercase alphanumeric characters.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as BookingReference);

    public bool Equals(BookingReference? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
}

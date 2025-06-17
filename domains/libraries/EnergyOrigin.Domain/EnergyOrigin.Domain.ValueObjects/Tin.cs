namespace EnergyOrigin.Domain.ValueObjects;

public class Tin : ValueObject
{
    public string Value { get; }

    private Tin()
    {
        Value = String.Empty;
    }

    private Tin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));
        }

        if (value.Length != 8 || !IsDigitsOnly(value))
        {
            throw new ArgumentException("Value must be exactly 8 digits.", nameof(value));
        }

        Value = value;
    }

    private static bool IsDigitsOnly(string str) => str.All(char.IsDigit);

    public static Tin Create(string value)
    {
        return new Tin(value);
    }

    public static Tin? TryParse(string? value)
    {
        try
        {
            return new Tin(value);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Tin Empty()
    {
        return new Tin();
    }

    public bool IsEmpty()
    {
        return this == Empty();
    }

    public override string ToString()
    {
        return Value;
    }
}

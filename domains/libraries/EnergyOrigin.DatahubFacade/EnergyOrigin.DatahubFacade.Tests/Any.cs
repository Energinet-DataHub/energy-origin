using EnergyOrigin.Domain.ValueObjects;

namespace EnergyOrigin.DatahubFacade.Tests;

public class Any
{
    public static Gsrn Gsrn()
    {
        return new Gsrn("57" + IntString(16));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }
}

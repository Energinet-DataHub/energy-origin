using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.Memory;

internal class MemoryPointer
{
    public readonly long Issued;
    public readonly long Fraction;
    public string Serialized => $"{Issued}-{Fraction}";

    internal MemoryPointer(long issued, long fraction)
    {
        Issued = issued;
        Fraction = fraction;
    }

    internal MemoryPointer(string pointer) => (Issued, Fraction) = Parse(pointer);

    internal MemoryPointer(InternalEvent model)
    {
        Issued = model.Issued;
        Fraction = model.IssuedFraction;
    }

    internal bool IsAfter(MemoryPointer other) => Issued > other.Issued || (Issued == other.Issued && Fraction > other.Fraction);

    private static (long issued, long fraction) Parse(string pointer)
    {
        long issued;
        long fraction;
        try
        {
            var parts = pointer.Split('-');
            issued = long.Parse(parts[0]);
            fraction = long.Parse(parts[1]);
        }
        catch (Exception)
        {
            throw new InvalidDataException($"Pointer '{pointer}' not a valid format");
        }
        return (issued, fraction);
    }
}

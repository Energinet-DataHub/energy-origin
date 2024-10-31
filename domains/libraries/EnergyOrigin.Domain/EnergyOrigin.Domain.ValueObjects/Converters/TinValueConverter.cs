using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyOrigin.Domain.ValueObjects.Converters;

public class TinValueConverter() : ValueConverter<Tin, string>(v => v.Value, v => String.IsNullOrWhiteSpace(v) ? Tin.Empty() : Tin.Create(v));

public class NullableTinValueConverter()
    : ValueConverter<Tin?, string?>(v => v != null ? v.Value : null, v => String.IsNullOrWhiteSpace(v) ? null : Tin.Create(v));

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyOrigin.Domain.ValueObjects.Converters;

public class UnixTimestampValueToSecondsConverter() : ValueConverter<UnixTimestamp, long>(v => v.EpochSeconds, v => UnixTimestamp.Create(v));

public class UnixTimestampValueToDateTimeOffsetConverter()
    : ValueConverter<UnixTimestamp, DateTimeOffset>(v => v.ToDateTimeOffset(), v => UnixTimestamp.Create(v));

public class NullableUnixTimestampValueToDateTimeOffsetConverter()
    : ValueConverter<UnixTimestamp?, DateTimeOffset?>(v => v != null ? v.ToDateTimeOffset() : null,
        v => v != null ? UnixTimestamp.Create(v.Value) : UnixTimestamp.Empty());

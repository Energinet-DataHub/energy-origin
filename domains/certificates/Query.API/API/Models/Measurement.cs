using System;
using Energinet.DataHub.Measurements.Abstractions.Api.Models;

namespace API.Models;

public record Measurement
{
    public required string Gsrn { get; init; }
    public required long DateFrom { get; init; }
    public required long DateTo { get; init; }
    public decimal Quantity { get; init; } //in kWh
    public EnergyQuality Quality { get; init; }

    public bool IsQuantityMissing => Quality == EnergyQuality.Missing;
}

public enum EnergyQuality
{
    Measured = 0,
    Estimated = 1,
    Calculated = 2,
    Missing = 3
}

public static class MeasurementExtensions
{
    public static EnergyQuality ToEnergyQuality(this Quality quality) =>
        quality switch
        {
            Quality.Measured => EnergyQuality.Measured,
            Quality.Estimated => EnergyQuality.Estimated,
            Quality.Calculated => EnergyQuality.Calculated,
            Quality.Missing => EnergyQuality.Missing,
            _ => throw new ArgumentException($"Unknown quality: {quality}")
        };

    public static uint ToWattHours(this decimal kiloWattHours) => (uint)Math.Round(kiloWattHours * 1000);
}

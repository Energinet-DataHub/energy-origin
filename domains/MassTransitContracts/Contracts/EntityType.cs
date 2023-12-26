using System.Text.Json.Serialization;

namespace MassTransitContracts.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityType
{
    TransferAgreements,
    MeteringPoints,
    Measurements
}

using System.Text.Json.Serialization;

namespace API.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityType
{
    TransferAgreements,
    MeteringPoints,
    Measurements
}

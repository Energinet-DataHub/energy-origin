using System.Text.Json.Serialization;

namespace API.Models
{
    public enum QuantityUnit
    {
        Wh,
        kWh,
        MWh,
        GWh,

        W,
        kW,
        MW,
        GW,

        mg,
        g,
        kg,
        Mg,

        [JsonPropertyName("mg/kWh")]
        mgPerkWh,
        [JsonPropertyName("g/kWh")]
        gPerkWh,
        [JsonPropertyName("kg/kWh")]
        kgPerkWh
    }
}

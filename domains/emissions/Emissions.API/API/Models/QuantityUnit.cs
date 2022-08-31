using System.ComponentModel;

namespace API.Models
{
    public enum QuantityUnit
    {
        [Description("Wh")]
        Wh,

        [Description("kWh")]
        kWh,

        [Description("MWh")]
        MWh,

        [Description("GWh")]
        GWh,

        [Description("kW")]
        kW,

        [Description("MW")]
        MW,

        [Description("GW")]
        GW,

        [Description("mg")]
        mg,

        [Description("g")]
        g,

        [Description("kg")]
        kg,

        [Description("Mg")]
        Mg,

        [Description("mg/kWh")]
        mgPerkWh,

        [Description("g/kWh")]
        gPerkWh,

        [Description("kg/kWh")]
        kgPerkWh,
    }
}

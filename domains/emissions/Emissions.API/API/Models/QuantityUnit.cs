using System.ComponentModel;

namespace API.Models
{
    public enum QuantityUnit
    {
        [Description("Wh")]
        Wh = 1,

        [Description("kWh")]
        kWh = 2,

        [Description("MWh")]
        MWh = 3,

        [Description("GWh")]
        GWh = 3,

        [Description("kW")]
        kW = 4,

        [Description("MW")]
        MW = 5,

        [Description("GW")]
        GW = 6,

        [Description("mg")]
        mg = 7,

        [Description("g")]
        g = 8,

        [Description("kg")]
        kg = 9,

        [Description("Mg")]
        Mg = 10,

        [Description("mg/kWh")]
        mgPerkWh = 11,

        [Description("g/kWh")]
        gPerkWh = 12,

        [Description("kg/kWh")]
        kgPerkWh = 13,
    }
}

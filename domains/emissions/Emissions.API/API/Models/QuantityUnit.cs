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

        [Description("g")]
        g = 7,

        [Description("kg")]
        kg = 8,

        [Description("Mg")]
        Mg = 9,

        [Description("mg/kWh")]
        mgPerkWh = 10,

        [Description("g/kWh")]
        gPerkWh = 11,

        [Description("kg/kWh")]
        kgPerkWh = 12,
    }
}

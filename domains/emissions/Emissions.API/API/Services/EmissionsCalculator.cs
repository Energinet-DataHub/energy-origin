using API.Helpers;
using API.Models;

namespace API.Services;

class EmissionsCalculator : IEmissionsCalculator
{
    public Emissions CalculateTotalEmission(List<EmissionRecord> emissions,
        IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo)
    {
        float totalEmission = 0;
        uint totalConsumption = 0;
        float relativeCO2 = 0;
        
        foreach (var measurement in measurements)
        {
            var timeSeries = measurement.Measurements;
            totalConsumption += (uint)timeSeries.Sum(_ => _.Quantity);
            foreach (var emission in emissions)
            {
                var co2 = (emission.CO2PerkWh * timeSeries.First(_ => emission.GridArea ==  measurement.MeteringPoint.GridArea && DateTimeUtil.ToUtcDateTime(_.DateFrom) == emission.HourUTC).Quantity)/1000;
                relativeCO2 = 
                totalEmission += co2;
            }
        }

        relative = totalEmission / totalConsumption;
        return new Emissions
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Total = new Total {CO2 = totalEmission},
            Relative = new Relative {CO2 = relativeCO2},
        };
    }
}
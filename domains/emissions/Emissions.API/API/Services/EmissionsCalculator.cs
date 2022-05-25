using System.Linq.Expressions;
using API.Helpers;
using API.Models;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

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
                var co2 = emission.CO2PerkWh * timeSeries.First(_ => emission.GridArea ==  measurement.MeteringPoint.GridArea && _.DateFrom.ToUtcDateTime() == emission.HourUTC).Quantity;
                //relativeCO2 = 
                totalEmission += co2;
            }
        }

        relative = totalEmission / totalConsumption;
        return new Emissions
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Total = new Total {CO2 = totalEmission/1000},
            Relative = new Relative {CO2 = relativeCO2},
        };
    }
    
    public IEnumerable<Emissions> CalculateEmission(List<EmissionRecord> emissions,
        IEnumerable<TimeSeries> measurements, long dateFrom, long dateTo, Aggregation aggregation)
    {
        float totalEmission = 0;
        uint totalConsumption = 0;
        float relativeCO2 = 0;

        var listOfEmissions = new List<Emission>();

        foreach (var measurement in measurements)
        {
            var timeSeries = measurement.Measurements;
            totalConsumption += (uint)timeSeries.Sum(_ => _.Quantity);
            foreach (var emission in emissions)
            {
                var date = emission.HourUTC.ToUnixTime();
                var co2 = emission.CO2PerkWh * timeSeries.First(_ => emission.GridArea ==  measurement.MeteringPoint.GridArea && _.DateFrom == date).Quantity;
                listOfEmissions.Add(new Emission {CO2 = co2, Date = emission.HourUTC});
                totalEmission += co2;
            }
        }

        //var test = true;
        //if (test == true)
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Day);
        //}
        //if (test == false)
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Year);
        //}

        //if (aggregation == Aggregation.Hour)
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Hour);
        //}
        IEnumerable<IGrouping<string, Emission>> groupedEmissions;

        // var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Year);


        switch (aggregation)
        {
            case Aggregation.Year:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Year.ToString());
                break;

            case Aggregation.Month:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString("yyyy/MM"));
                break;

            case Aggregation.Day:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString("yyyy/MM/dd"));
                break;

            case Aggregation.Hour:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString("yyyy/MM/dd/HH"));
                break;

            case Aggregation.Quarter:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString("yyyy/MM/dd/HH/mm"));  // We need to make this for each quarter
                break;

            case Aggregation.Actual:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString("yyyy/MM/dd/HH"));
                break;

            case Aggregation.Total:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date);
                break;

            default:
                groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.ToString());
                break;
        }

        //if (aggregation.Equals(Aggregation.Year))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Year);
        //}
        //else if (aggregation.Equals(Aggregation.Month))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Month);
        //}
        //else if (aggregation.Equals(Aggregation.Day))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Day);
        //}
        //else if (aggregation.Equals(Aggregation.Hour))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Hour);
        //}
        //else if (aggregation.Equals(Aggregation.Quarter))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.Q);
        //}
        //else if (aggregation.Equals(Aggregation.Actual))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date.);
        //}
        //else if (aggregation.Equals(Aggregation.Total))
        //{
        //    var groupedEmissions = listOfEmissions.GroupBy(_ => _.Date);
        //}



        float totalForBucket = 0;
        var bucketEmissions = new List<Emissions>();
        foreach (var groupedEmission in groupedEmissions)
        {
            totalForBucket = groupedEmission.Sum(_ => _.CO2);
            bucketEmissions.Add(new Emissions
            {
                DateFrom = groupedEmission.First().Date.ToUnixTime(),
                DateTo = groupedEmission.Last().Date.ToUnixTime(),
                Total = new Total {CO2 = totalForBucket/1000},
                Relative = new Relative {CO2 = relativeCO2},
            });
                
        }

        return bucketEmissions;
    }


}

internal class Emission
{
    public DateTime Date { get; set; }
    public float CO2 { get; set; }
} 


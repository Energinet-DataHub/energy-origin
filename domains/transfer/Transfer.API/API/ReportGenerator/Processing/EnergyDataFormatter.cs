using System.Collections.Generic;
using System.Linq;
using API.ReportGenerator.Domain;
using API.Transfer.Api.Services;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.WalletClient;

namespace API.ReportGenerator.Processing;

public interface IEnergyDataFormatter
{
    (IEnumerable<DataPoint> Consumption, IEnumerable<DataPoint> StrictProduction, IEnumerable<DataPoint> AllProduction) Format(List<ConsumptionHour> consumption, List<Claim> claims);
}

public class EnergyDataFormatter : IEnergyDataFormatter
{
    public (IEnumerable<DataPoint> Consumption, IEnumerable<DataPoint> StrictProduction, IEnumerable<DataPoint> AllProduction)
        Format(List<ConsumptionHour> consumption, List<Claim> claims)
    {
        var consumptionFormatted = consumption
            .OrderBy(x => x.HourOfDay)
            .Select(x => new DataPoint(x.HourOfDay, (double)x.KwhQuantity));

        var strictProduction = new List<DataPoint>(capacity: claims.Count);
        var allProduction = new List<DataPoint>(capacity: claims.Count);

        foreach (var claim in claims)
        {
            var prodTs = UnixTimestamp.Create(claim.ProductionCertificate.Start);
            var consTs = UnixTimestamp.Create(claim.ConsumptionCertificate.Start);
            var delta = consTs.EpochSeconds - prodTs.EpochSeconds;

            var hour = UnixTimestamp.Create(claim.UpdatedAt)
                .ToDateTimeOffset()
                .Hour;
            var dp = new DataPoint(hour, claim.Quantity);

            allProduction.Add(dp);

            if (delta < UnixTimestamp.SecondsPerHour)
            {
                strictProduction.Add(dp);
            }
        }

        return (
            Consumption: consumptionFormatted,
            StrictProduction: strictProduction,
            AllProduction: allProduction
        );
    }
}

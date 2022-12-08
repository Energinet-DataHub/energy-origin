using System;
using CertificateEvents.Primitives;

namespace API.MasterDataService;

public record MasterData(string GSRN, string GridArea, MeteringPointType Type, Technology Technology,
    string MeteringPointOwner, DateTimeOffset MeteringPointOnboardedStartDate);

public enum MeteringPointType
{
    Production,
    Consumption
}

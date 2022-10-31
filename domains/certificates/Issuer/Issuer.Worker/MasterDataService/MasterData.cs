using CertificateEvents.Primitives;

namespace Issuer.Worker.MasterDataService;

public record MasterData(string GSRN, string GridArea, MeteringPointType Type, Technology Technology,
    string MeteringPointOwner, bool MeteringPointOnboarded);

public enum MeteringPointType
{
    Production,
    Consumption
}

using CertificateEvents.Primitives;

namespace Issuer.Worker.MasterDataService;

public record MasterData(string GSRN, string GridArea, MeteringPointType Type, Technology Technology, string MeteringPointOwner,
    byte[] OwnerPublicKey);

public record MasterDataCollection(MasterData[] Data);

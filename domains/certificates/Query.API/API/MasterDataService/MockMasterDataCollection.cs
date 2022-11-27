using System;
using Baseline;
using System.Collections.Generic;
using System.Linq;
using CertificateEvents.Primitives;

namespace API.MasterDataService;

internal record MockMasterDataCollection(MockMasterData[] Data)
{
    public IEnumerable<string> GetAllGsrns() => Data.Select(d => d.GSRN);
};

public record MockMasterData(string GSRN, string GridArea, MeteringPointType Type, Technology Technology,
    string CVR, DateTimeOffset MeteringPointOnboardedStartDate);

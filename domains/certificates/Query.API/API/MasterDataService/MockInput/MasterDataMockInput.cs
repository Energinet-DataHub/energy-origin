using System;
using CertificateEvents.Primitives;

namespace API.MasterDataService.MockInput;

public record MasterDataMockInput(string GSRN, string GridArea, MeteringPointType Type, Technology Technology,
    string CVR, DateTimeOffset MeteringPointOnboardedStartDate);

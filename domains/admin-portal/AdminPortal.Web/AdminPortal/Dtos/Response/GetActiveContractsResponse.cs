using System.Collections.Generic;
using AdminPortal.Models;

namespace AdminPortal.Dtos.Response;

public class GetActiveContractsResponse
{
    public required ResultsData Results { get; set; }
}

public class ResultsData
{
    public required List<MeteringPoint> MeteringPoints { get; set; }
}

public class MeteringPoint
{
    public required string GSRN { get; set; }
    public MeteringPointType MeteringPointType { get; set; }
    public required string OrganizationName { get; set; }
    public required string Tin { get; set; }
    public long Created { get; set; }
    public long StartDate { get; set; }
    public long? EndDate { get; set; }
}

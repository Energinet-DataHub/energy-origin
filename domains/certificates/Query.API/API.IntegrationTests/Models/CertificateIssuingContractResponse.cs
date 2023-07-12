using System;
using CertificateValueObjects;

namespace API.IntegrationTests.Models;

//TODO: Delete this
public class CertificateIssuingContractResponse
{
    public Guid Id { get; set; }
    public int ContractNumber { get; set; } = 0;
    public string GSRN { get; set; } = "";
    public string GridArea { get; set; } = "";
    public MeteringPointType MeteringPointType { get; set; }
    public string MeteringPointOwner { get; set; } = "";
    public long StartDate { get; set; }
    public long EndDate { get; set; }
    public long Created { get; set; }
}

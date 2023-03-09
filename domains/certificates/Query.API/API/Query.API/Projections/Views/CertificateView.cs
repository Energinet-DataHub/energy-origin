using System;

namespace API.Query.API.Projections.Views;

public class CertificateView
{
    public Guid CertificateId { get; set; }
    public long DateFrom { get; set; }
    public long DateTo { get; set; }
    public long Quantity { get; set; }
    public string GSRN { get; set; } = "";
    public string GridArea { get; set; } = "";
    public string TechCode { get; set; } = "";
    public string FuelCode { get; set; } = "";
    public CertificateStatus Status { get; set; }
}

public enum CertificateStatus
{
    Creating = 1,
    Issued = 2,
    Rejected = 3
};

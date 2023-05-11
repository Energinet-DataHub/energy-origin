using System;
using System.Collections.Generic;
using System.Linq;
using API.Query.API.ApiModels.Responses;
using Marten.Schema;

namespace API.Query.API.Projections.Views;

public class CertificatesByOwnerView
{
    [Identity] public string Owner { get; set; } = "";

    public Dictionary<Guid, CertificateView> Certificates { get; set; } = new();

    public CertificateList ToApiModel()
    {
        var certificates = Certificates.Values
            //TODO What to do with Creating and Rejected certificates?
            .Where(x => x.Status == CertificateStatus.Issued)
            .Select(c => new Certificate
            {
                Id = c.CertificateId,
                GSRN = c.GSRN,
                GridArea = c.GridArea,
                DateFrom = c.DateFrom,
                DateTo = c.DateTo,
                Quantity = c.Quantity,
                TechCode = c.TechCode,
                FuelCode = c.FuelCode
            });

        return new CertificateList
        {
            Result = certificates
                .OrderByDescending(c => c.DateFrom)
                .ThenBy(c => c.GSRN)
                .ToArray()
        };
    }
}

using System;
using System.Collections.Generic;
using AdminPortal.Models;

namespace AdminPortal.Dtos.Response;

public record GetContractsForAdminPortalResponseItem(
    Guid? Id,
    string GSRN,
    string MeteringPointOwner,
    long Created,
    long StartDate,
    long? EndDate,
    MeteringPointType MeteringPointType
);

public record GetContractsForAdminPortalResponse(IEnumerable<GetContractsForAdminPortalResponseItem> Result);



using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.UnitOfWork;
using DataContext.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace API.ContractService.Internal;

public class GetContractsForAdminPortalQueryHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<GetContractsForAdminPortalQuery, GetContractsForAdminPortalQueryResult>
{
    public async Task<GetContractsForAdminPortalQueryResult> Handle(GetContractsForAdminPortalQuery request, CancellationToken cancellationToken)
    {
        var resultItems = await unitOfWork.CertificateIssuingContractRepo.Query()
            .AsNoTracking()
            .Where(c => c.StartDate <= DateTimeOffset.UtcNow && (c.EndDate == null || c.EndDate >= DateTimeOffset.UtcNow))
            .GroupBy(c => c.GSRN)
            .Select(g =>
                    g.OrderByDescending(x => x.Created)
                        .Select(x => new GetContractsForAdminPortalQueryResultItem(
                            x.Id,
                            x.GSRN,
                            x.MeteringPointOwner,
                            x.Created,
                            x.StartDate,
                            x.EndDate,
                            x.MeteringPointType))
                        .First()
            )
            .ToListAsync(cancellationToken);

        return new GetContractsForAdminPortalQueryResult(resultItems);
    }
}

public record GetContractsForAdminPortalQuery() : IRequest<GetContractsForAdminPortalQueryResult>;

public record GetContractsForAdminPortalQueryResult(List<GetContractsForAdminPortalQueryResultItem> Result);

public record GetContractsForAdminPortalQueryResultItem(
    Guid Id,
    string GSRN,
    string MeteringPointOwner,
    DateTimeOffset Created,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    MeteringPointType MeteringPointType
);

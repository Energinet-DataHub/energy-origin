using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public class GetMeteringPointsQuery : IRequest<GetMeteringPointsQueryResult>
{

}

public class GetMeteringPointsQueryResult(List<GetMeteringPointsQueryResultItem> viewModel)
{
    public List<GetMeteringPointsQueryResultItem> ViewModel = viewModel;
}

public record GetMeteringPointsQueryResultItem(string GSRN, MeteringPointType MeterType, string OrganizationName, string Tin, bool ActiveContract)
{
}

public class GetMeteringPointsQueryHandler(
    IMeasurementsService measurementsService,
    IAuthorizationService authorizationService,
    ICertificatesService certificatesService)
    : IRequestHandler<GetMeteringPointsQuery, GetMeteringPointsQueryResult>
{
    public async Task<GetMeteringPointsQueryResult> Handle(GetMeteringPointsQuery request, CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);
        var meteringpoints = await measurementsService.GetMeteringPointsHttpRequestAsync(organizations.Result.Select(x => x.OrganizationId).ToList());

        var contracts = await certificatesService.GetContractsHttpRequestAsync();

        var result = meteringpoints.Result
            .Select(meteringpoint =>
            {
                var organization = organizations
                    .Result
                    .First(org => org.Tin.ToString() == meteringpoint.ConsumerCvr);
                var activeContract = contracts
                    .Result
                    .Any(contract => contract.GSRN == meteringpoint.GSRN);

                return new GetMeteringPointsQueryResultItem(
                    meteringpoint.GSRN,
                    meteringpoint.MeterType,
                    organization.OrganizationName,
                    organization.Tin,
                    activeContract
                );
            }
                )
            .ToList();

        return new GetMeteringPointsQueryResult(result);
    }
}

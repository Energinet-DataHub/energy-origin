using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Models;
using AdminPortal.Services;
using MediatR;

namespace AdminPortal._Features_;

public record GetMeteringPointsQuery(string Tin) : IRequest<GetMeteringPointsQueryResult>
{
}

public class GetMeteringPointsQueryResult(List<GetMeteringPointsQueryResultItem> viewModel)
{
    public List<GetMeteringPointsQueryResultItem> ViewModel = viewModel;
}

public record GetMeteringPointsQueryResultItem(
    string GSRN,
    MeteringPointType MeterType,
    string OrganizationName,
    string Tin,
    bool ActiveContract)
{
}

public class GetMeteringPointsQueryHandler(
    IMeasurementsService measurementsService,
    IAuthorizationService authorizationService,
    ICertificatesService certificatesService)
    : IRequestHandler<GetMeteringPointsQuery, GetMeteringPointsQueryResult>
{
    public async Task<GetMeteringPointsQueryResult> Handle(GetMeteringPointsQuery request,
        CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);
        var selectedOrganization = organizations
            .Result
            .SingleOrDefault(org => org.Tin == request.Tin);

        if (selectedOrganization == null)
        {
            return new GetMeteringPointsQueryResult([]);
        }

        try
        {
            var meteringpoints =
                await measurementsService.GetMeteringPointsHttpRequestAsync(selectedOrganization.OrganizationId);

            var contracts = await certificatesService.GetContractsHttpRequestAsync();

            var result = meteringpoints.Result
                .Select(meteringpoint =>
                    {

                        var activeContract = contracts
                            .Result
                            .Any(contract => contract.GSRN == meteringpoint.GSRN);

                        return new GetMeteringPointsQueryResultItem(
                            meteringpoint.GSRN,
                            meteringpoint.MeterType,
                            selectedOrganization.OrganizationName,
                            selectedOrganization.Tin,
                            activeContract
                        );
                    }
                )
                .ToList();
            return new GetMeteringPointsQueryResult(result);

        }
        catch (Exception)
        {
            return new GetMeteringPointsQueryResult([]);
        }
    }
}

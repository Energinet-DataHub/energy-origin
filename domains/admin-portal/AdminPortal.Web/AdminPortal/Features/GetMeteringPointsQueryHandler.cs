using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Models;
using AdminPortal.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminPortal.Features;

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
    ICertificatesService certificatesService,
    ILogger<GetMeteringPointsQueryHandler> logger)
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

            if (meteringpoints.Result.Count == 0)
            {
                return new GetMeteringPointsQueryResult([]);
            }

            var contracts = await certificatesService.GetContractsHttpRequestAsync();

            var result = meteringpoints.Result
                .Select(meteringpoint =>
                    {

                        var activeContract = contracts
                            .Result
                            .Any(contract => contract.GSRN == meteringpoint.GSRN);

                        return new GetMeteringPointsQueryResultItem(
                            meteringpoint.GSRN,
                            meteringpoint.Type,
                            selectedOrganization.OrganizationName,
                            selectedOrganization.Tin,
                            activeContract
                        );
                    }
                )
                .ToList();
            return new GetMeteringPointsQueryResult(result);

        }
        catch (Exception e)
        {
            logger.LogError("Something went wrong when getting metering points: {Message}", e.Message);
            return new GetMeteringPointsQueryResult([]);
        }
    }
}

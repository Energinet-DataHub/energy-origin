using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Models;
using AdminPortal.Services;
using MediatR;
using Microsoft.Extensions.Logging;

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
    ICertificatesService certificatesService,
    ILogger<GetMeteringPointsQueryHandler> logger)
    : IRequestHandler<GetMeteringPointsQuery, GetMeteringPointsQueryResult>
{
    public async Task<GetMeteringPointsQueryResult> Handle(GetMeteringPointsQuery request,
        CancellationToken cancellationToken)
    {
        var organizations = await authorizationService.GetOrganizationsAsync(cancellationToken);

        logger.LogInformation("Get organizations: {@Organizations}", JsonSerializer.Serialize(organizations.Result));

        var selectedOrganization = organizations
            .Result
            .SingleOrDefault(org => org.Tin == request.Tin);

        logger.LogInformation("Selected organization: {@Organization}", JsonSerializer.Serialize(selectedOrganization));

        if (selectedOrganization == null)
        {
            logger.LogWarning("Could not find organization {Tin}", request.Tin);
            return new GetMeteringPointsQueryResult([]);
        }

        try
        {
            var meteringpoints =
                await measurementsService.GetMeteringPointsHttpRequestAsync(selectedOrganization.OrganizationId);

            logger.LogInformation("Meteringpoints: {@Meteringpoints}", JsonSerializer.Serialize(meteringpoints));

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
        catch (Exception e)
        {
            logger.LogWarning("Could not get metering points for organization {OrganizationId}: {Message}", selectedOrganization.OrganizationId, e.Message);
            return new GetMeteringPointsQueryResult([]);
        }
    }
}

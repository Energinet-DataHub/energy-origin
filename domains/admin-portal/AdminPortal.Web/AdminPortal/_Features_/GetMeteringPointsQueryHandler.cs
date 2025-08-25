using System;
using System.Collections.Generic;
using System.Linq;
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

public class GetMeteringPointsQueryResult(GetMeteringPointsQueryResultViewModel viewModel)
{
    public GetMeteringPointsQueryResultViewModel ViewModel = viewModel;
}

public class GetMeteringPointsQueryResultViewModel(Guid organizationId, string orgName, string tin, List<GetMeteringPointsQueryResultItem> meteringPoints)
{
    public string OrganizationName = orgName;
    public string Tin = tin;
    public List<GetMeteringPointsQueryResultItem> MeteringPoints = meteringPoints;
    public Guid OrganizationId { get; set; } = organizationId;
}

public record GetMeteringPointsQueryResultItem(
    string GSRN,
    MeteringPointType MeterType,
    string Address,
    string BiddingZone,
    string GridArea,
    string SubMeterType,
    string Technology,
    string Capacity,
    Guid? ContractId,
    bool ActiveContract,
    bool CanBeUsedForIssuingCertificates)
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
            return new GetMeteringPointsQueryResult(new GetMeteringPointsQueryResultViewModel(Guid.Empty, "", "", []));
        }

        try
        {
            var meteringpoints =
                await measurementsService.GetMeteringPointsHttpRequestAsync(selectedOrganization.OrganizationId);

            if (meteringpoints.Result.Count == 0)
            {
                return new GetMeteringPointsQueryResult(new GetMeteringPointsQueryResultViewModel(selectedOrganization.OrganizationId, selectedOrganization.OrganizationName, selectedOrganization.Tin, []));
            }

            var contracts = await certificatesService.GetContractsHttpRequestAsync();

            var result = meteringpoints.Result
                .Select(meteringpoint =>
                    {

                        var contract = contracts
                            .Result
                            .FirstOrDefault(contract => contract.GSRN == meteringpoint.GSRN);
                        var activeContract = contract != null;

                        return new GetMeteringPointsQueryResultItem(
                            meteringpoint.GSRN,
                            meteringpoint.MeteringPointType,
                            meteringpoint.Address.ToString(),
                            meteringpoint.BiddingZone,
                            meteringpoint.GridArea,
                            meteringpoint.SubMeterType.ToString(),
                            meteringpoint.Technology.ToString(),
                            meteringpoint.Capacity,
                            contract?.Id,
                            activeContract,
                            meteringpoint.CanBeUsedForIssuingCertificates
                        );
                    }
                )
                .ToList();
            return new GetMeteringPointsQueryResult(new GetMeteringPointsQueryResultViewModel(selectedOrganization.OrganizationId, selectedOrganization.OrganizationName, selectedOrganization.Tin, result));

        }
        catch (Exception e)
        {
            logger.LogError("Something went wrong when getting metering points: {Message}", e.Message);
            return new GetMeteringPointsQueryResult(new GetMeteringPointsQueryResultViewModel(Guid.Empty, "", "", []));
        }
    }
}

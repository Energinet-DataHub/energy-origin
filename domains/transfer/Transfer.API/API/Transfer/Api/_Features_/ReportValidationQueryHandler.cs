using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Common;
using MediatR;
using Meteringpoint.V1;
using static Meteringpoint.V1.Meteringpoint;

namespace API.Transfer.Api._Features_;


public class ReportValidationQueryHandler(MeteringpointClient meteringpointClient)
    : IRequestHandler<ReportValidationQuery, ReportValidationQueryResult>
{
    public async Task<ReportValidationQueryResult> Handle(ReportValidationQuery request, CancellationToken cancellationToken)
    {
        var ownedMeteringPoints = await meteringpointClient.GetOwnedMeteringPointsAsync(
            new OwnedMeteringPointsRequest
            {
                Subject = request.OrganizatonId.ToString()
            },
            cancellationToken: cancellationToken);

        var anyConsumptionMeteringPoints = ownedMeteringPoints.MeteringPoints
            .Any(mp => MeteringPointTypeHelper.IsConsumption(mp.TypeOfMp));

        if (!anyConsumptionMeteringPoints)
        {
            return new ReportValidationQueryResult(
                    Valid: false,
                    "No consumption metering points found for the organization. Report generation is only available for organizations with at least one consumption metering point.");
        }

        return new ReportValidationQueryResult(Valid: true, null);
    }
}

public record ReportValidationQuery(Guid OrganizatonId) : IRequest<ReportValidationQueryResult>;
public record ReportValidationQueryResult(bool Valid, string? ErrorMessage);

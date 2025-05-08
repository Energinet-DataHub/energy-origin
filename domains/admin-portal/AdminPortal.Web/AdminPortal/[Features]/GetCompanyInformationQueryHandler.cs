using System;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Dtos.Response;
using AdminPortal.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdminPortal._Features_;

public class GetCompanyInformationQueryHandler(
    ITransferService transferService,
    ILogger<GetCompanyInformationQueryHandler> logger)
    : IRequestHandler<GetCompanyInformationQuery, GetCompanyInformationResponse?>
{
    public async Task<GetCompanyInformationResponse?> Handle(
        GetCompanyInformationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var companyInformation = await transferService.GetCompanyInformation(request.Tin);
            return new GetCompanyInformationResponse
            {
                Name = companyInformation.Name,
                Tin = companyInformation.Tin,
                Address = companyInformation.Address,
                City = companyInformation.City,
                ZipCode = companyInformation.ZipCode
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error when getting CVR information");
            return null;
        }
    }
}

public record GetCompanyInformationQuery(string Tin) : IRequest<GetCompanyInformationResponse?>;

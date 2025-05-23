using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Cvr.Api.Clients.Cvr;
using API.Cvr.Api.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Cvr.Api._Features_.Internal;

public class GetCvrCompaniesQueryHandler(ICvrClient client, ILogger<GetCvrCompaniesQueryHandler> logger)
    : IRequestHandler<GetCvrCompaniesQuery, GetCvrCompaniesQueryResult>
{
    public async Task<GetCvrCompaniesQueryResult> Handle(GetCvrCompaniesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var root = await client.CvrNumberSearch([.. request.Tins.Select(t => new CvrNumber(t))]);
            if (root?.hits?.hits is null)
            {
                return new GetCvrCompaniesQueryResult([]);
            }

            var companies = root.hits.hits
                .Select(hit => ToResultItem(hit._source?.Vrvirksomhed))
                .ToList();


            return new GetCvrCompaniesQueryResult(companies);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error when getting CVR information for multiple companies");
            return new GetCvrCompaniesQueryResult([]);
        }
    }

    private static GetCvrCompaniesQueryResultItem ToResultItem(Vrvirksomhed? vrvirksomhed)
    {
        return new GetCvrCompaniesQueryResultItem(
                vrvirksomhed?.cvrNummer?.ToString() ?? string.Empty,
                vrvirksomhed?.virksomhedMetadata?.nyesteNavn?.navn ?? string.Empty);
    }
}

public record GetCvrCompaniesQuery(List<string> Tins) : IRequest<GetCvrCompaniesQueryResult>;

public record GetCvrCompaniesQueryResult(List<GetCvrCompaniesQueryResultItem> Result);

public record GetCvrCompaniesQueryResultItem(string Tin, string Name);


using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.Cvr.Api.Clients.Cvr;
using API.Cvr.Api.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Cvr.Api._Features_.Internal;

public class GetCvrCompanyQueryHandler(ICvrClient client, ILogger<GetCvrCompanyQueryHandler> logger)
    : IRequestHandler<GetCvrCompanyQuery, GetCvrCompanyQueryResult?>
{
    public async Task<GetCvrCompanyQueryResult?> Handle(GetCvrCompanyQuery request, CancellationToken cancellationToken)
    {
        var cvrNumber = new CvrNumber(request.Tin);
        try
        {
            var root = await client.CvrNumberSearch([cvrNumber]);
            var company = root?.hits?.hits?.First()._source?.Vrvirksomhed;
            if (company?.cvrNummer is null || company.virksomhedMetadata?.nyesteNavn?.navn is null)
            {
                return null;
            }

            var address = company.virksomhedMetadata.nyesteBeliggenhedsadresse;
            var completeAddress = GetAddress(address);

            return new GetCvrCompanyQueryResult(
                company.cvrNummer.ToString() ?? string.Empty,
                company.virksomhedMetadata.nyesteNavn.navn,
                address?.bynavn ?? string.Empty,
                address?.postnummer.ToString() ?? string.Empty,
                completeAddress);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error when getting CVR information");
            return null;
        }
    }

    private static string GetAddress(NyesteBeliggenhedsadresse? address)
    {
        if (address is null) return string.Empty;

        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(address.vejnavn)) stringBuilder.Append(address.vejnavn).Append(' ');
        if (address.husnummerFra is not null) stringBuilder.Append(address.husnummerFra).Append(' ');

        if (!string.IsNullOrWhiteSpace(address.bogstavFra)) stringBuilder.Append(address.bogstavFra).Append(' ');

        if (!string.IsNullOrWhiteSpace(address.etage))
        {
            stringBuilder.Append(address.etage).Append(' ');
            if (!string.IsNullOrWhiteSpace(address.sidedoer)) stringBuilder.Append(address.sidedoer).Append(' ');
        }

        return stringBuilder.ToString().TrimEnd();
    }
}

public record GetCvrCompanyQueryResult(string Tin, string Name, string City, string ZipCode, string Address);

public record GetCvrCompanyQuery(string Tin) : IRequest<GetCvrCompanyQueryResult?>;

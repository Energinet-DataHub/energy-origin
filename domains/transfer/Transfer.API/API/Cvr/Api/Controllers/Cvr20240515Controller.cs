using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Cvr.Api.Clients.Cvr;
using API.Cvr.Api.Dto.Requests;
using API.Cvr.Api.Dto.Responses;
using API.Cvr.Api.Models;
using API.Transfer.Api.Controllers;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Cvr.Api.Controllers;

[Authorize(Policy.Frontend)]
[ApiController]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
[Route("api/transfer/cvr")]
public class Cvr20240515Controller(CvrClient client) : Controller
{
    /// <summary>
    /// Get CVR registered company information for multiple CVR numbers
    /// </summary>
    /// <response code="200">Successful operation</response>
    /// <response code="400">Bad request</response>
    [ProducesResponseType(typeof(CvrCompanyListResponse), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [HttpPost]
    public async Task<ActionResult<CvrCompanyListResponse>> GetCvrCompanies([FromBody] CvrRequestDto requestDto)
    {
        var parsedCvrNumbers = requestDto.CvrNumbers.Select(CvrNumber.TryParse).OfType<CvrNumber>().ToList();

        if (!parsedCvrNumbers.Any())
            return Ok(new CvrCompanyListResponse(new List<CvrCompanyDto>()));

        var searchResult = await client.CvrNumberSearch(parsedCvrNumbers);
        return Ok(CreateCvrCompanyListResponse(searchResult));
    }

    private CvrCompanyListResponse CreateCvrCompanyListResponse(Root? searchResult)
    {
        if (searchResult?.hits?.hits == null)
            return new CvrCompanyListResponse(new List<CvrCompanyDto>());

        var companies = searchResult.hits.hits
            .Select(hit => ToDto(hit._source?.Vrvirksomhed))
            .Where(dto => dto != null)
            .Cast<CvrCompanyDto>()
            .ToList();

        return new CvrCompanyListResponse(companies);
    }

    private CvrCompanyDto? ToDto(Vrvirksomhed? vrvirksomhed)
    {
        if (vrvirksomhed == null) return null;

        return new CvrCompanyDto
        {
            CompanyCvr = vrvirksomhed.cvrNummer?.ToString() ?? string.Empty,
            CompanyName = vrvirksomhed.virksomhedMetadata?.nyesteNavn?.navn,
            Address = ToDto(vrvirksomhed.virksomhedMetadata?.nyesteBeliggenhedsadresse)
        };
    }

    private static AddressDto? ToDto(NyesteBeliggenhedsadresse? address)
    {
        if (address == null) return null;

        return new AddressDto
        {
            AdresseId = address.adresseId,
            BogstavFra = address.bogstavFra,
            BogstavTil = address.bogstavTil,
            Bynavn = address.bynavn,
            Conavn = address.conavn,
            Etage = address.etage,
            Fritekst = address.fritekst,
            HusnummerFra = address.husnummerFra,
            HusnummerTil = address.husnummerTil,
            Landekode = address.landekode,
            Postboks = address.postboks,
            Postdistrikt = address.postdistrikt,
            Postnummer = address.postnummer,
            Sidedoer = address.sidedoer,
            Vejkode = address.vejkode,
            Vejnavn = address.vejnavn,
            Kommune = ToDto(address.kommune)
        };
    }

    private static KommuneDto? ToDto(Kommune? kommune)
    {
        if (kommune == null) return null;

        return new KommuneDto
        {
            KommuneKode = kommune.kommuneKode,
            KommuneNavn = kommune.kommuneNavn
        };
    }
}

using System.Linq;
using System.Threading.Tasks;
using API.Cvr.Api.Clients.Cvr;
using API.Cvr.Api.Models;
using API.Cvr.Api.v2023_01_01.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Cvr.Api.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101", Deprecated = true)]
[Route("api/cvr")]
public class CvrController : Controller
{
    private readonly CvrClient client;

    public CvrController(CvrClient client)
    {
        this.client = client;
    }

    /// <summary>
    /// Get CVR registered company information
    /// </summary>
    /// <param name="cvrNumber">CVR number of the company</param>
    /// <response code="200">Successful operation</response>
    /// <response code="404">CVR company not found</response>
    [ProducesResponseType(typeof(CvrCompanyDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{cvrNumber}")]
    public async Task<ActionResult<CvrCompanyDto>> GetCvrCompany(string cvrNumber)
    {
        var cvr = CvrNumber.TryParse(cvrNumber);
        if (cvr == null)
            return NotFound();

        var raw = await client.CvrNumberSearch(cvr);
        if (raw == null)
        {
            return NotFound();
        }

        var dto = ToDto(raw);
        if (dto == null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    private static CvrCompanyDto? ToDto(Root cvrCompany)
    {
        var cvrInfo = cvrCompany.hits?.hits?.SingleOrDefault()?._source?.Vrvirksomhed;

        if (cvrInfo == null)
            return null;

        return new CvrCompanyDto
        {
            CompanyName = cvrInfo.virksomhedMetadata?.nyesteNavn?.navn,
            CompanyCvr = cvrInfo.cvrNummer.ToString() ?? string.Empty,
            Address = ToDto(cvrInfo.virksomhedMetadata?.nyesteBeliggenhedsadresse)
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

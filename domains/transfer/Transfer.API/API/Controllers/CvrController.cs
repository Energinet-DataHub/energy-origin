using System.Linq;
using System.Threading.Tasks;
using API.Cvr;
using API.Cvr.Dtos;
using API.Cvr.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/cvr")]
public class CvrController : Controller
{
    private readonly CvrClient client;

    public CvrController(CvrClient client)
    {
        this.client = client;
    }

    /// <summary>
    /// Get cvr registered company information
    /// </summary>
    /// <param name="cvrNumber">CVR number of the company</param>
    /// <response code="200">Successful operation</response>
    /// <response code="404">Cvr company not found</response>
    [ProducesResponseType(typeof(CvrCompanyDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{cvrNumber}")]
    public async Task<ActionResult<CvrCompanyDto>> GetCvrCompany(string cvrNumber)
    {
        var raw = await client.CvrNumberSearch(cvrNumber);
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
        var cvrInfo = cvrCompany.hits?.hits?.FirstOrDefault()?._source?.Vrvirksomhed;

        if (cvrInfo == null)
            return null;

        return new CvrCompanyDto
        {
            CompanyName = cvrInfo.virksomhedMetadata?.nyesteNavn?.navn,
            CompanyCvr = cvrInfo.cvrNummer.ToString(),
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

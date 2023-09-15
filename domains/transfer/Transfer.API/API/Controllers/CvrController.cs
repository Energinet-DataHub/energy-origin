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
    /// <param name="cvrNummer">CVR number of the company</param>
    /// <response code="200">Successful operation</response>
    /// <response code="204">Cvr company not found</response>
    [ProducesResponseType(typeof(Root), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{cvrNummer}")]
    public async Task<ActionResult<CvrCompanyDto>> GetCvrCompany(string cvrNummer)
    {
        var raw = await client.CvrNumberSearch(cvrNummer);
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

    public static CvrCompanyDto? ToDto(Root cvrCompany)
    {
        var cvrInfo = cvrCompany.hits?.hits?.FirstOrDefault()?._source?.Vrvirksomhed;

        if (cvrInfo == null)
            return null;

        return new CvrCompanyDto
        {
            CompanyName = cvrInfo.virksomhedMetadata?.nyesteNavn?.navn,
            CompanyTin = cvrInfo.cvrNummer.ToString()
        };
    }
}

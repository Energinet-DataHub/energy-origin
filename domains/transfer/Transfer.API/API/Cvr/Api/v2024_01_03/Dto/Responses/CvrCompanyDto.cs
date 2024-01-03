using System;

namespace API.Cvr.Api.v2024_01_03.Dto.Responses;

public record CvrCompanyDto
{
    /// <summary>
    /// Company CVR number
    /// </summary>
    public required string CompanyCvr { get; init; }

    /// <summary>
    /// Company name
    /// </summary>
    public required string? CompanyName { get; init; }

    /// <summary>
    /// Company address
    /// </summary>
    public AddressDto? Address { get; init; }
}

public record AddressDto
{
    public string? Landekode { get; set; }
    public string? Fritekst { get; set; }
    public int? Vejkode { get; set; }
    public KommuneDto? Kommune { get; set; }
    public int? HusnummerFra { get; set; }
    public Guid? AdresseId { get; set; }
    public int? HusnummerTil { get; set; }
    public string? BogstavFra { get; set; }
    public string? BogstavTil { get; set; }
    public string? Etage { get; set; }
    public string? Sidedoer { get; set; }
    public string? Conavn { get; set; }
    public string? Postboks { get; set; }
    public string? Vejnavn { get; set; }
    public string? Bynavn { get; set; }
    public int? Postnummer { get; set; }
    public string? Postdistrikt { get; set; }
}

public class KommuneDto
{
    public int? KommuneKode { get; set; }
    public string? KommuneNavn { get; set; }
}

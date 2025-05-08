namespace API.Cvr.Api.Dto.Responses.Internal;

public class CvrCompanyInformationDto
{
    public required string Tin { get; init; }
    public required string Name { get; init; }
    public required string City { get; set; }
    public required string ZipCode { get; set; }
    public required string Address { get; set; }
}

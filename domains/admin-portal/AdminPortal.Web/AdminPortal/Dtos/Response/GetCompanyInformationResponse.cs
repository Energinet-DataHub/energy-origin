namespace AdminPortal.Dtos.Response;

public class GetCompanyInformationResponse
{
    public required string Tin { get; init; }
    public required string Name { get; init; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string ZipCode { get; set; }
}

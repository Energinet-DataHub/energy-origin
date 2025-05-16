using System.Collections.Generic;

namespace API.Cvr.Api.Dto.Responses.Internal;

public record CvrCompaniesListResponse(List<CvrCompaniesInformationDto> Result);

public class CvrCompaniesInformationDto
{
    public required string Tin { get; init; }
    public required string Name { get; init; }
}


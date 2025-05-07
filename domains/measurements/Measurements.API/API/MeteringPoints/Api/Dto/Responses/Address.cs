namespace API.MeteringPoints.Api.Dto.Responses;

public record Address(
    string Address1,
    string? Address2,
    string? Locality,
    string City,
    string PostalCode,
    string Country,
    string MunicipalityCode,
    string CitySubDivisionName
    );

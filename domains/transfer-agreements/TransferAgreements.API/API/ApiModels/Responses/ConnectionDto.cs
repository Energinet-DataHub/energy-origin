using System;

namespace API.ApiModels.Responses;

public record ConnectionDto(Guid CompanyId,
    string CompanyTin);

using System;

namespace API.ApiModels.Responses;

public record ConnectionDto(Guid Id,
    Guid CompanyId,
    string ComnpanyTin);

using System;

namespace API.ApiModels.Responses;

public record ConnectionDto(Guid OrganizationId,
    string OrganizationTin);

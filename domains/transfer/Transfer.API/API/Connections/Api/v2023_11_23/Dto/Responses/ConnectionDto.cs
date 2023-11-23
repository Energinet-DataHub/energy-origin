using System;

namespace API.Connections.Api.v2023_11_23.Dto.Responses;

public record ConnectionDto
{
    /// <summary>
    /// Connection ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// ID of the company
    /// </summary>
    public required Guid CompanyId { get; init; }

    /// <summary>
    /// Company TIN (e.g. CVR)
    /// </summary>
    public required string CompanyTin { get; init; }
};

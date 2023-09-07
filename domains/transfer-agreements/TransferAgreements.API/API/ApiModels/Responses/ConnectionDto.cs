using System;

namespace API.ApiModels.Responses;

public record ConnectionDto
{
    /// <summary>
    /// Connection ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Id for the company
    /// </summary>
    public required Guid CompanyId { get; init; }

    /// <summary>
    /// Company TIN, e.g. CVR
    /// </summary>
    public required string CompanyTin { get; init; }
};

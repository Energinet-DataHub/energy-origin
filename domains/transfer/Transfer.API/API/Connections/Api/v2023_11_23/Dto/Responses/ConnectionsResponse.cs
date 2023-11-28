using System.Collections.Generic;

namespace API.Connections.Api.v2023_11_23.Dto.Responses;

public record ConnectionsResponse
{
    /// <summary>
    /// List of connections
    /// </summary>
    public required List<ConnectionDto> Result { get; init; }
}

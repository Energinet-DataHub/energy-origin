using System.Collections.Generic;

namespace API.Connections.Api.Dto.Responses;

public record ConnectionsResponse
{
    /// <summary>
    /// List of connections
    /// </summary>
    public required List<ConnectionDto> Result { get; init; }
}

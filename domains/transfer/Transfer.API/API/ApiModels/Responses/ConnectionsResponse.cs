using System;
using System.Collections.Generic;

namespace API.ApiModels.Responses;

public record ConnectionsResponse
{
    /// <summary>
    /// List of connections
    /// </summary>
    public required List<ConnectionDto> Result { get; init; }
}

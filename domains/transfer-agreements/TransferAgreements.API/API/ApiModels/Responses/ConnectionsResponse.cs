using System.Collections.Generic;

namespace API.ApiModels.Responses;

public record ConnectionsResponse(List<ConnectionDto> Result);

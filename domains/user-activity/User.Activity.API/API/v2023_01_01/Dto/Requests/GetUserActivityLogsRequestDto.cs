using System.Collections.Generic;
using API.Models;
using MassTransitContracts.Contracts;

namespace API.v2023_01_01.Dto.Requests;

public record GetUserActivityLogsRequestDto
{
    public List<EntityType> EntityTypes { get; private set; } = [];
    public long? StartDate { get; init; }
    public long? EndDate { get; init; }
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = 10;
}


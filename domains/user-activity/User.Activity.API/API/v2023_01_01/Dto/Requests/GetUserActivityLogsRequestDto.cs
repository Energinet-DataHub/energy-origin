using System;
using System.Collections.Generic;
using API.Models;

namespace API.v2023_01_01.Dto.Requests;

public record GetUserActivityLogsRequestDto
{
    private readonly List<string>? entityType = [];

    public List<string>? EntityType
    {
        get => entityType;
        init
        {
            entityType = value ?? [];
            EntityTypes = ConvertAndValidateEntityType(entityType);
        }
    }

    public List<EntityType> EntityTypes { get; private set; } = [];
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = 10;

    private static List<EntityType> ConvertAndValidateEntityType(List<string>? entityTypes)
    {
        var validEntityTypes = new List<EntityType>();

        if (entityTypes == null) return validEntityTypes;
        foreach (var entityType in entityTypes)
        {
            if (Enum.TryParse<EntityType>(entityType.Replace("-", ""), true, out var validEntityType))
            {
                validEntityTypes.Add(validEntityType);
            }
        }

        return validEntityTypes;
    }
}


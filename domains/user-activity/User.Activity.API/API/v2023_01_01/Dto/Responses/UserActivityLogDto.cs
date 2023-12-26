using System;
using API.Models;
using MassTransitContracts.Contracts;

namespace API.v2023_01_01.Dto.Responses;

public record UserActivityLogDto(
    Guid Id,
    Guid ActorId,
    EntityType EntityType,
    long ActivityDate,
    Guid OrganizationId,
    string? Tin,
    string OrganizationName);

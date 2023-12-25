﻿using System;
using API.Models;

namespace API.v2023_01_01.Dto.Responses;

public record UserActivityLogDto(
    Guid Id,
    Guid ActorId,
    EntityType EntityType,
    DateTime ActivityDate,
    Guid OrganizationId,
    int Tin,
    string OrganizationName
);

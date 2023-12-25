﻿using System;

namespace API.Models;

public record UserActivityLog
(
    Guid Id,
    Guid ActorId,
    EntityType EntityType,
    DateTime ActivityDate,
    Guid OrganizationId,
    int Tin,
    string OrganizationName
);


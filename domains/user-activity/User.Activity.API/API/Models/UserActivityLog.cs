﻿using System;

namespace API.Models;

public record UserActivityLog
(
    Guid Id,
    Guid ActorId,
    EntityType EntityType,
    DateTimeOffset ActivityDate,
    Guid OrganizationId,
    string OrganizationName,
    string Tin
    );

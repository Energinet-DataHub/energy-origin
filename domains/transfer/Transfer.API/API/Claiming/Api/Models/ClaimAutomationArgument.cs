using System;

namespace API.Claiming.Api.Models;

public record ClaimAutomationArgument(Guid SubjectId, DateTimeOffset CreatedAt);

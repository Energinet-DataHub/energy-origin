using System;

namespace ClaimAutomation.Worker.Api.Models;

public record ClaimAutomationArgument(Guid SubjectId, DateTimeOffset CreatedAt);

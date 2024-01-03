using System;

namespace DataContext.Models;

public record ClaimAutomationArgument(Guid SubjectId, DateTimeOffset CreatedAt);

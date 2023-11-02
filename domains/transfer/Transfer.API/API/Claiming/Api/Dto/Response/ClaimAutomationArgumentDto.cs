using System;

namespace API.Claiming.Api.Dto.Response;

public record ClaimAutomationArgumentDto(Guid SubjectId, DateTimeOffset CreatedAt);

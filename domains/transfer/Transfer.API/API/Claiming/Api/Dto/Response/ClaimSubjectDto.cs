using System;

namespace API.Claiming.Api.Dto.Response;

public record ClaimSubjectDto(Guid SubjectId, DateTimeOffset CreatedAt);

using System;

namespace API.Claiming.Api.Models;

public record ClaimSubject(Guid SubjectId, DateTimeOffset CreatedAt);

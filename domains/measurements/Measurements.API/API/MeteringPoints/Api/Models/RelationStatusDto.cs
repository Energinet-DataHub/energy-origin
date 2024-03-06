using System;

namespace API.MeteringPoints.Api.Models;

public record RelationStatusDto(RelationStatus Status, Guid SubjectId);


public enum RelationStatus
{
    Pending,
    Created
}

using System;
using System.Text.Json.Serialization;

namespace API.MeteringPoints.Api.Models;

public class RelationDto()
{
    public RelationStatus Status { get; set; }
    public Guid SubjectId { get; set; }
    public Guid Actor { get; set; }
    public string? Tin { get; set; } = String.Empty;
};
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RelationStatus
{
    Pending,
    Created
}

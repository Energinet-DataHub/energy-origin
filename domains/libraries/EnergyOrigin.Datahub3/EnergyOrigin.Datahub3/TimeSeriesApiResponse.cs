namespace EnergyOrigin.Datahub3;

public class MeteringPoint
{
    public required string Id { get; set; }
}

public class PointAggregationGroup
{
    public required long MinObservationTime { get; set; }
    public required long MaxObservationTime { get; set; }
    public required string Resolution { get; set; }
    public required List<PointAggregation> PointAggregations { get; set; }
}

public class PointAggregation
{
    public required long MinObservationTime { get; set; }
    public required long MaxObservationTime { get; set; }
    public required decimal AggregatedQuantity { get; set; } //in kWh
    public required string Quality { get; set; }
}

public class MeteringPointData
{
    public required MeteringPoint MeteringPoint { get; set; }
    public required Dictionary<string, PointAggregationGroup> PointAggregationGroups { get; set; }
}

namespace API.Models;

public class TimeSeries
{
    public MeteringPoint MeteringPoint { get; }

    public IEnumerable<Measurement> Measurements { get; }

    public TimeSeries(MeteringPoint meteringPoint, IEnumerable<Measurement> measurements)
    {
        MeteringPoint = meteringPoint;
        Measurements = measurements;
    }
}

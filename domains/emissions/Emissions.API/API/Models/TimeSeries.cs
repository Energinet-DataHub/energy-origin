namespace API.Models;

public class TimeSeries
{
    public MeteringPoint MeteringPoint { get; set; }

    public IEnumerable<Measurement> Measurements { get; set; }
}
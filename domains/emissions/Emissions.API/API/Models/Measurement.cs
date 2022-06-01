namespace API.Models;

public class Measurement
{
    public long Gsrn { get; set; }

    public long DateFrom { get; set; }

    public long DateTo { get; set; }

    public int Quantity { get; set; }

    public Quality Quality { get; set; }
}
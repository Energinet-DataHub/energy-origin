namespace src.Models;

public class Certificate
{
    /// <summary>
    /// Start timestamp for the certificate in Unix time
    /// </summary>
    public long Start { get; set; }

    /// <summary>
    /// End timestamp for the certificate in Unix time
    /// </summary>
    public long End { get; set; }

    /// <summary>
    /// Amount of energy measured in Wh
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Id for the metering point
    /// </summary>
    public string MeteringPointId { get; set; }
}
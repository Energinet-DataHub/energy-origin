namespace src;

public class Certificate
{
    public string MeteringPoint { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int AmountWh { get; set; }
}

public class CertificateList
{
    public List<Certificate> Result { get; set; }
}

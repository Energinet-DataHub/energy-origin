namespace src;

public record Certificate(DateTimeOffset Start, DateTimeOffset End, int AmountWh, string MeteringPoint);
//public class Certificate
//{
//    public DateTimeOffset Start { get; set; }
//    public DateTimeOffset End { get; set; }
//    public int AmountWh { get; set; }
//    public string MeteringPoint { get; set; }

//}

//    public string MeteringPoint { get; set; }
//    public DateTimeOffset Start { get; set; }
//    public DateTimeOffset End { get; set; }
//    public int AmountWh { get; set; }
//}

//public class CertificateList
//{
//    public List<Certificate> Result { get; set; }
//}

public record CertificateList(List<Certificate> Result);

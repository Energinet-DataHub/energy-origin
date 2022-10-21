using System.Collections.Generic;

namespace API.Models;

public class CertificateList
{
    public List<Certificate> Result { get; set; } = new();
}

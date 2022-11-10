using System.Collections.Generic;
using System.Linq;

namespace API.Models;

public class CertificateList
{
    public IEnumerable<Certificate> Result { get; set; } = Enumerable.Empty<Certificate>();
}

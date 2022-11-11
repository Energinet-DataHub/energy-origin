using System.Collections.Generic;
using System.Linq;
using API.Query.API.ApiModels;

namespace API.Models;

public class CertificateList
{
    public IEnumerable<Certificate> Result { get; set; } = Enumerable.Empty<Certificate>();
}

using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.v2023_01_01.ApiModels.Responses;

public class CertificateList
{
    public IEnumerable<Certificate> Result { get; set; } = Enumerable.Empty<Certificate>();
}

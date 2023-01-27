using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.ApiModels.Responses;

public class CertificateList
{
    public IEnumerable<Certificate> Result { get; set; } = Enumerable.Empty<Certificate>();
}

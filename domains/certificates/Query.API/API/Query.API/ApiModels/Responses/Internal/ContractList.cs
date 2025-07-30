using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.ApiModels.Responses.Internal;

public class ContractList
{
    public IEnumerable<Contract> Result { get; set; } = Enumerable.Empty<Contract>();
}

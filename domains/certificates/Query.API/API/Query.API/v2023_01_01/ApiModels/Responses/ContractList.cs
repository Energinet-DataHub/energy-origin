using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.v2023_01_01.ApiModels.Responses;

public class ContractList
{
    public IEnumerable<Contract> Result { get; set; } = Enumerable.Empty<Contract>();
}

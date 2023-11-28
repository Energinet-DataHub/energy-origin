using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.v2023_11_27.ApiModels.Responses;

public class ContractList
{
    public IEnumerable<Contract> Result { get; set; } = Enumerable.Empty<Contract>();
}

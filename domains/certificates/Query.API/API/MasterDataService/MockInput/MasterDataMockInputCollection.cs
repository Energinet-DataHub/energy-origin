using System.Collections.Generic;
using System.Linq;

namespace API.MasterDataService.MockInput;

internal record MasterDataMockInputCollection(MasterDataMockInput[] Inputs)
{
    public IEnumerable<string> GetAllGsrns() => Inputs.Select(d => d.GSRN);
};

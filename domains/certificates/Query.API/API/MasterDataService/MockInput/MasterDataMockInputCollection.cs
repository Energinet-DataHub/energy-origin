using System.Collections.Generic;
using System.Linq;

namespace API.MasterDataService.MockInput;

internal record MasterDataMockInputCollection(MasterDataMockInput[] Data)
{
    public IEnumerable<string> GetAllGsrns() => Data.Select(d => d.GSRN);
};

using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.ApiModels.Responses;

public class ListResult<T>
{
    public IEnumerable<T> Result { get; set; } = Enumerable.Empty<T>();
}

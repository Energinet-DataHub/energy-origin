using System.Collections.Generic;
using System.Linq;

namespace API.Query.API.ApiModels.Responses;

public class SignUpList
{
    public IEnumerable<SignUp> Result { get; set; } = Enumerable.Empty<SignUp>();
}

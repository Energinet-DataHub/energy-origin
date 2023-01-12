using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace API.Query.API.Controllers;

public class CreateSignupValidator : AbstractValidator<CreateSignup>
{
    private readonly IHttpClientFactory httpClientFactory;

    public CreateSignupValidator(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;

        RuleFor(cs => cs.StartDate)
            .Must(l => l > 100);

        RuleFor(cs => cs.Gsrn)
            .Cascade(CascadeMode.Stop)
            .Must(Predicate0)
            .MustAsync(Predicate1);
    }

    private bool Predicate0(CreateSignup arg1, string s)
    {
        return s.Length > 10;
    }

    private async Task<bool> Predicate1(string arg1, CancellationToken cancellationToken)
    {
        var dataSyncHttpClient = httpClientFactory.CreateClient("DataSync");
        var res = await dataSyncHttpClient.GetAsync("meteringPoints", cancellationToken);

        return true;
    }
}

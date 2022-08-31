using API.Shared.Models;
using FluentValidation;

namespace API.Emissions.Models;

public record EmissionsRequest(long DateFrom, long DateTo, Aggregation Aggregation);

public class Validator : AbstractValidator<EmissionsRequest>
{
    public Validator()
    {
        RuleFor(request => request.DateFrom).NotEmpty().LessThan(request => request.DateTo);
        RuleFor(request => request.DateTo).NotEmpty().GreaterThan(request => request.DateFrom);
    }
}

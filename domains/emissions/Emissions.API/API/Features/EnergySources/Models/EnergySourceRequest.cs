using API.Shared.Models;
using FluentValidation;

namespace API.EnergySources.Models;

public record EnergySourceRequest(long DateFrom, long DateTo, Aggregation Aggregation);

public class Validator : AbstractValidator<EnergySourceRequest>
{
    public Validator()
    {
        RuleFor(request => request.DateFrom).NotEmpty().LessThan(request => request.DateTo);
        RuleFor(request => request.DateTo).NotEmpty().GreaterThan(request => request.DateFrom);
    }
}

using API.Models.Request;
using FluentValidation;

namespace API.Validation;

public class MeasurementsRequestValidator : AbstractValidator<MeasurementsRequest>
{
    public MeasurementsRequestValidator()
    {
        RuleFor(a => a.DateFrom).NotEmpty().LessThan(a => a.DateTo);
        RuleFor(a => a.DateTo).NotEmpty().GreaterThan(a => a.DateFrom);
    }
}

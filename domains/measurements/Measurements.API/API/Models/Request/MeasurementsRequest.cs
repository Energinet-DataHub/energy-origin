using System.ComponentModel;
using FluentValidation;

namespace API.Models.Request;

public class MeasurementsRequest
{
    public long DateFrom { get; set; }

    public long DateTo { get; set; }

    [DefaultValue(Aggregation.Total)]
    public Aggregation Aggregation { get; set; } = Aggregation.Total;

    public class Validator : AbstractValidator<MeasurementsRequest>
    {
        public Validator()
        {
            RuleFor(a => a.DateFrom).NotEmpty().LessThan(a => a.DateTo);
            RuleFor(a => a.DateTo).NotEmpty().GreaterThan(a => a.DateFrom);
        }
    }
}

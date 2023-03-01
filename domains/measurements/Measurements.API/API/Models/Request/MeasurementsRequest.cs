using System.ComponentModel;
using FluentValidation;

namespace API.Models.Request;

public class MeasurementsRequest
{
    public long DateFrom { get; init; }

    public long DateTo { get; init; }

    [DefaultValue(Aggregation.Total)]
    public Aggregation Aggregation { get; init; } = Aggregation.Total;

    public string TimeZone { get; init; } = TimeZoneInfo.Utc.Id;

    internal TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

    public class Validator : AbstractValidator<MeasurementsRequest>
    {
        public Validator()
        {
            RuleFor(x => x.DateFrom).NotEmpty().LessThan(x => x.DateTo);
            RuleFor(x => x.DateTo).NotEmpty().GreaterThan(x => x.DateFrom);
            RuleFor(x => x.TimeZone).Must(id => { try { _ = TimeZoneInfo.FindSystemTimeZoneById(id); return true; } catch { return false; } }).WithMessage("Must be a valid time zone identifier");
        }
    }
}

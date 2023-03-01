using System.ComponentModel;
using FluentValidation;

namespace API.Models;

public class EnergySourceRequest
{
    public long DateFrom { get; set; }

    public long DateTo { get; set; }

    [DefaultValue(Aggregation.Total)]
    public Aggregation Aggregation { get; set; } = Aggregation.Total;

    public string TimeZone { get; init; } = TimeZoneInfo.Utc.Id;

    internal TimeZoneInfo TimeZoneInfo => TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

    public class Validator : AbstractValidator<EnergySourceRequest>
    {
        public Validator()
        {
            RuleFor(a => a.DateFrom).NotEmpty().LessThan(a => a.DateTo);
            RuleFor(a => a.DateTo).NotEmpty().GreaterThan(a => a.DateFrom);
            RuleFor(x => x.TimeZone).Must(id => { try { _ = TimeZoneInfo.FindSystemTimeZoneById(id); return true; } catch { return false; } }).WithMessage("Must be a valid time zone identifier");
        }
    }
}

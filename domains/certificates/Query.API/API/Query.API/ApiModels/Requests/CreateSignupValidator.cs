using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public class CreateSignupValidator : AbstractValidator<CreateSignup>
{
    public CreateSignupValidator()
    {
        var now = DateTimeOffset.UtcNow;
        var utcMidnight = now.Subtract(now.TimeOfDay).ToUnixTimeSeconds();

        RuleFor(cs => cs.StartDate)
            .GreaterThanOrEqualTo(_ => utcMidnight);

        RuleFor(cs => cs.GSRN)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(gsrn => Regex.IsMatch(gsrn, "^\\d{18}$", RegexOptions.None, TimeSpan.FromSeconds(1))).WithMessage("Invalid GSRN. Must be 18 digits");
    }
}

using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.Query.API.ApiModels.Requests;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidGsrn<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Must(gsrn => Regex.IsMatch(gsrn ?? "", "^\\d{18}$", RegexOptions.None, TimeSpan.FromSeconds(1)))
            .WithMessage("Invalid {PropertyName}. Must be 18 digits");
}

using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.ApiModels.Requests;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, long> MustBeBeforeYear10000<T>(this IRuleBuilder<T, long> ruleBuilder)
        => ruleBuilder
            .LessThan(253402300800)
            .WithMessage("{PropertyName} must be before 253402300800 (10000-01-01T00:00:00+00:00)");

    public static IRuleBuilderOptions<T, long?> MustBeBeforeYear10000<T>(this IRuleBuilder<T, long?> ruleBuilder)
        => ruleBuilder
            .LessThan(253402300800)
            .WithMessage("{PropertyName} must be before 253402300800 (10000-01-01T00:00:00+00:00)");
}

using System;
using System.Text.RegularExpressions;
using FluentValidation;

namespace API.ApiModels.Requests;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, long> MustBeBeforeYear10000<T>(this IRuleBuilder<T, long> ruleBuilder)
        => ruleBuilder
            .LessThan(253402300800)
            .WithMessage("{PropertyName} too high! Please make sure the format is UTC in seconds.");

    public static IRuleBuilderOptions<T, long?> MustBeBeforeYear10000<T>(this IRuleBuilder<T, long?> ruleBuilder)
        => ruleBuilder
            .LessThan(253402300800)
            .WithMessage("{PropertyName} too high! Please make sure the format is UTC in seconds.");
}

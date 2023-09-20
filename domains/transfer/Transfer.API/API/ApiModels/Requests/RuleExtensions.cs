using System;
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

    public static IRuleBuilderOptions<T, string> MustBeValidBase64EncodedWalletDepositEndpoint<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .Must(IsValidBase64String)
                .WithMessage("Base64-encoded Wallet Deposit Endpoint is not valid");

    private static bool IsValidBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return false;

        try
        {
            _ = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

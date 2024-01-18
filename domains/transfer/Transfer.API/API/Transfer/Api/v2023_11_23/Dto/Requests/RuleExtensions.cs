using API.Transfer.Api.Converters;
using FluentValidation;

namespace API.Transfer.Api.v2023_11_23.Dto.Requests;

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

    public static IRuleBuilderOptions<T, string> MustBeValidWalletDepositEndpointBase64<T>(this IRuleBuilder<T, string> ruleBuilder,
        string message = "String is not a valid Base64-encoded WalletDepositEndpoint")
        => ruleBuilder
            .Must(TryConvert)
            .WithMessage(message);

    private static bool TryConvert(string base64String)
    {
        return Base64Converter.TryConvertToWalletDepositEndpoint(base64String, out _);
    }
}

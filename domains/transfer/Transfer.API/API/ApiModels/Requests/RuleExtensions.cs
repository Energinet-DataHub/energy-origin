using System;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public static IRuleBuilderOptions<T, string> MustBeValidBase64<T>(this IRuleBuilder<T, string> ruleBuilder,
        string message = "String is not Base64")
        => ruleBuilder
            .Must(IsValidBase64String)
            .WithMessage(message);

    private static bool IsValidBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return false;

        var buffer = new Span<byte>(new byte[base64.Length]);
        if (!Convert.TryFromBase64String(base64, buffer, out _))
            return false;

        var jsonString = Encoding.UTF8.GetString(buffer.ToArray());
        return IsValidJson(jsonString);
    }

    private static bool IsValidJson(string strInput)
    {
        strInput = strInput.Trim();
        if ((!strInput.StartsWith("{") || !strInput.EndsWith("}")) &&
            (!strInput.StartsWith("[") || !strInput.EndsWith("]"))) return false;
        try
        {
            JToken.Parse(strInput);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }
}

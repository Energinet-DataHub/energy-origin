namespace EnergyOrigin.TokenValidation.Unit.Tests.Analyzers;

public abstract class AnalyzerTestBase
{
    protected static string GetBasicTestCode(string testSpecificCode)
    {
        return @"
using Microsoft.AspNetCore.Authorization;
using System;
using EnergyOrigin.TokenValidation.B2C;

namespace EnergyOrigin.TokenValidation.B2C
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DisableTermsRequirementAttribute : Attribute { }

    public static class Policy
    {
        public const string B2CCvrClaim = nameof(B2CCvrClaim);
        public const string B2CSubTypeUserPolicy = nameof(B2CSubTypeUserPolicy);
    }
}

" + testSpecificCode;
    }
}

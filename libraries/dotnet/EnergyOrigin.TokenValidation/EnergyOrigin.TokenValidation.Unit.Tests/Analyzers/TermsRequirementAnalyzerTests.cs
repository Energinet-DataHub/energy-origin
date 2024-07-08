using EnergyOrigin.TokenValidation.Analyzers;
using EnergyOrigin.TokenValidation.B2C;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace EnergyOrigin.TokenValidation.Unit.Tests.Analyzers;

public class TermsRequirementAnalyzerTests : AnalyzerTestBase
{
    private const string DiagnosticId = "EOT001";

    [Fact]
    public async Task NoDisableTermsRequirement_NoDiagnostic()
    {
        var test = GetBasicTestCode(@"
public class TestController
{
    [Authorize(Policy = Policy.B2CSubTypeUserPolicy)]
    public void TestMethod() { }
}");

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DisableTermsRequirementWithCorrectPolicy_NoDiagnostic()
    {
        var test = GetBasicTestCode(@"
public class TestController
{
    [DisableTermsRequirement]
    [Authorize(Policy = Policy.B2CSubTypeUserPolicy)]
    public void TestMethod() { }
}");

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task DisableTermsRequirementWithIncorrectPolicy_ProducesDiagnostic()
    {
        var test = GetBasicTestCode(@"
public class TestController
{
    [DisableTermsRequirement]
    [Authorize(Policy = ""SomeOtherPolicy"")]
    public void TestMethod() { }
}");

        var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(21, 5);
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task DisableTermsRequirementWithoutAuthorize_ProducesDiagnostic()
    {
        var test = GetBasicTestCode(@"
public class TestController
{
    [DisableTermsRequirement]
    public void TestMethod() { }
}");

        var expected = VerifyCS.Diagnostic(DiagnosticId).WithLocation(21, 5);
        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }


    [Fact]
    public async Task DisableTermsRequirementWithB2CCvrClaimPolicy_NoDiagnostic()
    {
        var test = GetBasicTestCode(@"
public class TestController
{
    [DisableTermsRequirement]
    [Authorize(Policy = Policy.B2CCvrClaim)]
    public void TestMethod() { }
}");

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}

public static class VerifyCS
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TermsRequirementAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private class Test : CSharpAnalyzerTest<TermsRequirementAnalyzer, DefaultVerifier>
    {
        public Test()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            TestState.AdditionalReferences.Add(typeof(DisableTermsRequirementAttribute).Assembly);
            TestState.AdditionalReferences.Add(typeof(AuthorizeAttribute).Assembly);
        }
    }
}

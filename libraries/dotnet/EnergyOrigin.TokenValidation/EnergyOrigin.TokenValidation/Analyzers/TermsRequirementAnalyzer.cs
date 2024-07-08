using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace EnergyOrigin.TokenValidation.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TermsRequirementAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "EOT001";
    private const string Category = "Usage";

    private static readonly LocalizableString Title = "Invalid usage of DisableTermsRequirement";
    private static readonly LocalizableString MessageFormat = "DisableTermsRequirement can only be used with B2CSubTypeUserPolicy or B2CCvrClaimPolicy";
    private static readonly LocalizableString Description = "Ensure DisableTermsRequirement is used correctly.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        var hasDisableTermsRequirement = methodDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() == "DisableTermsRequirement");

        if (!hasDisableTermsRequirement)
            return;

        var hasCorrectPolicy = methodDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() == "Authorize" &&
                      (a.ArgumentList?.Arguments.Any(arg =>
                          arg.ToString().Contains("Policy = Policy.B2CSubTypeUserPolicy") ||
                          arg.ToString().Contains("Policy = Policy.B2CCvrClaim")) ?? false));

        if (hasCorrectPolicy) return;
        var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}

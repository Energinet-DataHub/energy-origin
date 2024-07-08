# TermsRequirementAnalyzer

## Purpose

The `TermsRequirementAnalyzer` is a Roslyn analyzer designed to enforce correct usage
of the `DisableTermsRequirementAttribute`,
in conjunction with specific authorization policies in the Energy Track & Trace™ project.

## How It Works

This analyzer scans C# code for methods decorated with the `DisableTermsRequirementAttribute`.
When found, it checks if the same method is also decorated with an `AuthorizeAttribute`,
using either the `B2CSubTypeUserPolicy` or `B2CCvrClaimPolicy`.
If the `DisableTermsRequirementAttribute` is used without these specific policies, the analyzer reports a diagnostic.

## Activation

The analyzer is active during code compilation,
and in real-time in IDEs that support Roslyn analyzers (e.g., Visual Studio, JetBrains Rider).

## Rules

- **Rule ID**: EOT001
- **Category**: Usage
- **Severity**: Error

## Diagnostic Messages

- **Title**: Invalid usage of DisableTermsRequirement
- **Message**: DisableTermsRequirement can only be used with B2CSubTypeUserPolicy or B2CCvrClaimPolicy
- **Description**: Ensure DisableTermsRequirement is used correctly.

## Context

The `TermsRequirementHandler` in the Energy Origin project checks if a user has accepted the Terms of Service (ToS),
before allowing access to certain endpoints.
The `DisableTermsRequirementAttribute` is used to bypass this check for specific endpoints.

However, this bypass should only be allowed in conjunction with certain authorization policies,
(`B2CSubTypeUserPolicy` or `B2CCvrClaimPolicy`),
to maintain security and ensure proper access control.

## Analyzer Behavior

1. The analyzer targets method declarations in C# code.
2. It first checks if the method has the `DisableTermsRequirementAttribute`.
3. If found, it then looks for an `AuthorizeAttribute` on the same method.
4. The `AuthorizeAttribute` must specify either `Policy.B2CSubTypeUserPolicy` or `Policy.B2CCvrClaim`.
5. If the `DisableTermsRequirementAttribute` is used without the correct `AuthorizeAttribute`, a diagnostic is reported.

## Usage

To use this analyzer:

1. Include the analyzer in your project (typically via a NuGet package).
2. The analyzer will automatically run during compilation and in supported IDEs.
3. If you use `[DisableTermsRequirement]`, ensure you also use `[Authorize]` with the correct policy:

```csharp
[DisableTermsRequirement]
[Authorize(Policy.B2CSubTypeUserPolicy)]
public void SomeMethod() { ... }
```

or

```csharp
[DisableTermsRequirement]
[Authorize(Policy.B2CCvrClaim)]
public void AnotherMethod() { ... }
```

4. Using `[DisableTermsRequirement]` without the correct `[Authorize]` attribute will trigger a diagnostic.

## Benefits

- Ensures consistent and secure usage of the `DisableTermsRequirementAttribute`.
- Prevents accidental misuse that could lead to security vulnerabilities.
- Helps maintain the integrity of the Terms of Service acceptance flow in the Energy Origin project.

## Note

This analyzer is specific to the Energy Track & Trace™ and its authentication/authorization setup.
It may need adjustments if the underlying `TermsRequirementHandler` or policy names change.

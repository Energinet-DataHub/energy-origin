# Terms Acceptance Handler

## Purpose

The Terms Acceptance Handler is designed to enforce the acceptance of Terms of Service (ToS),
for users accessing certain parts of Energy Track & Trace™.
It ensures that users have agreed to the latest terms before allowing access to protected resources.

## Implementation

The handler is implemented as a custom `AuthorizationHandler<TermsRequirement>` in ASP.NET Core.
It checks for the presence of a `TosAccepted` claim in the user's identity,
which indicates whether the user has accepted the current terms of service.

## Usage

The Terms Acceptance Handler is automatically applied, to these specific authorization policies in our application.
By default, it is enabled for the following policies:

- `B2CSubTypeUserPolicy`
- `B2CCvrClaimPolicy`

When these policies are applied to controllers or actions,
the Terms Acceptance Handler will be invoked as part of the authorization process.

## Default Behavior

By default, the Terms Acceptance Handler is enabled for the specified policies. This means:

1. If a user has a `tos_accepted` claim with a value of "true", access is granted.
2. If a user doesn't have a `tos_accepted` claim, or its value is not "true", access is denied.

## Disabling the Handler

In certain scenarios, you may want to disable the terms acceptance check.
To do this, you can use the `[DisableTermsRequirement]` Attribute on a controller or action method:

```csharp
[HttpGet]
[Route("some/api/endpoint/]
[DisableTermsRequirement] //<--- This Attribute disables Terms Acceptance requirement
[Authorize(Policy = Policy.B2CCvrClaim)]
public IActionResult SomeEndpoint() {} //<--- This action will not check for terms acceptance
```
When the `[DisableTermsRequirement]` attribute is present, the Terms Acceptance Handler will automatically succeed,
without checking the `TosAccepted` claim.

## Design Choices

1. **Opt-Out Approach**: It was chosen to enable the Terms Acceptance Handler, by default, for specific policies,
and allow opting out, using an attribute.
This ensures that terms acceptance is enforced, broadly, across the application, unless explicitly disabled.


2. **Claim-Based**: Using a claim for terms acceptance allows for flexible management of user consent,
across different parts of the system.


3. **Policy-Based**: By tying the handler to specific authorization policies,
separation of concerns is maintained,
and allows for easy configuration, and management of where terms acceptance is required.


4. **Granular Control**: The ability to disable the check at the controller, or action level,
provides developers with fine-grained control over terms acceptance enforcement.

## Rationale

The decision to have the Terms Acceptance Handler enabled by default
stems from the importance of ensuring users have agreed to the latest terms of service.

This approach:

1. Reduces the risk of oversight, where developers might forget to enable terms checking for new features.
2. Aligns with legal and compliance requirements by ensuring terms acceptance is the norm rather than the exception.
3. Provides a consistent user experience across the application.
4. Allows for easy auditing of where terms acceptance is and isn't being enforced.

By providing an opt-out mechanism,
we maintain flexibility for scenarios where terms acceptance might not be necessary or appropriate,
such as public endpoints or certain API calls.

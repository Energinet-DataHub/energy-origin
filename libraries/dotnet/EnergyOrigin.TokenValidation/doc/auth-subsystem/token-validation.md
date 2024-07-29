# Token Validation

Token validation is built around facilitating the [Security and Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-7.0) concept in ASP.NET Core.

For our specific use cases, see [restricting access](#restricting-access).

## Quick Start

1. Register token validation during start up:

```csharp
var options = new TokenValidationOptions {
    PublicKey = byteArrayOfPublicKeyPem,
    Audience = audience,
    Issuer = issuer
}

builder.AddTokenValidation(options);
```

For more further configuration, see [configuring token validation](#configuring-token-validation).

2. Done - you can move on to [reading token values](#reading-token-values) or [restricting access](#restricting-access)

## Restricting access

Access can be restricted using the `Authorize` attribute. This attribute can be applied to a `class` or a method dependent on the use case.

### Always allow access
```csharp
[AllowAnonymous]
```

### Require a valid token
```csharp
[Authorize]
```

### Require a valid token that is related to an organization
```csharp
[Authorize(Policy = PolicyName.RequiresCompany)]
```

There are a number of predefined policies available, that are referenced by `PolicyName`. These included policies should be general in nature.

### Require a valid token that has a specific role
```csharp
[Authorize(Roles = RoleKey.RoleAdmin)]
```

There are a number of predefined roles available, that are referenced by `RoleKey`. These included roles should be general in nature.

## Reading token values

The values in a token can be found by mapping the ClaimsPrincipal in a controller to a user descriptor like in the following example.

Attempting to map the ClaimPrincipal (`User`) while no token is present or the token is invalid will throw an exception.

```csharp
public async IActionResult Get()
{
    var descriptor = new UserDescriptor(User);
    // ...
}
```

### User descriptor

The user descriptor can be consider to be mostly a simple object with the values readily available. The most common values being:

- Id
- Name
- Organization (if present):
    - Id
    - Name
    - Tin

## Configuring Token Validation

There is an extension available on `WebApplicationBuilder` to register token validation that takes an argument of the type `ValidationParameters`.

```csharp
builder.AddTokenValidation(validationParameters);
```

Creating an instance of `ValidationParameters` requires a byte array argument containing a public key to verify signage by a specific private key. Token validation only works with tokens signed by a private key.

> Currently only public/private key sets using RSA encryption is supported

```csharp
new ValidationParameters(byteArrayOfPublicKeyPem);
```

`ValidationParameters` inherits from the class [TokenValidationParameters](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters?view=msal-web-dotnet-latest). This means you can adjust any setting normally controlled by `TokenValidationParameters`, like for instance setting clock skew tolerance:

```csharp
new ValidationParameters(byteArrayOfPublicKeyPem) {
    ClockSkew = TimeSpan.Zero
};
```

The purpose of `ValidationParameters` to configure a number of settings for `TokenValidationParameters` consistently.

### Notable validation parameters

While `ValidationParameters` takes care of setting up validation of the signage, if no other settings are specified a given token is likely to be considered invalid. It is very likely that the following parameters should be configured too.

#### ValidAudience
By default the audience encoded into a token should match this property.

#### ValidIssuer
By default the issuer encoded into a token should match this property.

## Generating Tokens for Tests

You can generate JWT tokens using the `TokenSigner` class. This class allows for the creation of signed JWT tokens.

You have the option to either provide your own private key or use the `RsaKeyGenerator` to create a random private key.

You can use `TokenSigner` with a randomly generated private key from `RsaKeyGenerator` as follows:

```csharp
var signer = new TokenSigner(RsaKeyGenerator.GenerateTestKey());
```

Once initialized you can use the `Sign` method to generate a token.

### Example Usage with Additional Claims

Here is an example of how to use the `Sign` method of `TokenSigner` to generate a JWT token with additional claims:

```csharp
var additionalClaims = new Dictionary<string, object>
{
    { JwtClaimNames.Tin, 12345678 },
    { "foo", "fooValue" },
    { "bar", "barValue" }
};

string token = signer.Sign(
    subject: "TestSubject",
    name: "TestName",
    issuer: "TestIssuer",
    audience: "TestAudience",
    claims: additionalClaims
);
```

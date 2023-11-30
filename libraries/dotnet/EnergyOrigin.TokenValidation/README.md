# Token Validation

Token validation is built around facilitating the [Security and Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-7.0) concept in ASP.NET Core.

For our specific use cases, see [restricting access](#restricting-access).

## Quick Start

1. Register token validation during start up:

```csharp
var tokenValidationOptions = new TokenValidationOptions
{
    PublicKey = byteArrayOfPublicKeyPem,
    Issuer = issuer,
    Audience = audience
};

builder.Services.AddTokenValidation(tokenValidationOptions);

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

Configure token validation by creating an instance of TokenValidationOptions and passing it to the AddTokenValidation extension method on WebApplicationBuilder.
```csharp
var tokenValidationOptions = new TokenValidationOptions
{
    PublicKey = Convert.FromBase64String(configuration["TokenValidation:PublicKey"]), // Convert from Base64 string to byte array
    Issuer = configuration["TokenValidation:Issuer"],
    Audience = configuration["TokenValidation:Audience"]
};

builder.Services.AddTokenValidation(tokenValidationOptions);
```

TokenValidationOptions requires the following parameters:

PublicKey: A byte array containing the public key to verify the signature of a JWT token.
Issuer: The valid issuer of the token.
Audience: The valid audience of the token.
These parameters can be configured directly or loaded from configuration sources like appsettings.json.

#### ValidAudience
By default the audience encoded into a token should match this property.

#### ValidIssuer
By default the issuer encoded into a token should match this property.

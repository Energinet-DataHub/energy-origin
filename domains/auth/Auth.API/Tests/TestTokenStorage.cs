using API.Models;
using API.TokenStorage;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Categories;

namespace Tests.Controller;


[UnitTest]
public sealed class TestTokenStorage
{
    public static IEnumerable<object[]> CookieValidation => new[]
    {
            new object[] { DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(5), true },
            new object[] { DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(6), false },
            new object[] { DateTime.UtcNow.AddHours(-10), DateTime.UtcNow.AddHours(-4), false },
    };

    [Theory, MemberData(nameof(CookieValidation))]
    public void TokenStorage_InteralTokenIssuedAndExpiresValidation(DateTime issued, DateTime expires, bool expectedIsValid)
    {
        var tokenStorage = new TokenStorage();

        InternalToken interalToken = new InternalToken()
        {
            Actor = "Actor",
            Subject = "Subject",
            Scope = new List<string> { "Scope1", "Scope2" },
            Issued = issued,
            Expires = expires,
        };

        var isValid = tokenStorage.InternalTokenValidation(interalToken);

        Assert.Equal(expectedIsValid, isValid);
    }

    public static IEnumerable<object[]> interalTokens => new[]
{
            new object[] { new InternalToken(){ Actor = "Actor", Subject = "Subject", Scope = new List<string> { "Scope1", "Scope2" }, Issued = DateTime.UtcNow.AddHours(-1), Expires = DateTime.UtcNow.AddHours(5) }, true },
            new object[] { null , false},
    };

    [Theory, MemberData(nameof(interalTokens))]
    public void TokenStorage_InteralTokenIsNotNullValidation(InternalToken? interalToken, bool expectedIsValid)
    {
        var tokenStorage = new TokenStorage();

        var isValid = tokenStorage.InternalTokenValidation(interalToken);

        Assert.Equal(expectedIsValid, isValid);
    }
}

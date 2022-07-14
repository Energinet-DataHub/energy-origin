﻿namespace Mock.Oidc.Jwt;

public interface IJwtTokenGenerator
{
    string Generate(Dictionary<string, object> claims);
}

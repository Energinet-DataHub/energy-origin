using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using API.DataSyncSyncer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.IdentityModel.Tokens;

namespace API.AppTests;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services => {
            //Remove DataSyncSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(DataSyncSyncerWorker)));
        });

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken());

        return client;
    }

    private static string GenerateToken(
        string scope = "",
        string actor = "d4f32241-442c-4043-8795-a4e6bf574e7f",
        string subject = "bdcb3287-3dd3-44cd-8423-1f94437648cc")
    {
        var key = Encoding.ASCII.GetBytes("TESTTESTTESTTESTTESTTESTTESTTESTTESTTESTTEST");

        var claims = new[]
        {
            new Claim("subject", subject),
            new Claim("scope", scope),
            new Claim("actor", actor)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

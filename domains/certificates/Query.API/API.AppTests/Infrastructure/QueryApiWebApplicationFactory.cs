using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using API.AppTests.Mocks;
using API.DataSyncSyncer;
using API.MasterDataService;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace API.AppTests.Infrastructure;

public class QueryApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestMasterDataService masterDataServiceMock;

    public QueryApiWebApplicationFactory() => masterDataServiceMock = new TestMasterDataService();

    public string MartenConnectionString { get; set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Marten", MartenConnectionString);

        builder.ConfigureTestServices(services =>
        {
            //Remove DataSyncSyncerWorker
            services.Remove(services.First(s => s.ImplementationType == typeof(DataSyncSyncerWorker)));

            // Replace IMasterDataService
            services.Remove(services.First(s => s.ServiceType == typeof(IMasterDataService)));
            services.AddSingleton<IMasterDataService>(masterDataServiceMock);
        });
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    public HttpClient CreateAuthenticatedClient(string subject)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(subject: subject));

        return client;
    }

    public IBus GetMassTransitBus() => Services.GetRequiredService<IBus>();

    public void AddMasterData(MasterData data) => masterDataServiceMock.Add(data);

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

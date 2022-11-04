using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using API.MasterDataService;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace API.DataSyncSyncer.Service;

public class DataSyncProvider : IDataSyncProvider
{
    private readonly HttpClient httpClient;
    public DataSyncProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    public async Task<List<MasterData>?> GetMasterData(string gsrn)
    {
        var coolUrl = $"http://localhost:8081/measurements?gsrn={gsrn}";
        httpClient.SetBearerToken(GenerateToken());
        var reponse = await httpClient.GetAsync(coolUrl);

        if (reponse == null || !reponse.IsSuccessStatusCode)
        {
            throw new Exception($"Fetch of measurements failed, base: {httpClient.BaseAddress} url: {coolUrl}");
        }

        return await reponse.Content.ReadFromJsonAsync<List<MasterData>>() ?? new List<MasterData>();
    }

    public static string GenerateToken()
    {
        var someClaims = new Claim[]{
            new(JwtRegisteredClaimNames.UniqueName,"username"),
            new(JwtRegisteredClaimNames.NameId,Guid.NewGuid().ToString())
        };

        SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test test test test test"));
        var token = new JwtSecurityToken(
            issuer: "test.test.com",
            audience: "test.test.com",
            claims: someClaims,
            expires: DateTime.Now.AddMinutes(3),
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(jwt));
    }
}

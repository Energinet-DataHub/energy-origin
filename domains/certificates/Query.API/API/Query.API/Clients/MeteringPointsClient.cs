using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace API.Query.API.Clients;

public class MeteringPointsClient
{
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        //Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public MeteringPointsClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<MeteringPointsResponse?> GetMeteringPoints(CancellationToken cancellationToken)
    {
        try
        {
            var meteringPointsResponse = await httpClient.GetFromJsonAsync<MeteringPointsResponse>("meteringPoints", cancellationToken: cancellationToken, options: jsonSerializerOptions);
            return meteringPointsResponse;

            var res = await httpClient.GetAsync("meteringPoints", cancellationToken);
            //var str = await res.Content.ReadAsStringAsync(cancellationToken);
            var content = await res.Content.ReadFromJsonAsync<MeteringPointsResponse>(jsonSerializerOptions, cancellationToken: cancellationToken); //jsonSerializerOptions
            return content;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
}

public record MeteringPointsResponse(List<MeteringPoint> MeteringPoints);

public record MeteringPoint(string GSRN, string GridArea, MeterType Type, Address Address);
public enum MeterType { Consumption, Production, Child }

public record Address(string Address1, string? Address2, string? Locality, string City, string PostalCode,
    string Country);

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EnergyOrigin.Setup.Exceptions;

namespace AdminPortal.Services;

public interface IContractService
{
    Task<ContractList> CreateContracts(CreateContracts request);
    Task EditContracts(EditContracts request);
}

public class ContractService(HttpClient client) : IContractService
{
    private readonly string _mediaType = "application/json";
    private readonly Encoding _encoding = Encoding.UTF8;

    public async Task<ContractList> CreateContracts(CreateContracts request)
    {
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, _encoding, _mediaType);
        var response = await client.PostAsync($"internal-contracts", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ContractList>();
            return result ?? throw new InvalidOperationException("The API could not be reached or returned null.");
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
            case HttpStatusCode.Conflict:
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ContractsException(errorContent, response.StatusCode);
            default:
                throw new ContractsException("It was not possible to create the contract", HttpStatusCode.BadRequest);
        }
    }

    public async Task EditContracts(EditContracts request)
    {
        var requestStr = JsonSerializer.Serialize(request);
        var content = new StringContent(requestStr, _encoding, _mediaType);
        var response = await client.PutAsync($"api/certificates/admin-portal/internal-contracts", content);

        if (response.IsSuccessStatusCode) return;

        switch (response.StatusCode)
        {
            case HttpStatusCode.BadRequest:
            case HttpStatusCode.Conflict:
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ContractsException(errorContent, response.StatusCode);
            case HttpStatusCode.Forbidden:
            case HttpStatusCode.NotFound:
                throw new ContractsException(string.Empty, response.StatusCode);
            default:
                throw new ContractsException("It was not possible to edit the contract", HttpStatusCode.BadRequest);
        }
    }
}

public class CreateContract
{
    public string Gsrn { get; init; } = "";
    public long StartDate { get; init; }
    public long? EndDate { get; set; }
}

public record CreateContracts(
        List<CreateContract> Contracts,
        Guid MeteringPointOwnerId,
        string OrganizationTin,
        string OrganizationName,
        bool IsTrial);

public class ContractList
{
    public required IEnumerable<Contract> Result { get; set; }
}

public class Contract
{
    public Guid Id { get; set; }
    public string Gsrn { get; set; } = "";
    public long StartDate { get; set; }
    public long? EndDate { get; set; }
    public long Created { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MeteringPointTypeResponse MeteringPointType { get; set; }

    public Technology? Technology { get; set; }
}

public enum MeteringPointTypeResponse
{
    Production,
    Consumption
}

public class Technology
{
    public required string AibFuelCode { get; set; }
    public required string AibTechCode { get; set; }
};

public class EditContractEndDate
{
    public required Guid Id { get; init; }
    public long? EndDate { get; set; }
}

public record EditContracts(
        List<EditContractEndDate> Contracts,
        [Required]
        Guid MeteringPointOwnerId,
        [Required]
        string OrganizationTin,
        [Required]
        string OrganizationName);

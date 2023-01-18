using System;
using System.Text.Json.Serialization;
using API.ContractService;

namespace API.Query.API.ApiModels.Responses;

public class SignUp
{
    public Guid Id { get; set; }

    /// <summary>
    /// Global Service Relation Number (GSRN) for the metering point
    /// </summary>
    [JsonPropertyName("gsrn")]
    public string GSRN { get; set; } = "";

    /// <summary>
    /// Starting date for generation of certificates in Unix time
    /// </summary>
    public long StartDate { get; set; }

    /// <summary>
    /// Date for the sign up
    /// </summary>
    public long Created { get; set; }

    public static SignUp CreateFrom(CertificateGenerationSignUp signUp) =>
        new()
        {
            Id = signUp.Id,
            GSRN = signUp.GSRN,
            StartDate = signUp.SignUpStartDate.ToUnixTimeSeconds(),
            Created = signUp.Created.ToUnixTimeSeconds()
        };
}

using System;
using System.Collections.Generic;
using AdminPortal.Dtos.Response;
using AdminPortal.Models;
using AdminPortal.Services;

namespace AdminPortal.Utilities.Local;

public static class MockData
{
    public static readonly Guid Org1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Org2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Org3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly string Tin1 = "11223344";
    public static readonly string Tin2 = "13371337";

    public static readonly List<GetOrganizationsResponseItem> Organizations = new()
    {
        new(Org1Id, "Producent A/S", "11223344", "Active"),
        new(Org2Id, "TrialCorp", "13371337", "Test"),
    };

    public static readonly List<GetWhitelistedOrganizationsResponseItem> WhitelistedOrganizations = new()
    {
        new(Org1Id, "11223344")
    };

    public static readonly List<GetContractsForAdminPortalResponseItem> Contracts = new()
    {
        new("571313131313131313", Org1Id.ToString(), DateTimeOffset.Now.AddDays(-30).ToUnixTimeSeconds(),
            DateTimeOffset.Now.AddDays(-29).ToUnixTimeSeconds(), null, MeteringPointType.Production),
        new("571414141414141414", Org2Id.ToString(), DateTimeOffset.Now.AddDays(-60).ToUnixTimeSeconds(),
            DateTimeOffset.Now.AddDays(-59).ToUnixTimeSeconds(), DateTimeOffset.Now.AddDays(30).ToUnixTimeSeconds(), MeteringPointType.Consumption)
    };

    public static readonly Dictionary<Guid, List<GetMeteringPointsResponseItem>> MeteringPoints = new()
    {
        {
            Org1Id,
            new List<GetMeteringPointsResponseItem>
            {
                new("571313131313131313", MeteringPointType.Production),
                new("571313131313131314", MeteringPointType.Consumption)
            }
        },
        {
            Org2Id,
            new List<GetMeteringPointsResponseItem>
            {
                new("571414141414141414", MeteringPointType.Consumption)
            }
        }
    };

    public static readonly Dictionary<string, CvrCompanyInformationDto> CompanyInformation = new()
    {
        {
            Tin1,
            new CvrCompanyInformationDto
            {
                Tin = Tin1,
                Name = "Producent A/S",
                City = "Producent City",
                ZipCode = "9999",
                Address = "Producentvej 10"
            }
        },
        {
            Tin2,
            new CvrCompanyInformationDto
            {
                Tin = Tin2,
                Name = "TrialCorp",
                City = "Trial City",
                ZipCode = "2222",
                Address = "Trial boulevard 15"
            }
        }
    };

    public static CvrCompaniesListResponse GetCompanies()
    {
        return new CvrCompaniesListResponse
        {
            Result =
            [
               new()
               {
                   Name = "Producent A/S",
                   Tin = Tin1
               },
               new()
               {
                   Name = "TrialCorp",
                   Tin = Tin2
               }
            ],

        };
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using EnergyOrigin.Domain.ValueObjects;

namespace API.IntegrationTests;

public class MockAuthorizationClient : IAuthorizationClient
{
    public static List<UserOrganizationConsentsResponseItem> MockedConsents = new List<UserOrganizationConsentsResponseItem>()
    {
        new UserOrganizationConsentsResponseItem(
            Guid.NewGuid(),
            new Guid("d37337e8-035a-4f1c-a416-eae9375148e1"),
            "12345678",
            "A",
            new Guid("7adc659d-ad17-4d2d-a92f-b9904bbd306d"),
            "87654321",
            "B",
            UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
        ),
        new UserOrganizationConsentsResponseItem(
            System.Guid.NewGuid(),
            new Guid("7adc659d-ad17-4d2d-a92f-b9904bbd306d"), // Sender
            "87654321",
            "B",
            new Guid("d37337e8-035a-4f1c-a416-eae9375148e1"),
            "12345678",
            "A",
            UnixTimestamp.Now().ToDateTimeOffset().ToUnixTimeSeconds()
        )
    };

    public async Task<UserOrganizationConsentsResponse?> GetConsentsAsync()
    {
        await Task.Delay(1);
        var response = new UserOrganizationConsentsResponse(MockedConsents);

        return response;
    }
}

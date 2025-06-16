using System;
using System.Linq;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;

namespace API.UnitTests;

public static class Any
{
    public const string Base64EncodedWalletDepositEndpoint = "eyJFbmRwb2ludCI6Imh0dHA6Ly9sb2NhbGhvc3Q6Nzg5MC8iLCJQdWJsaWNLZXkiOiJBVTBWVFVzQUFBQUJ5aE5KRmxENlZhVUZPajRGRzcybmVkSmxVbDRjK0xVejdpV0tRNEkzM1k0Q2J5OVBQTm5SdXRuaWUxT1NVRS9ud0RWTWV3bW14TnFFTkw5a0RZeHdMQWs9IiwiVmVyc2lvbiI6MX0=";

    public static OrganizationId OrganizationId()
    {
        return EnergyOrigin.Domain.ValueObjects.OrganizationId.Create(Guid.NewGuid());
    }

    public static OrganizationName OrganizationName()
    {
        return EnergyOrigin.Domain.ValueObjects.OrganizationName.Create("AnyOrgName" + Guid.NewGuid());
    }

    public static TransferAgreement TransferAgreement(OrganizationId senderId, OrganizationId receiverId)
    {
        return new TransferAgreement
        {
            ReceiverId = OrganizationId(),
            SenderId = OrganizationId(),
            EndDate = UnixTimestamp.Now(),
            StartDate = UnixTimestamp.Now().Add(TimeSpan.FromHours(-2)),
            ReceiverName = OrganizationName(),
            SenderName = OrganizationName(),
            Id = Guid.NewGuid(),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = EnergyTrackAndTrace.Testing.Any.Tin(),
            SenderTin = EnergyTrackAndTrace.Testing.Any.Tin(),
            TransferAgreementNumber = 0,
            Type = TransferAgreementType.TransferAllCertificates
        };
    }

    public static Gsrn Gsrn()
    {
        return new Gsrn("57" + IntString(16));
    }

    private static string IntString(int charCount)
    {
        var alphabet = "0123456789";
        var random = new Random();
        var characterSelector = new Func<int, string>(_ => alphabet.Substring(random.Next(0, alphabet.Length), 1));
        return Enumerable.Range(1, charCount).Select(characterSelector).Aggregate((a, b) => a + b);
    }
}

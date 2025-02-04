using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using TransferAgreementAutomation.Worker;
using Xunit;

namespace Worker.UnitTests;

public class TransferAgreementProcessOrderComparerTest
{
    [Fact]
    public void GivenTransferAgreements_WhenSortingInProcessOrder_TransferAllAgreementsAreLast()
    {
        var ta1 = CreateTransferAgreement(TransferAgreementType.TransferAllCertificates);
        var ta2 = CreateTransferAgreement(TransferAgreementType.TransferAllCertificates);
        var ta3 = CreateTransferAgreement(TransferAgreementType.TransferCertificatesBasedOnConsumption);
        var ta4 = CreateTransferAgreement(TransferAgreementType.TransferCertificatesBasedOnConsumption);
        var transferAgreements = new List<TransferAgreement> { ta4, ta1, ta3, ta2 };

        transferAgreements.Sort(new TransferAgreementProcessOrderComparer());

        transferAgreements[0].Id.ToString().Should().BeOneOf(ta3.Id.ToString(), ta4.Id.ToString());
        transferAgreements[1].Id.ToString().Should().BeOneOf(ta3.Id.ToString(), ta4.Id.ToString());
        transferAgreements[2].Id.ToString().Should().BeOneOf(ta1.Id.ToString(), ta2.Id.ToString());
        transferAgreements[3].Id.ToString().Should().BeOneOf(ta1.Id.ToString(), ta2.Id.ToString());
    }

    private static TransferAgreement CreateTransferAgreement(TransferAgreementType type)
    {
        var transferAgreement = new TransferAgreement
        {
            EndDate = UnixTimestamp.Now().AddHours(2),
            ReceiverReference = Guid.NewGuid(),
            ReceiverTin = Tin.Create("12345678"),
            SenderId = OrganizationId.Create(Guid.NewGuid()),
            StartDate = UnixTimestamp.Now().AddHours(1),
            Id = Guid.NewGuid(),
            SenderName = OrganizationName.Create("SomeSender"),
            SenderTin = Tin.Create("11223344"),
            TransferAgreementNumber = 0,
            Type = type
        };
        return transferAgreement;
    }
}

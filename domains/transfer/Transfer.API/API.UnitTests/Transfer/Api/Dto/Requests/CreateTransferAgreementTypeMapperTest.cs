using API.Transfer.Api.Dto.Requests;
using DataContext.Models;
using EnergyTrackAndTrace.Testing;
using Xunit;
using static API.Transfer.Api.Dto.Requests.CreateTransferAgreementTypeMapper;

namespace API.UnitTests.Transfer.Api.Dto.Requests;

public class CreateTransferAgreementTypeMapperTest
{
    [Fact]
    public void GivenRequestAndModelEnums_WhenMapping_NamesAndValuesMatch()
    {
        EnumTest.AssertEquivalentEnums<CreateTransferAgreementType, TransferAgreementType>();
    }

    [Theory]
    [InlineData(TransferAgreementType.TransferAllCertificates, null)]
    [InlineData(TransferAgreementType.TransferAllCertificates, CreateTransferAgreementType.TransferAllCertificates)]
    [InlineData(TransferAgreementType.TransferCertificatesBasedOnConsumption, CreateTransferAgreementType.TransferCertificatesBasedOnConsumption)]
    public void GivenRequestEnum_WhenMapping_MappingIsComplete(TransferAgreementType expected, CreateTransferAgreementType? request)
    {
        Assert.Equal(expected, MapCreateTransferAgreementType(request));
    }
}

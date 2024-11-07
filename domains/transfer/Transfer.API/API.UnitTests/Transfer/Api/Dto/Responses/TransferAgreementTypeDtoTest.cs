using API.Transfer.Api.Dto.Responses;
using DataContext.Models;
using EnergyTrackAndTrace.Testing;
using Xunit;

namespace API.UnitTests.Transfer.Api.Dto.Responses;

public class TransferAgreementTypeDtoTest
{
    [Fact]
    public void GivenRequestAndModelEnums_WhenMapping_NamesAndValuesMatch()
    {
        EnumTest.AssertEquivalentEnums<TransferAgreementTypeDto, TransferAgreementType>();
    }

    [Theory]
    [InlineData(TransferAgreementTypeDto.TransferAllCertificates, TransferAgreementType.TransferAllCertificates)]
    [InlineData(TransferAgreementTypeDto.TransferCertificatesBasedOnConsumption, TransferAgreementType.TransferCertificatesBasedOnConsumption)]
    public void GivenRequestEnum_WhenMapping_MappingIsComplete(TransferAgreementTypeDto expected, TransferAgreementType modelType)
    {
        Assert.Equal(expected, TransferAgreementTypeMapper.MapCreateTransferAgreementType(modelType));
    }
}

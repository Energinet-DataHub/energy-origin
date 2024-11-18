using System;
using System.Linq;
using API.Transfer.Api.Dto.Requests;
using Xunit;

namespace API.IntegrationTests.Transfer.Api.Dto.Requests;

public class CreateTransferAgreementProposalValidatorTest
{
    private readonly CreateTransferAgreementProposalValidator sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("123456789")]
    [InlineData("ABCDEFG")]
    public void Create_ShouldFail_WhenReceiverTinInvalid(string receiverTin)
    {
        var request = new CreateTransferAgreementProposal(
            StartDate: DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            EndDate: DateTimeOffset.UtcNow.AddDays(2).ToUnixTimeSeconds(),
            ReceiverTin: receiverTin
        );

        var result = sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains("ReceiverTin must be 8 digits without any spaces.", result.Errors.Select(e => e.ErrorMessage));
    }
}

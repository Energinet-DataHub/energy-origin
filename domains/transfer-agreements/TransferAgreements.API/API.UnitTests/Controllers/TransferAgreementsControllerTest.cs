using System.Threading.Tasks;
using API.Controllers;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace API.UnitTests.Controllers;

public class TransferAgreementsControllerTest
{
    [Fact]
    public void ShouldReturnSuccessfulAnswer()
    {

        var transferAgreementController = new TransferAgreementsController();
        var result = TransferAgreementsController.Create();

    }
}

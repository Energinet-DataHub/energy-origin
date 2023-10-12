using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;

namespace Unit.Tests.Services;

public class CompanyServiceTests
{
    private readonly ICompanyService companyService;
    private readonly ICompanyRepository repository = Substitute.For<ICompanyRepository>();

    public CompanyServiceTests() => companyService = new CompanyService(repository);

    [Fact]
    public async Task GetCompanyByTin_ShouldReturnCompany_WhenCompanyExists()
    {
        var tin = Guid.NewGuid().ToString();

        repository.GetCompanyByTinAsync(Arg.Any<string>()).Returns(new Company
        {
            Id = Guid.NewGuid(),
            Tin = tin
        });

        var result = await companyService.GetCompanyByTinAsync(tin);

        Assert.NotNull(result);
        Assert.Equal(tin, result.Tin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("89a65914-ccf8-473e-afa4-8055a31e906c")]
    public async Task GetCompanyByTin_ShouldReturnNull_WhenNoCompanyExists(string? tin)
    {
        repository.GetCompanyByTinAsync(Arg.Any<string>()).Returns(null as Company);

        var result = await companyService.GetCompanyByTinAsync(tin);

        Assert.Null(result);
    }
}

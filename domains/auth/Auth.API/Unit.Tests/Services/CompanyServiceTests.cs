using API.Models.Entities;
using API.Repositories.Interfaces;
using API.Services;
using API.Services.Interfaces;

namespace Tests.Services;

public class CompanyServiceTests
{
    private readonly ICompanyService companyService;
    private readonly ICompanyRepository repository = Mock.Of<ICompanyRepository>();

    public CompanyServiceTests() => companyService = new CompanyService(repository);

    [Fact]
    public async Task GetCompanyByTin_ShouldReturnCompany_WhenCompanyExists()
    {
        var tin = Guid.NewGuid().ToString();

        Mock.Get(repository)
            .Setup(x => x.GetCompanyByTinAsync(It.IsAny<string>()))
            .ReturnsAsync(value: new Company()
            {
                Tin = tin
            });

        var result = await companyService.GetCompanyByTinAsync(tin);

        Assert.NotNull(result);
        Assert.Equal(tin, result?.Tin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("89a65914-ccf8-473e-afa4-8055a31e906c")]
    public async Task GetCompanyByTin_ShouldReturnNull_WhenNoCompanyExists(string? tin)
    {
        Mock.Get(repository)
            .Setup(x => x.GetCompanyByTinAsync(It.IsAny<string>()))
            .ReturnsAsync(value: null);

        var result = await companyService.GetCompanyByTinAsync(tin);

        Assert.Null(result);
    }
}

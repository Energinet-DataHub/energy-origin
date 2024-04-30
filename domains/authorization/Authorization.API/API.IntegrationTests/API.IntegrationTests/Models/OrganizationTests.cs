using API.IntegrationTests.Setup;
using API.Models;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.IntegrationTests.Models;

[Collection(nameof(DatabaseTestCollection))]
public class OrganizationTests(IntegrationTestFactory factory) : DatabaseTest(factory)
{
    [Fact]
    public async Task InsertDuplicateOrganization_ThrowsException()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("testOrganizationName");

        var organization1 = Organization.Create(idpId, idpOrganizationId, tin, organizationName);
        var organization2 = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        await UnitOfWork.BeginTransactionAsync();
        await OrganizationRepository.AddAsync(organization1);
        await UnitOfWork.CommitAsync();

        await UnitOfWork.BeginTransactionAsync();
        await OrganizationRepository.AddAsync(organization2);
        var act = async () => await UnitOfWork.CommitAsync();

        await act.Should().ThrowAsync<DbUpdateException>()
            .Where(e => e.InnerException is PostgresException);
    }
}

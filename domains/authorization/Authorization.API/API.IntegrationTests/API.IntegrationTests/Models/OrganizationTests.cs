using API.IntegrationTests.Setup;
using API.Models;
using API.Repository;
using API.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace API.IntegrationTests.Models;

[Collection(nameof(DatabaseTestCollection))]
public class OrganizationTests(IntegrationTestFactory factory) : DatabaseTest(factory)
{
    [Fact]
    public async Task AddOrganization_Success()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("testOrganizationName");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        await UnitOfWork.BeginTransactionAsync();
        await OrganizationRepository.AddAsync(organization);
        await UnitOfWork.CommitAsync();

        var addedOrganization = await OrganizationRepository.GetAsync(organization.Id);
        addedOrganization.Should().BeEquivalentTo(organization);
    }

    [Fact]
    public async Task RemoveOrganization_Success()
    {
        var idpId = new IdpId(Guid.NewGuid());
        var idpOrganizationId = new IdpOrganizationId(Guid.NewGuid());
        var tin = new Tin("12345678");
        var organizationName = new OrganizationName("testOrganizationName");

        var organization = Organization.Create(idpId, idpOrganizationId, tin, organizationName);

        await UnitOfWork.BeginTransactionAsync();
        await OrganizationRepository.AddAsync(organization);
        await UnitOfWork.CommitAsync();

        await UnitOfWork.BeginTransactionAsync();
        OrganizationRepository.Remove(organization);
        await UnitOfWork.CommitAsync();

        var act = async () => await OrganizationRepository.GetAsync(organization.Id);
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

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

    [Theory]
    [InlineData("IdpId")]
    [InlineData("IdpOrganizationId")]
    [InlineData("Tin")]
    [InlineData("OrganizationName")]
    public async Task InsertOrganizationWithNullField_ThrowsException(string fieldName)
    {
        var sql = "INSERT INTO \"Organizations\" (\"Id\", \"IdpId\", \"IdpOrganizationId\", \"Tin\", \"OrganizationName\")" +
                  " VALUES (@Id, @IdpId, @IdpOrganizationId, @Tin, @OrganizationName)";

        await UnitOfWork.BeginTransactionAsync();
        var exception = await Record.ExceptionAsync(async () =>
        {
            await using var command = Db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            var id = Guid.NewGuid();
            var idpId = Guid.NewGuid();
            var idpOrganizationId = Guid.NewGuid();
            var tin = "12345678";
            var organizationName = "testOrganizationName";

            command.Parameters.Add(new NpgsqlParameter("Id", id) { NpgsqlDbType = NpgsqlDbType.Uuid });
            command.Parameters.Add(new NpgsqlParameter("IdpId", fieldName == "IdpId" ? DBNull.Value : idpId) { NpgsqlDbType = NpgsqlDbType.Uuid });
            command.Parameters.Add(new NpgsqlParameter("IdpOrganizationId", fieldName == "IdpOrganizationId" ? DBNull.Value : idpOrganizationId) { NpgsqlDbType = NpgsqlDbType.Uuid });
            command.Parameters.Add(new NpgsqlParameter("Tin", fieldName == "Tin" ? DBNull.Value : tin) { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter("OrganizationName", fieldName == "OrganizationName" ? DBNull.Value : organizationName) { NpgsqlDbType = NpgsqlDbType.Text });

            await command.ExecuteNonQueryAsync();
        });

        exception.Should().BeOfType<PostgresException>();
        ((PostgresException)exception!).SqlState.Should().Be("23502"); // Not null violation
    }

    [Theory]
    [InlineData("IdpId")]
    [InlineData("IdpOrganizationId")]
    [InlineData("Tin")]
    [InlineData("OrganizationName")]
    public async Task UpdateOrganizationWithNullField_ThrowsException(string fieldName)
    {
        var id = Guid.NewGuid();
        var idpId = Guid.NewGuid();
        var idpOrganizationId = Guid.NewGuid();
        var tin = "12345678";
        var organizationName = "testOrganizationName";

        var sql = "INSERT INTO \"Organizations\" (\"Id\", \"IdpId\", \"IdpOrganizationId\", \"Tin\", \"OrganizationName\")" +
                        " VALUES (@Id, @IdpId, @IdpOrganizationId, @Tin, @OrganizationName)";

        await UnitOfWork.BeginTransactionAsync();
        await using (var insertCommand = Db.Database.GetDbConnection().CreateCommand())
        {
            insertCommand.CommandText = sql;
            insertCommand.Parameters.Add(new NpgsqlParameter("Id", id) { NpgsqlDbType = NpgsqlDbType.Uuid });
            insertCommand.Parameters.Add(new NpgsqlParameter("IdpId", idpId) { NpgsqlDbType = NpgsqlDbType.Uuid });
            insertCommand.Parameters.Add(new NpgsqlParameter("IdpOrganizationId", idpOrganizationId) { NpgsqlDbType = NpgsqlDbType.Uuid });
            insertCommand.Parameters.Add(new NpgsqlParameter("Tin", tin) { NpgsqlDbType = NpgsqlDbType.Text });
            insertCommand.Parameters.Add(new NpgsqlParameter("OrganizationName", organizationName) { NpgsqlDbType = NpgsqlDbType.Text });
            await insertCommand.ExecuteNonQueryAsync();
        }
        await UnitOfWork.CommitAsync();

        var updateSql = $"UPDATE \"Organizations\" SET \"{fieldName}\" = NULL WHERE \"Id\" = @Id";

        await UnitOfWork.BeginTransactionAsync();
        var exception = await Record.ExceptionAsync(async () =>
        {
            await using var updateCommand = Db.Database.GetDbConnection().CreateCommand();
            updateCommand.CommandText = updateSql;
            updateCommand.Parameters.Add(new NpgsqlParameter("Id", id) { NpgsqlDbType = NpgsqlDbType.Uuid });
            await updateCommand.ExecuteNonQueryAsync();
        });

        exception.Should().BeOfType<PostgresException>();
        ((PostgresException)exception!).SqlState.Should().Be("23502"); // Not null violation
    }
}

// using API.Authorization.Exceptions;
// using API.IntegrationTests.Setup;
// using API.UnitTests;
// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Npgsql;
//
// namespace API.IntegrationTests.Models;
//
// public class OrganizationTests(IntegrationTestFixture fixture) : DatabaseTest(fixture)
// {
//     [Fact]
//     public async Task AddOrganization_Success()
//     {
//         var organization = Any.Organization();
//
//         await UnitOfWork.BeginTransactionAsync();
//         await OrganizationRepository.AddAsync(organization, CancellationToken.None);
//         await UnitOfWork.CommitAsync();
//
//         var addedOrganization = await OrganizationRepository.GetAsync(organization.Id, CancellationToken.None);
//         addedOrganization.Should().BeEquivalentTo(organization);
//     }
//
//     [Fact]
//     public async Task RemoveOrganization_Success()
//     {
//         var organization = Any.Organization();
//
//         await UnitOfWork.BeginTransactionAsync();
//         await OrganizationRepository.AddAsync(organization, CancellationToken.None);
//         await UnitOfWork.CommitAsync();
//
//         await UnitOfWork.BeginTransactionAsync();
//         OrganizationRepository.Remove(organization);
//         await UnitOfWork.CommitAsync();
//
//         var act = async () =>
//             await OrganizationRepository.GetAsync(organization.Id, CancellationToken.None);
//         await act.Should().ThrowAsync<EntityNotFoundException>();
//     }
//
//     [Fact]
//     public async Task InsertDuplicateOrganization_ThrowsException()
//     {
//         var organization = Any.Organization();
//
//         await UnitOfWork.BeginTransactionAsync();
//         await OrganizationRepository.AddAsync(organization, CancellationToken.None);
//         await UnitOfWork.CommitAsync();
//
//         await UnitOfWork.BeginTransactionAsync();
//         await OrganizationRepository.AddAsync(organization, CancellationToken.None);
//         var act = async () => await UnitOfWork.CommitAsync();
//
//         await act.Should().ThrowAsync<DbUpdateException>()
//             .Where(e => e.InnerException is PostgresException);
//     }
// }

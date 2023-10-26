using System;
using System.Linq;
using API.Claiming.Api.Models;
using API.IntegrationTests.Factories;
using API.Shared.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Claiming.Api.Repository;

public class ClaimSubjectRepositoryTest : IClassFixture<TransferAgreementsApiWebApplicationFactory>, IDisposable
{
    private readonly ApplicationDbContext dbContext;
    private readonly IServiceScope scope;

    public ClaimSubjectRepositoryTest(TransferAgreementsApiWebApplicationFactory factory)
    {
        scope = factory.Services.CreateScope();
        dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public void history_is_created_when_claimsubject_inserted()
    {
        TruncateDb();

        dbContext.ClaimSubjects.Add(new ClaimSubject(Guid.NewGuid()));
        dbContext.SaveChanges();

        var history = dbContext.ClaimSubjectHistory.ToList();
        history.Should().HaveCount(1);
        history[0].AuditAction.Should().Be("Insert");
    }

    [Fact]
    public void history_is_created_when_claimsubject_deleted()
    {
        TruncateDb();

        var subject = Guid.NewGuid();
        dbContext.ClaimSubjects.Add(new ClaimSubject(subject));
        dbContext.SaveChanges();

        dbContext.ClaimSubjects.Remove(dbContext.ClaimSubjects.First(c => c.SubjectId == subject));
        dbContext.SaveChanges();

        var history = dbContext.ClaimSubjectHistory.ToList();
        history.Should().HaveCount(2);
        history[1].AuditAction.Should().Be("Delete");
    }

    private void TruncateDb()
    {
        dbContext.RemoveAll(d => d.ClaimSubjects);
        dbContext.RemoveAll(d => d.ClaimSubjectHistory);
        dbContext.SaveChanges();
    }
    public void Dispose()
    {
        scope.Dispose();
        dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}

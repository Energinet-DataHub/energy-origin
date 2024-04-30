using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.IntegrationTests.Transfer.Api;

public class TestData
{
    public static async Task SeedTransferAgreements(ApplicationDbContext dbContext, IEnumerable<TransferAgreement> transferAgreements)
    {
        await dbContext.TruncateTableAsync<TransferAgreement>();
        foreach (var agreement in transferAgreements)
        {
            await InsertTransferAgreement(dbContext, agreement);
            await InsertTransferAgreementHistoryEntry(dbContext, agreement);
        }
    }

    public static async Task SeedTransferAgreementsSaveChangesAsync(ApplicationDbContext dbContext, TransferAgreement transferAgreement)
    {
        dbContext.TransferAgreements.Add(transferAgreement);
        await dbContext.SaveChangesAsync();
    }

    public static async Task SeedTransferAgreementProposals(ApplicationDbContext dbContext, IEnumerable<TransferAgreementProposal> proposals)
    {
        foreach (var proposal in proposals)
        {
            dbContext.TransferAgreementProposals.Add(proposal);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task InsertTransferAgreement(ApplicationDbContext dbContext, TransferAgreement agreement)
    {
        var agreementsTable = dbContext.Model.FindEntityType(typeof(TransferAgreement))!.GetTableName();

        var agreementQuery =
            $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverReference\", \"TransferAgreementNumber\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverReference, @TransferAgreementNumber)";
        object[] agreementFields =
        {
            new NpgsqlParameter("Id", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin),
            new NpgsqlParameter("ReceiverReference", agreement.ReceiverReference),
            new NpgsqlParameter("TransferAgreementNumber", agreement.TransferAgreementNumber)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }

    private static async Task InsertTransferAgreementHistoryEntry(ApplicationDbContext dbContext, TransferAgreement agreement)
    {
        var historyTable = dbContext.Model.FindEntityType(typeof(TransferAgreementHistoryEntry))!.GetTableName();

        var historyQuery =
            $"INSERT INTO \"{historyTable}\" (\"Id\", \"CreatedAt\", \"AuditAction\", \"ActorId\", \"ActorName\", \"TransferAgreementId\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\") " +
            "VALUES (@Id, @CreatedAt, @AuditAction, @ActorId, @ActorName, @TransferAgreementId, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin)";
        object[] historyFields =
        {
            new NpgsqlParameter("Id", Guid.NewGuid()),
            new NpgsqlParameter("CreatedAt", DateTime.UtcNow),
            new NpgsqlParameter("AuditAction", "Insert"),
            new NpgsqlParameter("ActorId", "Test"),
            new NpgsqlParameter("ActorName", "Test"),
            new NpgsqlParameter("TransferAgreementId", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate),
            new NpgsqlParameter("EndDate", agreement.EndDate),
            new NpgsqlParameter("SenderId", agreement.SenderId),
            new NpgsqlParameter("SenderName", agreement.SenderName),
            new NpgsqlParameter("SenderTin", agreement.SenderTin),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin)
        };

        await dbContext.Database.ExecuteSqlRawAsync(historyQuery, historyFields);
    }
}

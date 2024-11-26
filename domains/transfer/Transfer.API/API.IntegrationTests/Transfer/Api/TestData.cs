using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
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
            $"INSERT INTO \"{agreementsTable}\" (\"Id\", \"StartDate\", \"EndDate\", \"SenderId\", \"SenderName\", \"SenderTin\", \"ReceiverTin\", \"ReceiverId\", \"ReceiverReference\", \"TransferAgreementNumber\", \"Type\") VALUES (@Id, @StartDate, @EndDate, @SenderId, @SenderName, @SenderTin, @ReceiverTin, @ReceiverId, @ReceiverReference, @TransferAgreementNumber, @Type)";
        object[] agreementFields =
        {
            new NpgsqlParameter("Id", agreement.Id),
            new NpgsqlParameter("StartDate", agreement.StartDate.ToDateTimeOffset()),
            new NpgsqlParameter("EndDate", agreement.EndDate?.ToDateTimeOffset()),
            new NpgsqlParameter("SenderId", agreement.SenderId.Value),
            new NpgsqlParameter("SenderName", agreement.SenderName.Value),
            new NpgsqlParameter("SenderTin", agreement.SenderTin.Value),
            new NpgsqlParameter("ReceiverTin", agreement.ReceiverTin.Value),
            new NpgsqlParameter("ReceiverId", agreement.ReceiverId?.Value ?? OrganizationId.Create(Guid.NewGuid()).Value), // if clause can be removed when ReceiverId is made non-nullable
            new NpgsqlParameter("ReceiverReference", agreement.ReceiverReference),
            new NpgsqlParameter("TransferAgreementNumber", agreement.TransferAgreementNumber),
            new NpgsqlParameter("Type", (int)agreement.Type)
        };

        await dbContext.Database.ExecuteSqlRawAsync(agreementQuery, agreementFields);
    }
}

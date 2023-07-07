using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class TransferAgreementRepository : ITransferAgreementRepository
{
    private readonly ApplicationDbContext context;
    public TransferAgreementRepository(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement)
    {
        context.TransferAgreements.Add(transferAgreement);
        await context.SaveChangesAsync();
        return transferAgreement;
    }

    public async Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin) =>
        await context.TransferAgreements
            .Where(ta => ta.SenderId == subjectId
                         || ta.ReceiverTin == receiverTin)
            .ToListAsync();

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin) =>
        await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
            .FirstOrDefaultAsync();

    public async Task Save() => await context.SaveChangesAsync();

    public async Task<bool> HasDateOverlap(TransferAgreement transferAgreement)
    {

        var overlappingAgreements = await context.TransferAgreements
            .Where(t => t.SenderId == transferAgreement.SenderId &&
                        t.ReceiverTin == transferAgreement.ReceiverTin &&
                        t.Id != transferAgreement.Id)
            .ToListAsync();

        return overlappingAgreements.Any(a =>
            IsOverlappingTransferAgreement(a, transferAgreement.StartDate, transferAgreement.EndDate)
        );
    }

    public async Task<List<DateRange>> GetAvailableDateRanges(Guid senderId, string receiverTin)
    {
        var agreements = await context.TransferAgreements
            .Where(a => a.SenderId == senderId && a.ReceiverTin == receiverTin)
            .OrderBy(a => a.StartDate)
            .ToListAsync();

        if (!agreements.Any())
        {
            return new List<DateRange>
            {
                new()
                {
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = null
                }
            };
        }

        var firstGap = agreements.First().StartDate > DateTimeOffset.UtcNow
            ? new DateRange
            {
                StartDate = DateTimeOffset.UtcNow,
                EndDate = agreements.First().StartDate.AddDays(-1)
            }
            : null;

        var lastGap = agreements.Last().EndDate.HasValue
            ? new DateRange
            {
                StartDate = agreements.Last().EndDate.GetValueOrDefault().AddDays(1),
                EndDate = null
            }
            : null;

        var middleGaps = agreements
            .SkipLast(1)
            .Select((a, i) => new DateRange
            {
                StartDate = a.EndDate.GetValueOrDefault().AddDays(1),
                EndDate = agreements[i + 1].StartDate.AddDays(-1)
            })
            .Where(dr => dr.StartDate < dr.EndDate);

        var dateRanges = new[] { firstGap }
            .Concat(middleGaps)
            .Concat(new[] { lastGap })
            .Where(dr => dr != null)
            .ToList();

        return dateRanges;
    }

    private static bool IsOverlappingTransferAgreement(TransferAgreement transferAgreement, DateTimeOffset startDate, DateTimeOffset? endDate) =>
        !(startDate >= transferAgreement.EndDate || endDate <= transferAgreement.StartDate);
}

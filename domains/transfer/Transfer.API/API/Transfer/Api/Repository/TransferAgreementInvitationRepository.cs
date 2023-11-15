using System;
using System.Threading.Tasks;
using API.Connections.Api.Models;
using API.Shared.Data;
using API.Transfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementInvitationRepository
{
    Task AddTransferAgreementInvitation(TransferAgreementInvitation invitation);
    Task DeleteTransferAgreementInvitation(Guid id);
    Task DeleteOldTransferAgreementInvitations(DateTimeOffset olderThan);
    Task<TransferAgreementInvitation?> GetNonExpiredTransferAgreementInvitation(Guid id);
}

public class TransferAgreementInvitationRepository : ITransferAgreementInvitationRepository
{
    private readonly ApplicationDbContext context;

    public TransferAgreementInvitationRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task AddTransferAgreementInvitation(TransferAgreementInvitation invitation)
    {
        context.TransferAgreementInvitations.Add(invitation);
        await context.SaveChangesAsync();
    }

    public Task DeleteTransferAgreementInvitation(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task DeleteOldTransferAgreementInvitations(DateTimeOffset olderThan)
    {
        throw new NotImplementedException();
    }

    public async Task<TransferAgreementInvitation?> GetNonExpiredTransferAgreementInvitation(Guid id)
    {
        var invitation = await context.TransferAgreementInvitations
            .FirstOrDefaultAsync(i => i.CreatedAt > DateTimeOffset.UtcNow.AddDays(-14) && i.Id == id);

        return invitation;
    }
}

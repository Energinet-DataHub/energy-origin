using System;
using System.Threading.Tasks;
using API.Api.ApiModels;
using API.Infrastructure.Data;

namespace API.Api.Create;

public class CreateTransferAgreementService
{

    private readonly ApplicationDbContext context;

    public CreateTransferAgreementService(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> Create(DateTimeOffset startDate, DateTimeOffset endDate, Subject sender, Subject receiver)
    {



            var transferAgreement = new TransferAgreement
            {
                Id = Guid.NewGuid(),
                StartDate = startDate,
                EndDate = endDate,
                Sender = sender,
                Receiver = receiver
            };
            context.TransferAgreements.Add(transferAgreement);
            await context.SaveChangesAsync();
            return transferAgreement;


    }
}

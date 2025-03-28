using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace API.Transfer.Api._Features_;

public record DeleteTransferAgreementsCommand(OrganizationId OrganizationId) : IRequest;

public class DeleteTransferAgreementsCommandHandler : IRequestHandler<DeleteTransferAgreementsCommand>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteTransferAgreementsCommandHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteTransferAgreementsCommand request, CancellationToken cancellationToken)
    {
        var tasToDelete = _dbContext
            .TransferAgreements
            .Where(x => (x.ReceiverId != null && x.ReceiverId == request.OrganizationId) || x.SenderId == request.OrganizationId);

        if (tasToDelete.Any())
        {
            _dbContext.TransferAgreements.RemoveRange(tasToDelete);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}


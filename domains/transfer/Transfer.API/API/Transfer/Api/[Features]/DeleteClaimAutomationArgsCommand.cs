using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;

namespace API.Transfer.Api._Features_;

public record DeleteClaimAutomationArgsCommand(OrganizationId OrganizationId) : IRequest;

public class DeleteClaimAutomationArgsCommandHandler : IRequestHandler<DeleteClaimAutomationArgsCommand>
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteClaimAutomationArgsCommandHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteClaimAutomationArgsCommand request, CancellationToken cancellationToken)
    {
        var argsToDelete = _dbContext
            .ClaimAutomationArguments
            .Where(x => x.SubjectId == request.OrganizationId.Value);

        if (argsToDelete.Any())
        {
            _dbContext.ClaimAutomationArguments.RemoveRange(argsToDelete);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}


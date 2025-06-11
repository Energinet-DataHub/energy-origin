using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.UnitOfWork;
using DataContext;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api._Features_;

public record DeleteClaimAutomationArgsCommand(OrganizationId OrganizationId) : IRequest;

public class DeleteClaimAutomationArgsCommandHandler : IRequestHandler<DeleteClaimAutomationArgsCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteClaimAutomationArgsCommandHandler> _logger;

    public DeleteClaimAutomationArgsCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteClaimAutomationArgsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeleteClaimAutomationArgsCommand request, CancellationToken cancellationToken)
    {
        var argToDelete = await _unitOfWork
            .ClaimAutomationRepository
            .GetClaimAutomationArgument(request.OrganizationId.Value);

        if (argToDelete != null)
        {
            _unitOfWork.ClaimAutomationRepository.DeleteClaimAutomationArgument(argToDelete);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Successfully deleted claim automation argument");
        }
    }
}


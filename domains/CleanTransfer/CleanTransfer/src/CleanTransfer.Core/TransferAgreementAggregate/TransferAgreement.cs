using Ardalis.GuardClauses;
using Ardalis.SharedKernel;
using CleanTransfer.Core.TransferAgreementAggregate.Common;

namespace CleanTransfer.Core.TransferAgreementAggregate;

public class TransferAgreement(
  DateTimeOffset startDate, 
  Representative sender,
  Representative receiver,
  Guid receiverReference, 
  int transferAgreementNumber,
  DateTimeOffset? endDate = null
) : EntityBase, IAggregateRoot
{
  public DateTimeOffset StartDate { get; private set; } = 
    Guard.Against.OutOfRange(startDate, nameof(startDate), DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

  public DateTimeOffset? EndDate { get; private set; } = endDate;
  
   public Representative Sender { get; private set; } = Guard.Against.NullOrInvalidInput(
     sender,
     nameof(sender.Organization.OrganizationTin),
     representative => !representative.Organization.OrganizationTin.Equals(receiver.Organization.OrganizationTin),
     "Sender TIN and Receiver TIN cannot be the same"
   );

  public Representative Receiver { get; private set; } = 
    Guard.Against.Null(receiver, nameof(receiver), "Receiver cannot be null");

  public Guid ReceiverReference { get; private set; } = receiverReference;

  public int TransferAgreementNumber { get; private set; } = 
    Guard.Against.NegativeOrZero(transferAgreementNumber, nameof(transferAgreementNumber));
  
}

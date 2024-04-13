using Ardalis.SharedKernel;

namespace CleanTransfer.Core.TransferAgreementAggregate.Common;

public class Actor( Guid actorId, string name, string email) : ValueObject
{
  public Guid ActorId { get; set; } = actorId;
  public string Name { get; set; } = name;
  public string Email { get; set; } = email;

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Name;
    yield return ActorId;
    yield return Email;
  }
}

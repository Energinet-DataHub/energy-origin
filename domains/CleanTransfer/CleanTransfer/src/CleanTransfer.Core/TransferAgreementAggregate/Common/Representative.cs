using Ardalis.SharedKernel;
using System;

namespace CleanTransfer.Core.TransferAgreementAggregate.Common;

public class Representative(Organization organization, Actor actor) : ValueObject
{
  public Organization Organization { get; set; } = organization ?? throw new ArgumentNullException(nameof(organization), "Organization cannot be null");
  public Actor Actor { get; set; } = actor ?? throw new ArgumentNullException(nameof(actor), "Actor cannot be null");

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Organization;
    yield return Actor;
  }
}

using Ardalis.SharedKernel;

namespace CleanTransfer.Core.TransferAgreementAggregate.Common;

public class Organization( Guid organizationId, string organizationName, Tin organizationTin)
  : ValueObject
{
  public Guid OrganizationId { get; set; } = organizationId;
  public string OrganizationName { get; set; } = organizationName;
  public Tin OrganizationTin { get; set; } = organizationTin;

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return OrganizationId;
    yield return OrganizationName;
    yield return OrganizationTin;
  }
}

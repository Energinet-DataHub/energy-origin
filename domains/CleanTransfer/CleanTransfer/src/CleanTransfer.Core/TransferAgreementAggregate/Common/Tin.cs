using Ardalis.GuardClauses;
using Ardalis.SharedKernel;

namespace CleanTransfer.Core.TransferAgreementAggregate.Common;

public class Tin : ValueObject
{
  private string Value { get; init; }
  private Tin(string value)
  {
    Value = value;
  }
  
  public static Tin Create(string value)
  {
    Guard.Against.NullOrEmpty(value, nameof(value), "TIN cannot be null or empty.");
    Guard.Against.InvalidFormat(value, @"^\d{8}$", nameof(value), "TIN must be an 8-digit number.");

    return new Tin(value);
  }

  protected override IEnumerable<object> GetEqualityComponents()
  {
    yield return Value;
  }

  public override string ToString() => Value;
}

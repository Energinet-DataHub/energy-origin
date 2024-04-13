using Ardalis.Result;
using Ardalis.SharedKernel;

namespace CleanTransfer.UseCases.Contributors.Get;

public record GetContributorQuery(int ContributorId) : IQuery<Result<ContributorDTO>>;

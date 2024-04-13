using Ardalis.Result;
using Ardalis.SharedKernel;

namespace CleanTransfer.UseCases.Contributors.Delete;

public record DeleteContributorCommand(int ContributorId) : ICommand<Result>;

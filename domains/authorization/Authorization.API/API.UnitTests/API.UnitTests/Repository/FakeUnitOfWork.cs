using API.Data;

namespace API.UnitTests.Repository;

public class FakeUnitOfWork : IUnitOfWork
{
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public Task BeginTransactionAsync()
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync()
    {
        Committed = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        RolledBack = true;
        return Task.CompletedTask;
    }
}

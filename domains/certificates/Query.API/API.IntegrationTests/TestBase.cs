using System;
using System.Threading.Tasks;
using API.IntegrationTests.Attributes;
using Xunit;

namespace API.IntegrationTests;

[WriteToConsole]
public abstract class TestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;
    protected TestBase(IntegrationTestFixture fixture) => Fixture = fixture;

    public virtual async ValueTask InitializeAsync()
    {
        await Fixture.ResetDatabaseAsync();
    }

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

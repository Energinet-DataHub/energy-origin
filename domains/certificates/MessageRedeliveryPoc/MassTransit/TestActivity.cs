using MassTransit;

namespace MessageRedeliveryPoc.MassTransit;

public record TestActivityArgs(Guid Id);

public class TestActivity : IExecuteActivity<TestActivityArgs>
{
    private readonly ILogger<TestActivity> logger;

    public TestActivity(ILogger<TestActivity> logger)
    {
        this.logger = logger;
    }

    public Task<ExecutionResult> Execute(ExecuteContext<TestActivityArgs> context)
    {
        logger.LogInformation("TestActivity attempt {RetryAttempt} to consume {Id}", context.GetRetryAttempt(), context.Arguments.Id.ToString());
        throw new Exception("TestActivity");
    }
}

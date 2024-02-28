using MassTransit;

namespace MessageRedeliveryPoc.MassTransit;

public record SomeMessage(Guid Id);

public class SomeMessageConsumer : IConsumer<SomeMessage>
{
    private readonly ILogger<SomeMessageConsumer> logger;
    private readonly IEndpointNameFormatter endpointNameFormatter;

    public SomeMessageConsumer(ILogger<SomeMessageConsumer> logger, IEndpointNameFormatter endpointNameFormatter)
    {
        this.logger = logger;
        this.endpointNameFormatter = endpointNameFormatter;
    }

    public async Task Consume(ConsumeContext<SomeMessage> context)
    {
        var builder = new RoutingSlipBuilder(Guid.NewGuid());

        BuildRoutingSlip(builder, context.Message);

        var routingSlip = builder.Build();

        await context.Execute(routingSlip);
    }

    private void BuildRoutingSlip(RoutingSlipBuilder builder, SomeMessage msg)
    {
        AddActivity<TestActivity, TestActivityArgs>(builder,
            new TestActivityArgs(msg.Id));
    }

    private void AddActivity<T, TArguments>(RoutingSlipBuilder routingSlipBuilder, TArguments arguments)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var uri = new Uri($"exchange:{endpointNameFormatter.ExecuteActivity<T, TArguments>()}");
        routingSlipBuilder.AddActivity(typeof(T).Name, uri, arguments);
    }
}

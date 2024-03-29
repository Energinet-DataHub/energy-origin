using Grpc.Core;
using Grpc.Core.Testing;
using NSubstitute;
using NSubstitute.Core;
using System.Threading.Tasks;

namespace Tests.Extensions;

public static class NSubstituteExtensions
{
    public static ConfiguredCall Returns<TResponse>(this AsyncUnaryCall<TResponse> value, TResponse response)
        where TResponse : class
    {
        var call = TestCalls.AsyncUnaryCall(Task.FromResult(response), Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

        return value.Returns(call);
    }
}

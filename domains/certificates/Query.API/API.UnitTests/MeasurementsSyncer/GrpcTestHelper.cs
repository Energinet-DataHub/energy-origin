using System.Threading.Tasks;
using Grpc.Core;

namespace API.UnitTests.MeasurementsSyncer;

public static class GrpcTestHelper
{
    public static AsyncUnaryCall<T> CreateAsyncUnaryCall<T>(T response)
    {
        var taskResponse = Task.FromResult(response);
        return new AsyncUnaryCall<T>(
            taskResponse,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { }
        );
    }
}

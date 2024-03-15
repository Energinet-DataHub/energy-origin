using MediatR;

namespace Transfer.Application;

public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse> where TResponse : ICommandResponse
{

}

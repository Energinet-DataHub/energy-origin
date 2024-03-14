using MediatR;

namespace Transfer.Application;

public interface ICommand<T> : IRequest<T> where T : ICommandResponse
{

}

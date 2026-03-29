using MediatR;

namespace Shared.BuildingBlocks;

public interface ICommand<TResult> : IRequest<TResult> { }

public interface IQuery<TResult> : IRequest<TResult> { }

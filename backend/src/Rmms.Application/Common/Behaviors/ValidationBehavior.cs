using FluentValidation;
using Mediator;

namespace Rmms.Application.Common.Behaviors;

/// <summary>
/// Mediator pipeline behavior — runs all <see cref="IValidator{T}"/> for a request before reaching handler.
/// Aggregates failures and throws a single <see cref="ValidationException"/> caught by API middleware.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async ValueTask<TResponse> Handle(
        TRequest message,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(message, cancellationToken);

        var ctx = new ValidationContext<TRequest>(message);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(ctx, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next(message, cancellationToken);
    }
}

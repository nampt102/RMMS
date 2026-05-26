namespace Rmms.Application.Common.Interfaces;

/// <summary>
/// Wraps <c>DateTimeOffset.UtcNow</c> so handlers and validators can be tested deterministically.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateOnly UtcToday => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

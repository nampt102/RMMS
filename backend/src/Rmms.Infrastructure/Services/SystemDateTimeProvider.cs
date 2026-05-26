using Rmms.Application.Common.Interfaces;

namespace Rmms.Infrastructure.Services;

/// <summary>Default <see cref="IDateTimeProvider"/> backed by <c>DateTimeOffset.UtcNow</c>.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

namespace Rmms.Domain.Common;

/// <summary>
/// Base entity with strongly-typed UUID v7 primary key.
/// Per <c>knowledge-base/08-coding-standards.md</c> — Database section: PK is <c>uuid</c>, app-generated v7.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = UuidV7.NewGuid();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id && Id != Guid.Empty;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);
    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}

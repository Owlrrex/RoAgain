using System;

/// <summary>
/// Identifies a Localized String
/// Only an Int at the moment, but may contain information about multiple string tables or similar in the future
/// </summary>
[Serializable]
public struct LocalizedStringId
{
    public static readonly LocalizedStringId INVALID = new() { Id = -1 };

    public int Id;
    // Can add stuff like "string bank" here, if that's being added

    public override bool Equals(object obj)
    {
        return obj is LocalizedStringId other && Equals(other);
    }

    public bool Equals(LocalizedStringId other)
    {
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(LocalizedStringId left, LocalizedStringId right) => left.Equals(right);
    public static bool operator !=(LocalizedStringId left, LocalizedStringId right) => !(left == right);

    public override string ToString()
    {
        return Id.ToString();
    }
}
using System;

namespace Sewer56.Update.Tests.TestUtilities;

public class DummyStruct : IEquatable<DummyStruct>
{
    public string String { get; set; } = "Default Name";
    public int Integer { get; set; } = 42;
    public float Float { get; set; } = 6.987654F;
    public bool Boolean { get; set; } = true;

    public bool Equals(DummyStruct other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return String == other.String && Integer == other.Integer && Float.Equals(other.Float) && Boolean == other.Boolean;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DummyStruct)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(String, Integer, Float, Boolean);
    }
}
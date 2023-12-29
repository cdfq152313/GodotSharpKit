namespace GodotSharpKit;

public class SeqList<T> : List<T>
{
    public SeqList() { }

    public SeqList(IEnumerable<T> collection)
        : base(collection) { }

    public SeqList(int capacity)
        : base(capacity) { }

    private bool Equals(SeqList<T> other)
    {
        return this.SequenceEqual(other);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((SeqList<T>)obj);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var v in this)
        {
            hash.Add(v);
        }
        return hash.ToHashCode();
    }
}

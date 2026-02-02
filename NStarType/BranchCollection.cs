namespace CSharp.NStar;

[DebuggerDisplay("{ToString()}")]
public class BranchCollection : Dictionary<String, TreeBranch>
{
	public TreeBranch this[int index] { get => Values.ElementAt(index); set => this[Keys.ElementAt(index)] = value; }

	public BranchCollection() : base()
	{
	}

	public BranchCollection(G.IDictionary<String, TreeBranch> collection) : base(collection)
	{
	}

	public BranchCollection(G.IEnumerable<TreeBranch> collection) : this()
	{
		foreach (var elem in collection)
			Add(elem);
	}

	public virtual void Add(TreeBranch item) => Add("Item" + (Length + 1).ToString(), item);

	public virtual void AddRange(BranchCollection collection)
	{
		foreach (var elem in collection)
			Add(elem.Key, elem.Value);
	}

	public virtual void AddRange(G.IEnumerable<TreeBranch> collection)
	{
		foreach (var elem in collection)
			Add(elem);
	}

	public override bool Equals(object? obj)
	{
		if (obj is not BranchCollection m)
			return false;
		if (Length != m.Length)
			return false;
		for (var i = 0; i < Length; i++)
			if (this[i] != m[i])
				return false;
		return true;
	}

	public override int GetHashCode()
	{
		var hash = 486187739;
		var en = GetEnumerator();
		if (en.MoveNext())
		{
			hash = (hash * 16777619) ^ en.Current.GetHashCode();
			if (en.MoveNext())
			{
				hash = (hash * 16777619) ^ en.Current.GetHashCode();
				hash = (hash * 16777619) ^ this[^1].GetHashCode();
			}
		}
		return hash;
	}

	public virtual void Replace(BranchCollection collection)
	{
		Clear();
		AddRange(collection);
	}

	public virtual void Replace(G.IEnumerable<TreeBranch> collection)
	{
		Clear();
		AddRange(collection);
	}

	public override string ToString() => string.Join(", ", Values.ToArray(x => x.ToShortString()));

	public static bool operator ==(BranchCollection? x, BranchCollection? y) => x?.Equals(y) ?? y is null;

	public static bool operator !=(BranchCollection? x, BranchCollection? y) => !(x == y);
}

public readonly struct BranchCollectionEComparer : G.IEqualityComparer<BranchCollection>
{
	public readonly bool Equals(BranchCollection? x, BranchCollection? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;
		if (x.Length != y.Length)
			return false;
		else if (x.Length == 0 && y.Length == 0)
			return true;
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			if (x[i] != y[i])
				return false;
		}
		return true;
	}

	public readonly int GetHashCode(BranchCollection x)
	{
		var hash = 0;
		for (var i = 0; i < x.Length; i++)
			hash ^= x[i].GetHashCode();
		return hash;
	}
}

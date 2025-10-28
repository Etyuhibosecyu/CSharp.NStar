namespace CSharp.NStar;

[DebuggerDisplay("{ToString()}")]
public sealed class TreeBranch
{
	public String Info { get; set; }
	public int Pos { get; set; }
	public int EndPos { get; set; }
	public List<TreeBranch> Elements { get; set; }
	public BlockStack Container { get; set; }
	public object? Extra { get; set; }
	public TreeBranch? Parent { get; private set; }

	public TreeBranch this[Index index]
	{
		get => Elements[index];
		set
		{
			Elements[index] = value;
			value.Parent = this;
			EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
		}
	}

	public int Length => Elements.Length;
	public int FullCount => Elements.Length + Elements.Sum(x => x.FullCount);
	public int FirstPos => Length == 0 ? Pos : Elements[0].Pos;

	public TreeBranch(String info, int pos, BlockStack container)
	{
		Info = info;
		Pos = pos;
		EndPos = pos + 1;
		Elements = [];
		Container = container;
	}

	public TreeBranch(String info, int pos, int endPos, BlockStack container)
	{
		Info = info;
		Pos = pos;
		EndPos = endPos;
		Elements = [];
		Container = container;
	}

	public TreeBranch(String info, TreeBranch element, BlockStack container)
	{
		Info = info;
		Pos = element.Pos;
		EndPos = element.EndPos;
		Elements = [element];
		element.Parent = this;
		Container = container;
	}

	public TreeBranch(String info, List<TreeBranch> elements, BlockStack container)
	{
		Info = info;
		Pos = elements.Length == 0 ? throw new ArgumentException(null, nameof(elements)) : elements[0].Pos;
		EndPos = elements.Length == 0 ? throw new ArgumentException(null, nameof(elements)) : elements[^1].EndPos;
		Elements = elements;
		Elements.ForEach(x => x.Parent = this);
		Container = container;
	}

	public static TreeBranch DoNotAdd() => new("DoNotAdd", 0, int.MaxValue, new());

	public void Add(TreeBranch item)
	{
		if (item is TreeBranch branch && branch != DoNotAdd())
		{
			Elements.Add(item);
			item.Parent = this;
			EndPos = item.EndPos;
		}
	}

	public void AddRange(G.IEnumerable<TreeBranch> collection)
	{
		Elements.AddRange(collection);
		foreach (var x in collection)
			x.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public List<TreeBranch> GetRange(int index, int count) => Elements.GetRange(index, count);

	public void Insert(int index, TreeBranch item)
	{
		Elements.Insert(index, item);
		item.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void Insert(int index, G.IEnumerable<TreeBranch> collection)
	{
		Elements.Insert(index, collection);
		foreach (var x in collection)
			x.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void Remove(int index, int count)
	{
		Elements.Remove(index, count);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void RemoveEnd(int index)
	{
		Elements.RemoveEnd(index);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void RemoveAt(int index)
	{
		Elements.RemoveAt(index);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void Replace(TreeBranch branch)
	{
		Info = branch.Info;
		Pos = branch.Pos;
		EndPos = branch.EndPos;
		Elements = branch.Elements;
		Container = branch.Container;
		Extra = branch.Extra;
	}

	public override bool Equals(object? obj) => obj is not null
		&& obj is TreeBranch m
		&& Info == m.Info && RedStarLinq.Equals(Elements, m.Elements, (x, y) => new TreeBranchComparer().Equals(x, y));

	public override int GetHashCode() => Info.GetHashCode() ^ Elements.GetHashCode() ^ (Extra?.GetHashCode() ?? 77777777);

	public string ToShortString()
	{
		if (Length != 0)
			return "(" + string.Join(", ", Elements.ToArray(x => x.ToShortString()))
				+ (Extra is null ? "" : " : " + Extra.ToString()) + ")";
		else if (Extra is null)
			return Info.ToString();
		else if (Info == "type" && Extra is NStarType UnvType)
			return UnvType.ToString();
		else
			return "(" + Info + " :: " + Extra.ToString() + ")";
	}

	public override string ToString() => ToString(new());

	private string ToString(BlockStack container)
	{
		var infoString = Info + (RedStarLinq.Equals(container, Container) ? "" : Container.StartsWith(container)
			? "@" + string.Join(".", Container.Skip(container.Length).ToArray(x => x.ToString()))
			: throw new ArgumentException(null, nameof(container)));
		if (Length == 0)
			return (Extra is null ? infoString : "(" + infoString + " :: " + Extra.ToString() + ")") + "#" + Pos.ToString();
		return "(" + infoString + " : " + string.Join(", ", Elements.ToArray(x => x.ToString(Container)))
			+ (Extra is null ? "" : " : " + Extra.ToString()) + ")";
	}

	public static bool operator ==(TreeBranch? x, TreeBranch? y) => x is null && y is null || x is not null && y is not null
		&& x.Info == y.Info && RedStarLinq.Equals(x.Elements, y.Elements, (x, y) => new TreeBranchComparer().Equals(x, y))
		&& (x.Extra?.Equals(y.Extra) ?? y.Extra is null);

	public static bool operator !=(TreeBranch? x, TreeBranch? y) => !(x == y);

	private sealed class TreeBranchComparer : G.IEqualityComparer<TreeBranch>
	{
		public bool Equals(TreeBranch? x, TreeBranch? y) => x is null && y is null || (x?.Equals(y) ?? false);

		public int GetHashCode(TreeBranch x) => x.GetHashCode();
	}
}

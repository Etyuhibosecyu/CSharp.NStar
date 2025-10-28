namespace CSharp.NStar;

[DebuggerDisplay("{ToString()}")]
public sealed class Block(BlockType blockType, String name, int unnamedIndex)
{
	public BlockType BlockType { get; private set; } = blockType;
	public String Name { get; private set; } = name;
	public int UnnamedIndex { get; set; } = unnamedIndex;

	public override bool Equals(object? obj) => obj is not null && obj is Block m && BlockType == m.BlockType && Name == m.Name;

	public override int GetHashCode() => BlockType.GetHashCode() ^ Name.GetHashCode();

	public override string ToString() => (BlockType == BlockType.Unnamed) ? "Unnamed(" + Name + ")" : (ExplicitNameBlockTypes.Contains(BlockType) ? BlockType.ToString() : "") + Name;
}

[DebuggerDisplay("{ToString()}")]
public class BlockStack : Stack<Block>
{
	public BlockStack()
	{
	}

	public BlockStack(int capacity) : base(capacity)
	{
	}

	public BlockStack(G.IEnumerable<Block> collection) : base(collection)
	{
	}

	public BlockStack(params Block[] array) : base(array)
	{
	}

	public BlockStack(int capacity, params Block[] array) : base(capacity, array)
	{
	}

	public override bool Equals(object? obj)
	{
		if (obj is not BlockStack m)
			return false;
		if (Length != m.Length)
			return false;
		for (var i = 0; i < Length && i < m.Length; i++)
		{
			if (!this.ElementAt(i).Equals(m.ElementAt(i)))
				return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		var hash = 0;
		for (var i = 0; i < Length; i++)
			hash ^= this.ElementAt(i).GetHashCode();
		return hash;
	}

	public string ToShortString() => string.Join(".", this.ToArray(x => (x.BlockType == BlockType.Unnamed) ? "Unnamed(" + x.Name + ")" : x.Name.ToString()));

	public override string ToString() => string.Join(".", this.ToArray(x => x.ToString()));

	public static bool operator ==(BlockStack? x, BlockStack? y) => x?.Equals(y) ?? y is null;

	public static bool operator !=(BlockStack? x, BlockStack? y) => !(x == y);
}

public sealed class BlockComparer : G.IComparer<Block>
{
	public int Compare(Block? x, Block? y)
	{
		if (x is null || y is null)
			return (x is null ? 1 : 0) - (y is null ? 1 : 0);
		if (x.BlockType < y.BlockType)
			return 1;
		else if (x.BlockType > y.BlockType)
			return -1;
		return x.Name.ToString().CompareTo(y.Name.ToString());
	}
}

public sealed class BlockStackComparer : G.IComparer<BlockStack>
{
	public int Compare(BlockStack? x, BlockStack? y)
	{
		if (x is null || y is null)
			return (x is null ? 1 : 0) - (y is null ? 1 : 0);
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			var comp = new BlockComparer().Compare(x.ElementAt(i), y.ElementAt(i));
			if (comp != 0)
				return comp;
		}
		if (x.Length < y.Length)
			return 1;
		else if (x.Length > y.Length)
			return -1;
		return 0;
	}
}

public sealed class BlockStackAndStringComparer : G.IComparer<(BlockStack, String)>
{
	public int Compare((BlockStack, String) x, (BlockStack, String) y)
	{
		var comp = new BlockStackComparer().Compare(x.Item1, y.Item1);
		if (comp != 0)
			return comp;
		else
			return x.Item2.ToString().CompareTo(y.Item2.ToString());
	}
}

public sealed class BlockStackEComparer : G.IEqualityComparer<BlockStack>
{
	public bool Equals(BlockStack? x, BlockStack? y) => x?.Equals(y) ?? y is null;

	public int GetHashCode(BlockStack x) => x.GetHashCode();
}

public sealed class BlockStackAndStringEComparer : G.IEqualityComparer<(BlockStack, String)>
{
	public bool Equals((BlockStack, String) x, (BlockStack, String) y) => x.Item1.Equals(y.Item1) && x.Item2 == y.Item2;

	public int GetHashCode((BlockStack, String) x) => x.Item1.GetHashCode() ^ x.Item2.GetHashCode();
}

global using NStar.Core;
global using NStar.Linq;
global using System;
global using System.Collections;
global using System.Collections.Immutable;
global using System.Diagnostics;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;

namespace CSharp.NStar;

[DebuggerDisplay("{ToString()}")]
public sealed class Block(BlockType blockType, String name, int unnamedIndex)
{
	public static readonly List<BlockType> ExplicitNameBlockTypes = new(BlockType.Constructor, BlockType.Destructor, BlockType.Operator, BlockType.Other);
	public BlockType BlockType { get; private set; } = blockType;
	public String Name { get; private set; } = name;
	public int UnnamedIndex { get; set; } = unnamedIndex;

	public override bool Equals(object? obj) => obj is not null && obj is Block m && BlockType == m.BlockType && Name == m.Name;

	public override int GetHashCode() => BlockType.GetHashCode() ^ Name.GetHashCode();

	public override string ToString() => (BlockType == BlockType.Unnamed) ? "Unnamed(" + Name + ")" : (ExplicitNameBlockTypes.Contains(BlockType) ? BlockType.ToString() + ": " : "") + Name;
}

[DebuggerDisplay("{ToString()}")]
public readonly struct BlockStack : IReadOnlyCollection<Block>
{
	private readonly ImmutableArray<Block> _items;

	public readonly int Length => _items.IsDefaultOrEmpty ? 0 : _items.Length;

	public BlockStack() => _items = [];

	public BlockStack(G.IEnumerable<Block> collection) => _items = ImmutableArray.Create(collection.ToList().AsSpan());

	public BlockStack(params Block[] array) => _items = ImmutableArray.Create(array);

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

	public Enumerator GetEnumerator() => new(this);
	G.IEnumerator<Block> G.IEnumerable<Block>.GetEnumerator() => GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public override int GetHashCode()
	{
		var hash = 0;
		for (var i = 0; i < Length; i++)
			hash ^= this.ElementAt(i).GetHashCode();
		return hash;
	}

	public Block Peek() => _items[^1];

	public override string ToString() => string.Join(".", this.ToArray(x => x.ToString()));

	public bool TryPeek(out Block item)
	{
		if (Length == 0)
		{
			item = default!;
			return false;
		}
		item = _items[^1];
		return true;
	}

	public static bool operator ==(BlockStack? x, BlockStack? y) => x?.Equals(y) ?? y is null;

	public static bool operator !=(BlockStack? x, BlockStack? y) => !(x == y);

	public struct Enumerator : G.IEnumerator<Block>
	{
		private readonly BlockStack collection;
		private int index;

		internal Enumerator(BlockStack collection)
		{
			this.collection = collection;
			index = 0;
			Current = default!;
		}

		public readonly void Dispose()
		{
		}

		public bool MoveNext()
		{
			var localCollection = collection;
			if ((uint)index < (uint)localCollection.Length)
			{
				Current = localCollection._items[index++];
				return true;
			}
			return MoveNextRare();
		}

		private bool MoveNextRare()
		{
			index = collection.Length + 1;
			Current = default!;
			return false;
		}

		public Block Current { get; private set; }

		readonly object IEnumerator.Current
		{
			get
			{
				if (index == 0 || index == collection.Length + 1)
					throw new InvalidOperationException("Указатель находится за границей коллекции.");
				return Current!;
			}
		}

		void IEnumerator.Reset()
		{
			index = 0;
			Current = default!;
		}
	}
}

public readonly struct BlockComparer : G.IComparer<Block>
{
	public readonly int Compare(Block? x, Block? y)
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

public readonly struct BlockStackComparer : G.IComparer<BlockStack>
{
	public readonly int Compare(BlockStack x, BlockStack y)
	{
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

public readonly struct BlockStackAndStringComparer : G.IComparer<(BlockStack, String)>
{
	public readonly int Compare((BlockStack, String) x, (BlockStack, String) y)
	{
		var comp = new BlockStackComparer().Compare(x.Item1, y.Item1);
		if (comp != 0)
			return comp;
		else
			return x.Item2.ToString().CompareTo(y.Item2.ToString());
	}
}

public readonly struct BlockStackEComparer : G.IEqualityComparer<BlockStack>
{
	public readonly bool Equals(BlockStack x, BlockStack y) => x.Equals(y);

	public readonly int GetHashCode(BlockStack x) => x.GetHashCode();
}

public readonly struct BlockStackAndStringEComparer : G.IEqualityComparer<(BlockStack, String)>
{
	public readonly bool Equals((BlockStack, String) x, (BlockStack, String) y) => x.Item1.Equals(y.Item1) && x.Item2 == y.Item2;

	public readonly int GetHashCode((BlockStack, String) x) => x.Item1.GetHashCode() ^ x.Item2.GetHashCode();
}

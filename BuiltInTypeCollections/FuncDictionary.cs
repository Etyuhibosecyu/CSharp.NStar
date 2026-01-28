using NStar.Dictionaries;
using System.Diagnostics.CodeAnalysis;

namespace CSharp.NStar;

public sealed class FuncDictionary<TKey, TValue> : BaseDictionary<TKey, TValue, FuncDictionary<TKey, TValue>>
	where TKey : notnull
{
	private readonly Dictionary<TKey, Func<TKey, TValue>> low;
	private readonly List<(Func<TKey, bool> Key, Func<TKey, TValue> Value)> high;

	public FuncDictionary()
	{
		low = [];
		high = [];
	}

	public FuncDictionary(Dictionary<TKey, Func<TKey, TValue>> low, List<(Func<TKey, bool> Key, Func<TKey, TValue> Value)> high)
	{
		this.low = low;
		this.high = high;
	}

	public FuncDictionary(params (Func<TKey, bool> Key, Func<TKey, TValue> Value)[] collection) : this([], collection) { }

	public override int Length => low.Length + high.Length;

	public override G.ICollection<TKey> Keys => throw new NotSupportedException();

	public override G.ICollection<TValue> Values => throw new NotSupportedException();

	public override TValue this[TKey key]
	{
		get
		{
			if (low.TryGetValue(key, out var value))
				return value(key);
			foreach (var (KeyFunc, ValueFunc) in high)
				if (KeyFunc(key))
					return ValueFunc(key);
			throw new G.KeyNotFoundException();
		}
		set
		{
			if (low.ContainsKey(key))
			{
				low[key] = key => value;
				return;
			}
			for (var i = 0; i < high.Length; i++)
			{
				var (KeyFunc, _) = high[i];
				if (KeyFunc(key))
				{
					high[i] = (KeyFunc, key => value);
					return;
				}
			}
			low[key] = key => value;
		}
	}

	public override void Add(TKey key, TValue value) => low.Add(key, key => value);

	public void Add(TKey key, Func<TKey, TValue> valueFunc) => low.Add(key, valueFunc);

	public void Add(Func<TKey, bool> keyFunc, Func<TKey, TValue> valueFunc) => high.Add((keyFunc, valueFunc));

	public override void Clear()
	{
		low.Clear();
		high.Clear();
	}

	public override bool ContainsKey(TKey key)
	{
		if (low.ContainsKey(key))
			return true;
		foreach (var (KeyFunc, _) in high)
			if (KeyFunc(key))
				return true;
		return false;
	}

	protected override void CopyToHelper(Array array, int arrayIndex) => throw new NotSupportedException();

	protected override void CopyToHelper(G.KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
		throw new NotSupportedException();

	public override void ExceptWith(G.IEnumerable<(TKey Key, TValue Value)> other) => throw new NotSupportedException();
	public override void ExceptWith(G.IEnumerable<G.KeyValuePair<TKey, TValue>> other) => throw new NotSupportedException();
	public override void ExceptWith(G.IEnumerable<TKey> other) => throw new NotSupportedException();
	public override G.IEnumerator<G.KeyValuePair<TKey, TValue>> GetEnumerator() => new Enumerator(this);
	protected override IDictionaryEnumerator GetEnumeratorHelper() => throw new NotImplementedException();
	public override void IntersectWith(G.IEnumerable<(TKey Key, TValue Value)> other) => throw new NotSupportedException();
	public override void IntersectWith(G.IEnumerable<G.KeyValuePair<TKey, TValue>> other) => throw new NotSupportedException();
	public override void IntersectWith(G.IEnumerable<TKey> other) => throw new NotSupportedException();
	public override bool Remove(TKey key)
	{
		if (low.Remove(key))
			return true;
		for (var i = 0; i < high.Length; i++)
			if (high[i].Key(key))
			{
				high.RemoveAt(i);
				return true;
			}
		return false;
	}

	public override bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (low.Remove(key, out var valueFunc))
		{
			value = valueFunc(key);
			return true;
		}
		for (var i = 0; i < high.Length; i++)
			if (high[i].Key(key))
			{
				valueFunc = high.GetAndRemove(i).Value;
				value = valueFunc(key);
				return true;
			}
		value = default;
		return false;
	}

	public override bool RemoveValue(G.KeyValuePair<TKey, TValue> keyValuePair) => throw new NotSupportedException();
	public override void TrimExcess()
	{
		low.TrimExcess();
		high.TrimExcess();
	}

	public bool TryAdd(TKey key, Func<TKey, TValue> valueFunc)
	{
		if (!ContainsKey(key))
		{
			Add(key, valueFunc);
			return true;
		}
		else
			return false;
	}

	public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (low.TryGetValue(key, out var valueFunc))
		{
			value = valueFunc(key);
			return true;
		}
		for (var i = 0; i < high.Length; i++)
			if (high[i].Key(key))
			{
				valueFunc = high[i].Value;
				value = valueFunc(key);
				return true;
			}
		value = default;
		return false;
	}

	public struct Enumerator(FuncDictionary<TKey, TValue> dictionary) : G.IEnumerator<G.KeyValuePair<TKey, TValue>>
	{
		private readonly G.IEnumerator<G.KeyValuePair<TKey, Func<TKey, TValue>>> lowEnumerator = dictionary.low.GetEnumerator();

		public readonly void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (lowEnumerator.MoveNext())
			{
				var current = lowEnumerator.Current;
				Current = new(current.Key, current.Value(current.Key));
				return true;
			}
			return MoveNextRare();
		}

		private bool MoveNextRare()
		{
			Current = default!;
			return false;
		}

		public G.KeyValuePair<TKey, TValue> Current { get; private set; }

		readonly object IEnumerator.Current => Current;

		void IEnumerator.Reset()
		{
			lowEnumerator.Reset();
			Current = default!;
		}
	}
}

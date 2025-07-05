global using System.Diagnostics;
using System.Runtime.InteropServices;
using static NStar.Core.Extents;

namespace NHashSets;

[ComVisible(true), DebuggerDisplay("Length = {Length}"), Serializable]
public abstract unsafe class BaseNHashSet<T, TCertain> : BaseSet<T, TCertain> where T : unmanaged where TCertain : BaseNHashSet<T, TCertain>, new()
{
	protected struct Entry
	{
		public int hashCode;
		public int next;
		public T item;
	}

	protected int _capacity = 0;
	protected int* buckets = null;
	protected Entry* entries = null;

	public override int Capacity
	{
		get => _capacity;
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, _size);
			Resize(value, false);
			Changed();
		}
	}
	public virtual G.IEqualityComparer<T> Comparer { get; protected set; } = G.EqualityComparer<T>.Default;

	protected override void ClearInternal()
	{
		if (_size > 0)
		{
			for (var i = 0; i < _capacity; i++)
			{
				buckets[i] = 0;
				entries[i] = new();
			}
			_size = 0;
			Changed();
		}
	}

	protected override void ClearInternal(int index, int length)
	{
		for (var i = 0; i < length; i++)
			SetNull(index + i);
		Changed();
	}

	protected override void CopyToInternal(int sourceIndex, TCertain destination, int destinationIndex, int length)
	{
		if (this != destination || sourceIndex >= destinationIndex)
			for (var i = 0; i < length; i++)
				CopyOne(sourceIndex + i, destination, destinationIndex + i);
		else
			for (var i = length - 1; i >= 0; i--)
				destination.SetInternal(destinationIndex + i, GetInternal(sourceIndex + i));
		if (destination._size < destinationIndex + length)
			destination._size = destinationIndex + length;
		destination.Changed();
	}

	protected virtual void CopyOne(int sourceIndex, TCertain destination, int destinationIndex)
	{
		var hashCode = entries[sourceIndex].hashCode;
		if (hashCode < 0)
		{
			destination.SetInternal(destinationIndex, entries[sourceIndex].item);
			if (destination is NListHashSet<T> && destinationIndex == destination._size)
				destination._size++;
		}
		else if (destination.entries[destinationIndex].hashCode < 0)
			destination.SetNull(destinationIndex);
		else if (destinationIndex == destination._size)
		{
			destination.SetInternal(destinationIndex, default!);
			destination.SetNull(destinationIndex);
		}
	}

	protected virtual void CopyToCommon(int index, T[] array, int arrayIndex, int length)
	{
		var skipped = 0;
		for (var i = 0; i < index; i++)
			if (entries[i].hashCode >= 0)
				skipped++;
		for (var i = 0; i < length; i++)
			if (entries[i].hashCode < 0)
				array[arrayIndex++] = entries[index + i + skipped].item;
			else
				length++;
	}

	public override void Dispose()
	{
		if (buckets != null)
			Marshal.FreeHGlobal((nint)buckets);
		buckets = null;
		if (entries != null)
			Marshal.FreeHGlobal((nint)entries);
		entries = null;
		_size = 0;
		GC.SuppressFinalize(this);
	}

	protected override T GetInternal(int index, bool invoke = true)
	{
		var item = entries[index].item;
		if (invoke)
			Changed();
		return item;
	}

	protected override int IndexOfInternal(T item, int index, int length) => IndexOfInternal(item, index, length, Comparer.GetHashCode(item) & 0x7FFFFFFF);

	protected virtual int IndexOfInternal(T item, int index, int length, int hashCode)
	{
		if (buckets == null)
			return -1;
		uint collisionCount = 0;
		Debug.Assert(hashCode >= 0);
		for (var i = ~buckets[hashCode % _capacity]; i >= 0; i = ~entries[i].next)
		{
			if (~entries[i].next == i)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
			if (entries[i].hashCode == ~hashCode && Comparer.Equals(entries[i].item, item) && i >= index && i < index + length)
				return i;
			collisionCount++;
			if (collisionCount > _capacity)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
		}
		return -1;
	}

	protected virtual void Initialize(int capacity)
	{
		var size = HashHelpers.GetPrime(capacity);
		buckets = (int*)Marshal.AllocHGlobal(sizeof(int) * size);
		FillMemory(buckets, size, 0);
		entries = (Entry*)Marshal.AllocHGlobal(sizeof(Entry) * size);
		_capacity = size;
	}

	protected abstract TCertain Insert(T item, out int index, int hashCode);

	protected override TCertain InsertInternal(int index, G.IEnumerable<T> collection)
	{
		var this2 = (TCertain)this;
		var set = CollectionCreator(collection).ExceptWith(this);
		if (CreateVar(set.GetType(), out var type).Name.Contains("FastDelHashSet") || type.Name.Contains("ParallelHashSet"))
			(type.GetMethod("FixUpFakeIndexes")
				?? throw new MissingMethodException("Не удалось загрузить метод \"починки\" фейковых индексов." +
				" Обратитесь к разработчикам .NStar.")).Invoke(set, null);
		var length = set.Length;
		if (length > 0)
		{
			if (this == collection)
				return this2;
			EnsureCapacity(_size + length);
			if (index < _size)
				CopyToInternal(index, this2, index + length, _size - index);
			set.CopyToInternal(0, this2, index, length);
		}
		return this2;
	}

	protected override TCertain InsertInternal(int index, ReadOnlySpan<T> span)
	{
		var this2 = (TCertain)this;
		var set = SpanCreator(span).ExceptWith(this);
		if (CreateVar(set.GetType(), out var type).Name.Contains("FastDelHashSet") || type.Name.Contains("ParallelHashSet"))
			(type.GetMethod("FixUpFakeIndexes")
				?? throw new MissingMethodException("Не удалось загрузить метод \"починки\" фейковых индексов." +
				" Обратитесь к разработчикам .NStar.")).Invoke(set, null);
		var length = set.Length;
		if (length > 0)
		{
			EnsureCapacity(_size + length);
			if (index < _size)
				CopyToInternal(index, this2, index + length, _size - index);
			set.CopyToInternal(0, this2, index, length);
		}
		return this2;
	}

	protected virtual void RemoveAtCommon(int index, ref Entry t)
	{
		uint collisionCount = 0;
		var bucket = ~t.hashCode % _capacity;
		var last = -1;
		for (var i = ~buckets[bucket]; i >= 0; last = i, i = ~entries[i].next)
		{
			if (~entries[i].next == i || ~entries[i].next == last && last != -1)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
			if (i == index)
			{
				if (last < 0)
					buckets[bucket] = entries[i].next;
				else
				{
					var tLast = entries[last];
					tLast.next = entries[i].next;
					entries[last] = tLast;
				}
				break;
			}
			collisionCount++;
			if (collisionCount > _capacity)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
		}
		t.hashCode = 0;
		t.item = default!;
	}

	protected virtual bool RemoveValueCommon(T item, int hashCode, RemoveValueAction action)
	{
		uint collisionCount = 0;
		var bucket = hashCode % _capacity;
		var last = -1;
		for (var i = ~buckets[bucket]; i >= 0; last = i, i = ~entries[i].next)
		{
			if (~entries[i].next == i || ~entries[i].next == last && last != -1)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
			if (entries[i].hashCode == ~hashCode && Comparer.Equals(entries[i].item, item))
			{
				if (last < 0)
					buckets[bucket] = entries[i].next;
				else
				{
					var tLast = entries[last];
					tLast.next = entries[i].next;
					entries[last] = tLast;
				}
				var t = entries[i];
				t.hashCode = 0;
				t.item = default!;
				action(ref t, i);
				entries[i] = t;
				return true;
			}
			collisionCount++;
			if (collisionCount > _capacity)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
					" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
					" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
		}
		return false;
	}

	protected delegate void RemoveValueAction(ref Entry t, int i);

	protected virtual void Resize() => Resize(HashHelpers.ExpandPrime(_size), false);

	protected virtual void Resize(int newSize, bool forceNewHashCodes)
	{
		if (newSize == 0)
		{
			if (buckets != null)
				Marshal.FreeHGlobal((nint)buckets);
			buckets = null;
			if (entries != null)
				Marshal.FreeHGlobal((nint)entries);
			entries = null;
			_capacity = 0;
			return;
		}
		var smallerSize = Min(_size, newSize);
		if (buckets != null)
			Marshal.FreeHGlobal((nint)buckets);
		buckets = (int*)Marshal.AllocHGlobal(sizeof(int) * newSize);
		FillMemory(buckets, newSize, 0);
		var newEntries = (Entry*)Marshal.AllocHGlobal(sizeof(Entry) * newSize);
		if (entries != null)
		{
			CopyMemory(entries, newEntries, smallerSize);
			Marshal.FreeHGlobal((nint)entries);
		}
		entries = newEntries;
		_capacity = newSize;
		if (forceNewHashCodes)
			for (var i = 0; i < smallerSize; i++)
			{
				var t = entries[i];
				if (t.hashCode == 0)
					continue;
				t.hashCode = ~Comparer.GetHashCode(t.item) & 0x7FFFFFFF;
				entries[i] = t;
			}
		for (var i = 0; i < smallerSize; i++)
			if (entries[i].hashCode < 0)
			{
				var bucket = ~entries[i].hashCode % newSize;
				var t = entries[i];
				t.next = buckets[bucket];
				entries[i] = t;
				buckets[bucket] = ~i;
			}
	}

	protected virtual void SetNull(int index)
	{
		if (this is not NListHashSet<T>)
		{
			RemoveAt(index);
			return;
		}
		var t = entries[index];
		if (t.hashCode >= 0)
			return;
		RemoveAtCommon(index, ref t);
		t.next = 0;
		entries[index] = t;
		Debug.Assert(entries[index].hashCode >= 0);
	}

	public override bool TryAdd(T item, out int index)
	{
		var hashCode = Comparer.GetHashCode(item) & 0x7FFFFFFF;
		if (TryGetIndexOf(item, out index, hashCode))
			return false;
		Insert(item, out index, hashCode);
		return true;
	}

	public override bool TryGetIndexOf(T item, out int index) => TryGetIndexOf(item, out index, Comparer.GetHashCode(item) & 0x7FFFFFFF);

	protected virtual bool TryGetIndexOf(T item, out int index, int hashCode) => (index = IndexOfInternal(item, 0, _size, hashCode)) >= 0;
}

[ComVisible(true), DebuggerDisplay("Length = {Length}"), Serializable]
/// <summary>
/// Внимание! Рекомендуется не использовать в этом хэш-множестве удаление в цикле, так как такое действие
/// имеет асимптотику O(n²), и при большом размере хэш-множества программа может зависнуть. Дело в том,
/// что здесь, в отличие от класса FastDelHashSet<T>, индексация гарантированно "правильная", но за это
/// приходится платить тем, что после каждого удаления нужно сдвинуть все следующие элементы влево, а это
/// имеет сложность по времени O(n), соответственно, цикл таких действий - O(n²). Если вам нужно произвести
/// серию удалений, используйте FastDelHashSet<T>, а по завершению серии вызовите FixUpFakeIndexes().
/// </summary>
public abstract unsafe class NListHashSet<T, TCertain> : BaseNHashSet<T, TCertain> where T : unmanaged where TCertain : NListHashSet<T, TCertain>, new()
{
	public NListHashSet() : this(0, (G.IEqualityComparer<T>?)null) { }

	public NListHashSet(int capacity) : this(capacity, (G.IEqualityComparer<T>?)null) { }

	public NListHashSet(G.IEqualityComparer<T>? comparer) : this(0, comparer) { }

	public NListHashSet(int capacity, G.IEqualityComparer<T>? comparer)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(capacity);
		if (capacity > 0)
			Initialize(capacity);
		else
		{
			buckets = null;
			entries = null;
		}
		Comparer = comparer ?? G.EqualityComparer<T>.Default;
	}

	public NListHashSet(G.IEnumerable<T> collection) : this(collection, null) { }

	public NListHashSet(G.IEnumerable<T> collection, G.IEqualityComparer<T>? comparer) : this(collection is G.ISet<T> set ? set.Count : typeof(T).Equals(typeof(byte)) ? ValuesInByte : collection.TryGetLengthEasily(out var length) ? (int)(Sqrt(length) * 10) : 0, comparer)
	{
		ArgumentNullException.ThrowIfNull(collection);
		foreach (var item in collection)
			TryAdd(item);
	}

	public NListHashSet(int capacity, G.IEnumerable<T> collection) : this(capacity, collection, null) { }

	public NListHashSet(int capacity, G.IEnumerable<T> collection, G.IEqualityComparer<T>? comparer) : this(capacity, comparer)
	{
		ArgumentNullException.ThrowIfNull(collection);
		foreach (var item in collection)
			TryAdd(item);
	}

	public NListHashSet(params T[] array) : this((G.IEnumerable<T>)array) { }

	public NListHashSet(int capacity, params T[] array) : this(capacity, (G.IEnumerable<T>)array) { }

	public NListHashSet(ReadOnlySpan<T> span) : this((G.IEnumerable<T>)span.ToArray()) { }

	public NListHashSet(int capacity, ReadOnlySpan<T> span) : this(capacity, (G.IEnumerable<T>)span.ToArray()) { }

	public override T this[Index index, bool invoke = true]
	{
		get => base[index, invoke];
		set
		{
			var index2 = index.GetOffset(_size);
			if ((uint)index2 >= (uint)_size)
				throw new IndexOutOfRangeException();
			if (entries[index2].item.Equals(value))
				return;
			if (Contains(value))
				throw new ArgumentException("Ошибка, такой элемент уже был добавлен.", nameof(value));
			SetInternal(index2, value);
		}
	}

	protected override void CopyToInternal(int index, T[] array, int arrayIndex, int length)
	{
		for (var i = 0; i < length; i++)
			array[arrayIndex++] = entries[index++].item;
	}

	protected override TCertain Insert(T item, out int index, int hashCode)
	{
		if (buckets == null)
			Initialize(0);
		if (buckets == null)
			throw new InvalidOperationException("Произошла внутренняя ошибка." +
				" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
				" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
		var targetBucket = hashCode % _capacity;
		if (_size == _capacity)
		{
			Resize();
			targetBucket = hashCode % _capacity;
		}
		index = _size;
		_size++;
		var t = entries[index];
		t.hashCode = ~hashCode;
		t.next = buckets[targetBucket];
		t.item = item;
		entries[index] = t;
		buckets[targetBucket] = ~index;
		Changed();
		return (TCertain)this;
	}

	public override TCertain RemoveAt(int index)
	{
		if ((uint)index >= (uint)_size)
			throw new ArgumentOutOfRangeException(nameof(index));
		var this2 = (TCertain)this;
		_size--;
		if (index < _size)
			CopyToInternal(index + 1, this2, index, _size - index);
		SetNull(_size);
		Changed();
		return this2;
	}

	protected override void SetInternal(int index, T item)
	{
		var hashCode = Comparer.GetHashCode(item) & 0x7FFFFFFF;
		var bucket = hashCode % _capacity;
		var t = entries[index];
		uint collisionCount = 0;
		var oldBucket = ~t.hashCode % _capacity;
		var last = -1;
		for (var i = oldBucket >= 0 ? ~buckets[oldBucket] : -1; i >= 0; last = i, i = ~entries[i].next)
		{
			if (~entries[i].next == i || ~entries[i].next == last && last != -1)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
				" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
				" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
			if (i == index)
			{
				if (last < 0)
					buckets[oldBucket] = entries[i].next;
				else
				{
					var tLast = entries[last];
					tLast.next = entries[i].next;
					entries[last] = tLast;
				}
				break;
			}
			collisionCount++;
			if (collisionCount > _capacity)
				throw new InvalidOperationException("Произошла внутренняя ошибка." +
				" Возможно, вы пытаетесь писать в одно множество в несколько потоков?" +
				" Если нет, повторите попытку позже, возможно, какая-то аппаратная ошибка.");
		}
		t.hashCode = ~hashCode;
		t.next = buckets[bucket];
		t.item = item;
		entries[index] = t;
		buckets[bucket] = ~index;
		Changed();
	}
}

[ComVisible(true), DebuggerDisplay("Length = {Length}"), Serializable]
/// <summary>
/// Внимание! Рекомендуется не использовать в этом хэш-множестве удаление в цикле, так как такое действие
/// имеет асимптотику O(n²), и при большом размере хэш-множества программа может зависнуть. Дело в том,
/// что здесь, в отличие от класса FastDelHashSet<T>, индексация гарантированно "правильная", но за это
/// приходится платить тем, что после каждого удаления нужно сдвинуть все следующие элементы влево, а это
/// имеет сложность по времени O(n), соответственно, цикл таких действий - O(n²). Если вам нужно произвести
/// серию удалений, используйте FastDelHashSet<T>, а по завершению серии вызовите FixUpFakeIndexes().
/// </summary>
public class NListHashSet<T> : NListHashSet<T, NListHashSet<T>> where T : unmanaged
{
	public NListHashSet() : base() { }

	public NListHashSet(int capacity) : base(capacity) { }

	public NListHashSet(G.IEqualityComparer<T>? comparer) : base(comparer) { }

	public NListHashSet(G.IEnumerable<T> collection) : base(collection) { }

	public NListHashSet(G.IEnumerable<T> collection, G.IEqualityComparer<T>? comparer) : base(collection, comparer) { }

	public NListHashSet(int capacity, G.IEnumerable<T> collection) : base(capacity, collection) { }

	public NListHashSet(int capacity, params T[] array) : base(capacity, array) { }

	public NListHashSet(int capacity, ReadOnlySpan<T> span) : base(capacity, span) { }

	public NListHashSet(int capacity, G.IEnumerable<T> collection, G.IEqualityComparer<T>? comparer) : base(capacity, collection, comparer) { }

	public NListHashSet(int capacity, G.IEqualityComparer<T>? comparer) : base(capacity, comparer) { }

	public NListHashSet(params T[] array) : base(array) { }

	public NListHashSet(ReadOnlySpan<T> span) : base(span) { }

	protected override Func<int, NListHashSet<T>> CapacityCreator => x => new(x);

	protected override Func<G.IEnumerable<T>, NListHashSet<T>> CollectionCreator => x => new(x);

	protected override Func<ReadOnlySpan<T>, NListHashSet<T>> SpanCreator => x => new(x);
}

public static class Extents
{
	public static NListHashSet<T> ToNHashSet<T>(this G.IEnumerable<T> collection) where T : unmanaged => new(collection);
}

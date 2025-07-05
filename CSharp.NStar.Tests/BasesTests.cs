namespace NStar.Core.Tests;

public record class BaseIndexableTests<T, TCertain>(TCertain TestCollection, ImmutableArray<T> OriginalCollection, T DefaultString, G.IEnumerable<T> DefaultCollection) where TCertain : BaseIndexable<T, TCertain>, new()
{
	public void TestGetRange()
	{
		var b = TestCollection.GetRange(..);
		var c = new G.List<T>(OriginalCollection);
		Assert.IsTrue(TestCollection.Equals(OriginalCollection));
		Assert.IsTrue(E.SequenceEqual(OriginalCollection, TestCollection));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(..^1);
		c = new G.List<T>(OriginalCollection).GetRange(0, OriginalCollection.Length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(1..);
		c = new G.List<T>(OriginalCollection).GetRange(1, OriginalCollection.Length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(1..^1);
		c = new G.List<T>(OriginalCollection).GetRange(1, OriginalCollection.Length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(1..5);
		c = new G.List<T>(OriginalCollection).GetRange(1, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(^5..);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 5);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(^5..^1);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetRange(^5..5);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 10 - OriginalCollection.Length);
		Assert.IsTrue(TestCollection.Equals(OriginalCollection));
		Assert.IsTrue(E.SequenceEqual(OriginalCollection, TestCollection));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = TestCollection.GetRange(-1..5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = TestCollection.GetRange(^1..1));
		Assert.ThrowsExactly<ArgumentException>(() => b = TestCollection.GetRange(1..1000));
	}

	public void TestGetSlice()
	{
		var b = TestCollection.GetSlice(..);
		var c = new G.List<T>(OriginalCollection);
		Assert.IsTrue(TestCollection.Equals(OriginalCollection));
		Assert.IsTrue(E.SequenceEqual(OriginalCollection, TestCollection));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(1);
		c = new G.List<T>(OriginalCollection).GetRange(1, OriginalCollection.Length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(1, 4);
		c = new G.List<T>(OriginalCollection).GetRange(1, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(^5);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 5);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(..^1);
		c = new G.List<T>(OriginalCollection).GetRange(0, OriginalCollection.Length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(1..);
		c = new G.List<T>(OriginalCollection).GetRange(1, OriginalCollection.Length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(1..^1);
		c = new G.List<T>(OriginalCollection).GetRange(1, OriginalCollection.Length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(1..5);
		c = new G.List<T>(OriginalCollection).GetRange(1, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(^5..);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 5);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(^5..^1);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = TestCollection.GetSlice(^5..5);
		c = new G.List<T>(OriginalCollection).GetRange(OriginalCollection.Length - 5, 10 - OriginalCollection.Length);
		Assert.IsTrue(TestCollection.Equals(OriginalCollection));
		Assert.IsTrue(E.SequenceEqual(OriginalCollection, TestCollection));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = TestCollection.GetSlice(-1..5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = TestCollection.GetSlice(^1..1));
		Assert.ThrowsExactly<ArgumentException>(() => b = TestCollection.GetSlice(1..1000));
	}
}

public record class BaseListTests<T, TCertain>(TCertain TestCollection, ImmutableArray<T> OriginalCollection, T DefaultString, G.IEnumerable<T> DefaultCollection) where TCertain : BaseList<T, TCertain>, new()
{
	public static void BreakFilterInPlaceAsserts(TCertain a, TCertain b, TCertain c, G.List<T> d, G.List<T> e)
	{
		Assert.IsTrue(a.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.IsTrue(c.Equals(e));
		Assert.IsTrue(E.SequenceEqual(e, c));
	}

	public static void TestToArray(Func<T> randomizer)
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, capacity;
		G.List<T> b;
		T[] array;
		T[] array2;
		T elem;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			capacity = length + random.Next(151);
			var method = typeof(TCertain).GetConstructor([typeof(int)]);
			var a = method?.Invoke([capacity]) as TCertain ?? throw new InvalidOperationException();
			b = new(capacity);
			for (var j = 0; j < length; j++)
			{
				a.Add(elem = randomizer());
				b.Add(elem);
			}
			array = a.ToArray();
			array2 = [.. b];
			Assert.IsTrue(RedStarLinq.Equals(array, array2));
			Assert.IsTrue(E.SequenceEqual(array, array2));
		}
	}

	public static void TestTrimExcess(Func<T> randomizer)
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, capacity;
		G.List<T> b;
		T elem;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			capacity = length + random.Next(9951);
			var method = typeof(TCertain).GetConstructor([typeof(int)]);
			var a = method?.Invoke([capacity]) as TCertain ?? throw new InvalidOperationException();
			b = new(capacity);
			for (var j = 0; j < length; j++)
			{
				a.Add(elem = randomizer());
				b.Add(elem);
			}
			a.TrimExcess();
			Assert.IsTrue(RedStarLinq.Equals(a, b));
			Assert.IsTrue(E.SequenceEqual(a, b));
		}
	}
}

public record class BaseStringIndexableTests<TCertain>(TCertain TestCollection, ImmutableArray<string> OriginalCollection, string DefaultString, G.IEnumerable<string> DefaultCollection) where TCertain : BaseIndexable<string, TCertain>, new()
{
	public void TestContains()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, startIndex;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.Contains(elem), a.Contains(elem));
			startIndex = random.Next(length);
			Assert.AreEqual(b.GetRange(startIndex, length - startIndex).Contains(elem), a.Contains(elem, startIndex));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(E.Contains(E.Zip(b, E.Skip(b, 1)), (elem, elem2)), a.Contains([elem, elem2]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Contains((G.IEnumerable<string>)null!));
	}

	public void TestContainsAny()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, startIndex;
		G.List<string> b;
		string elem, elem2, elem3;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.Contains(elem) || b.Contains(elem2), a.ContainsAny([elem, elem2]));
			startIndex = random.Next(length);
			Assert.AreEqual(b.GetRange(startIndex, length - startIndex).Contains(elem)
				|| b.GetRange(startIndex, length - startIndex).Contains(elem2), a.ContainsAny([elem, elem2], startIndex));
			elem3 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.Contains(elem) || b.Contains(elem2) || b.Contains(elem3), a.ContainsAny([elem, elem2, elem3]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.ContainsAny((G.IEnumerable<string>)null!));
	}

	public void TestContainsAnyExcluding()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, startIndex;
		G.List<string> b;
		string elem, elem2, elem3;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(E.Except(b, [elem, elem2]).Any(), a.ContainsAnyExcluding([elem, elem2]));
			startIndex = random.Next(length);
			Assert.AreEqual(E.Except(E.Skip(b, startIndex), [elem, elem2]).Any(), a.ContainsAnyExcluding([elem, elem2], startIndex));
			elem3 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(E.Except(b, [elem, elem2, elem3]).Any(), a.ContainsAnyExcluding([elem, elem2, elem3]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.ContainsAnyExcluding((G.IEnumerable<string>)null!));
	}

	public void TestEndsWith()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.Count != 0 && b[^1] == elem, a.EndsWith(elem));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.Count >= 2 && E.Last(E.Zip(b, E.Skip(b, 1))) == (elem, elem2), a.EndsWith([elem, elem2]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.EndsWith((G.IEnumerable<string>)null!));
	}

	public void TestEquals()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length;
		G.List<string> b;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. a];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.Append(b, random.Next(1000).ToString("D3"))];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.Skip(b, 1)];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.Prepend(b, random.Next(1000).ToString("D3"))];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.SkipLast(b, 1)];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.Append(E.SkipLast(b, 1), random.Next(1000).ToString("D3"))];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
			b = [.. E.Prepend(E.Skip(b, 1), random.Next(1000).ToString("D3"))];
			Assert.AreEqual(RedStarLinq.Equals(a, b), a.Equals(b));
#pragma warning disable IDE0028 // Упростите инициализацию коллекции
			b = new G.List<string>();
			Assert.AreEqual(RedStarLinq.Equals(a, b), E.SequenceEqual(a, b));
#pragma warning restore IDE0028 // Упростите инициализацию коллекции
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals((G.IEnumerable<string>)null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals((G.IEnumerable<string>)null!, 3));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals(null!, 3));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TestCollection.Equals((G.IEnumerable<string>)null!, -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TestCollection.Equals(null!, -1));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals((G.IEnumerable<string>)null!, 1000));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.Equals(null!, 1000));
	}

	public void TestFind()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int start, length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.Find(x => x.Length != 3), a.Find(x => x.Length != 3));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.Find(x => !x.All(y => y is >= 'A' and <= 'Y')), a.Find(x => !x.All(y => y is >= 'A' and <= 'Y')));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			start = random.Next(a.Length + 1);
			Assert.IsTrue(start <= b.Count);
		}
	}

	public void TestFindAll()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int start, length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.IsTrue(E.SequenceEqual(b.FindAll(x => x.Length != 3), a.FindAll(x => x.Length != 3)));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.IsTrue(E.SequenceEqual(b.FindAll(x => !x.All(y => y is >= 'A' and <= 'Y')), a.FindAll(x => !x.All(y => y is >= 'A' and <= 'Y'))));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			start = random.Next(a.Length + 1);
			Assert.IsTrue(start <= b.Count);
		}
	}

	public void TestFindIndex()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int start, length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.FindIndex(x => x.Length != 3), a.FindIndex(x => x.Length != 3));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.FindIndex(x => !x.All(y => y is >= 'A' and <= 'Y')), a.FindIndex(x => !x.All(y => y is >= 'A' and <= 'Y')));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			start = random.Next(a.Length + 1);
			Assert.IsTrue(start <= b.Count);
		}
	}

	public void TestFindLast()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int start, length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.FindLast(x => x.Length != 3), a.FindLast(x => x.Length != 3));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.FindLast(x => !x.All(y => y is >= 'A' and <= 'Y')), a.FindLast(x => !x.All(y => y is >= 'A' and <= 'Y')));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			start = random.Next(a.Length + 1);
			Assert.IsTrue(start <= b.Count);
		}
	}

	public void TestFindLastIndex()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int start, length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.FindLastIndex(x => x.Length != 3), a.FindLastIndex(x => x.Length != 3));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.FindLastIndex(x => !x.All(y => y is >= 'A' and <= 'Y')), a.FindLastIndex(x => !x.All(y => y is >= 'A' and <= 'Y')));
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
			start = random.Next(a.Length + 1);
			Assert.IsTrue(start <= b.Count);
		}
	}

	public void TestIndexOf()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int index, length, startIndex;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.IndexOf(elem), a.IndexOf(elem));
			startIndex = random.Next(length);
			index = b.GetRange(startIndex, length - startIndex).IndexOf(elem);
			if (index >= 0)
				index += startIndex;
			Assert.AreEqual(index, a.IndexOf(elem, startIndex));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(E.ToList(E.Zip(b, E.Skip(b, 1))).IndexOf((elem, elem2)), a.IndexOf([elem, elem2]));
		}
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf("BBB", -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf("BBB", -1, 5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf("BBB", 3, -5));
		Assert.ThrowsExactly<ArgumentException>(() => index = TestCollection.IndexOf("BBB", 1, 1000));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.IndexOf((G.IEnumerable<string>)null!));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf(["MMM", "EEE"], -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf(["MMM", "EEE"], -1, 5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.IndexOf(["MMM", "EEE"], 3, -5));
		Assert.ThrowsExactly<ArgumentException>(() => index = TestCollection.IndexOf(["MMM", "EEE"], 1, 1000));
	}

	public void TestIndexOfAny()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int index, index2, length, startIndex;
		G.List<string> b;
		string elem, elem2, elem3;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			index = b.IndexOf(elem);
			index2 = b.IndexOf(elem2);
			if (index2 >= 0 && index2 < index || index == -1)
				index = index2;
			Assert.AreEqual(index, a.IndexOfAny([elem, elem2]));
			startIndex = random.Next(length);
			index = b.IndexOf(elem, startIndex);
			index2 = b.IndexOf(elem2, startIndex);
			if (index2 >= 0 && index2 < index || index == -1)
				index = index2;
			Assert.AreEqual(index, a.IndexOfAny([elem, elem2], startIndex));
			elem3 = new((char)random.Next('A', 'Z' + 1), 3);
			index = b.IndexOf(elem);
			index2 = b.IndexOf(elem2);
			if (index2 >= 0 && index2 < index || index == -1)
				index = index2;
			index2 = b.IndexOf(elem3);
			if (index2 >= 0 && index2 < index || index == -1)
				index = index2;
			Assert.AreEqual(index, a.IndexOfAny([elem, elem2, elem3]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.IndexOfAny((G.IEnumerable<string>)null!));
	}

	public void TestLastIndexOf()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int index, length, startIndex;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.LastIndexOf(elem), a.LastIndexOf(elem));
			startIndex = length == 0 ? -1 : random.Next(length);
			index = b.GetRange(0, startIndex + 1).LastIndexOf(elem);
			Assert.AreEqual(index, a.LastIndexOf(elem, startIndex));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(E.ToList(E.Zip(b, E.Skip(b, 1))).LastIndexOf((elem, elem2)), a.LastIndexOf([elem, elem2]));
		}
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf("BBB", -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf("BBB", -1, 5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf("BBB", 3, -5));
		Assert.ThrowsExactly<ArgumentException>(() => index = TestCollection.LastIndexOf("BBB", 1, 1000));
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.LastIndexOf((G.IEnumerable<string>)null!));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf(["MMM", "EEE"], -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf(["MMM", "EEE"], -1, 5));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => index = TestCollection.LastIndexOf(["MMM", "EEE"], 3, -5));
		Assert.ThrowsExactly<ArgumentException>(() => index = TestCollection.LastIndexOf(["MMM", "EEE"], 1, 1000));
	}

	public void TestLastIndexOfAny()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int index, index2, length, startIndex;
		G.List<string> b;
		string elem, elem2, elem3;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			index = b.LastIndexOf(elem);
			index2 = b.LastIndexOf(elem2);
			if (index2 > index)
				index = index2;
			Assert.AreEqual(index, a.LastIndexOfAny([elem, elem2]));
			startIndex = length == 0 ? -1 : random.Next(length);
			index = b.LastIndexOf(elem, startIndex);
			index2 = b.LastIndexOf(elem2, startIndex);
			if (index2 > index)
				index = index2;
			Assert.AreEqual(index, a.LastIndexOfAny([elem, elem2], startIndex));
			elem3 = new((char)random.Next('A', 'Z' + 1), 3);
			index = b.LastIndexOf(elem);
			index2 = b.LastIndexOf(elem2);
			if (index2 > index)
				index = index2;
			index2 = b.LastIndexOf(elem3);
			if (index2 > index)
				index = index2;
			Assert.AreEqual(index, a.LastIndexOfAny([elem, elem2, elem3]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.LastIndexOfAny((G.IEnumerable<string>)null!));
	}

	public void TestStartsWith()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length;
		G.List<string> b;
		string elem, elem2;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			var array = new string[length];
			for (var j = 0; j < length; j++)
				array[j] = new((char)random.Next('A', 'Z' + 1), 3);
			elem = new((char)random.Next('A', 'Z' + 1), 3);
			var method = typeof(TCertain).GetConstructor([typeof(string[])]);
			var a = method?.Invoke([array]) as TCertain ?? throw new InvalidOperationException();
			b = [.. array];
			Assert.AreEqual(b.Count != 0 && b[0] == elem, a.StartsWith(elem));
			elem2 = new((char)random.Next('A', 'Z' + 1), 3);
			Assert.AreEqual(b.Count >= 2 && E.First(E.Zip(b, E.Skip(b, 1))) == (elem, elem2), a.StartsWith([elem, elem2]));
		}
		Assert.ThrowsExactly<ArgumentNullException>(() => TestCollection.StartsWith((G.IEnumerable<string>)null!));
	}
}

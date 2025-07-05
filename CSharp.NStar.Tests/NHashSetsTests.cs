namespace NStar.Core.Tests;

[TestClass]
public class NListHashSetTests
{
	[TestMethod]
	public void ComplexTest()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		var arr = RedStarLinq.FillArray(16, _ => random.Next(16));
		NListHashSet<int> lhs = new(arr);
		G.HashSet<int> gs = [.. arr];
		var collectionActions = new[] { (int[] arr) =>
		{
			lhs.ExceptWith(arr);
			gs.ExceptWith(arr);
			if (random.Next(2) == 1)
				lhs.TrimExcess();
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, arr =>
		{
			lhs.IntersectWith(arr);
			gs.IntersectWith(arr);
			if (random.Next(2) == 1)
				lhs.TrimExcess();
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, arr =>
		{
			lhs.SymmetricExceptWith(arr);
			gs.SymmetricExceptWith(arr);
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, arr =>
		{
			lhs.UnionWith(arr);
			gs.UnionWith(arr);
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		} };
		var actions = new[] { () =>
		{
			var n = random.Next(16);
			lhs.Add(n);
			gs.Add(n);
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, () =>
		{
			if (lhs.Length == 0) return;
			if (random.Next(2) == 0)
			{
				var n = random.Next(lhs.Length);
				gs.Remove(lhs[n]);
				lhs.RemoveAt(n);
			}
			else
			{
				var n = random.Next(16);
				lhs.RemoveValue(n);
				gs.Remove(n);
			}
			if (random.Next(2) == 1)
				lhs.TrimExcess();
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, () =>
		{
			var arr = RedStarLinq.FillArray(5, _ => random.Next(16));
			collectionActions.Random(random)(arr);
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, () =>
		{
			if (lhs.Length == 0)
				return;
			var index = random.Next(lhs.Length);
			var n = random.Next(16);
			if (lhs[index] == n)
				return;
			gs.Remove(lhs[index]);
			if (lhs.TryGetIndexOf(n, out var index2) && index2 < index)
				index--;
			lhs.RemoveValue(n);
			if (random.Next(2) == 1)
				lhs.TrimExcess();
			lhs[index] = n;
			gs.Add(n);
			Assert.IsTrue(lhs.SetEquals(gs));
			Assert.IsTrue(gs.SetEquals(lhs));
		}, () =>
		{
			if (lhs.Length == 0) return;
			var n = random.Next(lhs.Length);
			Assert.AreEqual(lhs.IndexOf(lhs[n]), n);
		} };
		for (var i = 0; i < 1000; i++)
			actions.Random(random)();
	}

	[TestMethod]
	public void TestAdd()
	{
		var a = nList.ToNHashSet().Add(defaultNString);
		var b = E.ToHashSet(new G.List<(char, char, char)>(nList) { defaultNString });
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
	}

	[TestMethod]
	public void TestAddRange()
	{
		var a = nList.ToNHashSet().AddRange(defaultNCollection);
		var b = new G.List<(char, char, char)>(nList);
		b.AddRange(defaultNCollection);
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
	}

	[TestMethod]
	public void TestAddSeries()
	{
		var a = nList.ToNHashSet();
		a.AddSeries(('X', 'X', 'X'), 0);
		G.HashSet<(char, char, char)> b = [.. nList];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(('X', 'X', 'X'), 3);
		for (var i = 0; i < 3; i++)
			b.Add(('X', 'X', 'X'));
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(('X', 'X', 'X'), 101);
		for (var i = 0; i < 101; i++)
			b.Add(('X', 'X', 'X'));
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.AddSeries(('X', 'X', 'X'), -1));
		a.Replace(nList);
		a.AddSeries(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 0);
		b = [.. nList];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 3);
		b.Add(('0', '0', '0'));
		b.Add(('0', '0', '1'));
		b.Add(('0', '0', '3'));
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 101);
		foreach (var x in E.Select(E.Range(0, 101), index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")))
			b.Add(x);
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.AddSeries(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), -1));
		a.Replace(nList);
		a.AddSeries(0, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"));
		b.Clear();
		b = [.. nList];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(3, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"));
		b.Add(('0', '0', '0'));
		b.Add(('0', '0', '1'));
		b.Add(('0', '0', '3'));
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.AddSeries(101, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"));
		foreach (var x in E.Select(E.Range(0, 101), index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")))
			b.Add(x);
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.AddSeries(-1, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")));
	}

	[TestMethod]
	public void TestAppend()
	{
		var a = nList.ToNHashSet();
		var b = a.Append(defaultNString);
		var c = E.Append(E.Distinct(nList), defaultNString);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestBreakFilter()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.BreakFilter(x => ((String)x).Length == 3, out var c);
		var d = new G.List<(char, char, char)>(E.Distinct(nList));
		d.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var e = E.Where(d, x => ((String)x).Length == 3);
		var f = E.Where(d, x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, a));
		Assert.IsTrue(b.Equals(e));
		Assert.IsTrue(E.SequenceEqual(e, b));
		Assert.IsTrue(c.Equals(f));
		Assert.IsTrue(E.SequenceEqual(f, c));
		b = a.BreakFilter(x => ((String)x).All(y => y is >= 'A' and <= 'Z'), out c);
		e = E.Where(d, x => ((String)x).All(y => y is >= 'A' and <= 'Z'));
		f = E.Where(d, x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, a));
		Assert.IsTrue(b.Equals(e));
		Assert.IsTrue(E.SequenceEqual(e, b));
		Assert.IsTrue(c.Equals(f));
		Assert.IsTrue(E.SequenceEqual(f, c));
	}

	[TestMethod]
	public void TestBreakFilterInPlace()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.BreakFilterInPlace(x => ((String)x).Length == 3, out var c);
		var d = new G.List<(char, char, char)>(E.Distinct(nList));
		d.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var e = E.ToList(E.Where(d, x => ((String)x).Length != 3));
		d = E.ToList(E.Where(d, x => ((String)x).Length == 3));
		BaseListTests<(char, char, char), NListHashSet<(char, char, char)>>.BreakFilterInPlaceAsserts(a, b, c, d, e);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.BreakFilterInPlace(x => ((String)x).All(y => y is >= 'A' and <= 'Z'), out c);
		d = [.. E.Distinct(nList)];
		d.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		e = E.ToList(E.Where(d, x => !((String)x).All(y => y is >= 'A' and <= 'Z')));
		d = E.ToList(E.Where(d, x => ((String)x).All(y => y is >= 'A' and <= 'Z')));
		BaseListTests<(char, char, char), NListHashSet<(char, char, char)>>.BreakFilterInPlaceAsserts(a, b, c, d, e);
	}

	[TestMethod]
	public void TestClear()
	{
		var a = nList.ToNHashSet();
		a.Clear(2, 3);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		for (var i = 0; i < 3; i++)
			b[2 + i] = default!;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
	}

	[TestMethod]
	public void TestCompare()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		for (var i = 0; i < 1000; i++)
		{
			var a = new NListHashSet<(char, char, char)>(E.Select(E.Range(0, random.Next(3, 100)), _ => ((char, char, char))(String)random.Next(1000).ToString("D3")));
			var b = new NListHashSet<(char, char, char)>(a);
			var n = random.Next(0, a.Length);
			do
			{
				var item = ((char, char, char))(String)random.Next(1000).ToString("D3");
				if (!b.Contains(item))
					b[n] = item;
			}
			while (b[n] == a[n]);
			Assert.AreEqual(n, a.Compare(b));
			a = [.. E.Select(E.Range(0, random.Next(5, 100)), _ => ((char, char, char))(String)random.Next(1000).ToString("D3"))];
			b = [.. a];
			n = random.Next(2, a.Length);
			do
			{
				var item = ((char, char, char))(String)random.Next(1000).ToString("D3");
				if (!b.Contains(item))
					b[n] = item;
			}
			while (b[n] == a[n]);
			Assert.AreEqual(n - 1, a.Compare(b, n - 1));
			a = [.. E.Select(E.Range(0, random.Next(5, 100)), _ => ((char, char, char))(String)random.Next(1000).ToString("D3"))];
			b = [.. a];
			var length = a.Length;
			n = random.Next(2, a.Length);
			do
			{
				var item = ((char, char, char))(String)random.Next(1000).ToString("D3");
				if (!b.Contains(item))
					b[n] = item;
			}
			while (b[n] == a[n]);
			int index = random.Next(2, 50), otherIndex = random.Next(2, 50);
			a.Insert(0, E.Select(E.Range(0, index), _ => ((char, char, char))(String)random.Next(1000).ToString("D3")));
			index = a.Length - b.Length;
			b.Insert(0, E.Select(E.Range(0, otherIndex), _ => ((char, char, char))(String)random.Next(1000).ToString("D3")));
			otherIndex = b.Length - a.Length + index;
			Assert.AreEqual(n, a.Compare(index, b, otherIndex));
			Assert.AreEqual(n, a.Compare(index, b, otherIndex, length));
		}
	}

	[TestMethod]
	public void TestConcat()
	{
		var a = nList.ToNHashSet();
		var b = a.Concat(defaultNCollection);
		var c = E.ToHashSet(E.Concat(nList, defaultNCollection));
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestContains()
	{
		var a = nList.ToNHashSet();
		var b = a.Contains(('M', 'M', 'M'));
		Assert.IsTrue(b);
		b = a.Contains(('B', 'B', 'B'), 2);
		Assert.IsFalse(b);
		b = a.Contains(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('E', 'E', 'E')));
		Assert.IsTrue(b);
		b = a.Contains(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('N', 'N', 'N')));
		Assert.IsFalse(b);
		Assert.ThrowsExactly<ArgumentNullException>(() => a.Contains((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestContainsAny()
	{
		var a = nList.ToNHashSet();
		var b = a.ContainsAny(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M')));
		Assert.IsTrue(b);
		b = a.ContainsAny(new List<(char, char, char)>(('L', 'L', 'L'), ('M', 'M', 'M'), ('N', 'N', 'N')));
		Assert.IsTrue(b);
		b = a.ContainsAny(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z')));
		Assert.IsFalse(b);
	}

	[TestMethod]
	public void TestContainsAnyExcluding()
	{
		var a = nList.ToNHashSet();
		var b = a.ContainsAnyExcluding(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M')));
		Assert.IsTrue(b);
		b = a.ContainsAnyExcluding(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z')));
		Assert.IsTrue(b);
		b = a.ContainsAnyExcluding(a);
		Assert.IsFalse(b);
	}

	[TestMethod]
	public void TestConvert()
	{
		var a = nList.ToNHashSet();
		var b = a.Convert((x, index) => (x, index));
		var c = E.Select(E.ToHashSet(nList), (x, index) => (x, index));
		var d = a.Convert(x => x + "A");
		var e = E.Select(E.ToHashSet(nList), x => x + "A");
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.IsTrue(d.Equals(e));
		Assert.IsTrue(E.SequenceEqual(e, d));
	}

	[TestMethod]
	public void TestCopyTo()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		var a = nList.ToNHashSet();
		var b = RedStarLinq.FillArray(16, x => ((char, char, char))new String(RedStarLinq.FillArray(3, x => (char)random.Next(65536))));
		var c = ((char, char, char)[])b.Clone();
		var d = ((char, char, char)[])b.Clone();
		var e = ((char, char, char)[])b.Clone();
		a.CopyTo(b);
		new G.List<(char, char, char)>(E.Distinct(nList)).CopyTo(c);
		a.CopyTo(d, 3);
		new G.List<(char, char, char)>(E.Distinct(nList)).CopyTo(e, 3);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.IsTrue(E.SequenceEqual(e, d));
	}

	[TestMethod]
	public void TestEndsWith()
	{
		var a = nList.ToNHashSet();
		var b = a.EndsWith(('E', 'E', 'E'));
		Assert.IsTrue(b);
		b = a.EndsWith(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('E', 'E', 'E')));
		Assert.IsTrue(b);
		b = a.EndsWith(new List<(char, char, char)>(('M', 'M', 'M'), ('D', 'D', 'D'), ('E', 'E', 'E')));
		Assert.IsFalse(b);
		b = a.EndsWith(new List<(char, char, char)>(('M', 'M', 'M'), ('E', 'E', 'E'), ('N', 'N', 'N')));
		Assert.IsFalse(b);
	}

	[TestMethod]
	public void TestEquals()
	{
		var a = nList.ToNHashSet();
		var b = a.Contains(('M', 'M', 'M'));
		Assert.IsTrue(b);
		b = a.Equals(new List<(char, char, char)>(('B', 'B', 'B'), ('P', 'P', 'P'), ('D', 'D', 'D')), 1);
		Assert.IsTrue(b);
		b = a.Equals(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('N', 'N', 'N')), 1);
		Assert.IsFalse(b);
		b = a.Equals(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M')), 2);
		Assert.IsFalse(b);
		b = a.Equals(new List<(char, char, char)>(('B', 'B', 'B'), ('P', 'P', 'P'), ('D', 'D', 'D')), 1, true);
		Assert.IsFalse(b);
	}

	[TestMethod]
	public void TestFillInPlace()
	{
		var a = nList.ToNHashSet();
		a.FillInPlace(('X', 'X', 'X'), 0);
		G.HashSet<(char, char, char)> b = [];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.FillInPlace(('X', 'X', 'X'), 1);
		b = [('X', 'X', 'X')];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(('X', 'X', 'X'), -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(('X', 'X', 'X'), 2));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(('X', 'X', 'X'), 101));
		a.FillInPlace(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 0);
		b = [];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.FillInPlace(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 1);
		b = [('0', '0', '0')];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), -1));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 2));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"), 101));
		a.FillInPlace(0, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"));
		b = [];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a.FillInPlace(1, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3"));
		b = [('0', '0', '0')];
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(-1, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(2, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a.FillInPlace(101, index => ((char, char, char))(String)(index ^ index >> 1).ToString("D3")));
	}

	[TestMethod]
	public void TestFilter()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.Filter(x => ((String)x).Length == 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = E.Where(c, x => ((String)x).Length == 3);
		var chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
		b = a.Filter((x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') && index >= 1);
		d = E.Where(c, (x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') && index >= 1);
		chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
	}

	[TestMethod]
	public void TestFilterInPlace()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.FilterInPlace(x => ((String)x).Length == 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		c = E.ToList(E.Where(c, x => ((String)x).Length == 3));
		var chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.FilterInPlace((x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') && index >= 1);
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		c = E.ToList(E.Where(c, (x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') && index >= 1));
		chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
	}

	[TestMethod]
	public void TestFind()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.Find(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.Find(x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.Find(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.Find(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
	}

	[TestMethod]
	public void TestFindAll()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.FindAll(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.FindAll(x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.FindAll(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.FindAll(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
	}

	[TestMethod]
	public void TestFindIndex()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.FindIndex(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.FindIndex(x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.FindIndex(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.FindIndex(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
	}

	[TestMethod]
	public void TestFindLast()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.FindLast(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.FindLast(x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.FindLast(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.FindLast(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
	}

	[TestMethod]
	public void TestFindLastIndex()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.FindLastIndex(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.FindLastIndex(x => ((String)x).Length != 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.FindLastIndex(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.FindLastIndex(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
	}

	[TestMethod]
	public void TestGetAfter()
	{
		var a = nList.ToNHashSet();
		var b = a.GetAfter(new(('P', 'P', 'P')));
		var c = new G.List<(char, char, char)>() { ('D', 'D', 'D'), ('E', 'E', 'E') };
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetAfter([]);
		c = [.. E.Distinct(nList)];
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetAfter(new(('D', 'D', 'D'), ('E', 'E', 'E')));
		c = [];
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestGetBefore()
	{
		var a = nList.ToNHashSet();
		var b = a.GetBefore(new(('D', 'D', 'D')));
		var c = new G.List<(char, char, char)>() { ('M', 'M', 'M'), ('B', 'B', 'B'), ('P', 'P', 'P') };
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetBefore([]);
		c = [];
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetBefore(new(('D', 'D', 'D'), ('E', 'E', 'E')));
		c = [('M', 'M', 'M'), ('B', 'B', 'B'), ('P', 'P', 'P')];
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestGetBeforeSetAfter()
	{
		var a = nList.ToNHashSet();
		var b = a.GetBeforeSetAfter(new(('D', 'D', 'D')));
		var c = new G.List<(char, char, char)>() { ('M', 'M', 'M'), ('B', 'B', 'B'), ('P', 'P', 'P') };
		var d = new G.List<(char, char, char)>() { ('E', 'E', 'E') };
		Assert.IsTrue(a.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestGetRange()
	{
		var length = E.ToHashSet(nList).Count;
		var a = nList.ToNHashSet();
		var b = a.GetRange(..);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(.., true);
		b.Add(defaultNString);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(0, length);
		c.Add(defaultNString);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(0, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(1..);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(1..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(1..4);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(^4..);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(^4..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetRange(^4..4);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 8 - length);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = a.GetRange(-1..4));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = a.GetRange(^1..1));
		Assert.ThrowsExactly<ArgumentException>(() => b = a.GetRange(1..1000));
	}

	[TestMethod]
	public void TestGetSlice()
	{
		var length = E.ToHashSet(nList).Count;
		var a = nList.ToNHashSet();
		var b = a.GetSlice(..);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(1, 3);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(^4);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(0, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(1..);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(1..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(1..4);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(1, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(^4..);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 4);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(^4..^1);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = a.GetSlice(^4..4);
		c = new G.List<(char, char, char)>(E.Distinct(nList)).GetRange(length - 4, 8 - length);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = a.GetSlice(-1..4));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = a.GetSlice(^1..1));
		Assert.ThrowsExactly<ArgumentException>(() => b = a.GetSlice(1..1000));
	}

	[TestMethod]
	public void TestIndexOf()
	{
		var a = nList.ToNHashSet();
		var b = a.IndexOf(('M', 'M', 'M'));
		Assert.AreEqual(0, b);
		b = a.IndexOf(('B', 'B', 'B'), 2);
		Assert.AreEqual(-1, b);
		b = a.IndexOf(('B', 'B', 'B'), 1, 2);
		Assert.AreEqual(1, b);
		b = a.IndexOf(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('E', 'E', 'E')));
		Assert.AreEqual(2, b);
		b = a.IndexOf(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('N', 'N', 'N')));
		Assert.AreEqual(-1, b);
		b = a.IndexOf(new[] { ('D', 'D', 'D'), ('E', 'E', 'E') }, 3);
		Assert.AreEqual(3, b);
		b = a.IndexOf(new[] { ('D', 'D', 'D'), ('E', 'E', 'E') }, 0, 3);
		Assert.AreEqual(-1, b);
		Assert.ThrowsExactly<ArgumentNullException>(() => a.IndexOf((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestIndexOfAny()
	{
		var a = nList.ToNHashSet();
		var b = a.IndexOfAny(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M')));
		Assert.AreEqual(0, b);
		b = a.IndexOfAny(new List<(char, char, char)>(('L', 'L', 'L'), ('N', 'N', 'N'), ('P', 'P', 'P')));
		Assert.AreEqual(2, b);
		b = a.IndexOfAny(new[] { ('L', 'L', 'L'), ('N', 'N', 'N'), ('P', 'P', 'P') }, 3);
		Assert.AreEqual(-1, b);
		b = a.IndexOfAny(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z')));
		Assert.AreEqual(-1, b);
		Assert.ThrowsExactly<ArgumentNullException>(() => a.IndexOfAny((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestIndexOfAnyExcluding()
	{
		var a = nList.ToNHashSet();
		var b = a.IndexOfAnyExcluding(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M')));
		Assert.AreEqual(1, b);
		b = a.IndexOfAnyExcluding(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z')));
		Assert.AreEqual(0, b);
		b = a.IndexOfAnyExcluding(a);
		Assert.AreEqual(-1, b);
		Assert.ThrowsExactly<ArgumentNullException>(() => a.IndexOfAnyExcluding((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestInsert()
	{
		var a = nList.ToNHashSet().Insert(3, defaultNString);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		b.Insert(3, defaultNString);
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		a = nList.ToNHashSet().Insert(3, defaultNCollection);
		b = [.. E.Distinct(nList)];
		b.InsertRange(3, defaultNCollection);
		bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => a = nList.ToNHashSet().Insert(1000, defaultNString));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => nList.ToNHashSet().Insert(-1, defaultNCollection));
		Assert.ThrowsExactly<ArgumentNullException>(() => nList.ToNHashSet().Insert(4, (G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestLastIndexOfAny()
	{
		var a = nList.ToNHashSet();
		int b;
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAny(new List<(char, char, char)>(('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M'))));
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAny(new List<(char, char, char)>(('L', 'L', 'L'), ('N', 'N', 'N'), ('P', 'P', 'P'))));
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAny(new[] { ('L', 'L', 'L'), ('N', 'N', 'N'), ('E', 'E', 'E') }, 3));
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAny(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z'))));
		Assert.ThrowsExactly<NotSupportedException>(() => a.LastIndexOfAny((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestLastIndexOfAnyExcluding()
	{
		var a = nList.ToNHashSet();
		int b;
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAnyExcluding(new List<(char, char, char)>(('B', 'B', 'B'), ('E', 'E', 'E'), ('D', 'D', 'D'))));
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAnyExcluding(new List<(char, char, char)>(('X', 'X', 'X'), ('Y', 'Y', 'Y'), ('Z', 'Z', 'Z'))));
		Assert.ThrowsExactly<NotSupportedException>(() => b = a.LastIndexOfAnyExcluding(a));
		Assert.ThrowsExactly<NotSupportedException>(() => a.LastIndexOfAnyExcluding((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestRemove()
	{
		var length = E.ToHashSet(nList).Count;
		var a = nList.ToNHashSet();
		var b = new NListHashSet<(char, char, char)>(a).RemoveEnd(4);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.RemoveRange(4, 1);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(0, 1);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(0, 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(1, length - 2);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(1, length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(1, 3);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(1, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(length - 4, 3);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(length - 4, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(length - 4, 8 - length);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(length - 4, 8 - length);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(-1, 6));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(length - 1, 2 - length));
		Assert.ThrowsExactly<ArgumentException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(1, 1000));
		b = new NListHashSet<(char, char, char)>(a).Remove(..);
		c = [];
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(..^1);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(0, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(1..);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(1, length - 1);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(1..^1);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(1, length - 2);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(1..4);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(1, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(^4..^1);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(length - 4, 3);
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		b = new NListHashSet<(char, char, char)>(a).Remove(^4..4);
		c = [.. E.Distinct(nList)];
		c.RemoveRange(length - 4, 8 - length);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(-1..4));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(^1..1));
		Assert.ThrowsExactly<ArgumentException>(() => b = new NListHashSet<(char, char, char)>(a).Remove(1..1000));
	}

	[TestMethod]
	public void TestRemoveAll()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.RemoveAll(x => ((String)x).Length != 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.RemoveAll(x => ((String)x).Length != 3);
		var chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.AreEqual(d, b);
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.RemoveAll(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = c.RemoveAll(x => !((String)x).All(y => y is >= 'A' and <= 'Z'));
		chs = E.ToHashSet(c);
		Assert.IsTrue(a.Equals(chs));
		Assert.IsTrue(E.SequenceEqual(chs, a));
		Assert.AreEqual(d, b);
	}

	[TestMethod]
	public void TestRemoveAt()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		var a = nList.ToNHashSet();
		for (var i = 0; i < 1000; i++)
		{
			var index = random.Next(a.Length);
			var b = new NListHashSet<(char, char, char)>(a).RemoveAt(index);
			var c = new G.List<(char, char, char)>(a);
			c.RemoveAt(index);
			Assert.IsTrue(a[..index].Equals(b[..index]));
			Assert.IsTrue(E.SequenceEqual(b[..index], a[..index]));
			Assert.IsTrue(a[(index + 1)..].Equals(b[index..]));
			Assert.IsTrue(E.SequenceEqual(b[index..], a[(index + 1)..]));
			Assert.IsTrue(b.Equals(c));
			Assert.IsTrue(E.SequenceEqual(c, b));
		}
	}

	[TestMethod]
	public void TestRemoveValue()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		var a = new Chain(15, 10).ToNHashSet();
		for (var i = 0; i < 1000; i++)
		{
			var value = a.Random(random);
			var b = new NListHashSet<int>(a);
			b.RemoveValue(value);
			var c = new G.List<int>(a);
			c.Remove(value);
			foreach (var x in a)
				Assert.AreEqual(b.Contains(x), x != value);
			Assert.IsTrue(b.Equals(c));
			Assert.IsTrue(E.SequenceEqual(c, b));
		}
	}

	[TestMethod]
	public void TestReplace()
	{
		var a = nList.ToNHashSet().Replace(defaultNCollection);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		b.Clear();
		b.AddRange(defaultNCollection);
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
	}

	[TestMethod]
	public void TestReplaceRange()
	{
		var a = nList.ToNHashSet().ReplaceRange(2, 1, defaultNCollection);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		b.RemoveRange(2, 1);
		b.InsertRange(2, defaultNCollection);
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		a = nList.ToNHashSet().ReplaceRange(1, 3, defaultNCollection);
		b = [.. E.Distinct(nList)];
		b.RemoveRange(1, 3);
		b.InsertRange(1, defaultNCollection);
		bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		Assert.ThrowsExactly<ArgumentException>(() => a = nList.ToNHashSet().ReplaceRange(1, 1000, new(defaultNString)));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => nList.ToNHashSet().ReplaceRange(-1, 3, defaultNCollection));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => nList.ToNHashSet().ReplaceRange(3, -2, defaultNCollection));
		Assert.ThrowsExactly<ArgumentNullException>(() => nList.ToNHashSet().ReplaceRange(3, 1, null!));
	}

	[TestMethod]
	public void TestSetAll()
	{
		var a = nList.ToNHashSet().SetAll(defaultNString);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		for (var i = 0; i < b.Count; i++)
			b[i] = defaultNString;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a = nList.ToNHashSet().SetAll(defaultNString, 3);
		b = [.. E.Distinct(nList)];
		for (var i = 3; i < b.Count; i++)
			b[i] = defaultNString;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a = nList.ToNHashSet().SetAll(defaultNString, 2, 2);
		b = [.. E.Distinct(nList)];
		for (var i = 2; i < 4; i++)
			b[i] = defaultNString;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a = nList.ToNHashSet().SetAll(defaultNString, ^4);
		b = [.. E.Distinct(nList)];
		for (var i = b.Count - 4; i < b.Count; i++)
			b[i] = defaultNString;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
		a = nList.ToNHashSet().SetAll(defaultNString, ^4..3);
		b = [.. E.Distinct(nList)];
		for (var i = b.Count - 4; i < 3; i++)
			b[i] = defaultNString;
		Assert.IsTrue(a.Equals(b));
		Assert.IsTrue(E.SequenceEqual(b, a));
	}

	[TestMethod]
	public void TestSetOrAdd()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		var a = nList.ToNHashSet();
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		for (var i = 0; i < 1000; i++)
		{
			var index = (int)Floor(Cbrt(random.NextDouble()) * (a.Length + 1));
			(char, char, char) n;
			do
			{
				n = ((char, char, char))(String)random.Next(1000).ToString("D3");
			} while (a.Contains(n));
			a.SetOrAdd(index, n);
			if (index < b.Count)
				b[index] = n;
			else
				b.Add(n);
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(E.SequenceEqual(b, a));
		}
	}

	[TestMethod]
	public void TestSetRange()
	{
		var hs = defaultNCollection.ToNHashSet().GetSlice(..3);
		var hs2 = E.Except(hs, nList).ToNHashSet().GetSlice();
		var a = nList.ToNHashSet().SetRange(2, hs);
		var b = new G.List<(char, char, char)>(E.Distinct(nList));
		for (var i = 0; i < hs2.Length; i++)
			b[i + 2] = hs2[i];
		var bhs = E.ToHashSet(b);
		Assert.IsTrue(a.Equals(bhs));
		Assert.IsTrue(E.SequenceEqual(bhs, a));
		Assert.ThrowsExactly<ArgumentException>(() => a = nList.ToNHashSet().SetRange(4, hs));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => nList.ToNHashSet().SetRange(-1, hs));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => nList.ToNHashSet().SetRange(1000, hs));
		Assert.ThrowsExactly<ArgumentNullException>(() => nList.ToNHashSet().SetRange(3, null!));
	}

	[TestMethod]
	public void TestSkip()
	{
		var a = nList.ToNHashSet();
		var b = a.Skip(2);
		var c = E.Skip(E.Distinct(nList), 2);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Skip(0);
		c = E.Skip(E.Distinct(nList), 0);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Skip(1000);
		c = E.Skip(E.Distinct(nList), 1000);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Skip(-3);
		c = E.Skip(E.Distinct(nList), -3);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestSkipLast()
	{
		var a = nList.ToNHashSet();
		var b = a.SkipLast(2);
		var c = E.SkipLast(E.Distinct(nList), 2);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.SkipLast(0);
		c = E.SkipLast(E.Distinct(nList), 0);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.SkipLast(1000);
		c = E.SkipLast(E.Distinct(nList), 1000);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.SkipLast(-3);
		c = E.SkipLast(E.Distinct(nList), -3);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestSkipWhile()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.SkipWhile(x => ((String)x).Length == 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = E.ToList(E.SkipWhile(c, x => ((String)x).Length == 3));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.SkipWhile((x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') || index < 1);
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = E.ToList(E.SkipWhile(E.Skip(c, 1), x => ((String)x).All(y => y is >= 'A' and <= 'Z')));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
	}

	[TestMethod]
	public void TestStartsWith()
	{
		var a = nList.ToNHashSet();
		var b = a.StartsWith(('M', 'M', 'M'));
		Assert.IsTrue(b);
		b = a.StartsWith(new List<(char, char, char)>(('M', 'M', 'M'), ('B', 'B', 'B'), ('P', 'P', 'P')));
		Assert.IsTrue(b);
		b = a.StartsWith(new List<(char, char, char)>(('M', 'M', 'M'), ('B', 'B', 'B'), ('X', 'X', 'X')));
		Assert.IsFalse(b);
		Assert.ThrowsExactly<ArgumentNullException>(() => a.StartsWith((G.IEnumerable<(char, char, char)>)null!));
	}

	[TestMethod]
	public void TestTake()
	{
		var a = nList.ToNHashSet();
		var b = a.Take(2);
		var c = E.Take(E.Distinct(nList), 2);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Take(0);
		c = E.Take(E.Distinct(nList), 0);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Take(1000);
		c = E.Take(E.Distinct(nList), 1000);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.Take(-3);
		c = E.Take(E.Distinct(nList), -3);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestTakeLast()
	{
		var a = nList.ToNHashSet();
		var b = a.TakeLast(2);
		var c = E.TakeLast(E.Distinct(nList), 2);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.TakeLast(0);
		c = E.TakeLast(E.Distinct(nList), 0);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.TakeLast(1000);
		c = E.TakeLast(E.Distinct(nList), 1000);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
		a = [.. nList];
		b = a.TakeLast(-3);
		c = E.TakeLast(E.Distinct(nList), -3);
		Assert.IsTrue(a.Equals(E.Distinct(nList)));
		Assert.IsTrue(E.SequenceEqual(E.Distinct(nList), a));
		Assert.IsTrue(b.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, b));
	}

	[TestMethod]
	public void TestTakeWhile()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.TakeWhile(x => ((String)x).Length == 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = E.ToList(E.TakeWhile(c, x => ((String)x).Length == 3));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
		a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		b = a.TakeWhile((x, index) => ((String)x).All(y => y is >= 'A' and <= 'Z') && index < 10);
		c = [.. E.Distinct(nList)];
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		d = E.ToList(E.TakeWhile(E.Take(c, 10), x => ((String)x).All(y => y is >= 'A' and <= 'Z')));
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.IsTrue(b.Equals(d));
		Assert.IsTrue(E.SequenceEqual(d, b));
	}

	[TestMethod]
	public void TestToArray()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, capacity;
		G.List<(char, char, char)> b;
		(char, char, char)[] array;
		(char, char, char)[] array2;
		(char, char, char) elem;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			capacity = length + random.Next(151);
			NListHashSet<(char, char, char)> a = new(capacity);
			b = new(capacity);
			for (var j = 0; j < length; j++)
			{
				a.Add(elem = ((char, char, char))new String((char)random.Next(33, 127), 3));
				if (!b.Contains(elem))
					b.Add(elem);
			}
			array = a.ToArray();
			array2 = [.. b];
			Assert.IsTrue(RedStarLinq.Equals(array, array2));
			Assert.IsTrue(E.SequenceEqual(array, array2));
		}
	}

	[TestMethod]
	public void TestTrimExcess()
	{
		var random = Lock(lockObj, () => new Random(Global.random.Next()));
		int length, capacity;
		G.List<(char, char, char)> b;
		(char, char, char) elem;
		for (var i = 0; i < 1000; i++)
		{
			length = random.Next(51);
			capacity = length + random.Next(9951);
			NListHashSet<(char, char, char)> a = new(capacity);
			b = new(capacity);
			for (var j = 0; j < length; j++)
			{
				a.Add(elem = ((char, char, char))new String((char)random.Next(33, 127), 3));
				if (!b.Contains(elem))
					b.Add(elem);
			}
			a.TrimExcess();
			Assert.IsTrue(RedStarLinq.Equals(a, b));
			Assert.IsTrue(E.SequenceEqual(a, b));
		}
	}

	[TestMethod]
	public void TestTrueForAll()
	{
		var a = nList.ToNHashSet().Insert(3, new List<(char, char, char)>(('$', '\0', '\0'), ('#', '#', '#')));
		var b = a.TrueForAll(x => ((String)x).Length == 3);
		var c = new G.List<(char, char, char)>(E.Distinct(nList));
		c.InsertRange(3, [('$', '\0', '\0'), ('#', '#', '#')]);
		var d = c.TrueForAll(x => ((String)x).Length == 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		b = a.TrueForAll(x => ((String)x).Length <= 3);
		d = c.TrueForAll(x => ((String)x).Length <= 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
		b = a.TrueForAll(x => ((String)x).Length > 3);
		d = c.TrueForAll(x => ((String)x).Length > 3);
		Assert.IsTrue(a.Equals(c));
		Assert.IsTrue(E.SequenceEqual(c, a));
		Assert.AreEqual(d, b);
	}
}

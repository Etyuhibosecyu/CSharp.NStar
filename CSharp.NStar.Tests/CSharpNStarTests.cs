global using NHashSets;
global using System;
global using System.Collections.Immutable;
global using E = System.Linq.Enumerable;
global using G = System.Collections.Generic;
global using static NStar.Core.Extents;
global using static NStar.Core.Tests.Global;
global using static System.Math;

namespace NStar.Core.Tests;

public static class Global
{
	public static readonly Random random = new(1234567890);
	public static readonly object lockObj = new();
	internal static readonly G.IEnumerable<(char, char, char)> defaultNCollection = new NList<(char, char, char)>(('A', 'A', 'A'), ('B', 'B', 'B'), ('A', 'A', 'A'), ('B', 'B', 'B'), ('C', 'C', 'C'), ('B', 'B', 'B'), ('C', 'C', 'C'), ('D', 'D', 'D'), ('C', 'C', 'C'));
	internal static readonly (char, char, char) defaultNString = ('X', 'X', 'X');
	internal static readonly ImmutableArray<(char, char, char)> nList = [('M', 'M', 'M'), ('B', 'B', 'B'), ('P', 'P', 'P'), ('D', 'D', 'D'), ('M', 'M', 'M'), ('E', 'E', 'E'), ('D', 'D', 'D')];
}

global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using NStar.MathLib.Extras;
global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Reflection;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.NStarUtilityFunctions;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Avalonia.Controls;

namespace CSharp.NStar;

public static class MemberConverters
{

	public static String FunctionMapping(String function, List<NStarType> parameterTypes, List<String>? parameters)
	{
		var result = function.ToString() switch
		{
			"Add" => parameterTypes.Length != 0 && GetSubtype(parameterTypes[0]) == NullType
				&& !TypeEqualsToPrimitive(parameterTypes[0], "tuple", false)
				? nameof(function.Add) : nameof(function.AddRange),
			"Ceil" => "(int)" + nameof(Ceiling),
			nameof(Ceiling) => [],
			"Chain" => ((String)nameof(NStarUtilityFunctions)).Add('.').AddRange(nameof(Chain)),
			nameof(RedStarLinq.Fill) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)),
			"FillList" => [],
			nameof(Floor) => "(int)" + nameof(Floor),
			"IntRandom" => nameof(IntRandomNumber),
			nameof(IntRandomNumber) => [],
			"IntToReal" => "(double)",
			"IsSummertime" => nameof(DateTime.IsDaylightSavingTime),
			nameof(DateTime.IsDaylightSavingTime) => [],
			"Log" => ((String)nameof(NStarUtilityFunctions)).Add('.').AddRange(nameof(Log)),
			nameof(RedStarLinqMath.Max) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Max)),
			"Max3" => [],
			nameof(RedStarLinqMath.Mean) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Mean)),
			"Mean3" => [],
			nameof(RedStarLinqMath.Min) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Min)),
			"Min3" => [],
			"Random" => nameof(RandomNumber),
			nameof(RandomNumber) => [],
			nameof(Round) => "(int)" + nameof(Round),
			nameof(ToString) => [],
			"ToUnsafeString" => nameof(ToString),
			nameof(Truncate) => "(int)" + nameof(Truncate),
			_ => function.Copy(),
		};
		if (parameters == null)
			return result;
		result.Add('(');
		if (function.ToString() is nameof(parameters.RemoveAt)
			or nameof(parameters.RemoveEnd) or nameof(parameters.Reverse) && parameters.Length >= 1
			|| function.ToString() is nameof(parameters.GetRange) or nameof(parameters.Remove) && parameters.Length == 2)
			parameters[0].Insert(0, '(').AddRange(") - 1");
		if (function.ToString() is nameof(parameters.IndexOf) or nameof(parameters.LastIndexOf)
			or nameof(Grid.SetColumn) or nameof(Grid.SetRow) && parameters.Length >= 2)
			parameters[1].Insert(0, '(').AddRange(") - 1");
		result.AddRange(String.Join(", ", parameters)).Add(')');
		if (function.ToString() is nameof(parameters.IndexOf) or nameof(parameters.LastIndexOf))
			result.Insert(0, '(').AddRange(") + 1");
		return result;
	}

	public static String PropertyMapping(String property) => property.ToString() switch
	{
		"UTCNow" => nameof(DateTime.UtcNow),
		nameof(DateTime.UtcNow) => [],
		_ => property.Copy(),
	};
}

using System.Collections;
using System.Text;

namespace CSharp.NStar;

public static class DeclaredConstructionMappings
{
	public static Type TypeMapping(UniversalTypeOrValue UnvType) =>
		UnvType.MainType.IsValue || !PrimitiveTypesList.TryGetValue(UnvType.MainType.Type.ToShortString(), out var innerType)
		&& !ExtraTypesList.TryGetValue((CreateVar(SplitType(UnvType.MainType.Type), out var split).Container.ToShortString(),
		split.Type), out innerType) && (UnvType.MainType.Type.Length != 1
		|| ExplicitlyConnectedNamespacesList.FindIndex(y => ExtraTypesList.TryGetValue((y,
		UnvType.MainType.Type.ToShortString()), out innerType)) < 0) ? throw new InvalidOperationException()
		: innerType.ContainsGenericParameters
		? innerType.MakeGenericType(UnvType.ExtraTypes.ToArray(x => TypeMapping(x.Value))) : innerType;

	public static UniversalType TypeMappingBack(Type netType, Type[] genericArguments, GeneralExtraTypes extraTypes)
	{
		if (netType.IsSZArray || netType.IsPointer)
			netType = typeof(List<>);
		else if (netType == typeof(BitArray))
			netType = typeof(BitList);
		else if (netType == typeof(Index))
			netType = typeof(int);
		var typeGenericArguments = netType.GetGenericArguments();
		if (netType.Name.Contains("Func"))
		{
			return new(FuncBlockStack, new([.. typeGenericArguments.GetSlice(..^1).Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes)),
				typeGenericArguments[^1].Wrap(x => TypeMappingBack(x, genericArguments, extraTypes))]));
		}
		int foundIndex;
		if ((foundIndex = genericArguments.FindIndex(x => x.Name == netType.Name)) >= 0)
			netType = TypeMapping(extraTypes[foundIndex]);
		List<Type> innerTypes = [];
		foreach (var genericArgument in netType.GenericTypeArguments)
		{
			if ((foundIndex = genericArguments.IndexOf(genericArgument)) < 0)
				continue;
			innerTypes.Add(TypeMapping(extraTypes[foundIndex]));
		}
		if (netType.IsGenericType)
			netType = netType.GetGenericTypeDefinition();
		l1:
		if (CreateVar(PrimitiveTypesList.Find(x => x.Value == netType).Key, out var typename) != null)
			return typename == "list" ? new(ListBlockStack,
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]))
				: GetPrimitiveType(typename);
		else if (CreateVar(ExtraTypesList.Find(x => x.Value == netType).Key, out var type2) != default)
			return new(GetBlockStack(type2.Namespace + "." + type2.Type),
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]));
		else if (CreateVar(InterfacesList.Find(x => x.Value.DotNetType == netType), out var type3).Key != default)
			return new(GetBlockStack(type3.Key.Namespace + "." + type3.Key.Interface),
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]));
		else if (netType == typeof(string))
			return StringType;
		else if (innerTypes.Length != 0)
		{
			netType = netType.MakeGenericType([.. innerTypes]);
			if (netType.Name.Contains("Tuple") || netType.Name.Contains("KeyValuePair"))
			{
				return new(TupleBlockStack, new(netType.GenericTypeArguments.ToList(x =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))));
			}
			innerTypes.Clear();
			goto l1;
		}
		else
			throw new InvalidOperationException();
	}

	public static String FunctionMapping(String function, List<String>? parameters)
	{
		var result = function.ToString() switch
		{
			"Add" => nameof(function.AddRange),
			"Ceil" => "(int)" + nameof(Ceiling),
			nameof(Ceiling) => [],
			"Chain" => ((String)nameof(IntermediateFunctions)).Add('.').AddRange(nameof(Chain)),
			nameof(RedStarLinq.Fill) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)),
			"FillList" => [],
			nameof(Floor) => "(int)" + nameof(Floor),
			"IntRandom" => nameof(IntRandomNumber),
			nameof(IntRandomNumber) => [],
			"IntToReal" => "(double)",
			"IsSummertime" => nameof(DateTime.IsDaylightSavingTime),
			nameof(DateTime.IsDaylightSavingTime) => [],
			"Log" => ((String)nameof(IntermediateFunctions)).Add('.').AddRange(nameof(Log)),
			nameof(RedStarLinqMath.Max) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinqMath.Max)),
			"Max3" => [],
			nameof(RedStarLinqMath.Mean) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinqMath.Mean)),
			"Mean3" => [],
			nameof(RedStarLinqMath.Min) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinqMath.Min)),
			"Min3" => [],
			"Random" => nameof(RandomNumber),
			nameof(RandomNumber) => [],
			nameof(Round) => "(int)" + nameof(Round),
			nameof(Truncate) => "(int)" + nameof(Truncate),
			_ => function.Copy(),
		};
		if (parameters == null)
			return result;
		result.Add('(');
		if (function.ToString() is nameof(parameters.GetRange) or nameof(parameters.Remove) or nameof(parameters.RemoveAt)
			or nameof(parameters.RemoveEnd) or nameof(parameters.Reverse) && parameters.Length >= 1)
			parameters[0].Insert(0, '(').AddRange(") - 1");
		if (function.ToString() is nameof(parameters.IndexOf) or nameof(parameters.LastIndexOf) && parameters.Length >= 2)
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

using System.Collections;
using System.Text;

namespace CSharp.NStar;

public static class DeclaredConstructionMappings
{
	public static Type TypeMapping(UniversalTypeOrValue UnvType) =>
		!UnvType.MainType.IsValue && (PrimitiveTypesList.TryGetValue(UnvType.MainType.Type.ToShortString(), out var innerType)
		|| ExtraTypesList.TryGetValue((CreateVar(SplitType(UnvType.MainType.Type), out var split).Container.ToShortString(),
		split.Type), out innerType) || UnvType.MainType.Type.Length == 1
		&& ExplicitlyConnectedNamespacesList.FindIndex(y => ExtraTypesList.TryGetValue((y,
		UnvType.MainType.Type.ToShortString()), out innerType)) >= 0) ? innerType : throw new InvalidOperationException();

	public static UniversalType TypeMappingBack(Type type, Type[] genericArguments, GeneralExtraTypes extraTypes)
	{
		if (type.IsSZArray || type.IsPointer)
			type = typeof(List<>);
		else if (type == typeof(BitArray))
			type = typeof(BitList);
		else if (type == typeof(Index))
			type = typeof(int);
		var typeGenericArguments = type.GetGenericArguments();
		if (type.Name.Contains("Func"))
		{
			return new(FuncBlockStack, new([.. typeGenericArguments.GetSlice(..^1).Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes)),
				typeGenericArguments[^1].Wrap(x => TypeMappingBack(x, genericArguments, extraTypes))]));
		}
		int foundIndex;
		if ((foundIndex = genericArguments.FindIndex(x => x.Name == type.Name)) >= 0)
			type = TypeMapping(extraTypes[foundIndex]);
		List<Type> innerTypes = [];
		foreach (var genericArgument in type.GenericTypeArguments)
		{
			if ((foundIndex = genericArguments.IndexOf(genericArgument)) < 0)
				continue;
			innerTypes.Add(TypeMapping(extraTypes[foundIndex]));
		}
		if (type.IsGenericType)
			type = type.GetGenericTypeDefinition();
		l1:
		if (CreateVar(PrimitiveTypesList.Find(x => x.Value == type).Key, out var typename) != null)
			return typename == "list" ? new(ListBlockStack,
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]))
				: GetPrimitiveType(typename);
		else if (CreateVar(ExtraTypesList.Find(x => x.Value == type).Key, out var type2) != default)
			return new(GetBlockStack(type2.Namespace + "." + type2.Type),
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]));
		else if (CreateVar(InterfacesList.Find(x => x.Value.DotNetType == type), out var type3).Key != default)
			return new(GetBlockStack(type3.Key.Namespace + "." + type3.Key.Interface),
				new([.. typeGenericArguments.Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]));
		else if (type == typeof(string))
			return StringType;
		else if (innerTypes.Length != 0)
		{
			type = type.MakeGenericType([.. innerTypes]);
			if (type.Name.Contains("Tuple") || type.Name.Contains("KeyValuePair"))
			{
				return new(TupleBlockStack, new(type.GenericTypeArguments.ToList(x =>
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
			nameof(RedStarLinq.Max) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Max)),
			"Max3" => [],
			nameof(RedStarLinq.Mean) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Mean)),
			"Mean3" => [],
			nameof(RedStarLinq.Min) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Min)),
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

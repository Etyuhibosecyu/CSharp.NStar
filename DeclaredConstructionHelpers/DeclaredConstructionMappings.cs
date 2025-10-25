using Mpir.NET;
using NStar.ExtraHS;
using System.Collections;
using System.Text;

namespace CSharp.NStar;

public static class DeclaredConstructionMappings
{
	public static Type TypeMapping(UniversalTypeOrValue UnvType)
	{
		if (UnvType.MainType.IsValue)
			throw new InvalidOperationException();
		if (new BlockStackEComparer().Equals(UnvType.MainType.Type, FuncBlockStack))
		{
			List<Type> funcComponents = [];
			var returnType = TypeMapping(new(UnvType.ExtraTypes[0].MainType.Type, UnvType.ExtraTypes[0].ExtraTypes));
			for (var i = 1; i < UnvType.ExtraTypes.Length; i++)
			{
				if (UnvType.ExtraTypes[i].MainType.IsValue)
					throw new InvalidOperationException();
				funcComponents.Add(TypeMapping(new(UnvType.ExtraTypes[i].MainType.Type, UnvType.ExtraTypes[i].ExtraTypes)));
			}
			return ConstructFuncType(returnType, funcComponents.GetSlice());
		}
		if (!TypeEqualsToPrimitive(new(UnvType.MainType.Type, UnvType.ExtraTypes), "tuple", false))
		{
			if (!TypeExists(SplitType(UnvType.MainType.Type), out var netType))
				throw new InvalidOperationException();
			else if (netType.ContainsGenericParameters)
				return netType.MakeGenericType(UnvType.ExtraTypes.ToArray(x => TypeMapping(x.Value)));
			else
				return netType;
		}
		if (UnvType.ExtraTypes.Length == 0)
			return typeof(void);
		List<Type> tupleComponents = [];
		var first = TypeMapping(new(UnvType.ExtraTypes[0].MainType.Type, UnvType.ExtraTypes[0].ExtraTypes));
		if (UnvType.ExtraTypes.Length == 1)
			return first;
		var innerResult = first;
		for (var i = 1; i < UnvType.ExtraTypes.Length; i++)
		{
			if (!UnvType.ExtraTypes[i].MainType.IsValue)
			{
				tupleComponents.Add(innerResult);
				first = TypeMapping(new(UnvType.ExtraTypes[i].MainType.Type, UnvType.ExtraTypes[i].ExtraTypes));
				continue;
			}
			innerResult = ConstructTupleType(RedStarLinq.FillArray(innerResult,
				int.TryParse(UnvType.ExtraTypes[i].MainType.Value.ToString(), out var n) ? n : 1).GetSlice());
		}
		return ConstructTupleType(tupleComponents.Add(innerResult).GetSlice());
	}

	public static Type ConstructFuncType(Type returnType, Slice<Type> netTypes) => netTypes.Length switch
	{
		0 => typeof(Func<>).MakeGenericType(returnType),
		1 => typeof(Func<,>).MakeGenericType(netTypes[0], returnType),
		2 => typeof(Func<,,>).MakeGenericType(netTypes[0], netTypes[1], returnType),
		3 => typeof(Func<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], returnType),
		4 => typeof(Func<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], returnType),
		5 => typeof(Func<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], returnType),
		6 => typeof(Func<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5], returnType),
		7 => typeof(Func<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5], netTypes[6], returnType),
		8 => typeof(Func<,,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5], netTypes[6], netTypes[7], returnType),
		_ => throw new InvalidOperationException(),
	};

	public static Type ConstructTupleType(Slice<Type> netTypes) => netTypes.Length switch
	{
		0 => throw new InvalidOperationException(),
		1 => typeof(ValueTuple<>).MakeGenericType(netTypes[0]),
		2 => typeof(ValueTuple<,>).MakeGenericType(netTypes[0], netTypes[1]),
		3 => typeof(ValueTuple<,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2]),
		4 => typeof(ValueTuple<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3]),
		5 => typeof(ValueTuple<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4]),
		6 => typeof(ValueTuple<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5]),
		7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5], netTypes[6]),
		_ => typeof(ValueTuple<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4], netTypes[5], netTypes[6], ConstructTupleType(netTypes[7..])),
	};

	public static UniversalType TypeMappingBack(Type netType, Type[] genericArguments, GeneralExtraTypes extraTypes)
	{
		if (netType.IsGenericParameter)
		{
			var genericArgumentsIndex = Array.IndexOf(genericArguments, netType);
			if (genericArgumentsIndex < 0 || extraTypes.Length <= genericArgumentsIndex)
				return new(new([new(BlockType.Extra, netType.Name, 1)]), []);
			else if (extraTypes[genericArgumentsIndex].MainType.IsValue)
				throw new InvalidOperationException();
			else
				return new(extraTypes[genericArgumentsIndex].MainType.Type, extraTypes[genericArgumentsIndex].ExtraTypes);
		}
		if (netType.IsSZArray || netType.IsPointer)
			netType = typeof(List<>).MakeGenericType(netType.GetElementType() ?? throw new InvalidOperationException());
		else if (netType == typeof(BitArray))
			netType = typeof(BitList);
		else if (netType == typeof(Index))
			netType = typeof(int);
		var typeGenericArguments = netType.GetGenericArguments();
		if (netType.Name.Contains("Func"))
		{
			return new(FuncBlockStack, new([typeGenericArguments[^1].Wrap(x =>
			TypeMappingBack(x, genericArguments, extraTypes)),
				.. typeGenericArguments.GetSlice(..^1).Convert((x, index) =>
				(UniversalTypeOrValue)TypeMappingBack(x, genericArguments, extraTypes))]));
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
		var oldNetType = netType;
		if (netType.IsGenericType)
			netType = netType.GetGenericTypeDefinition();
		l1:
		if (CreateVar(PrimitiveTypesList.Find(x => x.Value == netType).Key, out var typename) != null)
			return typename == "list" ? GetListType(TypeMappingBack(typeGenericArguments[0], genericArguments, extraTypes))
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
		else if (netType == typeof(BitList))
			return GetListType(BoolType);
		else if (netType == typeof(NList<>))
			return GetListType(TypeMappingBack(typeGenericArguments[0], genericArguments, extraTypes));
		else if (netType == typeof(NListHashSet<>))
			return new(ListHashSetBlockStack, new([TypeMappingBack(typeGenericArguments[0], genericArguments, extraTypes)]));
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
		else if (!typeof(ITuple).IsAssignableFrom(oldNetType))
			throw new InvalidOperationException();
		GeneralExtraTypes result = [];
		var tupleTypes = new Queue<Type>();
		tupleTypes.Enqueue(oldNetType);
		while (tupleTypes.Length != 0 && tupleTypes.Dequeue() is Type tupleType)
			foreach (var field in tupleType.GetFields())
				if (field.Name == "Rest")
					tupleTypes.Enqueue(tupleType);
				else
					result.Add(TypeMappingBack(field.FieldType, genericArguments, extraTypes));
		return new(TupleBlockStack, result);
	}

	public static bool IsAssignableFromExt(this Type destination, Type source) =>
		destination.IsAssignableFrom(source) || destination == typeof(MpzT) && new[] { typeof(byte), typeof(short),
		typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(MpzT) }.Contains(source)
		|| destination == typeof(long) && new[] { typeof(byte),
		typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long) }.Contains(source)
		|| destination == typeof(int) && new[] { typeof(byte), typeof(short), typeof(ushort), typeof(int) }.Contains(source)
		|| destination == typeof(short) && new[] { typeof(byte), typeof(short) }.Contains(source)
		|| destination == typeof(ulong) && new[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) }.Contains(source)
		|| destination == typeof(uint) && new[] { typeof(byte), typeof(ushort), typeof(uint) }.Contains(source)
		|| destination == typeof(ushort) && new[] { typeof(byte), typeof(ushort) }.Contains(source);

	public static UniversalType ReplaceExtraType(UniversalType originalType, String extraType, UniversalType typeToInsert)
	{
		if (originalType.MainType.Length == 1 && originalType.MainType.Peek().BlockType == BlockType.Extra
			&& originalType.MainType.Peek().Name == extraType && originalType.ExtraTypes.Length == 0)
			return typeToInsert;
		else
		{
			return new(originalType.MainType, [.. originalType.ExtraTypes.Convert(x =>
				new G.KeyValuePair<String, UniversalTypeOrValue>(x.Key, x.Value.MainType.IsValue
				? new UniversalTypeOrValue((TypeOrValue)x.Value.MainType.Value, [])
				: ReplaceExtraType(new(x.Value.MainType.Type, x.Value.ExtraTypes), extraType, typeToInsert)))]);
		}
	}

	public static Type ReplaceExtraNetType(Type originalType, (Type ExtraType, Type TypeToInsert) pattern)
	{
		if (originalType.Equals(pattern.ExtraType))
			return pattern.TypeToInsert;
		else
		{
			var genericArguments = originalType.GetGenericArguments();
			if (genericArguments.Length == 0)
				return originalType;
			else
				return originalType.GetGenericTypeDefinition().MakeGenericType([.. genericArguments.Convert(x =>
				ReplaceExtraNetType(x, pattern))]);
		}
	}

	public static List<(Type ExtraType, Type TypeToInsert)> GetReplacementPatterns(Type[] genericArguments,
		Type[] parameterTypes)
	{
		var length = Min(genericArguments.Length, parameterTypes.Length);
		List<(Type ExtraType, Type TypeToInsert)> result = [];
		for (var i = 0; i < length; i++)
		{
			var genericArgument = genericArguments[i];
			var parameterType = parameterTypes[i];
			if (!parameterType.IsGenericType)
				continue;
			var parameterGenericArguments = parameterType.GetGenericTypeDefinition().GetGenericArguments();
			var index = parameterGenericArguments.FindIndex(x => x.Name == genericArgument.Name);
			if (index != -1)
			{
				result.Add((genericArgument, parameterType.GenericTypeArguments[index]));
				continue;
			}
			result.AddRange(GetReplacementPatterns(genericArgument.GetGenericArguments(),
				parameterType.GetGenericArguments()));
		}
		return result;
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
			nameof(RedStarLinqMath.Max) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Max)),
			"Max3" => [],
			nameof(RedStarLinqMath.Mean) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Mean)),
			"Mean3" => [],
			nameof(RedStarLinqMath.Min) => ((String)nameof(RedStarLinqMath)).Add('.').AddRange(nameof(RedStarLinqMath.Min)),
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
		if (function.ToString() is nameof(parameters.RemoveAt)
			or nameof(parameters.RemoveEnd) or nameof(parameters.Reverse) && parameters.Length >= 1
			|| function.ToString() is nameof(parameters.GetRange) or nameof(parameters.Remove) && parameters.Length == 2)
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

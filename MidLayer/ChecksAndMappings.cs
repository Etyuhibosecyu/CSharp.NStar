global using Corlib.NStar;
global using System;
global using System.Diagnostics;
global using System.Drawing;
global using System.Reflection;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.ChecksAndMappings;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.IntermediateFunctions;
global using static CSharp.NStar.TypeHelpers;
global using static System.Math;
global using String = Corlib.NStar.String;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace CSharp.NStar;
public struct DelegateParameters
{
	public TreeBranch? Location { get; private set; }
	public object? Function { get; private set; }
	public Universal? ContainerValue { get; private set; }

	public DelegateParameters(TreeBranch? location, (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function, Universal? containerValue = null)
	{
		Location = location;
		Function = function;
		ContainerValue = containerValue;
	}

	public DelegateParameters(TreeBranch? location, (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function, Universal? containerValue = null)
	{
		Location = location;
		Function = function;
		ContainerValue = containerValue;
	}
}

public static class ChecksAndMappings
{
	public static readonly String[] operators = ["or", "and", "^^", "||", "&&", "==", "!=", ">=", "<=", ">", "<", "^=", "|=", "&=", ">>=", "<<=", "+=", "-=", "*=", "/=", "%=", "pow=", "=", "^", "|", "&", ">>", "<<", "+", "-", "*", "/", "%", "pow", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "!", "~", "++", "--", "!!"];
	public static readonly bool[] areOperatorsInverted = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false];
	public static readonly bool[] areOperatorsAssignment = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true];
	public static readonly List<String> CollectionTypesList = [nameof(Dictionary<bool, bool>), nameof(FastDelHashSet<bool>), "HashTable", nameof(Corlib.NStar.ICollection), nameof(G.IEnumerable<bool>), nameof(Corlib.NStar.IList), nameof(IReadOnlyCollection<bool>), nameof(IReadOnlyList<bool>), nameof(LimitedQueue<bool>), nameof(G.LinkedList<bool>), nameof(G.LinkedListNode<bool>), nameof(ListHashSet<bool>), nameof(Mirror<bool, bool>), nameof(NList<bool>), nameof(Queue<bool>), nameof(ParallelHashSet<bool>), nameof(ReadOnlySpan<bool>), nameof(Slice<bool>), nameof(SortedDictionary<bool, bool>), nameof(SortedSet<bool>), nameof(Span<bool>), nameof(Stack<bool>), nameof(TreeHashSet<bool>), nameof(TreeSet<bool>)];

	private static readonly Dictionary<Type, bool> memoizedTypes = [];

	public static JsonSerializerSettings SerializerSettings { get; } = new() { Converters = [new StringConverter(), new IEnumerableConverter(), new TupleConverter(), new UniversalConverter(), new ValueTypeConverter(), new IClassConverter(), new DoubleConverter()] };

	public static bool IsUnmanaged(this Type type)
	{
		if (!memoizedTypes.TryGetValue(type, out var answer))
		{
			if (!type.IsValueType)
				answer = false;
			else if (type.IsPrimitive || type.IsPointer || type.IsEnum)
				answer = true;
			else
				answer = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).All(f => IsUnmanaged(f.FieldType));
			memoizedTypes[type] = answer;
		}
		return answer;
	}

	public static String TypeMapping(String type)
	{
		var after = type.GetAfter(((String)"System.Collections.").AddRange(nameof(G.LinkedList<bool>)));
		if (after.Length != 0)
			return "G.LinkedList" + after;
		after = type.GetAfter("System.Collections.");
		if (after.Length != 0)
			return after;
		return type;
	}

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

	public static bool CheckContainer(BlockStack container, Func<BlockStack, bool> check, out BlockStack type)
	{
		if (check(container))
		{
			type = container;
			return true;
		}
		var list = container.ToList().GetSlice();
		BlockStack stack;
		while (list.Any())
		{
			list = list.SkipLast(1);
			if (check(stack = new(list)))
			{
				type = stack;
				return true;
			}
		}
		type = new();
		return false;
	}

	public static bool ExtraTypeExists(BlockStack container, String type)
	{
		if (VariablesList.TryGetValue(container, out var list))
		{
			if (list.TryGetValue(type, out var type2))
			{
				return TypeIsPrimitive(type2.MainType) && type2.MainType.Peek().Name == "typename" && type2.ExtraTypes.Length == 0;
			}
			else
			{
				return false;
			}
		}
		if (UserDefinedPropertiesList.TryGetValue(container, out var list_))
		{
			if (list_.TryGetValue(type, out var a))
			{
				return TypeIsPrimitive(a.UnvType.MainType) && a.UnvType.MainType.Peek().Name == "typename" && a.UnvType.ExtraTypes.Length == 0;
			}
			else
			{
				return false;
			}
		}
		return false;
	}

	public static bool IsNotImplementedNamespace(String @namespace)
	{
		if (NotImplementedNamespacesList.Contains(@namespace))
		{
			return true;
		}
		return false;
	}

	public static bool IsOutdatedNamespace(String @namespace, out String useInstead)
	{
		var index = OutdatedNamespacesList.IndexOfKey(@namespace);
		if (index != -1)
		{
			useInstead = OutdatedNamespacesList.Values[index];
			return true;
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedNamespace(String @namespace)
	{
		if (ReservedNamespacesList.Contains(@namespace))
		{
			return true;
		}
		return false;
	}

	public static bool IsNotImplementedType(String @namespace, String type)
	{
		if (NotImplementedTypesList.Contains((@namespace, type)))
		{
			return true;
		}
		return false;
	}

	public static bool IsOutdatedType(String @namespace, String type, out String useInstead)
	{
		var index = OutdatedTypesList.IndexOfKey((@namespace, type));
		if (index != -1)
		{
			useInstead = OutdatedTypesList.Values[index];
			return true;
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedType(String @namespace, String type)
	{
		if (ReservedTypesList.Contains((@namespace, type)))
		{
			return true;
		}
		return false;
	}

	public static bool IsNotImplementedEndOfIdentifier(String identifier, out String typeEnd)
	{
		foreach (var te in NotImplementedTypeEndsList)
		{
			if (identifier.EndsWith(te))
			{
				typeEnd = te;
				return true;
			}
		}
		typeEnd = [];
		return false;
	}

	public static bool IsOutdatedEndOfIdentifier(String identifier, out String useInstead, out String typeEnd)
	{
		foreach (var te in OutdatedTypeEndsList)
		{
			if (identifier.EndsWith(te.Key))
			{
				useInstead = te.Value;
				typeEnd = te.Key;
				return true;
			}
		}
		useInstead = [];
		typeEnd = [];
		return false;
	}

	public static bool IsReservedEndOfIdentifier(String identifier, out String typeEnd)
	{
		foreach (var te in ReservedTypeEndsList)
		{
			if (identifier.EndsWith(te))
			{
				typeEnd = te;
				return true;
			}
		}
		typeEnd = [];
		return false;
	}

	public static bool IsNotImplementedMember(BlockStack type, String member)
	{
		var index = NotImplementedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			if (NotImplementedMembersList.Values[index].Contains(member))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsOutdatedMember(BlockStack type, String member, out String useInstead)
	{
		var index = OutdatedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			var list = OutdatedMembersList.Values[index];
			var index2 = list.IndexOfKey(member);
			if (index2 != -1)
			{
				useInstead = list.Values[index2];
				return true;
			}
		}
		useInstead = [];
		return false;
	}

	public static bool IsReservedMember(BlockStack type, String member)
	{
		var index = ReservedMembersList.IndexOfKey(type);
		if (index != -1)
		{
			if (ReservedMembersList.Values[index].Contains(member))
			{
				return true;
			}
		}
		return false;
	}

	public static bool PropertyExists(UniversalType container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property)
	{
		if (UserDefinedPropertiesList.TryGetValue(container.MainType, out var list) && list.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			property = null;
			return false;
		}
		if (!type.TryWrap(x => x.GetProperty(name.ToString()), out var prop))
			prop = type.GetProperties().Find(x => x.Name == name.ToString());
		if (prop == null)
		{
			property = null;
			return false;
		}
		property = (TypeMappingBack(prop.PropertyType, type.GetGenericArguments(), container.ExtraTypes), PropertyAttributes.None);
		return true;
	}

	public static bool UserDefinedPropertyExists(BlockStack container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property, out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedPropertiesList.ContainsKey, out matchingContainer))
		{
			var list = UserDefinedPropertiesList[matchingContainer];
			if (list.TryGetValue(name, out var a))
			{
				property = a;
				return true;
			}
		}
		property = null;
		return false;
	}

	public static bool PublicFunctionExists(String name, out (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		if (PublicFunctionsList.TryGetValue(name, out var function2))
		{
			function = function2;
			return true;
		}
		function = null;
		return false;
	}

	public static bool MethodExists(UniversalType container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			function = null;
			return false;
		}
		if (!type.TryWrap(x => x.GetMethod(name.ToString()), out var method))
			method = type.GetMethods().Find(x => x.Name == name.ToString());
		if (method == null)
		{
			function = null;
			return false;
		}
		function = ([], TypeMappingBack(method.ReturnType, type.GetGenericArguments(), container.ExtraTypes),
			(method.IsAbstract ? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
			new(method.GetParameters().ToList(x => new GeneralMethodParameter(CreateVar(TypeMappingBack(x.ParameterType,
			type.GetGenericArguments(), container.ExtraTypes), out var UnvType).MainType,
			x.Name ?? "x", UnvType.ExtraTypes,
			(x.IsOptional ? ParameterAttributes.Optional : 0) | (x.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (x.IsOut ? ParameterAttributes.Out : 0), x.DefaultValue?.ToString() ?? "null"))));
		return true;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function, out bool user)
	{
		if (UserDefinedFunctionsList.TryGetValue(container, out var methods) && methods.TryGetValue(name, out var method_overloads))
		{
			function = method_overloads[0];
			user = true;
			return true;
		}
		var index = GeneralMethodsList.IndexOfKey(container);
		if (index != -1)
		{
			var list = GeneralMethodsList.Values[index];
			var index2 = list.IndexOfKey(name);
			if (index2 != -1)
			{
				function = list.Values[index2][0];
				user = false;
				return true;
			}
		}
		function = null;
		user = false;
		return false;
	}

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function) => UserDefinedFunctionExists(container, name, out function, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters)? function, out BlockStack matchingContainer)
	{
		if (CheckContainer(container, UserDefinedFunctionsList.ContainsKey, out matchingContainer))
		{
			var list = UserDefinedFunctionsList[matchingContainer];
			if (list.TryGetValue(name, out var method_overloads))
			{
				function = method_overloads[0];
				return true;
			}
		}
		function = null;
		return false;
	}

	public static bool ConstructorsExist(UniversalType container, out GeneralConstructorOverloads? constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type)
			|| ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)
			|| containerType.Container.Length == 0
			&& ExplicitlyConnectedNamespacesList.FindIndex(x => ExtraTypesList.TryGetValue((x, containerType.Type), out type)) >= 0))
		{
			constructors = null;
			return false;
		}
		var typeConstructors = type.GetConstructors();
		if (typeConstructors == null)
		{
			constructors = null;
			return false;
		}
		constructors = new(typeConstructors.ToList(x => ((x.IsAbstract ? ConstructorAttributes.Abstract : 0)
		| (x.IsStatic ? ConstructorAttributes.Static : 0), new GeneralMethodParameters(x.GetParameters().ToList(y =>
			new GeneralMethodParameter(CreateVar(TypeMappingBack(y.ParameterType,
			type.GetGenericArguments(), container.ExtraTypes),
			out var UnvType).MainType, y.Name ?? "x", UnvType.ExtraTypes, (y.IsOptional ? ParameterAttributes.Optional : 0)
			| (y.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (y.IsOut ? ParameterAttributes.Out : 0), y.DefaultValue?.ToString() ?? "null"))))));
		return true;
	}

	public static bool UserDefinedConstructorsExist(BlockStack type, out GeneralConstructorOverloads? constructors)
	{
		if (UserDefinedConstructorsList.TryGetValue(type, out var temp_constructors))
		{
			constructors = [.. temp_constructors];
			if (constructors.Length != 0)
			{
				return true;
			}
		}
		constructors = null;
		return false;
	}
}

public sealed class FullTypeEComparer : G.IEqualityComparer<UniversalType>
{
	public bool Equals(UniversalType x, UniversalType y) => new BlockStackEComparer().Equals(x.MainType, y.MainType) && new GeneralExtraTypesEComparer().Equals(x.ExtraTypes, y.ExtraTypes);

	public int GetHashCode(UniversalType x) => new BlockStackEComparer().GetHashCode(x.MainType) ^ new GeneralExtraTypesEComparer().GetHashCode(x.ExtraTypes);
}

public class DoubleConverter : JsonConverter<double>
{
	public override double ReadJson(JsonReader reader, Type objectType, double existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
	public override void WriteJson(JsonWriter writer, double value, JsonSerializer serializer)
	{
		if (value is (double)1 / 0)
		{
			writer.WriteRaw("Infty");
			return;
		}
		if (value is (double)-1 / 0)
		{
			writer.WriteRaw("-Infty");
			return;
		}
		if (value is (double)0 / 0)
		{
			writer.WriteRaw("Uncty");
			return;
		}
		var truncated = unchecked((long)Truncate(value));
		if (truncated == value)
			writer.WriteValue(truncated);
		else
			writer.WriteValue(value);
	}
}

public interface IClass { }

public class IClassConverter : JsonConverter<IClass>
{
	public override IClass? ReadJson(JsonReader reader, Type objectType, IClass? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();
	public override void WriteJson(JsonWriter writer, IClass? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		var type = value.GetType();
		writer.WriteRaw("new " + type.Name + "(");
		var en = type.GetProperties().GetEnumerator();
		if (!en.MoveNext())
		{
			writer.WriteRaw(")");
			return;
		}
		writer.WriteRaw(JsonConvert.SerializeObject(((PropertyInfo)en.Current).GetValue(value), SerializerSettings));
		while (en.MoveNext())
			writer.WriteRaw(", " + JsonConvert.SerializeObject(((PropertyInfo)en.Current).GetValue(value), SerializerSettings));
		writer.WriteRaw(")");
	}
}

public class IEnumerableConverter : JsonConverter<IEnumerable>
{
	public override IEnumerable? ReadJson(JsonReader reader, Type objectType, IEnumerable? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

	public override void WriteJson(JsonWriter writer, IEnumerable? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}
		var en = value.GetEnumerator();
		if (!en.MoveNext())
		{
			writer.WriteRaw("()");
			return;
		}
		writer.WriteRaw("(" + JsonConvert.SerializeObject(en.Current, SerializerSettings));
		while (en.MoveNext())
			writer.WriteRaw(", " + JsonConvert.SerializeObject(en.Current, SerializerSettings));
		writer.WriteRaw(")");
	}
}

public class StringConverter : JsonConverter<String>
{
	public override String? ReadJson(JsonReader reader, Type objectType, String? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

	public override void WriteJson(JsonWriter writer, String? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}
		if (value.GetAfter('\"').Contains('\"') && value.TryTakeIntoRawQuotes(out var rawString))
			writer.WriteRaw(rawString.ToString());
		else if (!value.GetAfter('\\').Contains('\\'))
			writer.WriteRaw(value.TakeIntoQuotes().ToString());
		else
			writer.WriteRaw(value.TakeIntoVerbatimQuotes().ToString());
	}
}

public class TupleConverter : JsonConverter<ITuple>
{
	public override ITuple? ReadJson(JsonReader reader, Type objectType, ITuple? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

	public override void WriteJson(JsonWriter writer, ITuple? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}
		writer.WriteRaw("(" + string.Join(", ", new Chain(value.Length).ToArray(index => JsonConvert.SerializeObject(value[index], SerializerSettings))) + ")");
	}
}

public class UniversalConverter : JsonConverter<Universal>
{
	public override Universal ReadJson(JsonReader reader, Type objectType, Universal existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();
	public override void WriteJson(JsonWriter writer, Universal value, JsonSerializer serializer) => writer.WriteRaw(value.ToString(true).ToString());
}

public class ValueTypeConverter : JsonConverter<ValueType>
{
	public override ValueType? ReadJson(JsonReader reader, Type objectType, ValueType? existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotSupportedException();

	public override void WriteJson(JsonWriter writer, ValueType? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}
		var type = value.GetType();
		var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		var joined = string.Join(", ", fields.ToArray(x => x.DeclaringType == typeof(bool) ? value.ToString()?.ToLower() : x.FieldType == type ? value.ToString() : JsonConvert.SerializeObject(x.GetValue(value), SerializerSettings)));
		writer.WriteRaw(fields.Length == 1 ? joined : "(" + joined + ")");
	}
}

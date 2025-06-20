global using Corlib.NStar;
global using Dictionaries.NStar;
global using LINQ.NStar;
global using System;
global using System.Diagnostics;
global using System.Drawing;
global using System.Reflection;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.DeclaredConstructions;
global using static System.Math;
global using String = Corlib.NStar.String;
using ParallelHS.NStar;
using SortedSets.NStar;
using System.Text;
using TreeSets.NStar;

namespace CSharp.NStar;

public static class TypeHelpers
{
	public static readonly Random globalRandom = new();
	public static readonly String[] operators = ["or", "and", "^^", "||", "&&", "==", "!=", ">=", "<=", ">", "<", "^=", "|=", "&=", ">>=", "<<=", "+=", "-=", "*=", "/=", "%=", "pow=", "=", "^", "|", "&", ">>", "<<", "+", "-", "*", "/", "%", "pow", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "!", "~", "++", "--", "!!"];
	public static readonly List<String> CollectionTypesList = [nameof(Dictionary<bool, bool>), nameof(FastDelHashSet<bool>), "HashTable", nameof(Corlib.NStar.ICollection), nameof(G.IEnumerable<bool>), nameof(Corlib.NStar.IList), nameof(IReadOnlyCollection<bool>), nameof(IReadOnlyList<bool>), nameof(LimitedQueue<bool>), nameof(G.LinkedList<bool>), nameof(G.LinkedListNode<bool>), nameof(ListHashSet<bool>), nameof(Mirror<bool, bool>), nameof(NList<bool>), nameof(Queue<bool>), nameof(ParallelHashSet<bool>), nameof(ReadOnlySpan<bool>), nameof(Slice<bool>), nameof(SortedDictionary<bool, bool>), nameof(SortedSet<bool>), nameof(Span<bool>), nameof(Stack<bool>), nameof(TreeHashSet<bool>), nameof(TreeSet<bool>)];

	private static readonly Dictionary<Type, bool> memoizedTypes = [];

	public static bool IsUnmanaged(this Type netType)
	{
		if (!memoizedTypes.TryGetValue(netType, out var answer))
		{
			if (!netType.IsValueType)
				answer = false;
			else if (netType.IsPrimitive || netType.IsPointer || netType.IsEnum)
				answer = true;
			else
				answer = netType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).All(f => IsUnmanaged(f.FieldType));
			memoizedTypes[netType] = answer;
		}
		return answer;
	}

	public static UniversalType GetSubtype(UniversalType type, int levels = 1)
	{
		if (levels <= 0)
			return type;
		else if (levels == 1)
		{
			if (TypeIsPrimitive(type.MainType))
			{
				if (type.MainType.Peek().Name == "list")
					return GetListSubtype(type);
				else
					return NullType;
			}
			else
				return NullType;
		}
		else
		{
			var t = type;
			for (var i = 0; i < levels; i++)
				t = GetSubtype(t);
			return t;
		}
	}

	private static UniversalType GetListSubtype(UniversalType type)
	{
		if (type.ExtraTypes.Length == 1)
			return (type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes);
		else if (!(type.ExtraTypes[0].MainType.IsValue && int.TryParse(type.ExtraTypes[0].MainType.Value.ToString(), out var n)))
			return NullType;
		else if (n <= 2)
			return GetListType(type.ExtraTypes[1]);
		else
			return (ListBlockStack, new GeneralExtraTypes { ((TypeOrValue)(n - 1).ToString(), NoGeneralExtraTypes), type.ExtraTypes[1] });
	}

	public static (int Depth, UniversalType LeafType) GetTypeDepthAndLeafType(UniversalType type)
	{
		var Depth = 0;
		var LeafType = type;
		while (true)
		{
			if (TypeEqualsToPrimitive(LeafType, "list", false))
			{
				if (LeafType.ExtraTypes.Length == 1)
				{
					Depth++;
					LeafType = (LeafType.ExtraTypes[0].MainType.Type, LeafType.ExtraTypes[0].ExtraTypes);
				}
				else if (LeafType.ExtraTypes[0].MainType.IsValue && int.TryParse(LeafType.ExtraTypes[0].MainType.Value.ToString(), out var n))
				{
					Depth += n;
					LeafType = (LeafType.ExtraTypes[1].MainType.Type, LeafType.ExtraTypes[1].ExtraTypes);
				}
				else
				{
					Depth++;
					LeafType = (LeafType.ExtraTypes[1].MainType.Type, LeafType.ExtraTypes[1].ExtraTypes);
				}
			}
			else if (LeafType.MainType.Length != 0 && LeafType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface && CollectionTypesList.Contains(LeafType.MainType.ToShortString().ToNString().GetAfterLast(".")))
			{
				Depth++;
				LeafType = (LeafType.ExtraTypes[^1].MainType.Type, LeafType.ExtraTypes[^1].ExtraTypes);
			}
			else
				return (Depth, LeafType);
		}
	}

	public static UniversalType GetResultType(UniversalType type1, UniversalType type2)
	{
		try
		{
			if (TypesAreEqual(type1, type2))
				return type1;
			if (TypeIsPrimitive(type1.MainType) && TypeIsPrimitive(type2.MainType))
			{
				var left_type = type1.MainType.Peek().Name;
				var right_type = type2.MainType.Peek().Name;
				if (type1.ExtraTypes.Length == 0 && type2.ExtraTypes.Length == 0)
					return GetPrimitiveType(GetPrimitiveResultType(left_type, right_type));
				else if (left_type == "list" || right_type == "list")
					return GetListResultType(type1, type2, left_type, right_type);
				else
					return NullType;
			}
			else
				return NullType;
		}
		catch (StackOverflowException)
		{
			return NullType;
		}
	}

	private static String GetPrimitiveResultType(String left_type, String right_type)
	{
		if (left_type == "dynamic" || right_type == "dynamic")
			return "dynamic";
		else if (left_type == "string" || right_type == "string")
			return "string";
		else if (left_type == "long complex" || right_type == "long complex")
			return "long complex";
		else if (left_type == "long real" || right_type == "long real")
			return "long real";
		else if (left_type == "long long" || right_type == "long long")
		{
			if (left_type == "complex" || right_type == "complex")
				return "long complex";
			else if (left_type == "real" || right_type == "real")
				return "long real";
			else
				return "long long";
		}
		else if (left_type == "unsigned long long" || right_type == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(right_type))
				return "long long";
			else
				return "unsigned long long";
		}
		else if (left_type == "complex" || right_type == "complex")
			return "complex";
		else if (left_type == "real" || right_type == "real")
			return "real";
		else if (left_type == "unsigned long int" || right_type == "unsigned long int")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(right_type))
				return "long long";
			else
				return "unsigned long int";
		}
		else if (left_type == "TimeSpan" || right_type == "TimeSpan")
			return "TimeSpan";
		else if (left_type == "DateTime" || right_type == "DateTime")
			return "DateTime";
		else if (left_type == "long int" || right_type == "long int")
			return "long int";
		else if (left_type == "long char" || right_type == "long char")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
				return "long int";
			else
				return "long char";
		}
		else if (left_type == "unsigned int" || right_type == "unsigned int")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
				return "long int";
			else
				return "unsigned int";
		}
		else if (left_type == "int" || right_type == "int")
			return "int";
		else if (left_type == "char" || right_type == "char")
		{
			if (left_type == "short int" || right_type == "short int")
				return "int";
			else
				return "char";
		}
		else if (left_type == "unsigned short int" || right_type == "unsigned short int")
		{
			if (left_type == "short int" || right_type == "short int")
				return "int";
			else
				return "unsigned short int";
		}
		else if (left_type == "short int" || right_type == "short int")
			return "short int";
		else if (left_type == "short char" || right_type == "short char")
			return "short char";
		else if (left_type == "byte" || right_type == "byte")
			return "byte";
		else if (left_type == "bool" || right_type == "bool")
			return "bool";
		else if (left_type == "BaseClass" || right_type == "BaseClass")
			return "BaseClass";
		else
			return "null";
	}

	private static UniversalType GetListResultType(UniversalType type1, UniversalType type2, String left_type, String right_type)
	{
		if (CollectionTypesList.Contains(left_type) || CollectionTypesList.Contains(right_type))
			return GetListType(GetResultType(GetSubtype(type1), GetSubtype(type2)));
		else if (left_type == "list")
			return GetListType(GetResultType(GetSubtype(type1), (right_type == "list") ? GetSubtype(type2) : type2));
		else
			return GetListType(GetResultType(type1, GetSubtype(type2)));
	}

	public static UniversalType PartialTypeToGeneralType(String mainType, List<String> extraTypes) => (GetBlockStack(mainType), GetGeneralExtraTypes(extraTypes));

	public static GeneralExtraTypes GetGeneralExtraTypes(List<String> partialBlockStack) => new(partialBlockStack.Convert(x => (UniversalTypeOrValue)((TypeOrValue)new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoGeneralExtraTypes)));

	public static (BlockStack Container, String Type) SplitType(BlockStack blockStack) => (new(blockStack.ToList().SkipLast(1)), blockStack.TryPeek(out var block) ? block.Name : []);

	public static bool TypesAreCompatible(UniversalType sourceType, UniversalType destinationType, out bool warning, String? srcExpr, out String? destExpr, out String? extraMessage)
	{
		warning = false;
		extraMessage = null;
		while (TypeEqualsToPrimitive(sourceType, "tuple", false) && sourceType.ExtraTypes.Length == 1)
			sourceType = (sourceType.ExtraTypes[0].MainType.Type, sourceType.ExtraTypes[0].ExtraTypes);
		while (TypeEqualsToPrimitive(destinationType, "tuple", false) && destinationType.ExtraTypes.Length == 1)
			destinationType = (destinationType.ExtraTypes[0].MainType.Type, destinationType.ExtraTypes[0].ExtraTypes);
		if (TypesAreEqual(sourceType, destinationType))
		{
			destExpr = srcExpr;
			return true;
		}
		if (TypeEqualsToPrimitive(sourceType, "null", false))
		{
			destExpr = "default!";
			return true;
		}
		if (ImplicitConversionsFromAnythingList.Contains(destinationType, new FullTypeEComparer()))
		{
			if (srcExpr == null)
				destExpr = null;
			else if (TypeEqualsToPrimitive(destinationType, "string"))
				destExpr = ((String)"(").AddRange(srcExpr).AddRange(").ToString()");
			else if (TypeEqualsToPrimitive(destinationType, "list", false))
				destExpr = ((String)"ListWithSingle(").AddRange(srcExpr).Add(')');
			else
				destExpr = srcExpr;
			return true;
		}
		if (TypeEqualsToPrimitive(destinationType, "tuple", false))
		{
			if (!TypeEqualsToPrimitive(sourceType, "tuple", false))
			{
				destExpr = "default!";
				return false;
			}
			if (sourceType.ExtraTypes.Length != destinationType.ExtraTypes.Length)
			{
				destExpr = "default!";
				return false;
			}
			destExpr = srcExpr;
			return sourceType.ExtraTypes.Values.Combine(destinationType.ExtraTypes.Values).All(x => TypesAreCompatible((x.Item1.MainType.Type, x.Item1.ExtraTypes), (x.Item2.MainType.Type, x.Item2.ExtraTypes), out var warning2, null, out _, out _) && !warning2);
		}
		if (TypeEqualsToPrimitive(destinationType, "list", false) || destinationType.MainType.Length != 0
			&& destinationType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
			&& CollectionTypesList.Contains(destinationType.MainType.ToShortString().ToNString().GetAfterLast(".")))
		{
			if (TypeEqualsToPrimitive(sourceType, "tuple", false))
			{
				var subtype = GetSubtype(destinationType);
				if (sourceType.ExtraTypes.Length > 16)
				{
					destExpr = "default!";
					extraMessage = "list can be constructed from tuple of up to 16 elements, if you need more, use the other ways like Chain() or Fill()";
					return false;
				}
				else if (!sourceType.ExtraTypes.All(x => TypesAreCompatible((x.Value.MainType.Type, x.Value.ExtraTypes), subtype, out var warning2, null, out _, out _) && !warning2))
				{
					destExpr = "default!";
					return false;
				}
				else
				{
					destExpr = srcExpr;
					return true;
				}
			}
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			if (SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, "string"))
			{
				destExpr = srcExpr == null ? null : DestinationDepth == 0 ? ((String)"(").AddRange(srcExpr).AddRange(").ToString()") : srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth && TypesAreCompatible(SourceLeafType, DestinationLeafType, out warning, null, out _, out _) && !warning)
			{
				if (srcExpr == null)
					destExpr = null;
				else
				{
					srcExpr.Insert(0, ((String)nameof(TypeHelpers)).Add('.').AddRange(nameof(ListWithSingle)).Add('(').Repeat(DestinationDepth - SourceDepth));
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				return true;
			}
			else
			{
				destExpr = "default!";
				return false;
			}
		}
		if (new BlockStackEComparer().Equals(sourceType.MainType, FuncBlockStack) && new BlockStackEComparer().Equals(destinationType.MainType, FuncBlockStack))
		{
			destExpr = srcExpr;
			try
			{
				var warning2 = false;
				if (!(sourceType.ExtraTypes.Length >= destinationType.ExtraTypes.Length
					&& destinationType.ExtraTypes.Length >= 1 && !sourceType.ExtraTypes[0].MainType.IsValue
					&& !destinationType.ExtraTypes[0].MainType.IsValue
					&& TypesAreCompatible((sourceType.ExtraTypes[0].MainType.Type, sourceType.ExtraTypes[0].ExtraTypes),
					(destinationType.ExtraTypes[0].MainType.Type, destinationType.ExtraTypes[0].ExtraTypes),
					out warning, null, out _, out _)))
					return false;
				if (destinationType.ExtraTypes.Skip(1).Combine(sourceType.ExtraTypes.Skip(1), (x, y) =>
				{
					var warning3 = false;
					var b = !x.Value.MainType.IsValue && !y.Value.MainType.IsValue && TypesAreCompatible((x.Value.MainType.Type, x.Value.ExtraTypes), (y.Value.MainType.Type, y.Value.ExtraTypes), out warning3, null, out _, out _);
					warning2 |= warning3;
					return b;
				}).All(x => x))
				{
					warning |= warning2;
					return true;
				}
				else
					return false;
			}
			catch (StackOverflowException)
			{
				return false;
			}
		}
		if (destinationType.MainType.ToShortString() is "System." + nameof(ReadOnlySpan<bool>) or "System." + nameof(Span<bool>))
		{
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			if (SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, "string"))
			{
				destExpr = srcExpr == null ? null : DestinationDepth == 0 ? ((String)"(").AddRange(srcExpr).AddRange(").ToString()") : srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth && TypesAreCompatible(SourceLeafType, DestinationLeafType, out warning, null, out _, out _) && !warning)
			{
				destExpr = srcExpr ?? null;
				return true;
			}
			else
			{
				destExpr = "default!";
				return false;
			}
		}
		if (UserDefinedTypesList.TryGetValue(SplitType(sourceType.MainType), out var userDefinedType)
			&& userDefinedType.BaseType != NullType && TypesAreCompatible(userDefinedType.BaseType, destinationType,
			out warning, srcExpr, out destExpr, out extraMessage))
			return true;
		var index = ImplicitConversionsList.IndexOfKey(sourceType.MainType);
		if (index == -1)
		{
			destExpr = "default!";
			return false;
		}
		if (!ImplicitConversionsList.Values[index].TryGetValue(sourceType.ExtraTypes, out var list2))
		{
			destExpr = "default!";
			return false;
		}
		var index2 = list2.FindIndex(x => TypesAreEqual(x.DestType, destinationType));
		if (index2 != -1)
		{
			warning = list2[index2].Warning;
			destExpr = srcExpr == null ? null : !warning ? srcExpr : AdaptTerminalType(srcExpr, sourceType, destinationType);
			return true;
		}
		List<(UniversalType Type, bool Warning)> types_list = [(sourceType, false)];
		List<(UniversalType Type, bool Warning)> new_types_list = [(sourceType, false)];
		while (true)
		{
			List<(UniversalType Type, bool Warning)> new_types2_list = new(16);
			for (var i = 0; i < new_types_list.Length; i++)
			{
				var new_types3_list = GetCompatibleTypes(new_types_list[i], types_list);
				index2 = new_types3_list.FindIndex(x => TypesAreEqual(x.Type, destinationType));
				if (index2 != -1)
				{
					warning = new_types3_list[index2].Warning;
					destExpr = srcExpr == null ? null : !warning ? srcExpr : AdaptTerminalType(srcExpr, sourceType, destinationType);
					return true;
				}
				new_types2_list.AddRange(new_types3_list);
			}
			new_types_list = [.. new_types2_list];
			types_list.AddRange(new_types2_list);
			if (new_types2_list.Length == 0)
				break;
		}
		destExpr = null;
		return false;
	}

	private static String AdaptTerminalType(String source, UniversalType srcType, UniversalType destType)
	{
		Debug.Assert(TypeIsPrimitive(srcType.MainType));
		Debug.Assert(TypeIsPrimitive(destType.MainType));
		var srcType2 = srcType.MainType.Peek().Name.ToString();
		var destType2 = destType.MainType.Peek().Name.ToString();
		Debug.Assert(destType2 != "string");
		var destTypeconverter = destType2 switch
		{
			"null" => "void",
			"short char" => "byte",
			"short int" => "short",
			"unsigned short int" => "ushort",
			"unsigned int" => "uint",
			"long char" => "(char, char)",
			"long int" => "long",
			"unsigned long int" => "ulong",
			"real" => "double",
			"string" => nameof(String),
			"typename" => "Type",
			"universal" => "object",
			_ => destType2,
		};
		if (srcType2 == "string")
		{
			Debug.Assert(destType2 != "string");
			if (destType2 is "bool" or "byte" or "char" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" or "double")
			{
				var result = ((String)"(").AddRange(destTypeconverter).Add('.').AddRange(nameof(int.TryParse)).Add('(');
				var varName = RedStarLinq.NFill(32, _ =>
					(char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
				result.AddRange(source).AddRange(", out var ").AddRange(varName).AddRange(") ? ").AddRange(varName);
				return result.AddRange(" : ").AddRange(destType2 == "bool" ? "false)" : "0)");
			}
			else
				return ((String)"(").AddRange(destTypeconverter).AddRange(")(").AddRange(source).Add(')');
		}
		else if (destType2 == "bool")
		{
			Debug.Assert(srcType2 != "bool");
			return ((String)"(").AddRange(source).AddRange(") >= 1");
		}
		else if (srcType2 == "real")
		{
			Debug.Assert(destType2 != "real");
			return ((String)"(").AddRange(destTypeconverter).Add(')').AddRange(nameof(Truncate)).Add('(').AddRange(source).Add(')');
		}
		else
			return ((String)"unchecked((").AddRange(destTypeconverter).AddRange(")(").AddRange(source).AddRange("))");
	}

	private static List<(UniversalType Type, bool Warning)> GetCompatibleTypes((UniversalType Type, bool Warning) source, List<(UniversalType Type, bool Warning)> blackList)
	{
		List<(UniversalType Type, bool Warning)> list = new(16);
		list.AddRange(ImplicitConversionsFromAnythingList.Convert(x => (x, source.Warning)).Filter(x => !blackList.Contains(x)));
		var index = ImplicitConversionsList.IndexOfKey(source.Type.MainType);
		if (index != -1)
		{
			var list2 = ImplicitConversionsList.Values[index];
			if (list2.TryGetValue(source.Type.ExtraTypes, out var list3))
				list.AddRange(list3.Convert(x => (x.DestType, x.Warning || source.Warning)).Filter(x => !blackList.Contains(x)));
		}
		return list;
	}

	public static dynamic ListWithSingle<T>(T item)
	{
		if (item is bool b)
			return new BitList([b]);
		else if (typeof(T).IsUnmanaged())
			return typeof(NList<>).MakeGenericType(typeof(T)).GetConstructor([typeof(G.IEnumerable<T>)])
				?.Invoke([(G.IEnumerable<T>)[item]]) ?? throw new InvalidOperationException();
		else
			return new List<T>(item);
	}

	private sealed class FullTypeEComparer : G.IEqualityComparer<UniversalType>
	{
		public bool Equals(UniversalType x, UniversalType y) => new BlockStackEComparer().Equals(x.MainType, y.MainType) && new GeneralExtraTypesEComparer().Equals(x.ExtraTypes, y.ExtraTypes);

		public int GetHashCode(UniversalType x) => new BlockStackEComparer().GetHashCode(x.MainType) ^ new GeneralExtraTypesEComparer().GetHashCode(x.ExtraTypes);
	}
}

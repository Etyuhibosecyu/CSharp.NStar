global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using System.Reflection;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using NStar.ParallelHS;
using NStar.SortedSets;
using NStar.TreeSets;
using System.Text;

namespace CSharp.NStar;

public static class TypeConverters
{
	public static readonly Random globalRandom = new();
	public static readonly String[] operators = ["or", "and", "^^", "||", "&&", "==", "!=", ">=", "<=", ">", "<", "^=", "|=", "&=", ">>=", "<<=", "+=", "-=", "*=", "/=", "%=", "pow=", "=", "^", "|", "&", ">>", "<<", "+", "-", "*", "/", "%", "pow", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "!", "~", "++", "--", "!!"];
	public static readonly List<String> CollectionTypesList = [nameof(Buffer), nameof(Dictionary<bool, bool>), nameof(FastDelHashSet<bool>), "HashTable", nameof(ICollection), nameof(G.IEnumerable<bool>), nameof(IList), nameof(IReadOnlyCollection<bool>), nameof(IReadOnlyList<bool>), nameof(LimitedQueue<bool>), nameof(G.LinkedList<bool>), nameof(G.LinkedListNode<bool>), nameof(ListHashSet<bool>), nameof(Mirror<bool, bool>), nameof(NList<bool>), nameof(Queue<bool>), nameof(ParallelHashSet<bool>), nameof(ReadOnlySpan<bool>), nameof(Slice<bool>), nameof(SortedDictionary<bool, bool>), nameof(SortedSet<bool>), nameof(Span<bool>), nameof(Stack<bool>), nameof(TreeHashSet<bool>), nameof(TreeSet<bool>)];

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
				answer = netType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.All(f => IsUnmanaged(f.FieldType));
			memoizedTypes[netType] = answer;
		}
		return answer;
	}

	public static NStarType GetSubtype(NStarType type, int levels = 1)
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
			if (type.ExtraTypes.Length == 1 && TypesAreCompatible(type, new(IEnumerableBlockStack, type.ExtraTypes),
				out var warning, null, out _, out _) && !warning
				&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType Subtype)
				return Subtype;
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

	private static NStarType GetListSubtype(NStarType type)
	{
		if (type.ExtraTypes.Length == 1
				&& type.ExtraTypes[0].Name == "type" && type.ExtraTypes[0].Extra is NStarType Subtype)
			return Subtype;
		else if (!(type.ExtraTypes[0].Name != "type" && int.TryParse(type.ExtraTypes[0].Name.ToString(), out var n)))
			return NullType;
		else if (n <= 1 && type.ExtraTypes[1].Name == "type" && type.ExtraTypes[1].Extra is NStarType Subtype2)
			return Subtype2;
		else if (n == 2)
			return GetListType(type.ExtraTypes[1]);
		else
			return (ListBlockStack, new BranchCollection { new((n - 1).ToString(), 0, []), type.ExtraTypes[1] });
	}

	public static (int Depth, NStarType LeafType) GetTypeDepthAndLeafType(NStarType type)
	{
		var Depth = 0;
		var LeafType = type;
		while (true)
		{
			if (TypeEqualsToPrimitive(LeafType, "list", false))
			{
				if (LeafType.ExtraTypes.Length == 1
					&& LeafType.ExtraTypes[0].Name == "type" && LeafType.ExtraTypes[0].Extra is NStarType Subtype)
				{
					Depth++;
					LeafType = Subtype;
				}
				else if (LeafType.ExtraTypes[0].Name != "type"
					&& int.TryParse(LeafType.ExtraTypes[0].Name.ToString(), out var n)
					&& LeafType.ExtraTypes[1].Name == "type" && LeafType.ExtraTypes[1].Extra is NStarType Subtype2)
				{
					Depth += n;
					LeafType = Subtype2;
				}
				else if (LeafType.ExtraTypes[1].Name == "type" && LeafType.ExtraTypes[1].Extra is NStarType Subtype3)
				{
					Depth++;
					LeafType = Subtype3;
				}
				else
					return (Depth, LeafType);
			}
			else if (LeafType.MainType.Length != 0
				&& LeafType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
				&& CollectionTypesList.Contains(LeafType.MainType.ToShortString().ToNString().GetAfterLast("."))
					&& LeafType.ExtraTypes[^1].Name == "type" && LeafType.ExtraTypes[^1].Extra is NStarType Subtype)
			{
				Depth++;
				LeafType = Subtype;
			}
			else
				return (Depth, LeafType);
		}
	}

	public static NStarType GetResultType(NStarType leftType, NStarType rightType, String leftValue, String rightValue)
	{
		try
		{
			if (TypesAreEqual(leftType, rightType))
				return leftType;
			if (TypeIsPrimitive(leftType.MainType) && TypeIsPrimitive(rightType.MainType))
			{
				var leftTypeName = leftType.MainType.Peek().Name;
				var rightTypeName = rightType.MainType.Peek().Name;
				if (leftType.ExtraTypes.Length == 0 && rightType.ExtraTypes.Length == 0)
					return GetPrimitiveType(GetPrimitiveResultType(leftTypeName, rightTypeName, leftValue, rightValue));
				else if (leftTypeName == "list" || rightTypeName == "list")
					return GetListResultType(leftType, rightType, leftTypeName, rightTypeName, leftValue, rightValue);
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

	private static String GetPrimitiveResultType(String leftTypeName, String rightTypeName, String leftValue, String rightValue)
	{
		if (leftTypeName == "bool" && rightTypeName.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex")
			leftValue.Insert(0, '(').AddRange(" ? 1 : 0)");
		else if (rightTypeName == "bool" && leftTypeName.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex")
			rightValue.Insert(0, '(').AddRange(" ? 1 : 0)");
		if (leftTypeName == "dynamic" || rightTypeName == "dynamic")
			return "dynamic";
		else if (leftTypeName == "string" || rightTypeName == "string")
			return "string";
		else if (leftTypeName == "long complex" || rightTypeName == "long complex")
			return "long complex";
		else if (leftTypeName == "long real" || rightTypeName == "long real")
			return "long real";
		else if (leftTypeName == "long long" || rightTypeName == "long long")
		{
			if (leftTypeName == "complex" || rightTypeName == "complex")
				return "long complex";
			else if (leftTypeName == "real" || rightTypeName == "real")
				return "long real";
			else
				return "long long";
		}
		else if (leftTypeName == "unsigned long long" || rightTypeName == "unsigned long long")
		{
			if (leftTypeName.ToString() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan" or "real" or "complex"
				|| rightTypeName.ToString() is "short int" or "int" or "long int"
				or "DateTime" or "TimeSpan" or "real" or "complex")
				return "long long";
			else
				return "unsigned long long";
		}
		else if (leftTypeName == "complex" || rightTypeName == "complex")
			return "complex";
		else if (leftTypeName == "real" || rightTypeName == "real")
			return "real";
		else if (leftTypeName == "unsigned long int" || rightTypeName == "unsigned long int")
		{
			if (leftTypeName.ToString() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan"
				|| rightTypeName.ToString() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan")
				return "long long";
			else
				return "unsigned long int";
		}
		else if (leftTypeName == "TimeSpan" || rightTypeName == "TimeSpan")
			return "TimeSpan";
		else if (leftTypeName == "DateTime" || rightTypeName == "DateTime")
			return "DateTime";
		else if (leftTypeName == "long int" || rightTypeName == "long int")
			return "long int";
		else if (leftTypeName == "long char" || rightTypeName == "long char")
		{
			if (leftTypeName == "short int" || rightTypeName == "short int" || leftTypeName == "int" || rightTypeName == "int")
				return "long int";
			else
				return "long char";
		}
		else if (leftTypeName == "unsigned int" || rightTypeName == "unsigned int")
		{
			if (leftTypeName == "short int" || rightTypeName == "short int" || leftTypeName == "int" || rightTypeName == "int")
				return "long int";
			else
				return "unsigned int";
		}
		else if (leftTypeName == "int" || rightTypeName == "int")
			return "int";
		else if (leftTypeName == "char" || rightTypeName == "char")
		{
			if (leftTypeName == "short int" || rightTypeName == "short int")
				return "int";
			else
				return "char";
		}
		else if (leftTypeName == "unsigned short int" || rightTypeName == "unsigned short int")
		{
			if (leftTypeName == "short int" || rightTypeName == "short int")
				return "int";
			else
				return "unsigned short int";
		}
		else if (leftTypeName == "short int" || rightTypeName == "short int")
			return "short int";
		else if (leftTypeName == "short char" || rightTypeName == "short char")
			return "short char";
		else if (leftTypeName == "byte" || rightTypeName == "byte")
			return "byte";
		else if (leftTypeName == "bool" || rightTypeName == "bool")
			return "bool";
		else if (leftTypeName == "BaseClass" || rightTypeName == "BaseClass")
			return "BaseClass";
		else
			return "null";
	}

	private static NStarType GetListResultType(NStarType leftType, NStarType rightType,
		String leftTypeString, String rightTypeString, String leftValue, String rightValue)
	{
		if (CollectionTypesList.Contains(leftTypeString) || CollectionTypesList.Contains(rightTypeString))
			return GetListType(GetResultType(GetSubtype(leftType), GetSubtype(rightType), leftValue, rightValue));
		else if (leftTypeString == "list")
			return GetListType(GetResultType(GetSubtype(leftType), (rightTypeString == "list")
				? GetSubtype(rightType) : rightType, leftValue, rightValue));
		else
			return GetListType(GetResultType(leftType, GetSubtype(rightType), leftValue, rightValue));
	}

	public static NStarType PartialTypeToGeneralType(String mainType, List<String> extraTypes) =>
		(GetBlockStack(mainType), GetBranchCollection(extraTypes));

	public static BranchCollection GetBranchCollection(List<String> partialBlockStack) =>
		new(partialBlockStack.Convert(x => new TreeBranch("type", 0, []) { Extra
			= new NStarType(new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoBranches) }));

	public static (BlockStack Container, String Type) SplitType(BlockStack blockStack) =>
		(new(blockStack.ToList().SkipLast(1)), blockStack.TryPeek(out var block) ? block.Name : []);

	public static bool TypesAreCompatible(NStarType sourceType, NStarType destinationType,
		out bool warning, String? srcExpr, out String? destExpr, out String? extraMessage)
	{
		warning = false;
		extraMessage = null;
		while (TypeEqualsToPrimitive(sourceType, "tuple", false) && sourceType.ExtraTypes.Length == 1
			&& sourceType.ExtraTypes[0].Name == "type" && sourceType.ExtraTypes[0].Extra is NStarType SourceSubtype)
			sourceType = SourceSubtype;
		while (TypeEqualsToPrimitive(destinationType, "tuple", false) && destinationType.ExtraTypes.Length == 1
			&& destinationType.ExtraTypes[0].Name == "type"
			&& destinationType.ExtraTypes[0].Extra is NStarType DestinationSubtype)
			destinationType = DestinationSubtype;
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
			return sourceType.ExtraTypes.Values.Combine(destinationType.ExtraTypes.Values).All(x =>
			x.Item1.Name == "type" && x.Item1.Extra is NStarType LeftType
			&& x.Item2.Name == "type" && x.Item2.Extra is NStarType RightType
			&& TypesAreCompatible(LeftType, RightType, out var innerWarning, null, out _, out _) && !innerWarning);
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
				else if (!sourceType.ExtraTypes.All(x => x.Value.Name == "type" && x.Value.Extra is NStarType ValueType
					&& TypesAreCompatible(ValueType, subtype, out var innerWarning, null, out _, out _) && !innerWarning))
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
				destExpr = srcExpr == null ? null : DestinationDepth == 0
					? ((String)"(").AddRange(srcExpr).AddRange(").ToString()") : srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth
				&& TypesAreCompatible(SourceLeafType, DestinationLeafType, out warning, null, out _, out _) && !warning)
			{
				if (srcExpr == null)
					destExpr = null;
				else
				{
					srcExpr.Insert(0, ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(').Repeat(DestinationDepth - SourceDepth));
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				return true;
			}
			else if (SourceDepth <= DestinationDepth + 1
				&& TypesAreEqual(SourceLeafType, StringType) && TypesAreEqual(DestinationLeafType, CharType))
			{
				if (srcExpr == null)
					destExpr = null;
				else
				{
					srcExpr.Insert(0, ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(').Repeat(DestinationDepth - SourceDepth - 1));
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth - 1));
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
		if (sourceType.MainType.Equals(FuncBlockStack) && destinationType.MainType.Equals(FuncBlockStack))
		{
			destExpr = srcExpr;
			try
			{
				var warning2 = false;
				if (!(sourceType.ExtraTypes.Length >= destinationType.ExtraTypes.Length
					&& destinationType.ExtraTypes.Length >= 1
					&& sourceType.ExtraTypes[0].Name == "type" && sourceType.ExtraTypes[0].Extra is NStarType SourceSubtype
					&& destinationType.ExtraTypes[0].Name == "type"
					&& destinationType.ExtraTypes[0].Extra is NStarType DestinationSubtype
					&& TypesAreCompatible(SourceSubtype, DestinationSubtype,
					out warning, null, out _, out _)))
					return false;
				if (destinationType.ExtraTypes.Skip(1).Combine(sourceType.ExtraTypes.Skip(1), (x, y) =>
				{
					var warning3 = false;
					var b = x.Value.Name == "type" && x.Value.Extra is NStarType LeftType
					&& y.Value.Name == "type" && y.Value.Extra is NStarType RightType
					&& TypesAreCompatible(LeftType, RightType, out warning3, null, out _, out _);
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
		if (destinationType.MainType.ToShortString() is "System."
			+ nameof(ReadOnlySpan<bool>) or "System." + nameof(Span<bool>))
		{
			var (SourceDepth, SourceLeafType) = GetTypeDepthAndLeafType(sourceType);
			var (DestinationDepth, DestinationLeafType) = GetTypeDepthAndLeafType(destinationType);
			if (SourceDepth >= DestinationDepth && TypeEqualsToPrimitive(DestinationLeafType, "string"))
			{
				destExpr = srcExpr == null ? null : DestinationDepth == 0
					? ((String)"(").AddRange(srcExpr).AddRange(").ToString()") : srcExpr;
				return true;
			}
			else if (SourceDepth <= DestinationDepth && TypesAreCompatible(SourceLeafType, DestinationLeafType,
				out warning, null, out _, out _) && !warning)
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
		if (!ImplicitConversionsList.TryGetValue(sourceType.MainType, out var containerConversions))
		{
			destExpr = "default!";
			return false;
		}
		if (!containerConversions.TryGetValue(sourceType.ExtraTypes, out var typeConversions))
		{
			destExpr = "default!";
			return false;
		}
		var foundIndex = typeConversions.FindIndex(x => TypesAreEqual(x.DestType, destinationType));
		if (foundIndex != -1)
		{
			warning = typeConversions[foundIndex].Warning;
			destExpr = srcExpr == null ? null : !warning ? srcExpr : AdaptTerminalType(srcExpr, sourceType, destinationType);
			return true;
		}
		List<(NStarType Type, bool Warning)> types_list = [(sourceType, false)];
		List<(NStarType Type, bool Warning)> new_types_list = [(sourceType, false)];
		while (true)
		{
			List<(NStarType Type, bool Warning)> new_types2_list = new(16);
			for (var i = 0; i < new_types_list.Length; i++)
			{
				var new_types3_list = GetCompatibleTypes(new_types_list[i], types_list);
				foundIndex = new_types3_list.FindIndex(x => TypesAreEqual(x.Type, destinationType));
				if (foundIndex != -1)
				{
					warning = new_types3_list[foundIndex].Warning;
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

	private static String AdaptTerminalType(String source, NStarType srcType, NStarType destType)
	{
		Debug.Assert(TypeIsPrimitive(srcType.MainType));
		Debug.Assert(TypeIsPrimitive(destType.MainType));
		var srcTypeBlockName = srcType.MainType.Peek().Name.ToString();
		var destTypeBlockName = destType.MainType.Peek().Name.ToString();
		Debug.Assert(destTypeBlockName != "string");
		var destTypeconverter = destTypeBlockName switch
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
			_ => destTypeBlockName,
		};
		if (srcTypeBlockName == "string")
		{
			Debug.Assert(destTypeBlockName != "string");
			if (destTypeBlockName is "bool" or "byte" or "char" or "short" or "ushort"
				or "int" or "uint" or "long" or "ulong" or "double")
			{
				var result = ((String)"(").AddRange(destTypeconverter).Add('.').AddRange(nameof(int.TryParse)).Add('(');
				var varName = RedStarLinq.NFill(32, _ =>
					(char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
				result.AddRange(source).AddRange(", out var ").AddRange(varName).AddRange(") ? ").AddRange(varName);
				return result.AddRange(" : ").AddRange(destTypeBlockName == "bool" ? "false)" : "0)");
			}
			else
				return ((String)"(").AddRange(destTypeconverter).AddRange(")(").AddRange(source).Add(')');
		}
		else if (destTypeBlockName == "bool")
		{
			Debug.Assert(srcTypeBlockName != "bool");
			return ((String)"(").AddRange(source).AddRange(") >= 1");
		}
		else if (srcTypeBlockName == "real")
		{
			Debug.Assert(destTypeBlockName != "real");
			return ((String)"(").AddRange(destTypeconverter).Add(')')
				.AddRange(nameof(Truncate)).Add('(').AddRange(source).Add(')');
		}
		else
			return ((String)"unchecked((").AddRange(destTypeconverter).AddRange(")(").AddRange(source).AddRange("))");
	}

	private static List<(NStarType Type, bool Warning)> GetCompatibleTypes((NStarType Type, bool Warning) source,
		List<(NStarType Type, bool Warning)> blackList)
	{
		List<(NStarType Type, bool Warning)> compatibleTypes = new(16);
		compatibleTypes.AddRange(ImplicitConversionsFromAnythingList.Convert(x => (x, source.Warning))
			.Filter(x => !blackList.Contains(x)));
		if (ImplicitConversionsList.TryGetValue(source.Type.MainType, out var containerConversions)
			&& containerConversions.TryGetValue(source.Type.ExtraTypes, out var typeConversions))
			compatibleTypes.AddRange(typeConversions.Convert(x => (x.DestType, x.Warning || source.Warning))
				.Filter(x => !blackList.Contains(x)));
		return compatibleTypes;
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

	public static NList<char> RandomVarName() => RedStarLinq.NFill(32, _ => (char)(globalRandom.Next(2) == 1
		? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));

	private sealed class FullTypeEComparer : G.IEqualityComparer<NStarType>
	{
		public bool Equals(NStarType x, NStarType y) => x.MainType.Equals(y.MainType) && x.ExtraTypes.Equals(y.ExtraTypes);

		public int GetHashCode(NStarType x) => x.MainType.GetHashCode() ^ x.ExtraTypes.GetHashCode();
	}
}

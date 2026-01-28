global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using System;
global using System.Diagnostics;
global using System.Reflection;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.TypeChecks;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Mpir.NET;
using NStar.EasyEvalLib;
using NStar.ParallelHS;
using NStar.SortedSets;
using NStar.TreeSets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CSharp.NStar;

public static class TypeConverters
{
	private static readonly Random random = new();
	private static readonly List<String> CollectionTypesList = [nameof(Buffer), nameof(Dictionary<,>),
		nameof(FastDelHashSet<>), nameof(FuncDictionary<,>), "HashTable",
		nameof(ICollection), nameof(G.IEnumerable<>), nameof(IList), nameof(IReadOnlyCollection<>), nameof(IReadOnlyList<>),
		nameof(LimitedQueue<>), nameof(G.LinkedList<>), nameof(G.LinkedListNode<>), nameof(ListHashSet<>),
		nameof(Memory<>), nameof(Mirror<,>),
		nameof(Queue<>), nameof(ParallelHashSet<>), nameof(ReadOnlyMemory<>), nameof(ReadOnlySpan<>),
		nameof(Slice<>), nameof(SortedDictionary<,>), nameof(SortedSet<>), nameof(Span<>), nameof(Stack<>),
		nameof(TreeHashSet<>), nameof(TreeSet<>)];
	private static readonly Dictionary<Type, bool> memoizedTypes = [];
	private static readonly Dictionary<String, Type> memoizedExtraTypes = [];

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
				&& CollectionTypesList.Contains(LeafType.MainType.ToString().ToNString().GetAfterLast("."))
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
			if (leftType.Equals(rightType) && !leftType.Equals(GetPrimitiveType("DateTime")))
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
		if (leftTypeName == "bool" && rightTypeName.AsSpan() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex")
			leftValue.Insert(0, '(').AddRange(" ? 1 : 0)");
		else if (rightTypeName == "bool" && leftTypeName.AsSpan() is "byte"
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
			if (leftTypeName.AsSpan() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan" or "real" or "complex"
				|| rightTypeName.AsSpan() is "short int" or "int" or "long int"
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
			if (leftTypeName.AsSpan() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan"
				|| rightTypeName.AsSpan() is "short int" or "int" or "long int" or "DateTime" or "TimeSpan")
				return "long long";
			else
				return "unsigned long int";
		}
		else if (leftTypeName == "TimeSpan" || rightTypeName == "TimeSpan"
			|| leftTypeName == "DateTime" && rightTypeName == "DateTime")
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

	public static NStarType BasicTypeToExtendedType(String mainType, List<String> extraTypes) =>
		(GetBlockStack(mainType), GetBranchCollection(extraTypes));

	public static BranchCollection GetBranchCollection(List<String> partialBlockStack) =>
		new(partialBlockStack.Convert(x => new TreeBranch("type", 0, []) { Extra
			= new NStarType(new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoBranches) }));

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
		if (IsEqualOrDerived(sourceType, destinationType))
		{
			destExpr = srcExpr;
			return true;
		}
		if (sourceType.Equals(StringType) && destinationType.Equals(UnsafeStringType))
		{
			destExpr = srcExpr?.Insert(0, '(').AddRange(").ToString()");
			return true;
		}
		if (sourceType.Equals(UnsafeStringType) && destinationType.Equals(StringType))
		{
			destExpr = srcExpr?.Insert(0, "((String)").Add(')');
			return true;
		}
		if (TypeEqualsToPrimitive(sourceType, "null", false))
		{
			destExpr = "default!";
			return true;
		}
		if (ImplicitConversionsFromAnything.Contains(destinationType, new FullTypeEComparer()))
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
		var destinationTypeString = destinationType.MainType.ToString();
		if (TypeEqualsToPrimitive(destinationType, "list", false) || destinationType.MainType.Length != 0
			&& destinationType.MainType.Peek().BlockType is BlockType.Class or BlockType.Struct or BlockType.Interface
			&& CollectionTypesList.Contains(destinationTypeString.ToNString().GetAfterLast(".")))
		{
			if (TypeEqualsToPrimitive(sourceType, "tuple", false))
			{
				var subtype = GetSubtype(destinationType);
				if (subtype.Equals(sourceType))
				{
					destExpr = srcExpr;
					return true;
				}
				if (sourceType.ExtraTypes.Length > 16)
				{
					destExpr = "default!";
					extraMessage = "list can be constructed from tuple of up to 16 elements,"
						+ " if you need more, use the other ways like Chain() or Fill()";
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
				var toInsert = ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(')
					.Repeat(DestinationDepth - SourceDepth);
				if (srcExpr == null)
				{
					destExpr = null;
					return true;
				}
				else if (!SourceLeafType.Equals(DestinationLeafType) && TypeIsPrimitive(SourceLeafType.MainType)
					&& TypeIsPrimitive(DestinationLeafType.MainType) && SourceLeafType.MainType.Peek().Name != "string"
					&& DestinationLeafType.MainType.Peek().Name != "string")
				{
					srcExpr.Replace(AdaptTerminalType(srcExpr, SourceLeafType, DestinationLeafType));
					srcExpr.Insert(0, toInsert);
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				else
				{
					srcExpr.Insert(0, toInsert);
					srcExpr.AddRange(((String)")").Repeat(DestinationDepth - SourceDepth));
					destExpr = srcExpr;
				}
				if (destinationTypeString is "System."
					+ nameof(ReadOnlySpan<>) or "System." + nameof(Span<>))
					srcExpr.Add('.').AddRange(nameof(srcExpr.AsSpan)).AddRange("()");
				if (destinationTypeString is "System.Unsafe."
					+ nameof(ReadOnlyMemory<>) or "System.Unsafe." + nameof(Memory<>))
					srcExpr.Add('.').AddRange(nameof(srcExpr.AsMemory)).AddRange("()");
				return true;
			}
			else if (SourceDepth <= DestinationDepth + 1
				&& SourceLeafType.Equals(StringType) && DestinationLeafType.Equals(CharType))
			{
				var toInsert = ((String)nameof(TypeConverters)).Add('.').AddRange(nameof(ListWithSingle)).Add('(')
					.Repeat(DestinationDepth - SourceDepth - 1);
				if (srcExpr == null)
					destExpr = null;
				else
				{
					srcExpr.Insert(0, toInsert);
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
		if (destinationTypeString is "System." + nameof(ReadOnlySpan<>) or "System." + nameof(Span<>))
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
		if (TaskBlockStacks.Contains(destinationType.MainType) && destinationType.ExtraTypes.Length == 1
			&& destinationType.ExtraTypes[0].Name == "type" && destinationType.ExtraTypes[0].Extra is NStarType TaskNStarType
			&& (TaskNStarType.Equals(sourceType) || sourceType.MainType.Equals(EmptyTaskBlockStack)))
		{
			destExpr = srcExpr;
			return true;
		}
		if (UserDefinedTypes.TryGetValue(SplitType(sourceType.MainType), out var userDefinedType)
			&& userDefinedType.BaseType != NullType && TypesAreCompatible(userDefinedType.BaseType, destinationType,
			out warning, srcExpr, out destExpr, out extraMessage))
			return true;
		if (sourceType.MainType.TryPeek(out var sourceBlock)
			&& (PrimitiveTypes.TryGetValue(sourceBlock.Name, out var sourceNetType)
			|| ExtraTypes.TryGetValue((new BlockStack(sourceType.MainType.SkipLast(1)).ToString(),
			 sourceBlock.Name), out sourceNetType))
			&& sourceNetType.GetGenericArguments().Length == 0
			&& destinationType.MainType.TryPeek(out var destinationBlock)
			&& (PrimitiveTypes.TryGetValue(destinationBlock.Name, out var destinationNetType)
			|| ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
			 destinationBlock.Name), out destinationNetType))
			&& (destinationNetType.GetGenericArguments().Length == 0
			&& destinationNetType.IsAssignableFrom(sourceNetType)
			|| destinationNetType.GetGenericArguments().Length == 1
			&& destinationNetType.GetGenericArguments()[0].Name == "TSelf"
			&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
			&& genericType.IsAssignableFrom(sourceNetType))
			|| ExplicitlyConnectedNamespaces.FindIndex(x =>
			ExtraTypes.TryGetValue((x,
			sourceType.MainType.TryPeek(out var sourceBlock) ? sourceBlock.Name : ""), out var sourceNetType)
			&& sourceNetType.GetGenericArguments().Length == 0
			&& ExtraTypes.TryGetValue((new BlockStack(destinationType.MainType.SkipLast(1)).ToString(),
			sourceType.MainType.TryPeek(out var destinationBlock) ? destinationBlock.Name : ""), out var destinationNetType)
			&& (destinationNetType.GetGenericArguments().Length == 0
			&& destinationNetType.IsAssignableFrom(sourceNetType)
			|| destinationNetType.GetGenericArguments()[0].Name == "TSelf"
			&& destinationNetType.TryWrap(x => x.MakeGenericType(sourceNetType), out var genericType)
			&& genericType.IsAssignableFrom(sourceNetType))) >= 0)
		{
			destExpr = srcExpr;
			return true;
		}
		if (!BuiltInMemberCollections.ImplicitConversions.TryGetValue(sourceType.MainType, out var containerConversions))
		{
			destExpr = "default!";
			return false;
		}
		if (!containerConversions.TryGetValue(sourceType.ExtraTypes, out var typeConversions))
		{
			destExpr = "default!";
			return false;
		}
		var foundIndex = typeConversions.FindIndex(x => x.DestType.Equals(destinationType));
		if (foundIndex != -1)
		{
			warning = typeConversions[foundIndex].Warning;
			if (srcExpr == null)
				destExpr = null;
			else if (!warning && !sourceType.Equals(BoolType))
				destExpr = srcExpr;
			else
				destExpr = AdaptTerminalType(srcExpr, sourceType, destinationType);
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
				foundIndex = new_types3_list.FindIndex(x => x.Type.Equals(destinationType));
				if (foundIndex == -1)
				{
					new_types2_list.AddRange(new_types3_list);
					continue;
				}
				warning = new_types3_list[foundIndex].Warning;
				if (srcExpr == null)
					destExpr = null;
				else if (!warning)
					destExpr = srcExpr;
				else
					destExpr = AdaptTerminalType(srcExpr, sourceType, destinationType);
				return true;
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
		var destTypeConverter = destTypeBlockName switch
		{
			"null" => "void",
			"short char" => "byte",
			"short int" => "short",
			"unsigned short int" => "ushort",
			"unsigned int" => "uint",
			"long char" => "(char, char)",
			"long int" => "long",
			"unsigned long int" => "ulong",
			"long long" => nameof(MpzT),
			"real" => "double",
			"complex" => "Complex",
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
				var result = ((String)"(").AddRange(destTypeConverter).Add('.').AddRange(nameof(int.TryParse)).Add('(');
				var varName = RedStarLinq.Fill(32, _ =>
					(char)(random.Next(2) == 1 ? random.Next('A', 'Z' + 1) : random.Next('a', 'z' + 1)));
				result.AddRange(source).AddRange(", out var ").AddRange(varName).AddRange(") ? ").AddRange(varName);
				return result.AddRange(" : ").AddRange(destTypeBlockName == "bool" ? "false)" : "0)");
			}
			else
				return ((String)"(").AddRange(destTypeConverter).AddRange(")(").AddRange(source).Add(')');
		}
		else if (destTypeBlockName == "bool")
		{
			Debug.Assert(srcTypeBlockName != "bool");
			return ((String)"(").AddRange(source).AddRange(") >= 1");
		}
		else if (srcTypeBlockName == "bool")
		{
			Debug.Assert(destTypeBlockName != "bool");
			return ((String)"(").AddRange(source).AddRange(") ? 1 : 0");
		}
		else if (srcTypeBlockName == "real")
		{
			Debug.Assert(destTypeBlockName != "real");
			return ((String)"(").AddRange(destTypeConverter).Add(')')
				.AddRange(nameof(Truncate)).Add('(').AddRange(source).Add(')');
		}
		else
			return ((String)"unchecked((").AddRange(destTypeConverter).AddRange(")(").AddRange(source).AddRange("))");
	}

	private static List<(NStarType Type, bool Warning)> GetCompatibleTypes((NStarType Type, bool Warning) source,
		List<(NStarType Type, bool Warning)> blackList)
	{
		List<(NStarType Type, bool Warning)> compatibleTypes = new(16);
		compatibleTypes.AddRange(ImplicitConversionsFromAnything.Convert(x => (x, source.Warning))
			.Filter(x => !blackList.Contains(x)));
		if (BuiltInMemberCollections.ImplicitConversions.TryGetValue(source.Type.MainType, out var containerConversions)
			&& containerConversions.TryGetValue(source.Type.ExtraTypes, out var typeConversions))
			compatibleTypes.AddRange(typeConversions.Convert(x => (x.DestType, x.Warning || source.Warning))
				.Filter(x => !blackList.Contains(x)));
		return compatibleTypes;
	}

	public static dynamic ListWithSingle<T>(T item)
	{
		if (item is bool b)
			return new BitList([b]);
		else
			return new List<T>(item);
	}

	public static List<char> RandomVarName() => RedStarLinq.Fill(32, _ => (char)(random.Next(2) == 1
		? random.Next('A', 'Z' + 1) : random.Next('a', 'z' + 1)));

	private sealed class FullTypeEComparer : G.IEqualityComparer<NStarType>
	{
		public bool Equals(NStarType x, NStarType y) => x.MainType.Equals(y.MainType) && x.ExtraTypes.Equals(y.ExtraTypes);

		public int GetHashCode(NStarType x) => x.MainType.GetHashCode() ^ x.ExtraTypes.GetHashCode();
	}

	public static Type TypeMapping(NStarType NStarType)
	{
		if (TypeEqualsToPrimitive(NStarType, "list", false))
		{
			if (NStarType.ExtraTypes.Length == 1)
			{
				if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				var netType = TypeMapping(InnerNStarType);
				return ConstructListType(netType);
			}
			else
			{
				if (NStarType.ExtraTypes[0].Name == "type"
					|| !int.TryParse(NStarType.ExtraTypes[0].Name.ToString(), out var levelsCount) || levelsCount < 1
					|| NStarType.ExtraTypes[^1].Name != "type"
					|| NStarType.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				var netType = TypeMapping(InnerNStarType);
				Type outputType;
				if (netType == typeof(bool))
					outputType = typeof(BitList);
				else
					outputType = typeof(List<>).MakeGenericType(netType);
				for (var i = 1; i < levelsCount; i++)
					outputType = typeof(List<>).MakeGenericType(outputType);
				return outputType;
			}
		}
		if (NStarType.MainType.Equals(FuncBlockStack))
		{
			List<Type> funcComponents = [];
			if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType)
				throw new InvalidOperationException();
			var returnType = TypeMapping(InnerNStarType);
			for (var i = 1; i < NStarType.ExtraTypes.Length; i++)
			{
				if (NStarType.ExtraTypes[i].Name != "type" || NStarType.ExtraTypes[i].Extra is not NStarType InnerNStarType2)
					throw new InvalidOperationException();
				funcComponents.Add(TypeMapping(InnerNStarType2));
			}
			return ConstructFuncType(returnType, funcComponents.GetSlice());
		}
		if (!TypeEqualsToPrimitive(NStarType, "tuple", false))
		{
			var split = SplitType(NStarType.MainType);
			if (TypeExists(split, out var netType))
			{
				if (netType == typeof(Task<>) && NStarType.ExtraTypes.Length == 1 && NStarType.ExtraTypes[0].Name == "type"
					&& NStarType.ExtraTypes[0].Extra is NStarType InnerNStarType && InnerNStarType.Equals(NullType))
					return typeof(Task);
				else if (netType == typeof(ValueTask<>) && NStarType.ExtraTypes.Length == 1
					&& NStarType.ExtraTypes[0].Name == "type"
					&& NStarType.ExtraTypes[0].Extra is NStarType ValueInnerNStarType && ValueInnerNStarType.Equals(NullType))
					return typeof(ValueTask);
				else if (netType.ContainsGenericParameters)
					return netType.MakeGenericType(NStarType.ExtraTypes.ToArray(x => TypeMapping(x.Value)));
				else
					return netType;
			}
			else if (Interfaces.TryGetValue((split.Container.ToString(), split.Type), out var @interface))
			{
				netType = @interface.DotNetType;
				if (netType.ContainsGenericParameters)
					return netType.MakeGenericType(NStarType.ExtraTypes.ToArray(x => TypeMapping(x.Value)));
				else
					return netType;
			}
			else if (NStarType.MainType.TryPeek(out var block) && block.BlockType == BlockType.Extra)
			{
				if (memoizedExtraTypes.TryGetValue(block.Name, out var memoized))
					return memoized;
				var assembly = EasyEval.CompileAndGetAssembly("class C<" + block.Name + ">{}class P{static void Main(){}}",
					[], out var errors);
				if (assembly == null || errors != "Compilation done without any error.\r\n")
					throw new InvalidOperationException();
				return memoizedExtraTypes[block.Name] = assembly.DefinedTypes.First().GetGenericArguments().First();
			}
			else
				throw new InvalidOperationException();
		}
		if (NStarType.ExtraTypes.Length == 0)
			return typeof(void);
		List<Type> tupleComponents = [];
		if (NStarType.ExtraTypes[0].Name != "type" || NStarType.ExtraTypes[0].Extra is not NStarType InnerNStarType3)
			throw new InvalidOperationException();
		var first = TypeMapping(InnerNStarType3);
		if (NStarType.ExtraTypes.Length == 1)
			return first;
		var innerResult = first;
		for (var i = 1; i < NStarType.ExtraTypes.Length; i++)
		{
			if (NStarType.ExtraTypes[i].Name == "type" && NStarType.ExtraTypes[i].Extra is NStarType InnerNStarType2)
			{
				tupleComponents.Add(innerResult);
				first = TypeMapping(InnerNStarType2);
				innerResult = first;
				continue;
			}
			innerResult = ConstructTupleType(RedStarLinq.FillArray(innerResult,
				int.TryParse(NStarType.ExtraTypes[i].Name.ToString(), out var n) ? n : 1).GetSlice());
		}
		return ConstructTupleType(tupleComponents.Add(innerResult).GetSlice());
	}

	private static Type TypeMapping(TreeBranch branch)
	{
		if (branch.Name != "type" || branch.Extra is not NStarType NStarType)
			throw new InvalidOperationException();
		return TypeMapping(NStarType);
	}

	public static Type ConstructListType(Type netType)
	{
		if (netType == typeof(bool))
			return typeof(BitList);
		else
			return typeof(List<>).MakeGenericType(netType);
	}

	public static Type ConstructFuncType(Type returnType)
	{
		if (returnType == typeof(void))
			return typeof(Action);
		return typeof(Func<>).MakeGenericType(returnType);
	}

	public static Type ConstructFuncType(Type returnType, Type paramType)
	{
		if (returnType == typeof(void))
			return typeof(Action<>).MakeGenericType(paramType);
		return typeof(Func<,>).MakeGenericType(paramType, returnType);
	}

	public static Type ConstructFuncType(Type returnType, Slice<Type> netTypes)
	{
		if (returnType == typeof(void))
			return netTypes.Length switch
			{
				0 => typeof(Action),
				1 => typeof(Action<>).MakeGenericType(netTypes[0]),
				2 => typeof(Action<,>).MakeGenericType(netTypes[0], netTypes[1]),
				3 => typeof(Action<,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2]),
				4 => typeof(Action<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3]),
				5 => typeof(Action<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4]),
				6 => typeof(Action<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5]),
				7 => typeof(Action<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5], netTypes[6]),
				8 => typeof(Action<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
					netTypes[5], netTypes[6], netTypes[7]),
				_ => throw new InvalidOperationException(),
			};
		return netTypes.Length switch
		{
			0 => typeof(Func<>).MakeGenericType(returnType),
			1 => typeof(Func<,>).MakeGenericType(netTypes[0], returnType),
			2 => typeof(Func<,,>).MakeGenericType(netTypes[0], netTypes[1], returnType),
			3 => typeof(Func<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], returnType),
			4 => typeof(Func<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], returnType),
			5 => typeof(Func<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				returnType),
			6 => typeof(Func<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], returnType),
			7 => typeof(Func<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], netTypes[6], returnType),
			8 => typeof(Func<,,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
				netTypes[5], netTypes[6], netTypes[7], returnType),
			_ => throw new InvalidOperationException(),
		};
	}

	public static Type ConstructTupleType(Slice<Type> netTypes) => netTypes.Length switch
	{
		0 => throw new InvalidOperationException(),
		1 => typeof(ValueTuple<>).MakeGenericType(netTypes[0]),
		2 => typeof(ValueTuple<,>).MakeGenericType(netTypes[0], netTypes[1]),
		3 => typeof(ValueTuple<,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2]),
		4 => typeof(ValueTuple<,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3]),
		5 => typeof(ValueTuple<,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4]),
		6 => typeof(ValueTuple<,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5]),
		7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5], netTypes[6]),
		_ => typeof(ValueTuple<,,,,,,,>).MakeGenericType(netTypes[0], netTypes[1], netTypes[2], netTypes[3], netTypes[4],
			netTypes[5], netTypes[6], ConstructTupleType(netTypes[7..])),
	};

	public static NStarType TypeMappingBack(Type netType, Type[] genericArguments, BranchCollection extraTypes)
	{
		if (netType.IsGenericParameter)
		{
			var genericArgumentsIndex = Array.IndexOf(genericArguments, netType);
			if (genericArgumentsIndex < 0 || extraTypes.Length <= genericArgumentsIndex)
				return new(new(new Block(BlockType.Extra, netType.Name, 1)), []);
			else if (extraTypes[genericArgumentsIndex].Name != "type"
				|| extraTypes[genericArgumentsIndex].Extra is not NStarType InnerNStarType)
				throw new InvalidOperationException();
			else
				return InnerNStarType;
		}
		if (netType.IsSZArray || netType.IsPointer)
			netType = typeof(List<>).MakeGenericType(netType.GetElementType() ?? throw new InvalidOperationException());
		else if (netType == typeof(System.Collections.BitArray))
			netType = typeof(BitList);
		else if (netType == typeof(Index))
			netType = typeof(int);
		var typeGenericArguments = netType.GetGenericArguments();
		if (netType.Name.Contains("Action"))
		{
			return new(FuncBlockStack, new([new TreeBranch("type", 0, []) { Extra = NullType },
				.. typeGenericArguments.Convert((x, index) =>
				new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes) })]));
		}
		if (netType.Name.Contains(nameof(Func<>)))
		{
			return new(FuncBlockStack, new([typeGenericArguments[^1].Wrap(x =>
				new TreeBranch("type", 0, []) 
				{
					Extra = TypeMappingBack(x.GetGenericArguments().Length != 0
					&& x.GetGenericTypeDefinition() == typeof(ValueTask<>)
					? x.GetGenericArguments()[0] : x, genericArguments, extraTypes) 
				}), .. typeGenericArguments.GetSlice(..^1).Convert((x, index) =>
				new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes) })]));
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
		while (true)
		{
			if (CreateVar(PrimitiveTypes.Find(x => x.Value == netType).Key, out var typename) != null)
				return typename == "list" ? GetListType(TypeMappingBack(typeGenericArguments[0], genericArguments,
					new(extraTypes.Values.TakeLast(genericArguments.Length))))
					: GetPrimitiveType(typename);
			else if (netType == typeof(Task))
				return new(TaskBlockStack, [new("type", 0, []) { Extra = NullType }]);
			else if (netType == typeof(ValueTask))
				return new(ValueTaskBlockStack, [new("type", 0, []) { Extra = NullType }]);
			else if (ExtraTypes.TryGetKey(netType, out var type2))
				return new(GetBlockStack(type2.Namespace + "." + type2.Type),
					new([.. typeGenericArguments.Convert((x, index) =>
					new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes) })]));
			else if (CreateVar(Interfaces.Find(x => x.Value.DotNetType == netType), out var type3).Key != default)
				return new(GetBlockStack(type3.Key.Namespace + "." + type3.Key.Interface),
					new([.. typeGenericArguments.Convert((x, index) =>
					new TreeBranch("type", 0,[]) { Extra = TypeMappingBack(x, genericArguments, extraTypes) })]));
			else if (netType == typeof(string))
				return StringType;
			else if (netType == typeof(BitList))
				return GetListType(BoolType);
			else if (innerTypes.Length != 0)
			{
				netType = netType.MakeGenericType([.. innerTypes]);
				if (!netType.Name.Contains("Tuple") && !netType.Name.Contains("KeyValuePair"))
				{
					innerTypes.Clear();
					continue;
				}
				return new(TupleBlockStack, new(netType.GenericTypeArguments.ToList(x =>
					new TreeBranch("type", 0, []) { Extra = TypeMappingBack(x, genericArguments, extraTypes) })));
			}
			else if (!typeof(ITuple).IsAssignableFrom(oldNetType))
				throw new InvalidOperationException();
			break;
		}
		BranchCollection result = [];
		var tupleTypes = new Queue<Type>();
		tupleTypes.Enqueue(oldNetType);
		while (tupleTypes.Length != 0 && tupleTypes.Dequeue() is Type tupleType)
			foreach (var field in tupleType.GetFields())
				if (field.Name == "Rest")
					tupleTypes.Enqueue(tupleType);
				else
					result.Add(new("type", 0, []) { Extra = TypeMappingBack(field.FieldType, genericArguments, extraTypes) });
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

	public static NStarType ReplaceExtraType(NStarType originalType, (String ExtraType, NStarType TypeToInsert) pattern)
	{
		if (originalType.MainType.Length == 1 && originalType.MainType.Peek().BlockType == BlockType.Extra
			&& originalType.MainType.Peek().Name == pattern.ExtraType && originalType.ExtraTypes.Length == 0)
			return pattern.TypeToInsert;
		else
		{
			return new(originalType.MainType, [.. originalType.ExtraTypes.Convert(x =>
				new G.KeyValuePair<String, TreeBranch>(x.Key, x.Value.Name != "type"
				|| x.Value.Extra is not NStarType InnerNStarType ? new TreeBranch(x.Value.Name, x.Value.Pos, x.Value.Container)
				: new TreeBranch("type", x.Value.Pos, x.Value.Container)
				{
					Extra = ReplaceExtraType(InnerNStarType, pattern)
				}))]);
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

	public static object? CastType(Type? type, dynamic value)
	{
		if (type == null || value == null)
			return null;
		if (value!.GetType() == type)
			return value;
		var valueAsString = value.ToString();
		if (type!.IsEnum)
		{
			if (Enum.IsDefined(type, valueAsString))
				return Enum.Parse(type, valueAsString);
		}
		if (type == typeof(bool))
		{
			return double.TryParse(valueAsString, out double doubleValue) && doubleValue >= 1
				|| valueAsString == "true" || valueAsString == "on" || valueAsString == "checked";
		}
		else if (type == typeof(Uri))
			return new Uri(Convert.ToString(valueAsString));
		else if (type == typeof(String))
			return (String)Convert.ChangeType(valueAsString, typeof(string));
		else
			return Convert.ChangeType(valueAsString, type);
	}
}

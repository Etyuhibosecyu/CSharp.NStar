using System.Collections.Immutable;

namespace CSharp.NStar;

public readonly record struct NStarType(BlockStack MainType, BranchCollection ExtraTypes)
{
	public static readonly BranchCollection NoBranches = [];
	public static readonly NStarType NullType = GetPrimitiveType("null");
	public static readonly NStarType ObjectType = GetPrimitiveType("object");
	public static readonly NStarType BoolType = GetPrimitiveType("bool");
	public static readonly NStarType ByteType = GetPrimitiveType("byte");
	public static readonly NStarType ShortIntType = GetPrimitiveType("short int");
	public static readonly NStarType UnsignedShortIntType = GetPrimitiveType("unsigned short int");
	public static readonly NStarType CharType = GetPrimitiveType("char");
	public static readonly NStarType IntType = GetPrimitiveType("int");
	public static readonly NStarType UnsignedIntType = GetPrimitiveType("unsigned int");
	public static readonly NStarType LongIntType = GetPrimitiveType("long int");
	public static readonly NStarType UnsignedLongIntType = GetPrimitiveType("unsigned long int");
	public static readonly NStarType RealType = GetPrimitiveType("real");
	public static readonly NStarType LongLongType = GetPrimitiveType("long long");
	public static readonly NStarType ComplexType = GetPrimitiveType("complex");
	public static readonly NStarType RecursiveType = GetPrimitiveType("typename");
	public static readonly NStarType StringType = GetPrimitiveType("string");
	public static readonly NStarType IndexType = GetPrimitiveType("index");
	public static readonly NStarType RangeType = GetPrimitiveType("range");
	public static readonly NStarType UnsafeStringType = new(new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Unsafe", 1), new(BlockType.Class, "UnsafeString", 1)), NoBranches);
	public static readonly NStarType BitListType = GetListType(BoolType);
	public static readonly BlockStack EmptyBlockStack = new();
	public static readonly BlockStack ListBlockStack = new(new Block(BlockType.Primitive, "list", 1));
	public static readonly BlockStack TupleBlockStack = new(new Block(BlockType.Primitive, "tuple", 1));
	public static readonly BlockStack EventHandlerBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Delegate, "EventHandler", 1));
	public static readonly BlockStack FuncBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Delegate, nameof(Func<>), 1));
	public static readonly BlockStack RecursiveBlockStack = GetPrimitiveBlockStack("typename");
	public static readonly BlockStack IEnumerableBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Collections", 1), new(BlockType.Interface, nameof(G.IEnumerable<>), 1));
	public static readonly BlockStack BaseIndexableBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(BaseIndexable<>), 1));
	public static readonly BlockStack DictionaryBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(Dictionary<,>), 1));
	public static readonly BlockStack ListHashSetBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(ListHashSet<>), 1));
	public static readonly BlockStack TaskBlockStack = new(new Block(BlockType.Class, "Task", 1));
	public static readonly BlockStack ValueTaskBlockStack = new(new Block(BlockType.Struct, "ValueTask", 1));
	public static readonly BlockStack TaskBlockStackNamespace = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Threading", 1), new(BlockType.Class, "Task", 1));
	public static readonly BlockStack ValueTaskBlockStackNamespace = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Threading", 1), new(BlockType.Struct, "ValueTask", 1));
	public static readonly BlockStack EmptyTaskBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Unsafe", 1), new(BlockType.Class, "EmptyTask", 1));
	public static readonly BlockStack ValueEmptyTaskBlockStack = new(new(BlockType.Namespace, "System", 1),
		new(BlockType.Namespace, "Unsafe", 1), new(BlockType.Struct, "ValueEmptyTask", 1));
	public static readonly ImmutableArray<BlockStack> TaskBlockStacks = [
		TaskBlockStack, ValueTaskBlockStack, TaskBlockStackNamespace, ValueTaskBlockStackNamespace,
		EmptyTaskBlockStack, ValueEmptyTaskBlockStack
	];

	public NStarType Copy() => new(new(MainType), new(ExtraTypes.Values.Convert(x =>
		new TreeBranch(x.Name.Copy(), x.Pos, x.Container)
		{
			Elements = x.Elements.Copy(),
			Extra = x.Extra is NStarType NStarType ? NStarType.Copy() : x
		})));

	public static NStarType GetListType(NStarType InnerType)
	{
		if (!TypeEqualsToPrimitive(InnerType, "list", false))
			return new(ListBlockStack, new([new("type", 0, []) { Extra = InnerType }]));
		else if (InnerType.ExtraTypes.Length >= 2 && InnerType.ExtraTypes[0].Name != "type"
			&& int.TryParse(InnerType.ExtraTypes[0].Name.ToString(), out var number))
			return new(ListBlockStack, new([new((number + 1).ToString(), 0, []), InnerType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new("2", 0, []), InnerType.ExtraTypes[^1]]));
	}

	public static NStarType GetListType(TreeBranch InnerType)
	{
		if (InnerType.Name != "type" || InnerType.Extra is not NStarType NStarType
			|| !TypeEqualsToPrimitive(NStarType, "list", false))
			return new(ListBlockStack, new([InnerType]));
		else if (NStarType.ExtraTypes.Length >= 2 && NStarType.ExtraTypes[0].Name != "type"
			&& int.TryParse(NStarType.ExtraTypes[0].Name.ToString(), out var number))
			return new(ListBlockStack, new([new((number + 1).ToString(), 0, []), NStarType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new("2", 0, []), NStarType.ExtraTypes[^1]]));
	}

	public static BlockStack GetPrimitiveBlockStack(String primitive) => new(new Block(BlockType.Primitive, primitive, 1));

	public static NStarType GetPrimitiveType(String primitive) =>
		(new(new Block(BlockType.Primitive, primitive, 1)), NoBranches);

	public static (BlockStack Container, String Type) SplitType(BlockStack blockStack) =>
		(new(blockStack.ToList().SkipLast(1)), blockStack.TryPeek(out var block) ? block.Name : []);

	public static (BlockStack Container, String Type) SplitType(Stack<Block> blockStack) =>
		(new(blockStack.ToList().SkipLast(1)), blockStack.TryPeek(out var block) ? block.Name : []);

	public override readonly string ToString()
	{
		if (TypeEqualsToPrimitive(this, "list", false))
			return "list(" + (ExtraTypes.Length == 2 ? ExtraTypes[0].ToShortString() : "") + ") "
				+ ExtraTypes[^1].ToShortString();
		else if (TypeEqualsToPrimitive(this, "tuple", false))
		{
			if (ExtraTypes.Length == 0 || ExtraTypes[0].Name != "type" || ExtraTypes[0].Extra is not NStarType prev)
				return "()";
			if (ExtraTypes.Length == 1)
				return prev.ToString();
			String result = [];
			var repeats = 1;
			for (var i = 1; i < ExtraTypes.Length; i++)
			{
				if (ExtraTypes[i].Name != "type" || ExtraTypes[i].Extra is not NStarType current)
					return "()";
				if (prev.Equals(current))
				{
					repeats++;
					continue;
				}
				if (result.Length == 0)
					result.Add('(');
				else
					result.AddRange(", ");
				var isList = TypeEqualsToPrimitive(prev, "list", false);
				if (isList)
					result.Add('(');
				result.AddRange(prev.ToString());
				if (isList)
					result.Add(')');
				if (repeats != 1)
					result.Add('[').AddRange(repeats.ToString()).Add(']');
				repeats = 1;
				prev = current;
			}
			var containsMultiple = result.Length != 0;
			if (containsMultiple)
				result.AddRange(", ");
			result.AddRange(prev.ToString());
			if (repeats != 1)
				result.Add('[').AddRange(repeats.ToString()).Add(']');
			if (containsMultiple)
				result.Add(')');
			return result.ToString();
		}
		else
			return MainType.ToString() + (ExtraTypes.Length == 0 ? "" : "[" + ExtraTypes.ToString() + "]");
	}

	public static bool TypeEqualsToPrimitive(NStarType type, String primitive, bool noExtra = true) =>
		TypeIsPrimitive(type.MainType) && type.MainType.Peek().Name == primitive && (!noExtra || type.ExtraTypes.Length == 0);

	public static bool TypeIsPrimitive(BlockStack type) => type.Length == 1
		&& type.Peek().BlockType == BlockType.Primitive;

	public static implicit operator NStarType((BlockStack MainType, BranchCollection ExtraTypes) value) =>
		new(value.MainType, value.ExtraTypes);
}

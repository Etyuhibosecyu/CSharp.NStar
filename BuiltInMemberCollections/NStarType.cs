using static CSharp.NStar.BuiltInMemberCollections;

namespace CSharp.NStar;

public readonly record struct NStarType(BlockStack MainType, BranchCollection ExtraTypes)
{
	public static readonly BranchCollection NoBranches = [];
	public static readonly NStarType NullType = GetPrimitiveType("null");
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
	public static readonly NStarType RecursiveType = GetPrimitiveType("typename");
	public static readonly NStarType StringType = GetPrimitiveType("string");
	public static readonly NStarType IndexType = GetPrimitiveType("index");
	public static readonly NStarType RangeType = GetPrimitiveType("range");
	public static readonly BlockStack EmptyBlockStack = new();
	public static readonly BlockStack ListBlockStack = new([new(BlockType.Primitive, "list", 1)]);
	public static readonly BlockStack TupleBlockStack = new([new(BlockType.Primitive, "tuple", 1)]);
	public static readonly BlockStack FuncBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]);
	public static readonly BlockStack IEnumerableBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Namespace, "Collections", 1), new(BlockType.Interface, nameof(G.IEnumerable<bool>), 1)]);
	public static readonly BlockStack BaseIndexableBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(BaseIndexable<bool>), 1)]);
	public static readonly BlockStack ListHashSetBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(ListHashSet<bool>), 1)]);
	public static readonly List<BlockType> ExplicitNameBlockTypes = new(BlockType.Constructor, BlockType.Destructor, BlockType.Operator, BlockType.Other);
	public static readonly NStarType BitListType = GetListType(BoolType);
	public static readonly NStarType ByteListType = GetListType(ByteType);
	public static readonly NStarType ShortIntListType = GetListType(ShortIntType);
	public static readonly NStarType UnsignedShortIntListType = GetListType(UnsignedShortIntType);
	public static readonly NStarType CharListType = GetListType(CharType);
	public static readonly NStarType IntListType = GetListType(IntType);
	public static readonly NStarType UnsignedIntListType = GetListType(UnsignedIntType);
	public static readonly NStarType LongIntListType = GetListType(LongIntType);
	public static readonly NStarType UnsignedLongIntListType = GetListType(UnsignedLongIntType);
	public static readonly NStarType RealListType = GetListType(RealType);
	public static readonly NStarType StringListType = GetListType(StringType);
	public static readonly NStarType UniversalListType = GetListType((new BlockStack([new(BlockType.Primitive, "universal", 1)]), NoBranches));

	public override readonly string ToString()
	{
		if (TypeEqualsToPrimitive(this, "list", false))
			return "list(" + (ExtraTypes.Length == 2 ? ExtraTypes[0].ToShortString() : "") + ") "
				+ ExtraTypes[^1].ToShortString();
		else if (TypeEqualsToPrimitive(this, "tuple", false))
		{
			if (ExtraTypes.Length == 0 || ExtraTypes[0].Info != "type" || ExtraTypes[0].Extra is not NStarType prev)
				return "()";
			if (ExtraTypes.Length == 1)
				return prev.ToString();
			String result = [];
			var repeats = 1;
			for (var i = 1; i < ExtraTypes.Length; i++)
			{
				if (ExtraTypes[i].Info != "type" || ExtraTypes[i].Extra is not NStarType current)
					return "()";
				if (TypesAreEqual(prev, current))
				{
					repeats++;
					continue;
				}
				if (result.Length == 0)
					result.Add('(');
				else
					result.AddRange(", ");
				var bList = TypeEqualsToPrimitive(prev, "list", false);
				if (bList)
					result.Add('(');
				result.AddRange(prev.ToString());
				if (bList)
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

	public static implicit operator NStarType((BlockStack MainType, BranchCollection ExtraTypes) value) => new(value.MainType, value.ExtraTypes);
}

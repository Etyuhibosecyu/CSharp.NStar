global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using System;
global using G = System.Collections.Generic;
global using static System.Math;
global using String = NStar.Core.String;
using NStar.BufferLib;
using NStar.MathLib;
using NStar.ParallelHS;
using NStar.RemoveDoubles;
using NStar.SortedSets;
using NStar.SumCollections;
using NStar.TreeSets;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using static CSharp.NStar.DeclaredConstructions;

namespace CSharp.NStar;

public sealed class Block(BlockType blockType, String name, int unnamedIndex)
{
	public BlockType BlockType { get; private set; } = blockType;
	public String Name { get; private set; } = name;
	public int UnnamedIndex { get; set; } = unnamedIndex;

	public override bool Equals(object? obj) => obj is not null && obj is Block m && BlockType == m.BlockType && Name == m.Name;

	public override int GetHashCode() => BlockType.GetHashCode() ^ Name.GetHashCode();

	public override string ToString() => (BlockType == BlockType.Unnamed) ? "Unnamed(" + Name + ")" : (ExplicitNameBlockTypes.Contains(BlockType) ? BlockType.ToString() : "") + Name;
}

public sealed class TypeOrValue
{
	public String Value { get; set; } = [];
	public BlockStack Type { get; set; } = new();
	public bool IsValue { get; set; }

	public TypeOrValue(String newValue)
	{
		Value = newValue;
		IsValue = true;
	}

	public TypeOrValue(BlockStack newType)
	{
		Type = newType;
		IsValue = false;
	}

	public override string ToString() => IsValue ? Value.ToString() : Type.ToString();

	public static explicit operator TypeOrValue(string x) => new((String)x);

	public static explicit operator TypeOrValue(String x) => new(x);

	public static implicit operator TypeOrValue(BlockStack x) => new(x);
}

[DebuggerDisplay("{ToString()}")]
public class BlockStack : Stack<Block>
{
	public BlockStack()
	{
	}

	public BlockStack(int capacity) : base(capacity)
	{
	}

	public BlockStack(G.IEnumerable<Block> collection) : base(collection)
	{
	}

	public BlockStack(params Block[] array) : base(array)
	{
	}

	public BlockStack(int capacity, params Block[] array) : base(capacity, array)
	{
	}

	public string ToShortString() => string.Join(".", this.ToArray(x => (x.BlockType == BlockType.Unnamed) ? "Unnamed(" + x.Name + ")" : x.Name.ToString()));

	public override string ToString() => string.Join(".", this.ToArray(x => x.ToString()));
}

public record struct UserDefinedType(GeneralArrayParameters ArrayParameters, TypeAttributes Attributes, UniversalType BaseType, GeneralExtraTypes Decomposition);
public record struct UserDefinedProperty(UniversalType UnvType, PropertyAttributes Attributes, String DefaultValue);
public record struct GeneralArrayParameter(bool ArrayParameterPackage, GeneralExtraTypes ArrayParameterRestrictions, BlockStack ArrayParameterType, String ArrayParameterName);
public record struct MethodParameter(String Type, String Name, List<String> ExtraTypes, ParameterAttributes Attributes, String DefaultValue);
public record struct GeneralMethodParameter(UniversalType Type, String Name, ParameterAttributes Attributes, String DefaultValue);
public record struct FunctionOverload(List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters);
public record struct GeneralMethodOverload(GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters);

public sealed class VariablesBlock<T>(IList<T> main, IList<bool> isNull)
{
	public IList<T> Main = main;
	public IList<bool> IsNull = isNull;
}

public class TypeSortedList<T> : SortedDictionary<BlockStack, T>
{
	public TypeSortedList() : base(new BlockStackComparer())
	{
	}
}
public class TypeDictionary<T> : Dictionary<BlockStack, T>
{
	public TypeDictionary() : base(new BlockStackEComparer())
	{
	}
}
public class TypeDictionary2<T> : Dictionary<BlockStack, IList<T>>
{
	public TypeDictionary2() : base(new BlockStackEComparer())
	{
	}
}
public class LexemGroup : List<(BlockStack Container, String Name, int Start, int End)>
{
}
public class BlocksToJump : List<(BlockStack Container, String Type, String Name, int Start, int End)>
{
}
public class ParameterValues : List<(BlockStack Container, String Name, int ParameterIndex, int Start, int End)>
{
}
public class GeneralTypes(G.IComparer<(BlockStack Container, String Type)> comparer) : SortedDictionary<(BlockStack Container, String Type), (GeneralArrayParameters ArrayParameters, TypeAttributes Attributes)>(comparer)
{
}
public class TypeVariables : SortedDictionary<String, UniversalType>
{
	public TypeVariables() : base()
	{
	}

	public TypeVariables(G.IDictionary<String, UniversalType> dictionary) : base(dictionary)
	{
	}
}
public class TypeProperties : SortedDictionary<String, (UniversalType UnvType, PropertyAttributes Attributes)>
{
}
public class UserDefinedTypeProperties : Dictionary<String, UserDefinedProperty>
{
}
public class TypeIndexers : SortedDictionary<String, (BlockStack IndexType, BlockStack Type, List<String> ExtraTypes, PropertyAttributes Attributes)>
{
}
public class MethodParameters : List<MethodParameter>
{
	public MethodParameters() : base() { }
	public MethodParameters(G.IEnumerable<MethodParameter> parameters) : base(parameters) { }
}
public class FunctionsList : SortedDictionary<String, FunctionOverload>
{
}

[DebuggerDisplay("{ToString()}")]
public class GeneralExtraTypes : Dictionary<String, UniversalTypeOrValue>
{
	public UniversalTypeOrValue this[int index] { get => Values.ElementAt(index); set => this[Keys.ElementAt(index)] = value; }

	public GeneralExtraTypes() : base()
	{
	}

	public GeneralExtraTypes(G.IDictionary<String, UniversalTypeOrValue> collection) : base(collection)
	{
	}

	public GeneralExtraTypes(G.IEnumerable<UniversalTypeOrValue> collection) : this()
	{
		foreach (var elem in collection)
			Add(elem);
	}

	public virtual void Add(UniversalTypeOrValue item) => Add("Item" + (Length + 1).ToString(), item);

	public virtual void AddRange(G.IEnumerable<UniversalTypeOrValue> collection)
	{
		foreach (var elem in collection)
			Add(elem);
	}

	public virtual void AddRange(GeneralExtraTypes collection)
	{
		foreach (var elem in collection)
			Add(elem.Key, elem.Value);
	}

	public override string ToString() => string.Join(", ", Values.ToArray(x => x.ToString()));
}

public class GeneralArrayParameters : List<GeneralArrayParameter>
{
}
public class GeneralMethodParameters : List<GeneralMethodParameter>
{
	public GeneralMethodParameters() : base()
	{
	}

	public GeneralMethodParameters(G.IEnumerable<GeneralMethodParameter> collection) : base(collection)
	{
	}
}
public class GeneralMethodOverloads : List<GeneralMethodOverload>
{
}
public class GeneralMethods : SortedDictionary<String, GeneralMethodOverloads>
{
}
public class UserDefinedMethods : Dictionary<String, GeneralMethodOverloads>
{
}
public class ConstructorOverloads : List<(ConstructorAttributes Attributes, GeneralMethodParameters Parameters)>
{
	public ConstructorOverloads() : base() { }
	public ConstructorOverloads(G.IEnumerable<(ConstructorAttributes Attributes, GeneralMethodParameters Parameters)> collection) : base(collection) { }
}
public class UnaryOperatorOverloads : List<(bool Postfix, UniversalType ReturnUnvType, UniversalType OpdUnvType)>
{
}
public class UnaryOperatorClasses(G.IComparer<BlockStack> comparer) : SortedDictionary<BlockStack, UnaryOperatorOverloads>(comparer)
{
}
public class BinaryOperatorOverloads : List<(UniversalType ReturnUnvType, UniversalType LeftOpdUnvType, UniversalType RightOpdUnvType)>
{
}
public class BinaryOperatorClasses(G.IComparer<BlockStack> comparer) : SortedDictionary<BlockStack, BinaryOperatorOverloads>(comparer)
{
}
public class DestTypes : List<(UniversalType DestType, bool Warning)>
{
}
public class ImplicitConversions : Dictionary<GeneralExtraTypes, DestTypes>
{
	public ImplicitConversions() : base(new GeneralExtraTypesEComparer())
	{
	}
}
public class OutdatedMethodOverloads : List<(GeneralMethodParameters Parameters, String UseInstead)>
{
}
public class OutdatedMethods : SortedDictionary<String, OutdatedMethodOverloads>
{
}

public interface IClass { }

public enum TypeAttributes
{
	None = 0,
	Sealed = 1,
	Abstract = 2,
	Static = 3,
	Struct = 4,
	Enum = 5,
	Delegate = 6,
	Closed = 16,
	Protected = 32,
	Internal = 64,
	Partial = 256,
}

public enum PropertyAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	NoSet = 16,
	ClosedSet = 32,
	ProtectedSet = 64,
}

public enum FunctionAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	Const = 16,
	Multiconst = 32,
	Abstract = 64,
	Sealed = 128,
	New = 192,
	Wrong = 256,
}

public enum ParameterAttributes
{
	None = 0,
	Optional = 1,
	Ref = 2,
	Out = 4,
	Params = 6,
}

public enum ConstructorAttributes
{
	None = 0,
	Static = 1,
	Closed = 2,
	Protected = 4,
	Internal = 8,
	Multiconst = 16,
	Abstract = 32,
}

public enum BlockType
{
	Unnamed,
	Primitive,
	Extra,
	Namespace,
	Class,
	Struct,
	Interface,
	Delegate,
	Enum,
	Function,
	Constructor,
	Destructor,
	Operator,
	Extent,
	Other,
}

public enum TypeConstraints
{
	None,
	BaseClassOrInterface,
	BaseInterface,
	NotAbstract,
}

public enum RawStringState
{
	Normal,
	ForwardSlash,
	Quote,
	ForwardSlashAndQuote,
	EmailSign,
}

public enum ExecutionFlags
{
	None = 0,
	NoPreserving = 1,
	TrueCondition = 2,
	EndCondition = 4,
	Cycle = 8,
	Continue = 16,
	Break = 32,
	Return = 64,
	Assignment = 128,
	PreservingFlags = Continue | Break | Return | Assignment,
}

public enum OptimizationStage
{
	None,
	CallGraph,
	Conditions,
	Functional,
	Clean,
	DirectTranslation,
}

public sealed class LexemTree(char @char, List<LexemTree> nextTree, bool allowAll = false, bool allowNone = true)
{
	public char Char { get; set; } = @char;
	public List<LexemTree> NextTree { get; set; } = nextTree;
	public bool AllowAll { get; set; } = allowAll;
	public bool AllowNone { get; set; } = allowNone;

	public LexemTree(char @char) : this(@char, [])
	{
	}

	public static implicit operator LexemTree(char x) => new(x);
}

public static class DeclaredConstructions
{
	public static readonly GeneralExtraTypes NoGeneralExtraTypes = [];
	public static readonly UniversalType NullType = GetPrimitiveType("null");
	public static readonly UniversalType BoolType = GetPrimitiveType("bool");
	public static readonly UniversalType ByteType = GetPrimitiveType("byte");
	public static readonly UniversalType ShortIntType = GetPrimitiveType("short int");
	public static readonly UniversalType UnsignedShortIntType = GetPrimitiveType("unsigned short int");
	public static readonly UniversalType CharType = GetPrimitiveType("char");
	public static readonly UniversalType IntType = GetPrimitiveType("int");
	public static readonly UniversalType UnsignedIntType = GetPrimitiveType("unsigned int");
	public static readonly UniversalType LongIntType = GetPrimitiveType("long int");
	public static readonly UniversalType UnsignedLongIntType = GetPrimitiveType("unsigned long int");
	public static readonly UniversalType RealType = GetPrimitiveType("real");
	public static readonly UniversalType StringType = GetPrimitiveType("string");
	public static readonly UniversalType IndexType = GetPrimitiveType("index");
	public static readonly UniversalType RangeType = GetPrimitiveType("range");
	public static readonly BlockStack EmptyBlockStack = new();
	public static readonly BlockStack ListBlockStack = new([new(BlockType.Primitive, "list", 1)]);
	public static readonly BlockStack TupleBlockStack = new([new(BlockType.Primitive, "tuple", 1)]);
	public static readonly BlockStack FuncBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]);
	public static readonly List<BlockType> ExplicitNameBlockTypes = new(BlockType.Constructor, BlockType.Destructor, BlockType.Operator, BlockType.Other);
	public static readonly UniversalType BitListType = GetListType(BoolType);
	public static readonly UniversalType ByteListType = GetListType(ByteType);
	public static readonly UniversalType ShortIntListType = GetListType(ShortIntType);
	public static readonly UniversalType UnsignedShortIntListType = GetListType(UnsignedShortIntType);
	public static readonly UniversalType CharListType = GetListType(CharType);
	public static readonly UniversalType IntListType = GetListType(IntType);
	public static readonly UniversalType UnsignedIntListType = GetListType(UnsignedIntType);
	public static readonly UniversalType LongIntListType = GetListType(LongIntType);
	public static readonly UniversalType UnsignedLongIntListType = GetListType(UnsignedLongIntType);
	public static readonly UniversalType RealListType = GetListType(RealType);
	public static readonly UniversalType StringListType = GetListType(StringType);
	public static readonly UniversalType UniversalListType = GetListType((new BlockStack([new(BlockType.Primitive, "universal", 1)]), NoGeneralExtraTypes));
	private static readonly List<String> NoExtraTypes = [];
	private static readonly List<String> ExtraTypesT = ["T"];
	private static readonly BlockStack GeneralTypeBool = new([new(BlockType.Primitive, "bool", 1)]);
	private static readonly BlockStack GeneralTypeInt = new([new(BlockType.Primitive, "int", 1)]);
	private static readonly BlockStack GeneralTypeList = new([new(BlockType.Primitive, "list", 1)]);
	private static readonly BlockStack GeneralTypeString = new([new(BlockType.Primitive, "string", 1)]);
	private static readonly GeneralExtraTypes GeneralExtraTypesT = [new(new BlockStack([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes)];
	private static readonly GeneralMethodParameter GeneralParameterIndex = new(IntType, "index", ParameterAttributes.None, []);
	private static readonly GeneralMethodParameter GeneralParameterStringS = new(StringType, "s", ParameterAttributes.None, []);
	private static readonly GeneralMethodParameter GeneralParameterString1 = new(StringType, "string1", ParameterAttributes.None, []);
	private static readonly GeneralMethodParameter GeneralParameterString2 = new(StringType, "string2", ParameterAttributes.None, []);
	private static readonly GeneralMethodParameter GeneralParameterString3 = new(StringType, "string3", ParameterAttributes.None, []);

	public static SortedSet<String> KeywordsList { get; } = new("_", "abstract", "break", "case", "Class", "closed", "const", "Constructor", "continue", "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst", "Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref", "repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while");

	public static SortedSet<String> EscapedKeywordsList { get; } = new("abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed, float, for, foreach, goto, if, implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while");

	public static ListHashSet<String> AssignmentOperatorsList { get; } = new("=", "+=", "-=", "*=", "/=", "%=", "pow=", "&=", "|=", "^=", ">>=", "<<=");

	public static ListHashSet<String> TernaryOperatorsList { get; } = new("?", "?=", "?>", "?<", "?>=", "?<=", "?!=");

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type and ExtraTypes.
	/// </summary>
	public static TypeSortedList<TypeVariables> VariablesList { get; } = [];

	public static SortedSet<String> NamespacesList { get; } = new("System", "System.Collections");

	public static G.HashSet<String> UserDefinedNamespacesList { get; } = [];

	public static SortedSet<String> ExplicitlyConnectedNamespacesList { get; } = [];

	public static SortedDictionary<String, Type> PrimitiveTypesList { get; } = new() { { "null", typeof(void) }, { "object", typeof(object) }, { "bool", typeof(bool) }, { "byte", typeof(byte) }, { "short char", typeof(byte) }, { "short int", typeof(short) }, { "unsigned short int", typeof(ushort) }, { "char", typeof(char) }, { "int", typeof(int) }, { "unsigned int", typeof(uint) }, { "long char", typeof(uint) }, { "long int", typeof(long) }, { "DateTime", typeof(DateTime) }, { "TimeSpan", typeof(TimeSpan) }, { "unsigned long int", typeof(long) }, { "real", typeof(double) }, { "typename", typeof(void) }, { "string", typeof(String) }, { "index", typeof(Index) }, { "range", typeof(Range) }, { "nint", typeof(nint) }, { "list", typeof(List<>) }, { "universal", typeof(object) }, { "dynamic", typeof(void) }, { "var", typeof(void) } };

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), Type> ExtraTypesList { get; } = new() { { ("System", nameof(DateTimeKind)), typeof(DateTimeKind) }, { ("System", nameof(DayOfWeek)), typeof(DayOfWeek) }, { ("System", nameof(Predicate<bool>)), typeof(Predicate<>) }, { ("System", nameof(ReadOnlySpan<bool>)), typeof(ReadOnlySpan<>) }, { ("System", nameof(RedStarLinq)), typeof(RedStarLinq) }, { ("System", nameof(RedStarLinqDictionaries)), typeof(RedStarLinqDictionaries) }, { ("System", nameof(RedStarLinqExtras)), typeof(RedStarLinqExtras) }, { ("System", nameof(RedStarLinqParallel)), typeof(RedStarLinqParallel) }, { ("System", nameof(RedStarLinqMath)), typeof(RedStarLinqMath) }, { ("System", nameof(RedStarLinqRemoveDoubles)), typeof(RedStarLinqRemoveDoubles) }, { ("System", nameof(Span<bool>)), typeof(Span<>) }, { ("System.Collections", nameof(BaseDictionary<bool, bool, Dictionary<bool, bool>>)), typeof(BaseDictionary<,,>) }, { ("System.Collections", nameof(BaseHashSet<bool, ListHashSet<bool>>)), typeof(BaseHashSet<,>) }, { ("System.Collections", nameof(BaseIndexable<bool, List<bool>>)), typeof(BaseIndexable<,>) }, { ("System.Collections", nameof(BaseList<bool, List<bool>>)), typeof(BaseList<,>) }, { ("System.Collections", nameof(BaseSet<bool, TreeSet<bool>>)), typeof(BaseSet<,>) }, { ("System.Collections", nameof(BaseSortedSet<bool, TreeSet<bool>>)), typeof(BaseSortedSet<,>) }, { ("System.Collections", nameof(BaseSumList<int, SumList>)), typeof(BaseSumList<int, SumList>) }, { ("System.Collections", nameof(BigSumList)), typeof(BigSumList) }, { ("System.Collections", nameof(Buffer)), typeof(Buffer<>) }, { ("System.Collections", nameof(Chain)), typeof(Chain) }, { ("System.Collections", nameof(Comparer<bool>)), typeof(Comparer<>) }, { ("System.Collections", nameof(Dictionary<bool, bool>)), typeof(Dictionary<,>) }, { ("System.Collections", nameof(EComparer<bool>)), typeof(EComparer<>) }, { ("System.Collections", nameof(Extents)), typeof(Extents) }, { ("System.Collections", nameof(FastDelHashSet<bool>)), typeof(FastDelHashSet<>) }, { ("System.Collections", nameof(Group<bool, bool>)), typeof(Group<,>) }, { ("System.Collections", nameof(G.LinkedList<bool>)), typeof(G.LinkedList<>) }, { ("System.Collections", nameof(G.LinkedListNode<bool>)), typeof(G.LinkedListNode<>) }, { ("System.Collections", nameof(LimitedQueue<bool>)), typeof(LimitedQueue<>) }, { ("System.Collections", nameof(ListEComparer<bool>)), typeof(ListEComparer<>) }, { ("System.Collections", nameof(ListHashSet<bool>)), typeof(ListHashSet<>) }, { ("System.Collections", nameof(Mirror<bool, bool>)), typeof(Mirror<,>) }, { ("System.Collections", nameof(NGroup<bool, bool>)), typeof(NGroup<,>) }, { ("System.Collections", nameof(NListEComparer<bool>)), typeof(NListEComparer<>) }, { ("System.Collections", nameof(ParallelHashSet<bool>)), typeof(ParallelHashSet<>) }, { ("System.Collections", nameof(Queue<bool>)), typeof(Queue<>) }, { ("System.Collections", nameof(Slice<bool>)), typeof(Slice<>) }, { ("System.Collections", nameof(SortedDictionary<bool, bool>)), typeof(SortedDictionary<,>) }, { ("System.Collections", nameof(SortedSet<bool>)), typeof(SortedSet<>) }, { ("System.Collections", nameof(Stack<bool>)), typeof(Stack<>) }, { ("System.Collections", nameof(SumList)), typeof(SumList) }, { ("System.Collections", nameof(SumSet<bool>)), typeof(SumSet<>) }, { ("System.Collections", nameof(TreeHashSet<bool>)), typeof(TreeHashSet<>) }, { ("System.Collections", nameof(TreeSet<bool>)), typeof(TreeSet<>) } };

	/// <summary>
	/// Sorted by Container and Type, also contains ArrayParameterPackage modifiers, ArrayParameterRestrictions, ArrayParameterTypes, ArrayParameterNames and Attributes.
	/// </summary>
	public static GeneralTypes GeneralTypesList { get; } = new(new BlockStackAndStringComparer()) { { (new([new(BlockType.Namespace, "System", 1)]), nameof(Action)), ([new(true, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "Types")], TypeAttributes.None) }, { (new([new(BlockType.Namespace, "System", 1)]), nameof(Func<bool>)), new([new(false, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "TReturn"), new(true, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "Types")], TypeAttributes.None) } };

	/// <summary>
	/// Sorted by Container and Type, also contains ArrayParameterPackage modifiers, ArrayParameterRestrictions, ArrayParameterTypes, ArrayParameterNames and Attributes.
	/// </summary>
	public static Dictionary<(BlockStack Container, String Type), UserDefinedType> UserDefinedTypesList { get; } = new(new BlockStackAndStringEComparer()) { };

	/// <summary>
	/// Sorted by tuple, contains Namespace, Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Interface), (List<String> ExtraTypes, Type DotNetType)> InterfacesList { get; } = new() { { ([], "IBase"), (ExtraTypesT, typeof(void)) }, { ([], "IChar"), (ExtraTypesT, typeof(void)) }, { ([], nameof(IComparable<bool>)), (ExtraTypesT, typeof(IComparable<>)) }, { ([], "IComparableRaw"), (NoExtraTypes, typeof(void)) }, { ([], nameof(IConvertible)), (NoExtraTypes, typeof(IConvertible)) }, { ([], nameof(IEquatable<bool>)), (ExtraTypesT, typeof(IEquatable<>)) }, { ([], "IIncreasable"), (ExtraTypesT, typeof(IIncrementOperators<>)) }, { ([], "IIntegerNumber"), (ExtraTypesT, typeof(IBinaryInteger<>)) }, { ([], "INumber"), (ExtraTypesT, typeof(INumber<>)) }, { ([], "IRealNumber"), (ExtraTypesT, typeof(IFloatingPoint<>)) }, { ([], "ISignedIntegerNumber"), (ExtraTypesT, typeof(ISignedNumber<>)) }, { ([], "IUnsignedIntegerNumber"), (ExtraTypesT, typeof(IUnsignedNumber<>)) }, { ("System.Collections", nameof(ICollection)), (ExtraTypesT, typeof(ICollection<>)) }, { ("System.Collections", "ICollectionRaw"), (NoExtraTypes, typeof(void)) }, { ("System.Collections", "IComparer"), (ExtraTypesT, typeof(G.IComparer<>)) }, { ("System.Collections", nameof(IDictionary)), (["TKey", "TValue"], typeof(G.IDictionary<,>)) }, { ("System.Collections", "IDictionaryRaw"), (NoExtraTypes, typeof(void)) }, { ("System.Collections", nameof(G.IEnumerable<bool>)), (ExtraTypesT, typeof(G.IEnumerable<>)) }, { ("System.Collections", "IEnumerableRaw"), (NoExtraTypes, typeof(void)) }, { ("System.Collections", "IEqualityComparer"), (ExtraTypesT, typeof(G.IEqualityComparer<>)) }, { ("System.Collections", nameof(IList)), (ExtraTypesT, typeof(IList<>)) }, { ("System.Collections", "IListRaw"), (NoExtraTypes, typeof(void)) } };

	/// <summary>
	/// Sorted by Class, also contains Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String Interface, List<String> ExtraTypes)>> UserDefinedImplementedInterfacesList { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeDictionary<UserDefinedTypeProperties> UserDefinedPropertiesList { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains list of Names.
	/// </summary>
	public static TypeDictionary<List<String>> UserDefinedPropertiesOrder { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Index.
	/// </summary>
	public static TypeDictionary<Dictionary<String, int>> UserDefinedPropertiesMapping { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains IndexType, Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeSortedList<TypeIndexers> UserDefinedIndexersList { get; } = [];

	/// <summary>
	/// Sorted by Name, also contains ExtraTypes, ReturnType, ReturnUnvType.ExtraTypes, Attributes, ParameterTypes, ParameterNames, ParameterExtraTypes, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static FunctionsList PublicFunctionsList { get; } = new() { { "Abs", new(ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Ceil", new(ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Chain", new(NoExtraTypes, "list", ["int"], FunctionAttributes.Multiconst, [new("int", "start", NoExtraTypes, ParameterAttributes.None, []), new("int", "end", NoExtraTypes, ParameterAttributes.None, [])]) }, { "Choose", new(NoExtraTypes, "universal", NoExtraTypes, FunctionAttributes.None, [new("universal", "variants", NoExtraTypes, ParameterAttributes.Params, [])]) }, { "Clamp", new(ExtraTypesT, "INumber", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, []), new("real"/*"INumber"*/, "min", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MinValue;\")"), new("real"/*"INumber"*/, "max", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MaxValue;\")")]) }, { "Exp", new(ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Fibonacci", new(NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.Multiconst, [new("int", "n", NoExtraTypes, ParameterAttributes.None, [])]) }, { "Fill", new(ExtraTypesT, "list", ExtraTypesT, FunctionAttributes.Multiconst, [new("T", "element", NoExtraTypes, ParameterAttributes.None, []), new("int", "count", NoExtraTypes, ParameterAttributes.None, [])]) }, { "Floor", new(ExtraTypesT, "int"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Frac", new(ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "IntRandom", new(NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [new("int", "max", NoExtraTypes, ParameterAttributes.None, [])]) }, { "IntToReal", new(ExtraTypesT, "real", NoExtraTypes, FunctionAttributes.Multiconst, [new("T", "x", NoExtraTypes, ParameterAttributes.None, [])]) }, { "Log", new(ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, []), new("real"/*"IRealNumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Max", new(ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "source", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, [])]) }, { "Mean", new(ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "source", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, [])]) }, { "Min", new(ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "source", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, [])]) }, { "Q", new(NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, []) }, { "Random", new(NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.None, [new("real", "max", NoExtraTypes, ParameterAttributes.None, [])]) }, { "RealRemainder", new(ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, []), new("real", "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "RGB", new(NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.Multiconst, [new("byte", "red", NoExtraTypes, ParameterAttributes.None, []), new("byte", "green", NoExtraTypes, ParameterAttributes.None, []), new("byte", "blue", NoExtraTypes, ParameterAttributes.None, [])]) }, { "Round", new(ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, []), new("int", "digits_after_dot", NoExtraTypes, ParameterAttributes.Optional, "0")]) }, { "Sign", new(ExtraTypesT, "short int", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Sqrt", new(ExtraTypesT, "real", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) }, { "Truncate", new(ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, [])]) } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ArrayParameters, ReturnType, ReturnArrayParameters, Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeSortedList<GeneralMethods> GeneralMethodsList { get; } = new() { { new(), new() { { "ExecuteString", new() { new([], (new([new(BlockType.Primitive, "universal", 1)]), NoGeneralExtraTypes), FunctionAttributes.Multiconst, [GeneralParameterStringS, new(new(new([new(BlockType.Primitive, "universal", 1)]), NoGeneralExtraTypes), "parameters", ParameterAttributes.Params, [])]) } } } } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ArrayParameters, ReturnType, ReturnArrayParameters, Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<UserDefinedMethods> UserDefinedFunctionsList { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedFunctionIndexesList { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<ConstructorOverloads> UserDefinedConstructorsList { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedConstructorIndexesList { get; } = [];

	/// <summary>
	/// Sorted by Operator, also contains Postfix modifiers, ReturnTypes, ReturnUnvType.ExtraTypes, OpdTypes and OpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, UnaryOperatorClasses> UnaryOperatorsList { get; } = new() { { "+", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "INumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { "-", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IRealNumber", 1)]), GeneralExtraTypesT)) } } } }/*, { "++", new(new BlockStackComparer()){ { "IBase", new() { (false, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoGeneralExtraTypes, "IBase", GeneralExtraTypesT), (true, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoGeneralExtraTypes, "IBase", GeneralExtraTypesT) } } } }, { "--", new(new BlockStackComparer()){ { "IBase", new() { (false, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoGeneralExtraTypes, "IBase", GeneralExtraTypesT), (true, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoGeneralExtraTypes, "IBase", GeneralExtraTypesT) } } } }*/, { "!", new(new BlockStackComparer()) { { GeneralTypeBool, new() { (false, (GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes)) } } } }/*, { "!!", new(new BlockStackComparer()){ { GetPrimitiveBlockStack("bool"), new() { (true, GetPrimitiveBlockStack("bool"), NoGeneralExtraTypes, GetPrimitiveBlockStack("bool"), NoGeneralExtraTypes) } } } }*/, { "~", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), GeneralExtraTypesT)) } } } } };

	/// <summary>
	/// Sorted by Operator, also contains ReturnTypes, ReturnUnvType.ExtraTypes, LeftOpdTypes and LeftOpdExtraTypes, RightOpdTypes and RightOpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, BinaryOperatorClasses> BinaryOperatorsList { get; } = new() { { "+", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (GeneralTypeInt, NoGeneralExtraTypes)), ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GeneralTypeInt, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } }, { GeneralTypeString, new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GetPrimitiveBlockStack("char"), NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes)), ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GeneralTypeList, [new(GetPrimitiveType("char"))]), (GeneralTypeString, NoGeneralExtraTypes)), ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes), (GetPrimitiveBlockStack("char"), NoGeneralExtraTypes)), ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes), (GeneralTypeList, [new(GetPrimitiveType("char"))])), ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes)) } } } }, { "-", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (GeneralTypeInt, NoGeneralExtraTypes)), ((GeneralTypeInt, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { "*", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } }, { GeneralTypeString, new() { ((GeneralTypeString, NoGeneralExtraTypes), (GeneralTypeInt, NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes)), ((GeneralTypeString, NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes), (GeneralTypeInt, NoGeneralExtraTypes)) } } } }, { "/", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IRealNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IRealNumber", 1)]), GeneralExtraTypesT)) } } } }, { "pow", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (GeneralTypeInt, NoGeneralExtraTypes)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IRealNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IRealNumber", 1)]), GeneralExtraTypesT)) } } } }, { "==", new(new BlockStackComparer()) { { GetPrimitiveBlockStack("object"), new() { ((new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes)) } } } }, { ">", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { "<", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { ">=", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { "<=", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIncreasable", 1)]), GeneralExtraTypesT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoGeneralExtraTypes), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "INumber", 1)]), GeneralExtraTypesT)) } } } }, { "!=", new(new BlockStackComparer()) { { GetPrimitiveBlockStack("object"), new() { ((new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "object", 1)]), NoGeneralExtraTypes)) } } } }, { ">>", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (GeneralTypeInt, NoGeneralExtraTypes)) } } } }, { "<<", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (GeneralTypeInt, NoGeneralExtraTypes)) } } } }, { "&", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT)) } } } }, { "|", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT)) } } } }, { "^", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), GeneralExtraTypesT)) } } } }, { "&&", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes)) } } } }, { "||", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes)) } } } }, { "^^", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes), (GeneralTypeBool, NoGeneralExtraTypes)) } } } } };

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, int Value)>> EnumConstantsList { get; } = new() { { new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), new() { ("Local", (int)DateTimeKind.Local), ("Unspecified", (int)DateTimeKind.Unspecified), ("UTC", (int)DateTimeKind.Utc) } }, { new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DayOfWeek", 1)]), new() { ("Friday", (int)DayOfWeek.Friday), ("Monday", (int)DayOfWeek.Monday), ("Saturday", (int)DayOfWeek.Saturday), ("Sunday", (int)DayOfWeek.Sunday), ("Thursday", (int)DayOfWeek.Thursday), ("Tuesday", (int)DayOfWeek.Tuesday), ("Wednesday", (int)DayOfWeek.Wednesday) } } };

	/// <summary>
	/// Sorted by Container, also contains Name, Type and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, UniversalType Type, dynamic Value)>> OtherConstantsList { get; } = new() { { GetPrimitiveBlockStack("byte"), new() { ("MaxValue", (new([new(BlockType.Interface, "byte", 1)]), NoGeneralExtraTypes), byte.MaxValue), ("MinValue", (new([new(BlockType.Interface, "byte", 1)]), NoGeneralExtraTypes), byte.MinValue) } }, { GetPrimitiveBlockStack("char"), new() { ("MaxValue", (new([new(BlockType.Interface, "char", 1)]), NoGeneralExtraTypes), char.MaxValue), ("MinValue", (new([new(BlockType.Interface, "char", 1)]), NoGeneralExtraTypes), char.MinValue) } }, { GeneralTypeInt, new() { ("MaxValue", (new([new(BlockType.Interface, "int", 1)]), NoGeneralExtraTypes), int.MaxValue), ("MinValue", (new([new(BlockType.Interface, "int", 1)]), NoGeneralExtraTypes), int.MinValue) } }, { new([new(BlockType.Primitive, "long char", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "long char", 1)]), NoGeneralExtraTypes), uint.MaxValue), ("MinValue", (new([new(BlockType.Interface, "long char", 1)]), NoGeneralExtraTypes), uint.MinValue) } }, { new([new(BlockType.Primitive, "long int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "long int", 1)]), NoGeneralExtraTypes), long.MaxValue), ("MinValue", (new([new(BlockType.Interface, "long int", 1)]), NoGeneralExtraTypes), long.MinValue) } }, { GetPrimitiveBlockStack("real"), new() { ("MaxValue", (new([new(BlockType.Interface, "real", 1)]), NoGeneralExtraTypes), double.MaxValue), ("MinPosValue", (new([new(BlockType.Interface, "real", 1)]), NoGeneralExtraTypes), double.Epsilon), ("MinValue", (new([new(BlockType.Interface, "real", 1)]), NoGeneralExtraTypes), double.MinValue) } }, { new([new(BlockType.Primitive, "short char", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "short char", 1)]), NoGeneralExtraTypes), byte.MaxValue), ("MinValue", (new([new(BlockType.Interface, "short char", 1)]), NoGeneralExtraTypes), byte.MinValue) } }, { new([new(BlockType.Primitive, "short int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "short int", 1)]), NoGeneralExtraTypes), short.MaxValue), ("MinValue", (new([new(BlockType.Interface, "short int", 1)]), NoGeneralExtraTypes), short.MinValue) } }, { new([new(BlockType.Primitive, "unsigned int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned int", 1)]), NoGeneralExtraTypes), uint.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned int", 1)]), NoGeneralExtraTypes), uint.MinValue) } }, { new([new(BlockType.Primitive, "unsigned long int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned long int", 1)]), NoGeneralExtraTypes), ulong.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned long int", 1)]), NoGeneralExtraTypes), ulong.MinValue) } }, { new([new(BlockType.Primitive, "unsigned short int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned short int", 1)]), NoGeneralExtraTypes), ushort.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned short int", 1)]), NoGeneralExtraTypes), ushort.MinValue) } } };

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, UniversalType Type, dynamic Value)>> UserDefinedConstantsList { get; } = [];

	/// <summary>
	/// Sorted by SrcType, also contains SrcUnvType.ExtraTypes, DestTypes and their DestUnvType.ExtraTypes.
	/// </summary>
	public static TypeSortedList<ImplicitConversions> ImplicitConversionsList { get; } = new() { { GeneralTypeBool, new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (ByteType, false) } } } }, { GetPrimitiveBlockStack("byte"), new() { { NoGeneralExtraTypes, new() { (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (GetPrimitiveType("short char"), true), (BoolType, true) } } } }, { GetPrimitiveBlockStack("char"), new() { { NoGeneralExtraTypes, new() { (UnsignedShortIntType, false), (StringType, false) } } } }, { GeneralTypeInt, new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (IndexType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true) } } } }, { GeneralTypeList, new() { { new() { GetPrimitiveType("char") }, new() { (StringType, false) } } } }, { GetPrimitiveBlockStack("long char"), new() { { NoGeneralExtraTypes, new() { (UnsignedIntType, false) } } } }, { GetPrimitiveBlockStack("long int"), new() { { NoGeneralExtraTypes, new() { (UnsignedLongIntType, false), (BoolType, true), (UnsignedShortIntListType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("real"), new() { { NoGeneralExtraTypes, new() { (BoolType, true), (UnsignedLongIntType, true), (LongIntType, true), (UnsignedIntType, true), (IntType, true) } } } }, { GetPrimitiveBlockStack("short char"), new() { { NoGeneralExtraTypes, new() { (ByteType, false) } } } }, { GetPrimitiveBlockStack("short int"), new() { { NoGeneralExtraTypes, new() { (LongIntType, false), (RealType, false), (IntType, false), (UnsignedShortIntType, false), (BoolType, true), (ByteType, true), (UnsignedIntType, true), (UnsignedLongIntType, true) } } } }, { GeneralTypeString, new() { { NoGeneralExtraTypes, new() { ((GeneralTypeList, [new(GetPrimitiveType("char"))]), false) } } } }, { GetPrimitiveBlockStack("unsigned int"), new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (IntType, true) } } } }, { GetPrimitiveBlockStack("unsigned long int"), new() { { NoGeneralExtraTypes, new() { (BoolType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (LongIntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("unsigned short int"), new() { { NoGeneralExtraTypes, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (BoolType, true), (ByteType, true), (ShortIntType, true) } } } } };

	/// <summary>
	/// Sorted by tuple, contains DestType and DestUnvType.ExtraTypes.
	/// </summary>
	public static List<UniversalType> ImplicitConversionsFromAnythingList { get; } = [(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), (GetPrimitiveBlockStack("null"), NoGeneralExtraTypes), (GeneralTypeList, [new(new BlockStack([new(BlockType.Primitive, "[this]", 1)]), NoGeneralExtraTypes)])];

	public static G.SortedSet<String> NotImplementedNamespacesList { get; } = ["System.Diagnostics", "System.Globalization", "System.IO", "System.Runtime", "System.Text", "System.Threading", "System.Windows", "System.Windows.Forms"];

	/// <summary>
	/// Sorted by Namespace, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedNamespacesList { get; } = new() { { "System.Collections.Generic", "System.Collections" }, { "System.Linq", "Hawaytenn operators" } };

	public static G.SortedSet<String> ReservedNamespacesList { get; } = ["Microsoft", "System.Activities", "System.AddIn", "System.CodeDom", "Concurrent", "ObjectModel", "Specialized", "System.ComponentModel", "System.Configuration", "System.Data", "System.Deployment", "System.Device", "System.Diagnostics.CodeAnalysis", "System.Diagnostics.Contracts", "System.Diagnostics.Design", "System.Diagnostics.Eventing", "System.Diagnostics.PerformanceData", "System.Diagnostics.SymbolStore", "System.Diagnostics.Tracing", "System.DirectoryServices", "System.Drawing", "System.Dynamic", "System.EnterpriseServices", "System.IdentityModel", "System.IO.Compression", "System.IO.IsolatedStorage", "System.IO.Log", "System.IO.MemoryMappedFiles", "System.IO.Packaging", "System.IO.Pipes", "System.IO.Ports", "System.Management", "System.Media", "System.Messaging", "System.Net", "System.Numerics", "System.Printing", "System.Reflection", "System.Resources", "System.Runtime.Caching", "System.Runtime.CompilerServices", "System.Runtime.ConstrainedExecution", "System.Runtime.DesignerServices", "System.Runtime.ExceptionServices", "System.Runtime.Hosting", "System.Runtime.InteropServices", "System.Runtime.Remoting", "System.Runtime.Serialization", "System.Runtime.Versioning", "System.Security", "System.ServiceModel", "System.ServiceProcess", "System.Speech", "System.StubHelpers", "System.Text.RegularExpressions", "System.Threading.Tasks", "System.Timers", "System.Transactions", "System.Web", "System.Windows.Annotations", "System.Windows.Automation", "System.Windows.Baml2006", "System.Windows.Controls", "System.Windows.Data", "System.Windows.Documents", "System.Windows.Forms.ComponentModel", "System.Windows.Forms.DataVisualization", "System.Windows.Forms.Design", "System.Windows.Forms.Interaction", "System.Windows.Forms.Layout", "System.Windows.Forms.PropertyGridInternal", "System.Windows.Forms.VisualStyles", "System.Windows.Ink", "System.Windows.Input", "System.Windows.Interop", "System.Windows.Markup", "System.Windows.Media", "System.Windows.Navigation", "System.Windows.Resources", "System.Windows.Shapes", "System.Windows.Threading", "System.Windows.Xps", "System.Workflow", "System.Xaml", "System.Xml", "Windows", "XamlGeneratedNamespace"];

	public static G.SortedSet<(String Namespace, String Type)> NotImplementedTypesList { get; } = [([], "complex"), ([], "long complex"), ([], "long long"), ([], "long real"), ("System", "Delegate"), ("System", "Enum"), ("System", "Environment"), ("System", "OperatingSystem")];

	/// <summary>
	/// Sorted by Namespace and Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), string> OutdatedTypesList { get; } = new() { { ([], "*Exception"), "\"if error ...\"" }, { ([], "double"), "real or long real" }, { ([], "float"), "real or long real" }, { ([], "uint"), "unsigned int" }, { ([], "ulong"), "unsigned long int" }, { ([], "ushort"), "unsigned short int" }, { ("System", "Array"), "list" }, { ("System", "Boolean"), "bool" }, { ("System", "Byte"), "byte (from the small letter)" }, { ("System", "Char"), "char (from the small letter), short char or long char" }, { ("System", "Console"), "labels and textboxes" }, { ("System", "ConsoleCancelEventArgs"), "TextBox.KeyDown, TextBox.KeyPress and TextBox.KeyUp" }, { ("System", "ConsoleCancelEventHandler"), "TextBox keyboard events" }, { ("System", "ConsoleColor"), "RichTextBox text color" }, { ("System", "ConsoleKey"), "other item enums" }, { ("System", "ConsoleKeyInfo"), "other item info classes" }, { ("System", "ConsoleModifiers"), "other item modifiers enums" }, { ("System", "ConsoleSpecialKey"), "other item enums" }, { ("System", "Double"), "real or long real" }, { ("System", "Int16"), "short int" }, { ("System", "Int32"), "int" }, { ("System", "Int64"), "long int" }, { ("System", "Object"), "object (from the small letter)" }, { ("System", "Random"), "Random(), IntRandom() etc." }, { ("System", "SByte"), "byte or short int" }, { ("System", "Single"), "real or long real" }, { ("System", "String"), "string (from the small letter)" }, { ("System", "Type"), "typename" }, { ("System", "UInt16"), "unsigned short int" }, { ("System", "UInt32"), "unsigned int" }, { ("System", "UInt64"), "unsigned long int" }, { ("System", "Void"), "null" }, { ("System.Collections", "BitArray"), "BitList" }, { ("System.Collections", "HashSet"), "ListHashSet" }, { ("System.Collections", "Hashtable"), "HashTable" }, { ("System.Collections", "KeyValuePair"), "tuples" }, { ("System.Collections", "SortedSet"), "SortedSet" } };

	public static G.SortedSet<(String Namespace, String Type)> ReservedTypesList { get; } = [([], "*Attribute"), ([], "*Comparer"), ([], "*Enumerator"), ([], "*UriParser"), ([], "decimal"), ("System", "ActivationContext"), ("System", "ActivationContext.ContextForm"), ("System", "Activator"), ("System", "AppContext"), ("System", "AppDomain"), ("System", "AppDomainInitializer"), ("System", "AppDomainManager"), ("System", "AppDomainManagerInitializationOptions"), ("System", "AppDomainSetup"), ("System", "ApplicationId"), ("System", "ApplicationIdentity"), ("System", "ArgIterator"), ("System", "ArraySegment"), ("System", "AssemblyLoadEventArgs"), ("System", "AsyncCallback"), ("System", "AttributeTargets"), ("System", "Base64FormattingOptions"), ("System", "BitConverter"), ("System", "Buffer"), ("System", "Comparison"), ("System", "ContextBoundObject"), ("System", "ContextStaticAttribute"), ("System", "Convert"), ("System", "Converter"), ("System", "CrossAppDomainDelegate"), ("System", "DateTimeOffset"), ("System", "DBNull"), ("System", "Decimal"), ("System", "Environment.SpecialFolder"), ("System", "Environment.SpecialFolderOption"), ("System", "EnvironmentVariableTarget"), ("System", "EventArgs"), ("System", "EventHandler"), ("System", "FormattableString"), ("System", "GC"), ("System", "GCCollectionMode"), ("System", "GCNotificationStatus"), ("System", "GenericUriParserOptions"), ("System", "Guid"), ("System", "IAppDomainSetup"), ("System", "IAsyncResult"), ("System", "ICloneable"), ("System", "ICustomFormattable"), ("System", "IDisposable"), ("System", "IFormatProvider"), ("System", "IFormattable"), ("System", "IObservable"), ("System", "IObserver"), ("System", "IProgress"), ("System", "IServiceProvider"), ("System", "Lazy"), ("System", "LoaderOptimization"), ("System", "LocalDataStoreSlot"), ("System", "MarshalByRefObject"), ("System", "Math"), ("System", "MidpointRounding"), ("System", "ModuleHandle"), ("System", "MulticastDelegate"), ("System", "Nullable"), ("System", "PlatformID"), ("System", "Progress"), ("System", "ResolveEventArgs"), ("System", "ResolveEventHandler"), ("System", "RuntimeArgumentHandle"), ("System", "RuntimeFieldHandle"), ("System", "RuntimeMethodHandle"), ("System", "RuntimeTypeHandle"), ("System", "StringComparer"), ("System", "StringComparison"), ("System", "StringSplitOptions"), ("System", "TimeZone"), ("System", "TimeZoneInfo"), ("System", "TimeZoneInfo.AdjustmentRule"), ("System", "TimeZoneInfo.TransitionTime"), ("System", "Tuple"), ("System", "TupleExtensions"), ("System", "TypeCode"), ("System", "TypedReference"), ("System", "UIntPtr"), ("System", "Uri"), ("System", "UriBuilder"), ("System", "UriComponents"), ("System", "UriFormat"), ("System", "UriHostNameType"), ("System", "UriIdnScope"), ("System", "UriKind"), ("System", "UriPartial"), ("System", "UriTemplate"), ("System", "UriTemplateEquivalenceComparer"), ("System", "UriTemplateMatch"), ("System", "UriTemplateTable"), ("System", "UriTypeConverter"), ("System", "ValueTuple"), ("System", "ValueType"), ("System", "Version"), ("System", "WeakReference"), ("System", "_AppDomain"), ("System.Collections", "ArrayList"), ("System.Collections", "CaseInsensitiveHashCodeProvider"), ("System.Collections", "CollectionBase"), ("System.Collections", "Dictionary.KeyCollection"), ("System.Collections", "Dictionary.ValueCollection"), ("System.Collections", "DictionaryBase"), ("System.Collections", "DictionaryEntry"), ("System.Collections", "IHashCodeProvider"), ("System.Collections", "IReadOnlyCollection"), ("System.Collections", "IReadOnlyDictionary"), ("System.Collections", "IReadOnlyList"), ("System.Collections", "ISet"), ("System.Collections", "IStructuralComparable"), ("System.Collections", "IStructuralEquatable"), ("System.Collections", "KeyedByTypeCollection"), ("System.Collections", "ReadOnlyCollectionBase"), ("System.Collections", "StructuralComparisons"), ("System.Collections", "SynchronizedCollection"), ("System.Collections", "SynchronizedKeyedCollection"), ("System.Collections", "SynchronizedReadOnlyCollection")];

	public static G.SortedSet<String> NotImplementedTypeEndsList { get; } = [];

	/// <summary>
	/// Sorted by Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedTypeEndsList { get; } = new() { { "Exception", "\"if error ...\"" } };

	public static G.SortedSet<String> ReservedTypeEndsList { get; } = ["Attribute", "Comparer", "Enumerator", "UriParser"];

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> NotImplementedMembersList { get; } = new() { { new([new(BlockType.Interface, "DateTime", 1)]), new() { "AddRange", "Subtract" } } };

	/// <summary>
	/// Sorted by Container, then by Member, also contains UseInstead.
	/// </summary>
	public static TypeSortedList<SortedDictionary<String, String>> OutdatedMembersList { get; } = new() { { GeneralTypeBool, new() { { "FalseString", "literal \"false\"" }, { "Parse", "implicit conversion" }, { "TrueString", "literal \"true\"" }, { "TryParse", "implicit conversion" } } }, { GetPrimitiveBlockStack("DateTime"), new() { { "IsDaylightSavingTime", "IsSummertime" }, { "Parse", "implicit conversion" }, { "TryParse", "implicit conversion" } } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { { "Parse", "implicit conversion" }, { "TryParse", "implicit conversion" } } }, { GeneralTypeList, new() { { "Length", "Length" } } }, { GetPrimitiveBlockStack("object"), new() { { "Equals", "==" } } } };

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> ReservedMembersList { get; } = new() { { new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Action", 1)]), new() { "BeginInvoke", "EndInvoke", "Invoke" } }, { GetPrimitiveBlockStack("DateTime"), new() { "FromBinary", "FromFileTime", "FromFileTimeUtc", "FromOADate", "GetDateTimeFormats", "ParseExact", "ToFileTime", "ToFileTimeUtc", "ToLongDateString", "ToLongTimeString", "ToOADate", "ToShortDateString", "ToShortTimeString", "TryParseExact" } }, { new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]), new() { "BeginInvoke", "EndInvoke", "Invoke" } }, { new([new(BlockType.Interface, "IChar", 1)]), new() { "ConvertFromUtf32", "ConvertToUtf32", "GetNumericValue", "GetUnicodeCategory", "IsControl", "IsHighSurrogate", "IsLowSurrogate", "IsNumber", "IsPunctuation", "IsSurrogate", "IsSurrogatePair", "IsSymbol", "ToLowerInvariant", "ToUpperInvariant" } }, { GeneralTypeList, new() { "AsReadOnly", "Capacity", "ConvertAll", "ForEach", "GetEnumerator", "TrimExcess" } }, { GetPrimitiveBlockStack("object"), new() { "GetType", "GetTypeCode", "ReferenceEquals" } }, { new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Predicate", 1)]), new() { "BeginInvoke", "EndInvoke", "Invoke" } }, { GeneralTypeString, new() { "Clone", "Copy", "CopyTo", "Empty", "Format", "GetEnumerator", "Intern", "IsInterned", "IsNormalized", "IsNullOrEmpty", "IsNullOrWhiteSpace", "Normalize", "ToLowerInvariant", "ToUpperInvariant" } } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes, ParameterDefaultValues and UseInstead suggestions.
	/// </summary>
	public static TypeSortedList<OutdatedMethods> OutdatedMethodOverloadsList { get; } = new() { { new([new(BlockType.Interface, "IChar", 1)]), new() { { "IsDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLetter", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLetterOrDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLower", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsSeparator", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsUpper", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsWhiteSpace", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } } } }, { GeneralTypeString, new() { { "Concat", new() { ([GeneralParameterString1, GeneralParameterString2, GeneralParameterString3, new(StringType, "string4", ParameterAttributes.None, [])], "concatenation in pairs, triples or in an array"), ([new(new(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), "object1", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), "object2", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), "object3", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), "object4", ParameterAttributes.None, [])], "concatenation in pairs, triples or in an array") } } } } };

	/// <summary>
	/// Sorted by Container, also contains ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes, ParameterDefaultValues and UseInstead suggestions.
	/// </summary>
	public static SortedDictionary<String, OutdatedMethodOverloads> OutdatedConstructorsList { get; } = [];

	public static G.SortedSet<String> NotImplementedOperatorsList { get; } = ["<<<", ">>>", "?!=", "?<", "?<=", "?=", "?>", "?>="];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	/// <summary>
	/// Sorted by OperandsCount and Operator, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedOperatorsList { get; } = [];

	public static G.SortedSet<String> ReservedOperatorsList { get; } = ["#", "G", "I", "K", "_", "g", "hexa", "hexa=", "penta", "penta=", "tetra", "tetra="];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	public static bool TypesAreEqual(UniversalType type1, UniversalType type2)
	{
		if (type1.MainType.Length != type2.MainType.Length)
			return false;
		for (var i = 0; i < type1.MainType.Length; i++)
		{
			if (type1.MainType.ElementAt(i).BlockType != type2.MainType.ElementAt(i).BlockType || type1.MainType.ElementAt(i).Name != type2.MainType.ElementAt(i).Name)
				return false;
		}
		if (type1.ExtraTypes.Length == 0)
		{
			if (type2.ExtraTypes.Length != 0)
				return false;
			return true;
		}
		if (type1.ExtraTypes.Length != type2.ExtraTypes.Length)
			return false;
		for (var i = 0; i < type1.ExtraTypes.Length; i++)
		{
			if (type1.ExtraTypes[i].MainType.IsValue ? !(type2.ExtraTypes[i].MainType.IsValue && type1.ExtraTypes[i].MainType.Value == type2.ExtraTypes[i].MainType.Value) : (type2.ExtraTypes[i].MainType.IsValue || !TypesAreEqual((type1.ExtraTypes[i].MainType.Type, type1.ExtraTypes[i].ExtraTypes), (type2.ExtraTypes[i].MainType.Type, type2.ExtraTypes[i].ExtraTypes))))
				return false;
		}
		return true;
	}

	public static bool TypeEqualsToPrimitive(UniversalType type, String primitive, bool noExtra = true) => TypeIsPrimitive(type.MainType) && type.MainType.Peek().Name == primitive && (!noExtra || type.ExtraTypes.Length == 0);

	public static bool TypeIsPrimitive(BlockStack type) => type is null || type.Length == 1 && type.Peek().BlockType == BlockType.Primitive;

	public static UniversalType GetPrimitiveType(String primitive) => (new([new(BlockType.Primitive, primitive, 1)]), NoGeneralExtraTypes);

	public static BlockStack GetPrimitiveBlockStack(String primitive) => new([new(BlockType.Primitive, primitive, 1)]);

	public static BlockStack GetBlockStack(String basic)
	{
		var typeName = basic.Copy();
		var namespace_ = typeName.GetBeforeSetAfterLast(".");
		var split = namespace_.Split('.');
		if (PrimitiveTypesList.ContainsKey(basic))
			return GetPrimitiveBlockStack(basic);
		else if (ExtraTypesList.TryGetValue((namespace_, typeName), out var netType))
			return new([.. split.Convert(x => new Block(BlockType.Namespace, x, 1)),
				new(netType.IsClass ? BlockType.Class : netType.IsValueType
				? BlockType.Struct : typeof(Delegate).IsAssignableFrom(netType) ? BlockType.Delegate
				: throw new InvalidOperationException(), typeName, 1)]);
		else if (InterfacesList.TryGetValue((namespace_, typeName), out var value) && value.DotNetType.IsInterface)
			return new([.. split.Convert(x => new Block(BlockType.Namespace, x, 1)),
				new(BlockType.Interface, typeName, 1)]);
		else if (basic.ToString() is nameof(Action) or nameof(Func<bool>))
			return new([new(BlockType.Delegate, basic, 1)]);
		else
			throw new InvalidOperationException();
	}

	public static UniversalType GetListType(UniversalTypeOrValue InnerType)
	{
		if (InnerType.MainType.IsValue || !TypeEqualsToPrimitive((InnerType.MainType.Type, InnerType.ExtraTypes), "list", false))
			return new(ListBlockStack, new([InnerType]));
		else if (InnerType.ExtraTypes.Length >= 2 && InnerType.ExtraTypes[0].MainType.IsValue && int.TryParse(InnerType.ExtraTypes[0].MainType.Value.ToString(), out var number))
			return new(ListBlockStack, new([new((TypeOrValue)(number + 1).ToString(), []), InnerType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new((TypeOrValue)"2", []), InnerType.ExtraTypes[^1]]));
	}

	public static void GenerateMessage(ref List<String>? errorsList, ushort code, int line, int column,
		params dynamic[] parameters) => GenerateMessage(errorsList ??= [], code, line, column, parameters);

	public static void GenerateMessage(List<String> errorsList, ushort code, int line, int column, params dynamic[] parameters)
	{
		var codeString = Convert.ToString(code, 16).ToUpper().PadLeft(4, '0');
		errorsList.Add(codeString[0] switch
		{
			>= '0' and <= '7' => "Error ",
			'8' => "Warning ",
			'9' => "Wreck ",
			'F' => "Technical wreck ",
			_ => throw new InvalidOperationException(),
		} + codeString + " in line " + line.ToString() + " at position " + column.ToString() + ": " + code switch
		{
			0x0000 => "unexpected end of code reached",
			0x0001 => "too large number; long long type is under development",
			0x0002 => "unrecognized escape-sequence",
			0x0003 => "unrecognized sequence of symbols",
			0x0004 => "expected: identifier",
			0x0005 => "incorrect word or order of words in construction declaration",
			0x0006 => "the class \"" + parameters[0] + "\" is the standard root C#.NStar type and cannot be redefined",
			0x0007 => "the class \"" + parameters[0] + "\" is already defined in this region",
			0x0008 => "the class \"" + parameters[0] + "\" is nearest to this outer class",
			0x0009 => "a static class cannot be derived",
			0x000A => "the static functions are allowed only inside the classes",
			0x000B => "the function \"" + parameters[0] + "\" is the standard root C#.NStar function and cannot be redefined",
			0x000C => "the function \"" + parameters[0] + "\" is already defined in this region;" +
				" overloaded functions are under development",
			0x000D => "the function cannot have same name as its container class",
			0x000E => "\";\" must follow the abstract function declaration, \"{\" - the non-abstract one",
			0x000F => "the constructors are allowed only inside the classes",
			0x0010 => parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x0011 => "expected: {",
			0x2000 => "unexpected end of code reached",
			0x2001 => "expected: identifier",
			0x2002 => "expected: \";\"",
			0x2003 => "expected: {",
			0x2004 => "expected: }",
			0x2005 => "expected: methods or }",
			0x2006 => "expected: \":\"",
			0x2007 => "unrecognized construction",
			0x2008 => "expected: " + parameters[0],
			0x2009 => throw new InvalidOperationException(),
			0x200A => "expected: (",
			0x200B => "expected: )",
			0x200C => "expected: [",
			0x200D => "expected: ]",
			0x200E => "expected: expression",
			0x200F => "expected: \"if\" or \"else\"",
			0x2010 => "expected: \"loop\" or \"while\" or \"repeat\" or \"for\"",
			0x2011 => "expected: \"continue\" or \"break\"",
			0x2012 => "expected: identifier or basic expression or expression in round brackets",
			0x2013 => "expected: \"in\"",
			0x2014 => "expected: \"null\" or identifier or tuple",
			0x2015 => "expected: non-sealed class or interface",
			0x2016 => "expected: int number; variables and complex expressions at this place are under development",
			0x2017 => "expected: int number or ); variables and complex expressions at this place are under development",
			0x2018 => "expected: comma; chain of indexes is not ended",
			0x2019 => "incorrect construction in parameters list",
			0x201A => "the parameter with the \"params\" keyword must be last in the list",
			0x201B => "the parameters without the default value and without the \"params\" modifier must appear" +
				" before the parameters with the default value" + (parameters[0] ? "; expected: identifier" : ""),
			0x201C => "at present time the \"is\" operator can be used only with \"null\" and the types",
			0x201D => "only the variables can be assigned",
			0x201E => "the keyword \"" + parameters[0] + "\" is under development",
			0x201F => "the division by the integer zero is forbidden",
			0x2020 => "incorrect word or order of words in construction declaration",
			0x2021 => "too many nested collections of types",
			0x2022 => "the keyword \"var\" is not a type and cannot be used inside the type",
			0x2023 => "cannot create an instance of the abstract type \"" + parameters[0] + "\"",
			0x2024 => "cannot create an instance of the static type \"" + parameters[0] + "\"",
			0x2025 => "incorrect construction after type name",
			0x2026 => "the type \"" + parameters[0] + "\" is not defined in this location",
			0x2027 => "incorrect type with \"short\" or \"long\" word",
			0x2028 => "incorrect type with \"unsigned\" word",
			0x2029 => throw new InvalidOperationException(),
			0x202A => "the identifier \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x202B => "the end of identifier \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x202C => "the namespace \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x202D => "the type \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x202E => "the end of identifier \"" + parameters[0] + "\" is outdated, consider using "
				+ parameters[1] + " instead",
			0x202F => "the properties can only have the proper access, no public",
			0x2030 => "the \"new\" keyword is forbidden here",
			0x2031 => "the \"new\" keyword with implicit type is under development",
			0x2032 => "the method \"" + parameters[0] + "\" with these parameter types is already defined in this region",
			0x203A => "the identifier \"" + parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x203B => "the end of identifier \"" + parameters[0] + "\" is reserved for next versions of C#.NStar" +
				" and cannot be used",
			0x2048 => "goto is a bad operator, it worsens the organization of the code;" +
				" C#.NStar refused from its using intentionally",
			0x4000 => "internal compiler error",
			0x4001 => "the identifier \"" + parameters[0] + "\" is not defined in this location",
			0x4002 => "cannot apply this operator to this constant",
			0x4003 => "cannot compute factorial of this constant",
			0x4004 => "the division by the integer zero is forbidden",
			0x4005 => "cannot apply the operator \"" + parameters[0] + "\" to the type \"" + parameters[1] + "\"",
			0x4006 => "cannot apply the operator \"" + parameters[0] + "\" to the types \"" + parameters[1]
				+ "\" and \"" + parameters[2] + "\"",
			0x4007 => "the strings cannot be subtracted",
			0x4008 => "the string cannot be multiplied by the string",
			0x4009 => "the strings cannot be divided or give the remainder (%)",
			0x400A => "the abstract members can be located only inside the abstract classes",
			0x400B => "at present time the index in the tuple must be a compilation-time constant",
			0x400C => "incorrect construction is passing with the \"" + parameters[0] + "\" keyword",
			0x400D => "cannot compute this expression",
			0x400E => "the operator \"" + parameters[0] + "\" requires the third operand",
			0x400F => "cannot use the \"?\" operator in this context",
			0x4010 => "the variable \"" + parameters[0] + "\" is not defined in this location;" +
				" multiconst functions cannot use variables that are outside of the function",
			0x4011 => "the variable declared with the keyword \"var\" must be assigned explicitly and in the same expression",
			0x4012 => "one cannot use the local variable \"" + parameters[0]
				+ "\" before it is declared or inside such declaration in line "
				+ parameters[1] + " at position " + parameters[2],
			0x4013 => "the variable \"" + parameters[0] + "\" is already defined in this location" +
				" or in the location that contains this in line " + parameters[1] + " at position " + parameters[2],
			0x4014 => parameters[0] ?? "cannot convert from the type \"" + parameters[1] + "\" to the type \""
				+ parameters[2] + "\"" + (TypeEqualsToPrimitive(parameters[2], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4015 => "there is no implicit conversion between the types \"" + parameters[0] + "\" and \""
				+ parameters[1] + "\"",
			0x4020 => "the function \"" + parameters[0] + "\" cannot be used in the delegate",
			0x4021 => "the function \"" + parameters[0] + "\" is linked with object instance so it cannot be used in delegate",
			0x4022 => "the function \"" + parameters[0] + "\" must have " + (parameters[1] == parameters[2]
				? parameters[1].ToString() : "from " + parameters[2].ToString() + " to " + parameters[1].ToString())
				+ " parameters",
			0x4023 => "the function \"" + parameters[0] + "\" does not have overlaods with parameters",
			0x4024 => "the function \"" + parameters[0] + "\" is not defined in this location;" +
				" the multiconst functions cannot call the external non-multiconst functions",
			0x4025 => "the function \"" + parameters[0] + "\" cannot be called from this location;" +
				" the static functions cannot call non-static functions",
			0x4026 => parameters[0] ?? "incompatibility between the type of the parameter of the call \"" + parameters[1]
					+ "\" and the type of the parameter of the function \"" + parameters[2] + "\""
					+ (TypeEqualsToPrimitive(parameters[3], "string")
					? " - use an addition of zero-length string for this" : ""),
			0x4027 => parameters[0] ?? "the conversion from the type \"" + parameters[1] + "\" to the type \""
					+ parameters[2] + "\" is possible only in the function return,"
				+ " not in the direct assignment and not in the call",
			0x4028 => "incompatibility between the type of the parameter of the call \""
				+ parameters[0] + "\" and all possible types of the parameter of the function (\""
				+ parameters[1] + "\")" + (parameters[2] == 1 && TypeEqualsToPrimitive(parameters[3], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4029 => "the conversion from the type \"" + parameters[0] + "\" to any of the possible target types (\""
				+ parameters[1] + "\" is possible only in the function return,"
				+ " not in the direct assignment and not in the call",
			0x402A => "this function or lambda must return the value on all execution paths",
			0x4030 => "the property \"" + parameters[0] + "\" is inaccessible from here",
			0x4031 => "the property \"" + parameters[0] + "\" is not defined in this location;" +
				" multiconst functions cannot use external properties",
			0x4032 => "the property \"" + parameters[0] + "\" cannot be used from this location;" +
				" static functions cannot use non-static properties",
			0x4033 => "the type \"" + parameters[0] + "\" does not contain member \"" + parameters[1] + "\"",
			0x4034 => "the type \"" + parameters[0] + "\" does not have constructors with parameters",
			0x4035 => "the constructor of the type \"" + parameters[0] + "\" must have " + (parameters[1] == parameters[2]
				? parameters[1].ToString() : "from " + parameters[2].ToString() + " to " + parameters[1].ToString())
				+ " parameters",
			0x4036 => parameters[0] ?? "incompatibility between the type of the parameter of the call \"" + parameters[1]
					+ "\" and the type of the parameter of the constructor \"" + parameters[2] + "\""
					+ (TypeEqualsToPrimitive(parameters[3], "string")
					? " - use an addition of zero-length string for this" : ""),
			0x4037 => "incompatibility between the type of the parameter of the call \""
				+ parameters[0] + "\" and all possible types of the parameter of the constructor (\""
				+ parameters[1] + "\")" + (parameters[2] == 1 && TypeEqualsToPrimitive(parameters[3], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4038 => "this call is forbidden",
			0x4039 => parameters[0] ?? "incompatibility between the type of the returning value \"" + parameters[1]
				+ "\" and the function return type \"" + parameters[2] + "\""
				+ (TypeEqualsToPrimitive(parameters[2], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4040 => "unexpected lambda expression here",
			0x4041 => "there is no overload of this function with the delegate parameter on this place",
			0x4042 => "incorrect list of the parameters of the lambda expression",
			0x4043 => "incorrect parameter #" + parameters[0] + " of the lambda expression",
			0x4044 => "incorrect type of the lambda expression",
			0x4045 => "this lambda must have " + parameters[0] + " parameters",
			0x8000 => "the properties and the methods are static in the static class implicitly;" +
				" the word \"static\" is not necessary",
			0x8001 => "the semicolon in the end of the line with condition or cycle may easily be unnoticed" +
				" and lead to hard-catchable errors",
			0x8002 => "the syntax \"return;\" is deprecated; consider using \"return null;\" instead",
			0x8003 => "two \"list\" modifiers in a row; consider using the multi-dimensional list instead",
			0x8004 => "the count of the identical types in the tuple cannot be zero; it has been set to 1",
			0x8005 => "the unreachable code has been detected",
			0x8006 => "at present time the word \"internal\" does nothing because C#.NStar does not have multiple assemblies",
			0x8007 => "the variable is assigned to itself - are you sure this is not a mistake?",
			0x8008 => "the method \"" + parameters[0]
				+ "\" has the same parameter types as its base method with the same name but it also" +
				" has the other significant differences such as the access modifier or the return type," +
				" so it cannot override that base method and creates a new one;" +
				" if this is intentional, and the \"new\" keyword, otherwise fix the differences",
			0x8009 => "this expression, used with conditional constructions, is constant;" +
				" maybe you wanted to check equality of these values? - it is done with the operator \"==\"",
			0x800A => parameters[0] ?? "the type of the returning value \"" + parameters[1]
				+ "\" and the function return type \"" + parameters[2] + "\" are badly compatible, you may lost data",
			0x9000 => "unexpected end of code reached; expected: single quote",
			0x9001 => "there must be a single character or a single escape-sequence in the single quotes",
			0x9002 => "unexpected end of code reached; expected: double quote",
			0x9003 => "classic string (not raw or verbatim) must be single-line; expected: double quote",
			0x9004 => "unexpected end of code reached; expected: " + parameters[0]
				+ " pairs \"double quote - reverse slash\" (starting with quote)",
			0x9005 => "unclosed comment in the end of code",
			0x9006 => "unclosed " + parameters[0] + " nested comments in the end of code",
			0x9007 => "unpaired bracket; expected: }",
			0x9008 => "unpaired closing bracket",
			0x9009 => "using namespace is declared not at the beginning of the code",
			0x900A => "expected: identifier",
			0x900B => "namespace \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x900C => "namespace \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x900D => "namespace \"" + parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x900E => "\"" + parameters[0] + "\" is not a valid namespace",
			0x900F => "using \"" + parameters[0] + "\" is already declared",
			0x9010 => "expected: \";\"",
			0x9011 => "unpaired brackets; expected: " + parameters[0],
			0x9012 => "at present time the abstract constructors is forbidden",
			0x9013 => "this parameter must pass with the \"" + parameters[0] + "\" keyword",
			0xF000 => "compilation failed because of internal compiler error",
			_ => throw new InvalidOperationException(),
		});
	}
}

public readonly record struct UniversalType(BlockStack MainType, GeneralExtraTypes ExtraTypes)
{
	public override readonly string ToString()
	{
		if (TypeEqualsToPrimitive(this, "list", false))
			return "list(" + (ExtraTypes.Length == 2 ? ExtraTypes[0].ToString() : "") + ") " + ExtraTypes[^1].ToString();
		else if (TypeEqualsToPrimitive(this, "tuple", false))
		{
			if (ExtraTypes.Length == 0)
				return "()";
			var prev = new UniversalType(ExtraTypes[0].MainType.Type, ExtraTypes[0].ExtraTypes);
			if (ExtraTypes.Length == 1)
				return prev.ToString();
			String result = [];
			var repeats = 1;
			for (var i = 1; i < ExtraTypes.Length; i++)
			{
				var current = new UniversalType(ExtraTypes[i].MainType.Type, ExtraTypes[i].ExtraTypes);
				if (TypesAreEqual(prev, current))
				{
					repeats++;
					continue;
				}
				if (result.Length == 0)
					result.Add('(');
				else
					result.AddRange(", ");
				result.AddRange(prev.ToString());
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

	public static implicit operator UniversalType((BlockStack MainType, GeneralExtraTypes ExtraTypes) value) => new(value.MainType, value.ExtraTypes);
}

public record struct UniversalTypeOrValue(TypeOrValue MainType, GeneralExtraTypes ExtraTypes)
{
	public UniversalTypeOrValue(UniversalType value) : this(value.MainType, value.ExtraTypes) { }
	public override readonly string ToString() => MainType.IsValue ? MainType.Value.ToString() : new UniversalType(MainType.Type, ExtraTypes).ToString();
	public static implicit operator UniversalTypeOrValue((TypeOrValue MainType, GeneralExtraTypes ExtraTypes) value) => new(value.MainType, value.ExtraTypes);
	public static implicit operator UniversalTypeOrValue(UniversalType value) => new(value);
}

public sealed class BlockComparer : G.IComparer<Block>
{
	public int Compare(Block? x, Block? y)
	{
		if (x is null || y is null)
			return (x is null ? 1 : 0) - (y is null ? 1 : 0);
		if (x.BlockType < y.BlockType)
			return 1;
		else if (x.BlockType > y.BlockType)
			return -1;
		return x.Name.ToString().CompareTo(y.Name.ToString());
	}
}

public sealed class BlockStackComparer : G.IComparer<BlockStack>
{
	public int Compare(BlockStack? x, BlockStack? y)
	{
		if (x is null || y is null)
			return (x is null ? 1 : 0) - (y is null ? 1 : 0);
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			var comp = new BlockComparer().Compare(x.ElementAt(i), y.ElementAt(i));
			if (comp != 0)
				return comp;
		}
		if (x.Length < y.Length)
			return 1;
		else if (x.Length > y.Length)
			return -1;
		return 0;
	}
}

public sealed class BlockStackAndStringComparer : G.IComparer<(BlockStack, String)>
{
	public int Compare((BlockStack, String) x, (BlockStack, String) y)
	{
		var comp = new BlockStackComparer().Compare(x.Item1, y.Item1);
		if (comp != 0)
			return comp;
		else
			return x.Item2.ToString().CompareTo(y.Item2.ToString());
	}
}

public sealed class BlockEComparer : G.IEqualityComparer<Block>
{
	public bool Equals(Block? x, Block? y) => x is null && y is null || x is not null && y is not null && x.BlockType == y.BlockType && x.Name == y.Name;

	public int GetHashCode(Block x) => x.BlockType.GetHashCode() ^ x.Name.GetHashCode();
}

public sealed class BlockStackEComparer : G.IEqualityComparer<BlockStack>
{
	public bool Equals(BlockStack? x, BlockStack? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;
		if (x.Length != y.Length)
			return false;
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			if (new BlockEComparer().Equals(x.ElementAt(i), y.ElementAt(i)) == false)
				return false;
		}
		return true;
	}

	public int GetHashCode(BlockStack x)
	{
		var hash = 0;
		for (var i = 0; i < x.Length; i++)
			hash ^= new BlockEComparer().GetHashCode(x.ElementAt(i));
		return hash;
	}
}

public sealed class BlockStackAndStringEComparer : G.IEqualityComparer<(BlockStack, String)>
{
	public bool Equals((BlockStack, String) x, (BlockStack, String) y) => new BlockStackEComparer().Equals(x.Item1, y.Item1) && x.Item2 == y.Item2;

	public int GetHashCode((BlockStack, String) x) => new BlockStackEComparer().GetHashCode(x.Item1) ^ x.Item2.GetHashCode();
}

public sealed class GeneralExtraTypesEComparer : G.IEqualityComparer<GeneralExtraTypes>
{
	public bool Equals(GeneralExtraTypes? x, GeneralExtraTypes? y)
	{
		if (x is null && y is null)
			return true;
		if (x is null || y is null)
			return false;
		if (x.Length != y.Length)
			return false;
		else if (x.Length == 0 && y.Length == 0)
			return true;
		for (var i = 0; i < x.Length && i < y.Length; i++)
		{
			if (x[i].MainType.IsValue != y[i].MainType.IsValue)
				return false;
			else if ((x[i].MainType.IsValue ? x[i].MainType.Value != y[i].MainType.Value : !new BlockStackEComparer().Equals(x[i].MainType.Type, y[i].MainType.Type)) || !Equals(x[i].ExtraTypes, y[i].ExtraTypes))
				return false;
		}
		return true;
	}

	public int GetHashCode(GeneralExtraTypes x)
	{
		var hash = 0;
		for (var i = 0; i < x.Length; i++)
			hash ^= (x[i].MainType.IsValue ? x[i].MainType.Value.GetHashCode() : new BlockStackEComparer().GetHashCode(x[i].MainType.Type)) ^ GetHashCode(x[i].ExtraTypes);
		return hash;
	}
}

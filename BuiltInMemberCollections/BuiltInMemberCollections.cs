global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Diagnostics;
global using G = System.Collections.Generic;
global using static CSharp.NStar.NStarType;
global using String = NStar.Core.String;
using NStar.BufferLib;
using NStar.ParallelHS;
using NStar.RemoveDoubles;
using NStar.SortedSets;
using NStar.SumCollections;
using NStar.TreeSets;
using System.Numerics;
using System.Text;

namespace CSharp.NStar;

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

public static class BuiltInMemberCollections
{
	private static readonly List<String> NoExtraTypes = [];
	private static readonly List<String> ExtraTypesT = ["T"];
	private static readonly BlockStack GeneralTypeBool = new([new(BlockType.Primitive, "bool", 1)]);
	private static readonly BlockStack GeneralTypeInt = new([new(BlockType.Primitive, "int", 1)]);
	private static readonly BlockStack GeneralTypeList = new([new(BlockType.Primitive, "list", 1)]);
	private static readonly BlockStack GeneralTypeString = new([new(BlockType.Primitive, "string", 1)]);
	private static readonly BranchCollection BranchCollectionT = [new("type", 0, []) { Extra = new NStarType(new BlockStack([new(BlockType.Extra, "T", 1)]), NoBranches) }];
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
	public static GeneralTypes GeneralTypesList { get; } = new(new BlockStackAndStringComparer()) { { (new([new(BlockType.Namespace, "System", 1)]), nameof(Action)), ([new(true, NoBranches, GetPrimitiveBlockStack("typename"), "Types")], TypeAttributes.Delegate) }, { (new([new(BlockType.Namespace, "System", 1)]), nameof(Func<bool>)), new([new(false, NoBranches, GetPrimitiveBlockStack("typename"), "TReturn"), new(true, NoBranches, GetPrimitiveBlockStack("typename"), "Types")], TypeAttributes.Delegate) } };

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
	public static TypeSortedList<GeneralMethods> GeneralMethodsList { get; } = new() { { new(), new() { { "ExecuteString", new() { new([], (new([new(BlockType.Primitive, "universal", 1)]), NoBranches), FunctionAttributes.Multiconst, [GeneralParameterStringS, new(new(new([new(BlockType.Primitive, "universal", 1)]), NoBranches), "parameters", ParameterAttributes.Params, [])]) } } } } };

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
	public static SortedDictionary<String, UnaryOperatorClasses> UnaryOperatorsList { get; } = new() { { "+", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "INumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { "-", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IRealNumber", 1)]), BranchCollectionT)) } } } }/*, { "++", new(new BlockStackComparer()){ { "IBase", new() { (false, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoBranches, "IBase", BranchCollectionT), (true, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoBranches, "IBase", BranchCollectionT) } } } }, { "--", new(new BlockStackComparer()){ { "IBase", new() { (false, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoBranches, "IBase", BranchCollectionT), (true, new(new Block[]{ new(BlockType.Extra, "T", 1) }), NoBranches, "IBase", BranchCollectionT) } } } }*/, { "!", new(new BlockStackComparer()) { { GeneralTypeBool, new() { (false, (GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches)) } } } }/*, { "!!", new(new BlockStackComparer()){ { GetPrimitiveBlockStack("bool"), new() { (true, GetPrimitiveBlockStack("bool"), NoBranches, GetPrimitiveBlockStack("bool"), NoBranches) } } } }*/, { "~", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), new() { (false, (new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]), BranchCollectionT)) } } } } };

	/// <summary>
	/// Sorted by Operator, also contains ReturnTypes, ReturnUnvType.ExtraTypes, LeftOpdTypes and LeftOpdExtraTypes, RightOpdTypes and RightOpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, BinaryOperatorClasses> BinaryOperatorsList { get; } = new() { { "+", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (GeneralTypeInt, NoBranches)), ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GeneralTypeInt, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } }, { GeneralTypeString, new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GetPrimitiveBlockStack("char"), NoBranches), (GeneralTypeString, NoBranches)), ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GeneralTypeList, [new("type", 0, []) { Extra = GetPrimitiveType("char") }]), (GeneralTypeString, NoBranches)), ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GeneralTypeString, NoBranches), (GetPrimitiveBlockStack("char"), NoBranches)), ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GeneralTypeString, NoBranches), (GeneralTypeList, [new("type", 0, []) { Extra = GetPrimitiveType("char") }])), ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (GeneralTypeString, NoBranches), (GeneralTypeString, NoBranches)) } } } }, { "-", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (GeneralTypeInt, NoBranches)), ((GeneralTypeInt, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { "*", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "INumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } }, { GeneralTypeString, new() { ((GeneralTypeString, NoBranches), (GeneralTypeInt, NoBranches), (GeneralTypeString, NoBranches)), ((GeneralTypeString, NoBranches), (GeneralTypeString, NoBranches), (GeneralTypeInt, NoBranches)) } } } }, { "/", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IRealNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IRealNumber", 1)]), BranchCollectionT)) } } } }, { "pow", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (GeneralTypeInt, NoBranches)) } }, { new([new(BlockType.Interface, "IRealNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IRealNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IRealNumber", 1)]), BranchCollectionT)) } } } }, { "==", new(new BlockStackComparer()) { { GetPrimitiveBlockStack("object"), new() { ((new([new(BlockType.Interface, "object", 1)]), NoBranches), (new([new(BlockType.Interface, "object", 1)]), NoBranches), (new([new(BlockType.Interface, "object", 1)]), NoBranches)) } } } }, { ">", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { "<", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { ">=", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { "<=", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIncreasable", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIncreasable", 1)]), BranchCollectionT)) } }, { new([new(BlockType.Interface, "INumber", 1)]), new() { ((GeneralTypeBool, NoBranches), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "INumber", 1)]), BranchCollectionT)) } } } }, { "!=", new(new BlockStackComparer()) { { GetPrimitiveBlockStack("object"), new() { ((new([new(BlockType.Interface, "object", 1)]), NoBranches), (new([new(BlockType.Interface, "object", 1)]), NoBranches), (new([new(BlockType.Interface, "object", 1)]), NoBranches)) } } } }, { ">>", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (GeneralTypeInt, NoBranches)) } } } }, { "<<", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (GeneralTypeInt, NoBranches)) } } } }, { "&", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT)) } } } }, { "|", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT)) } } } }, { "^", new(new BlockStackComparer()) { { new([new(BlockType.Interface, "IIntegerNumber", 1)]), new() { ((new([new(BlockType.Extra, "T", 1)]), NoBranches), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT), (new([new(BlockType.Interface, "IIntegerNumber", 1)]), BranchCollectionT)) } } } }, { "&&", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches)) } } } }, { "||", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches)) } } } }, { "^^", new(new BlockStackComparer()) { { GeneralTypeBool, new() { ((GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches), (GeneralTypeBool, NoBranches)) } } } } };

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, int Value)>> EnumConstantsList { get; } = new() { { new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), new() { ("Local", (int)DateTimeKind.Local), ("Unspecified", (int)DateTimeKind.Unspecified), ("UTC", (int)DateTimeKind.Utc) } }, { new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DayOfWeek", 1)]), new() { ("Friday", (int)DayOfWeek.Friday), ("Monday", (int)DayOfWeek.Monday), ("Saturday", (int)DayOfWeek.Saturday), ("Sunday", (int)DayOfWeek.Sunday), ("Thursday", (int)DayOfWeek.Thursday), ("Tuesday", (int)DayOfWeek.Tuesday), ("Wednesday", (int)DayOfWeek.Wednesday) } } };

	/// <summary>
	/// Sorted by Container, also contains Name, Type and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, NStarType Type, dynamic Value)>> OtherConstantsList { get; } = new() { { GetPrimitiveBlockStack("byte"), new() { ("MaxValue", (new([new(BlockType.Interface, "byte", 1)]), NoBranches), byte.MaxValue), ("MinValue", (new([new(BlockType.Interface, "byte", 1)]), NoBranches), byte.MinValue) } }, { GetPrimitiveBlockStack("char"), new() { ("MaxValue", (new([new(BlockType.Interface, "char", 1)]), NoBranches), char.MaxValue), ("MinValue", (new([new(BlockType.Interface, "char", 1)]), NoBranches), char.MinValue) } }, { GeneralTypeInt, new() { ("MaxValue", (new([new(BlockType.Interface, "int", 1)]), NoBranches), int.MaxValue), ("MinValue", (new([new(BlockType.Interface, "int", 1)]), NoBranches), int.MinValue) } }, { new([new(BlockType.Primitive, "long char", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "long char", 1)]), NoBranches), uint.MaxValue), ("MinValue", (new([new(BlockType.Interface, "long char", 1)]), NoBranches), uint.MinValue) } }, { new([new(BlockType.Primitive, "long int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "long int", 1)]), NoBranches), long.MaxValue), ("MinValue", (new([new(BlockType.Interface, "long int", 1)]), NoBranches), long.MinValue) } }, { GetPrimitiveBlockStack("real"), new() { ("MaxValue", (new([new(BlockType.Interface, "real", 1)]), NoBranches), double.MaxValue), ("MinPosValue", (new([new(BlockType.Interface, "real", 1)]), NoBranches), double.Epsilon), ("MinValue", (new([new(BlockType.Interface, "real", 1)]), NoBranches), double.MinValue) } }, { new([new(BlockType.Primitive, "short char", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "short char", 1)]), NoBranches), byte.MaxValue), ("MinValue", (new([new(BlockType.Interface, "short char", 1)]), NoBranches), byte.MinValue) } }, { new([new(BlockType.Primitive, "short int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "short int", 1)]), NoBranches), short.MaxValue), ("MinValue", (new([new(BlockType.Interface, "short int", 1)]), NoBranches), short.MinValue) } }, { new([new(BlockType.Primitive, "unsigned int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned int", 1)]), NoBranches), uint.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned int", 1)]), NoBranches), uint.MinValue) } }, { new([new(BlockType.Primitive, "unsigned long int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned long int", 1)]), NoBranches), ulong.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned long int", 1)]), NoBranches), ulong.MinValue) } }, { new([new(BlockType.Primitive, "unsigned short int", 1)]), new() { ("MaxValue", (new([new(BlockType.Interface, "unsigned short int", 1)]), NoBranches), ushort.MaxValue), ("MinValue", (new([new(BlockType.Interface, "unsigned short int", 1)]), NoBranches), ushort.MinValue) } } };

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<Dictionary<String, UserDefinedConstant>> UserDefinedConstantsList { get; } = [];

	/// <summary>
	/// Sorted by SrcType, also contains SrcUnvType.ExtraTypes, DestTypes and their DestUnvType.ExtraTypes.
	/// </summary>
	public static TypeSortedList<ImplicitConversions> ImplicitConversionsList { get; } = new() { { GeneralTypeBool, new() { { NoBranches, new() { (RealType, false), (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (ByteType, false) } } } }, { GetPrimitiveBlockStack("byte"), new() { { NoBranches, new() { (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (GetPrimitiveType("short char"), true), (BoolType, true) } } } }, { GetPrimitiveBlockStack("char"), new() { { NoBranches, new() { (UnsignedShortIntType, false), (StringType, false) } } } }, { GeneralTypeInt, new() { { NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (IndexType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true) } } } }, { GeneralTypeList, new() { { [new("type", 0, []) { Extra = GetPrimitiveType("char") }], new() { (StringType, false) } } } }, { GetPrimitiveBlockStack("long char"), new() { { NoBranches, new() { (UnsignedIntType, false) } } } }, { GetPrimitiveBlockStack("long int"), new() { { NoBranches, new() { (UnsignedLongIntType, false), (BoolType, true), (UnsignedShortIntListType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("real"), new() { { NoBranches, new() { (BoolType, true), (UnsignedLongIntType, true), (LongIntType, true), (UnsignedIntType, true), (IntType, true) } } } }, { GetPrimitiveBlockStack("short char"), new() { { NoBranches, new() { (ByteType, false) } } } }, { GetPrimitiveBlockStack("short int"), new() { { NoBranches, new() { (LongIntType, false), (RealType, false), (IntType, false), (UnsignedShortIntType, false), (BoolType, true), (ByteType, true), (UnsignedIntType, true), (UnsignedLongIntType, true) } } } }, { GeneralTypeString, new() { { NoBranches, new() { ((GeneralTypeList, [new("type", 0, []) { Extra = GetPrimitiveType("char") }]), false) } } } }, { GetPrimitiveBlockStack("unsigned int"), new() { { NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (IntType, true) } } } }, { GetPrimitiveBlockStack("unsigned long int"), new() { { NoBranches, new() { (BoolType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (LongIntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("unsigned short int"), new() { { NoBranches, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (BoolType, true), (ByteType, true), (ShortIntType, true) } } } } };

	/// <summary>
	/// Sorted by tuple, contains DestType and DestUnvType.ExtraTypes.
	/// </summary>
	public static List<NStarType> ImplicitConversionsFromAnythingList { get; } = [(GetPrimitiveBlockStack("universal"), NoBranches), (GetPrimitiveBlockStack("null"), NoBranches), (GeneralTypeList, [new("type", 0, []) { Extra = GetPrimitiveType("[this]") }])];

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
	public static SortedDictionary<(String Namespace, String Type), String> OutdatedTypesList { get; } = new() { { ([], "*Exception"), "\"if error ...\"" }, { ([], "double"), "real or long real" }, { ([], "float"), "real or long real" }, { ([], "uint"), "unsigned int" }, { ([], "ulong"), "unsigned long int" }, { ([], "ushort"), "unsigned short int" }, { ("System", "Array"), "list" }, { ("System", "Boolean"), "bool" }, { ("System", "Byte"), "byte (from the small letter)" }, { ("System", "Char"), "char (from the small letter), short char or long char" }, { ("System", "Console"), "labels and textboxes" }, { ("System", "ConsoleCancelEventArgs"), "TextBox.KeyDown, TextBox.KeyPress and TextBox.KeyUp" }, { ("System", "ConsoleCancelEventHandler"), "TextBox keyboard events" }, { ("System", "ConsoleColor"), "RichTextBox text color" }, { ("System", "ConsoleKey"), "other item enums" }, { ("System", "ConsoleKeyInfo"), "other item info classes" }, { ("System", "ConsoleModifiers"), "other item modifiers enums" }, { ("System", "ConsoleSpecialKey"), "other item enums" }, { ("System", "Double"), "real or long real" }, { ("System", "Int16"), "short int" }, { ("System", "Int32"), "int" }, { ("System", "Int64"), "long int" }, { ("System", "Object"), "object (from the small letter)" }, { ("System", "Random"), "Random(), IntRandom() etc." }, { ("System", "SByte"), "byte or short int" }, { ("System", "Single"), "real or long real" }, { ("System", "String"), "string (from the small letter)" }, { ("System", "Type"), "typename" }, { ("System", "UInt16"), "unsigned short int" }, { ("System", "UInt32"), "unsigned int" }, { ("System", "UInt64"), "unsigned long int" }, { ("System", "Void"), "null" }, { ("System.Collections", "BitArray"), "BitList" }, { ("System.Collections", "HashSet"), "ListHashSet" }, { ("System.Collections", "Hashtable"), "HashTable" }, { ("System.Collections", "KeyValuePair"), "tuples" }, { ("System.Collections", "SortedSet"), "SortedSet" } };

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
	public static TypeSortedList<OutdatedMethods> OutdatedMethodOverloadsList { get; } = new() { { new([new(BlockType.Interface, "IChar", 1)]), new() { { "IsDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "string[index] as a parameter") } }, { "IsLetter", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLetterOrDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLower", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsSeparator", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsUpper", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsWhiteSpace", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } } } }, { GeneralTypeString, new() { { "Concat", new() { ([GeneralParameterString1, GeneralParameterString2, GeneralParameterString3, new(StringType, "string4", ParameterAttributes.None, [])], "concatenation in pairs, triples or in an array"), ([new(new(GetPrimitiveBlockStack("universal"), NoBranches), "object1", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoBranches), "object2", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoBranches), "object3", ParameterAttributes.None, []), new(new(GetPrimitiveBlockStack("universal"), NoBranches), "object4", ParameterAttributes.None, [])], "concatenation in pairs, triples or in an array") } } } } };

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

	public static bool TypesAreEqual(NStarType type1, NStarType type2)
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
			if (type1.ExtraTypes[i] != type2.ExtraTypes[i])
				return false;
		}
		return true;
	}

	public static bool TypeEqualsToPrimitive(NStarType type, String primitive, bool noExtra = true) => TypeIsPrimitive(type.MainType) && type.MainType.Peek().Name == primitive && (!noExtra || type.ExtraTypes.Length == 0);

	public static bool TypeIsPrimitive(BlockStack type) => type is null || type.Length == 1 && type.Peek().BlockType == BlockType.Primitive;

	public static NStarType GetPrimitiveType(String primitive) => (new([new(BlockType.Primitive, primitive, 1)]), NoBranches);

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

	public static NStarType GetListType(NStarType InnerType)
	{
		if (!TypeEqualsToPrimitive(InnerType, "list", false))
			return new(ListBlockStack, new([new("type", 0, []) { Extra = InnerType }]));
		else if (InnerType.ExtraTypes.Length >= 2 && InnerType.ExtraTypes[0].Name != "type" && int.TryParse(InnerType.ExtraTypes[0].Name.ToString(), out var number))
			return new(ListBlockStack, new([new((number + 1).ToString(), 0, []), InnerType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new("2", 0, []), InnerType.ExtraTypes[^1]]));
	}

	public static NStarType GetListType(TreeBranch InnerType)
	{
		if (InnerType.Name != "type" || InnerType.Extra is not NStarType UnvType || !TypeEqualsToPrimitive(UnvType, "list", false))
			return new(ListBlockStack, new([InnerType]));
		else if (UnvType.ExtraTypes.Length >= 2 && UnvType.ExtraTypes[0].Name != "type" && int.TryParse(UnvType.ExtraTypes[0].Name.ToString(), out var number))
			return new(ListBlockStack, new([new((number + 1).ToString(), 0, []), UnvType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new("2", 0, []), UnvType.ExtraTypes[^1]]));
	}
}

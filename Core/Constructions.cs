﻿global using Corlib.NStar;
global using System;
global using System.Drawing;
global using System.Globalization;
global using System.IO;
global using System.Net.Http;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using G = System.Collections.Generic;
global using static CSharp.NStar.Constructions;
global using static System.Math;
global using String = Corlib.NStar.String;
using System.Diagnostics;
using System.Text;

namespace CSharp.NStar;
public sealed class Block(BlockType type, String name, int unnamedIndex)
{
	public BlockType Type { get; private set; } = type;
	public String Name { get; private set; } = name;
	public int UnnamedIndex { get; set; } = unnamedIndex;

	public override bool Equals(object? obj) => obj is not null && obj is Block m && Type == m.Type && Name == m.Name;

	public override int GetHashCode() => Type.GetHashCode() ^ Name.GetHashCode();

	public override string ToString() => (Type == BlockType.Unnamed) ? "Unnamed(" + Name + ")" : (ExplicitNameBlockTypes.Contains(Type) ? Type.ToString() : "") + Name;
}

public sealed class TypeOrValue
{
	public String Value { get; set; } = "";
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

	public string ToShortString() => string.Join(".", this.ToArray(x => (x.Type == BlockType.Unnamed) ? "Unnamed(" + x.Name + ")" : x.Name.ToString()));

	public override string ToString() => string.Join(".", this.ToArray(x => x.ToString()));
}

public record struct UserDefinedType(GeneralArrayParameters ArrayParameters, ClassAttributes Attributes, GeneralExtraTypes Decomposition);

public record struct MethodParameter(String Type, String Name, List<String> ExtraTypes, ParameterAttributes Attributes, String DefaultValue);

public record struct GeneralMethodParameter(BlockStack Type, String Name, GeneralExtraTypes ExtraTypes, ParameterAttributes Attributes, String DefaultValue);

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
public class GeneralTypes(G.IComparer<(BlockStack Container, String Type)> comparer) : SortedDictionary<(BlockStack Container, String Type), (GeneralArrayParameters ArrayParameters, ClassAttributes Attributes)>(comparer)
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
public class UserDefinedTypeProperties : Dictionary<String, (UniversalType UnvType, PropertyAttributes Attributes)>
{
}
public class TypeIndexers : SortedDictionary<String, (BlockStack IndexType, BlockStack Type, List<String> ExtraTypes, PropertyAttributes Attributes)>
{
}
public class MethodParameters : List<MethodParameter>
{
}
public class FunctionsList : SortedDictionary<String, (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)>
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

	public override string ToString() => string.Join(", ", [.. Values.Convert(x => x.ToString())]);
}

public class GeneralArrayParameters : List<(bool ArrayParameterPackage, GeneralExtraTypes ArrayParameterRestrictions, BlockStack ArrayParameterType, String ArrayParameterName)>
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
public class GeneralMethodOverloads : List<(GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)>
{
}
public class GeneralMethods : SortedDictionary<String, GeneralMethodOverloads>
{
}
public class UserDefinedMethods : Dictionary<String, GeneralMethodOverloads>
{
}
public class ConstructorOverloads : List<(ConstructorAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)>
{
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

public enum ClassAttributes
{
	None = 0,
	Sealed = 1,
	Abstract = 2,
	Static = 3,
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

[DebuggerDisplay("{ToString()}")]
public sealed class TreeBranch
{
	public String Info { get; set; }
	public int Pos { get; set; }
	public int EndPos { get; set; }
	public List<TreeBranch> Elements { get; set; }
	public BlockStack Container { get; set; }
	public object? Extra { get; set; }
	public TreeBranch? Parent { get; private set; }

	public TreeBranch this[Index index]
	{
		get => Elements[index];
		set
		{
			Elements[index] = value;
			value.Parent = this;
			EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
		}
	}

	public int Length => Elements.Length;
	public int FullCount => Elements.Length + Elements.Sum(x => x.FullCount);

	public TreeBranch(String info, int pos, BlockStack container)
	{
		Info = info;
		Pos = pos;
		EndPos = pos + 1;
		Elements = [];
		Container = container;
	}

	public TreeBranch(String info, int pos, int endPos, BlockStack container)
	{
		Info = info;
		Pos = pos;
		EndPos = endPos;
		Elements = [];
		Container = container;
	}

	public TreeBranch(String info, TreeBranch element, BlockStack container)
	{
		Info = info;
		Pos = element.Pos;
		EndPos = element.EndPos;
		Elements = [element];
		element.Parent = this;
		Container = container;
	}

	public TreeBranch(String info, List<TreeBranch> elements, BlockStack container)
	{
		Info = info;
		Pos = elements.Length == 0 ? throw new ArgumentException(null, nameof(elements)) : elements[0].Pos;
		EndPos = elements.Length == 0 ? throw new ArgumentException(null, nameof(elements)) : elements[^1].EndPos;
		Elements = elements;
		Elements.ForEach(x => x.Parent = this);
		Container = container;
	}

	public static TreeBranch DoNotAdd() => new("DoNotAdd", 0, int.MaxValue, new());

	public void Add(TreeBranch item)
	{
		if (item is TreeBranch branch && branch != DoNotAdd())
		{
			Elements.Add(item);
			item.Parent = this;
			EndPos = item.EndPos;
		}
	}

	public void AddRange(G.IEnumerable<TreeBranch> collection)
	{
		Elements.AddRange(collection);
		foreach (var x in collection)
			x.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public List<TreeBranch> GetRange(int index, int count) => Elements.GetRange(index, count);

	public void Insert(int index, TreeBranch item)
	{
		Elements.Insert(index, item);
		item.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void Insert(int index, G.IEnumerable<TreeBranch> collection)
	{
		Elements.Insert(index, collection);
		foreach (var x in collection)
			x.Parent = this;
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void Remove(int index, int count)
	{
		Elements.Remove(index, count);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void RemoveEnd(int index)
	{
		Elements.RemoveEnd(index);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public void RemoveAt(int index)
	{
		Elements.RemoveAt(index);
		EndPos = Length == 0 ? Pos + 1 : Elements[^1].EndPos;
	}

	public override bool Equals(object? obj) => obj is not null
&& obj is TreeBranch m
&& Info == m.Info && RedStarLinq.Equals(Elements, m.Elements, (x, y) => new TreeBranchComparer().Equals(x, y));

	public override int GetHashCode() => Info.GetHashCode() ^ Elements.GetHashCode();

	public override string ToString() => ToString(new());

	private string ToString(BlockStack container)
	{
		var infoString = Info + (RedStarLinq.Equals(container, Container) ? "" : Container.StartsWith(container) ? "@" + string.Join(".", Container.Skip(container.Length).ToArray(x => x.ToString())) : throw new ArgumentException(null, nameof(container)));
		return Length == 0 ? (Extra is null ? infoString : "(" + infoString + " :: " + Extra.ToString() + ")") + "#" + Pos.ToString() : "(" + infoString + " : " + string.Join(", ", Elements.ToArray(x => x.ToString(Container))) + (Extra is null ? "" : " : " + Extra.ToString()) + ")";
	}

	public static bool operator ==(TreeBranch? x, TreeBranch? y) => x is null && y is null || x is not null && y is not null && x.Info == y.Info && RedStarLinq.Equals(x.Elements, y.Elements, (x, y) => new TreeBranchComparer().Equals(x, y));

	public static bool operator !=(TreeBranch? x, TreeBranch? y) => !(x == y);
}

public sealed class TreeBranchComparer : G.IEqualityComparer<TreeBranch>
{
	public bool Equals(TreeBranch? x, TreeBranch? y) => x is null && y is null || (x?.Equals(y) ?? false);

	public int GetHashCode(TreeBranch x) => x.GetHashCode();
}

public static partial class Constructions
{
	public static readonly Random globalRandom = new();
	public static readonly BitList EmptyBoolList = [];
	private static readonly List<String> NoExtraTypes = [];
	private static readonly List<String> ExtraTypesT = ["T"];
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
	public static readonly BlockStack EmptyBlockStack = new();
	public static readonly BlockStack ListBlockStack = new([new(BlockType.Primitive, "list", 1)]);
	public static readonly BlockStack TupleBlockStack = new([new(BlockType.Primitive, "tuple", 1)]);
	public static readonly BlockStack FuncBlockStack = new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]);
	public static readonly List<BlockType> ExplicitNameBlockTypes = new(BlockType.Constructor, BlockType.Destructor, BlockType.Operator, BlockType.Other);
	public static readonly UniversalType BoolListType = GetListType(BoolType);
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
	public static readonly List<String> EmptyStringList = [];
	public static readonly List<String> ExprTypesList = ["Expr", "List", "Indexes", "Ternary", "PmExpr", "MuldivExpr", "XorList", "StringConcatenation", "Assignment", "UnaryAssignment", "Declaration", "Hypername"];
	public static readonly List<String> CycleTypesList = ["loop", "while", "while!", "repeat", "for", "loop_while", "for_while", "repeat_while"];
	public static readonly List<String> ConvertibleTypesList = ["bool", "byte", "short int", "unsigned short int", "char", "int", "unsigned int", "long int", "unsigned long int", "real", "string"];
	public static readonly List<String> NumberTypesList = ["byte", "short char", "char", "long char", "short int", "unsigned short int", "int", "unsigned int", "long int", "unsigned long int"];
	public static readonly List<String> ListTypesList = ["Dictionary", "G.HashSet", "HashTable", "G.LinkedList", "LinkedListNode", "Queue", "G.SortedSet", "Stack"];
	public static readonly List<String> StopLexemsList = ["\r\n", ";", "{", "}"];
	public static readonly List<(String, BlockType)> BlockTypesList = [("Main", BlockType.Unnamed), ("Namespace", BlockType.Namespace), ("Class", BlockType.Class), ("Struct", BlockType.Struct), ("Interface", BlockType.Interface), ("Delegate", BlockType.Delegate), ("Enum", BlockType.Enum), ("Function", BlockType.Function), ("Constructor", BlockType.Constructor), ("Destructor", BlockType.Destructor), ("Operator", BlockType.Operator), ("Extent", BlockType.Extent)];
	public static readonly CultureInfo EnUsCulture = new("en-US");
	private static readonly MethodParameter ParameterPredicate = new("System.Predicate", "match", ExtraTypesT, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterRealValue = new("real", "value", NoExtraTypes, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterICharT = new("IChar", "c", ExtraTypesT, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterListT = new("list", "list", ExtraTypesT, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterChars = new("list", "chars", ["char"], ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterSubstring = new("string", "substring", NoExtraTypes, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterIgnoreCase = new("bool", "ignore_case", NoExtraTypes, ParameterAttributes.Optional, "false");
	private static readonly MethodParameter ParameterIndex = new("int", "index", NoExtraTypes, ParameterAttributes.None, "");
	private static readonly MethodParameter ParameterLength = new("int", "length", NoExtraTypes, ParameterAttributes.None, "");
	private static readonly BlockStack GeneralTypeBool = new([new(BlockType.Primitive, "bool", 1)]);
	private static readonly BlockStack GeneralTypeInt = new([new(BlockType.Primitive, "int", 1)]);
	private static readonly BlockStack GeneralTypeList = new([new(BlockType.Primitive, "list", 1)]);
	private static readonly BlockStack GeneralTypeString = new([new(BlockType.Primitive, "string", 1)]);
	private static readonly GeneralExtraTypes GeneralExtraTypesT = [new(new BlockStack([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes)];
	private static readonly GeneralMethodParameter GeneralParameterTValue = new(new([new(BlockType.Extra, "T", 1)]), "value", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterIndex = new(GeneralTypeInt, "index", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterStartIndex = new(GeneralTypeInt, "start_index", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterLength = new(GeneralTypeInt, "length", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterComparer = new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]), "comparer", [new(ShortIntType), new(new BlockStack([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), new(new BlockStack([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes)], ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterStringComparer = new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Func", 1)]), "comparer", [new(ShortIntType), new(GeneralTypeString, NoGeneralExtraTypes), new(GeneralTypeString, NoGeneralExtraTypes)], ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterPredicate = new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Predicate", 1)]), "match", GeneralExtraTypesT, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterChars = new(GeneralTypeList, "chars", [new(GetPrimitiveType("char"))], ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterStrings = new(GeneralTypeList, "strings", [new(GeneralTypeString, NoGeneralExtraTypes)], ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterStringCollection = new(new([new(BlockType.Interface, "IEnumerable", 1), new(BlockType.Namespace, "Collections", 1), new(BlockType.Namespace, "System", 1)]), "collection", [new(GeneralTypeString, NoGeneralExtraTypes)], ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterTCollection = new(new([new(BlockType.Interface, "IEnumerable", 1), new(BlockType.Namespace, "Collections", 1), new(BlockType.Namespace, "System", 1)]), "collection", GeneralExtraTypesT, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterIgnoreCase = new(GeneralTypeBool, "ignore_case", NoGeneralExtraTypes, ParameterAttributes.Optional, "false");
	private static readonly GeneralMethodParameter GeneralParameterSeparator = new(GeneralTypeString, "separator", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterStringS = new(GeneralTypeString, "info", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterString1 = new(GeneralTypeString, "string1", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterString2 = new(GeneralTypeString, "string2", NoGeneralExtraTypes, ParameterAttributes.None, "");
	private static readonly GeneralMethodParameter GeneralParameterString3 = new(GeneralTypeString, "string3", NoGeneralExtraTypes, ParameterAttributes.None, "");

	public static G.SortedSet<String> KeywordsList { get; } = ["abstract", "break", "case", "Class", "closed", "const", "Constructor", "continue", "default", "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst", "Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref", "repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while"];

	public static G.SortedSet<String> EscapedKeywordsList { get; } = ["abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed, float, for, foreach, goto, if, implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type and ExtraTypes.
	/// </summary>
	public static TypeSortedList<TypeVariables> VariablesList { get; } = [];

	public static G.SortedSet<String> NamespacesList { get; } = ["System", "System.Collections"];

	public static G.HashSet<String> UserDefinedNamespacesList { get; } = [];

	public static List<String> PrimitiveTypesList { get; } = ["null", "object", "bool", "byte", "short char", "short int", "unsigned short int", "char", "int", "unsigned int", "long char", "long int", "DateTime", "TimeSpan", "unsigned long int", "real"/*, complex*/, "typename", "string", "IntPtr", "list", "universal", "dynamic", "var"];

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static List<(String Namespace, String Type)> ExtraTypesList { get; } = [("System", "DateTimeKind"), ("System", "DayOfWeek"), ("System.Collections", "BitArray")];

	/// <summary>
	/// Sorted by Namespace and Type, also contains ExtraTypes and Attributes.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), (List<String> ExtraTypes, ClassAttributes Attributes)> CompositeTypesList { get; } = new() { { ("", "out"), (ExtraTypesT, ClassAttributes.None) }, { ("", "ref"), (ExtraTypesT, ClassAttributes.None) }, { ("System", "Predicate"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "Dictionary"), (["TKey", "TValue"], ClassAttributes.None) }, { ("System.Collections", "HashSet"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "HashTable"), (["TKey", "TValue"], ClassAttributes.None) }, { ("System.Collections", "LinkedList"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "LinkedListNode"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "Queue"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "SortedSet"), (ExtraTypesT, ClassAttributes.None) }, { ("System.Collections", "Stack"), (ExtraTypesT, ClassAttributes.None) } };

	/// <summary>
	/// Sorted by Container and Type, also contains ArrayParameterPackage modifiers, ArrayParameterRestrictions, ArrayParameterTypes, ArrayParameterNames and Attributes.
	/// </summary>
	public static GeneralTypes GeneralTypesList { get; } = new(new BlockStackAndStringComparer()) { { (new([new(BlockType.Namespace, "System", 1)]), "Action"), ([(true, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "Types")], ClassAttributes.None) }, { (new([new(BlockType.Namespace, "System", 1)]), "Func"), ([(false, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "TReturn"), (true, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "Types")], ClassAttributes.None) } };

	/// <summary>
	/// Sorted by Container and Type, also contains ArrayParameterPackage modifiers, ArrayParameterRestrictions, ArrayParameterTypes, ArrayParameterNames and Attributes.
	/// </summary>
	public static Dictionary<(BlockStack Container, String Type), UserDefinedType> UserDefinedTypesList { get; } = new(new BlockStackAndStringEComparer()) { };

	/// <summary>
	/// Sorted by tuple, contains Namespace, Interface and ExtraTypes.
	/// </summary>
	public static G.SortedSet<(String Namespace, String Interface, List<String> ExtraTypes)> InterfacesList { get; } = [("", "IBase", ExtraTypesT), ("", "IChar", ExtraTypesT), ("", "IComparable", ExtraTypesT), ("", "IComparableRaw", NoExtraTypes), ("", "IConvertible", NoExtraTypes), ("", "IEquatable", ExtraTypesT), ("", "IIncreasable", ExtraTypesT), ("", "IIntegerNumber", ExtraTypesT), ("", "INumber", ExtraTypesT), ("", "IRealNumber", ExtraTypesT), ("", "ISignedIntegerNumber", ExtraTypesT), ("", "IUnsignedIntegerNumber", ExtraTypesT), ("System.Collections", "ICollection", ExtraTypesT), ("System.Collections", "ICollectionRaw", NoExtraTypes), ("System.Collections", "G.IDictionary", ["TKey", "TValue"]), ("System.Collections", "IDictionaryRaw", NoExtraTypes), ("System.Collections", "IEnumerable", ExtraTypesT), ("System.Collections", "IEnumerableRaw", NoExtraTypes), ("System.Collections", "IList", ExtraTypesT), ("System.Collections", "IListRaw", NoExtraTypes)];

	/// <summary>
	/// Sorted by DestInterface, also contains SrcInterface and SrcUnvType.ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String SrcInterface, List<String> SrcExtraTypes)>> NestedInterfacesList { get; } = new() { { "IChar", new() { ("IIncreasable", ExtraTypesT) } }, { "IIncreasable", new() { ("IComparable", ExtraTypesT) } }, { "IIntegerNumber", new() { ("INumber", ExtraTypesT) } }, { "INumber", new() { ("IComparable", ExtraTypesT) } }, { "IRealNumber", new() { ("INumber", ExtraTypesT) } }, { "ISignedIntegerNumber", new() { ("IIntegerNumber", ExtraTypesT) } }, { "IUnsignedIntegerNumber", new() { ("IIntegerNumber", ExtraTypesT) } } };

	/// <summary>
	/// Sorted by Class, also contains Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String Interface, List<String> ExtraTypes)>> ImplementedInterfacesList { get; } = new() { { "bool", new() { ("IComparable", ["bool"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["bool"]) } }, { "byte", new() { ("IComparable", ["byte"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["byte"]), ("ISignedIntegerNumber", ["byte"]) } }, { "char", new() { ("IChar", ["char"]), ("IComparable", ["char"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["char"]) } }, { "DateTime", new() { ("IComparable", ["DateTime"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["DateTime"]) } }, { "int", new() { ("IComparable", ["int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["int"]), ("ISignedIntegerNumber", ["int"]) } }, { "long char", new() { ("IChar", ["long char"]), ("IComparable", ["long char"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["long char"]) } }, { "long int", new() { ("IComparable", ["long int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["long int"]), ("ISignedIntegerNumber", ["long int"]) } }, { "real", new() { ("IComparable", ["real"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["real"]), ("IRealNumber", ["real"]) } }, { "short char", new() { ("IChar", ["short char"]), ("IComparable", ["short char"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["short char"]) } }, { "short int", new() { ("IComparable", ["short int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["short int"]), ("ISignedIntegerNumber", ["short int"]) } }, { "unsigned int", new() { ("IComparable", ["unsigned int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["unsigned int"]), ("IUnsignedIntegerNumber", ["unsigned int"]) } }, { "unsigned long int", new() { ("IComparable", ["unsigned long int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["unsigned long int"]), ("IUnsignedIntegerNumber", ["unsigned long int"]) } }, { "unsigned short int", new() { ("IComparable", ["unsigned short int"]), ("IConvertible", NoExtraTypes), ("IEquatable", ["unsigned short int"]), ("IUnsignedIntegerNumber", ["unsigned short int"]) } }, { "BitArray", new() { ("ICollection", ["bool"]), ("IEnumerable", ["bool"]) } }, { "Dictionary", new() { ("ICollection", ["TValue"]), ("G.IDictionary", ["TKey", "TValue"]), ("IEnumerable", ["TValue"]) } }, { "G.HashSet", new() { ("ICollection", ExtraTypesT), ("IEnumerable", ExtraTypesT) } }, { "HashTable", new() { ("ICollection", ["TValue"]), ("G.IDictionary", ["TKey", "TValue"]), ("IEnumerable", ["TValue"]) } }, { "G.LinkedList", new() { ("ICollection", ExtraTypesT), ("IEnumerable", ExtraTypesT) } }, { "Queue", new() { ("ICollection", ExtraTypesT), ("IEnumerable", ExtraTypesT) } }, { "G.SortedSet", new() { ("ICollection", ExtraTypesT), ("IEnumerable", ExtraTypesT) } }, { "Stack", new() { ("ICollection", ExtraTypesT), ("IEnumerable", ExtraTypesT) } } };

	/// <summary>
	/// Sorted by Class, also contains Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String Interface, List<String> ExtraTypes)>> UserDefinedImplementedInterfacesList { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type and Attributes.
	/// </summary>
	public static TypeSortedList<TypeProperties> PropertiesList { get; } = new() { { GetPrimitiveBlockStack("DateTime"), new() { { "Date", ((GetPrimitiveBlockStack("DateTime"), NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Day", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "DayOfWeek", ((new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DayOfWeek", 1)]), NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "DayOfYear", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Hour", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Kind", ((new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Millisecond", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Minute", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Month", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Now", ((GetPrimitiveBlockStack("DateTime"), NoGeneralExtraTypes), PropertyAttributes.Static | PropertyAttributes.NoSet) }, { "Second", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Ticks", ((new([new(BlockType.Primitive, "long int", 1)]), NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "TimeOfDay", ((new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "TimeSpan", 1)]), NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Today", ((GetPrimitiveBlockStack("DateTime"), NoGeneralExtraTypes), PropertyAttributes.Static | PropertyAttributes.NoSet) }, { "UTCNow", ((GetPrimitiveBlockStack("DateTime"), NoGeneralExtraTypes), PropertyAttributes.Static | PropertyAttributes.NoSet) }, { "Year", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) } } }, { GetPrimitiveBlockStack("IntPtr"), new() { { "Size", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.Static | PropertyAttributes.NoSet) } } }, { GeneralTypeList, new() { { "Last", ((new([new(BlockType.Extra, "T", 1)]), NoGeneralExtraTypes), PropertyAttributes.None) }, { "Length", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) }, { "Sorted", ((GeneralTypeBool, NoGeneralExtraTypes), PropertyAttributes.None) } } }, { GeneralTypeString, new() { { "Length", ((GeneralTypeInt, NoGeneralExtraTypes), PropertyAttributes.NoSet) } } } };

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
	public static TypeSortedList<TypeIndexers> IndexersList { get; } = new() { { GeneralTypeList, new() { { "this", (GeneralTypeInt, new([new(BlockType.Extra, "T", 1)]), NoExtraTypes, PropertyAttributes.None) } } }, { GeneralTypeString, new() { { "this", (GeneralTypeInt, GetPrimitiveBlockStack("char"), NoExtraTypes, PropertyAttributes.NoSet) } } } };

	/// <summary>
	/// Sorted by Container, also contains IndexType, Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeSortedList<TypeIndexers> UserDefinedIndexersList { get; } = [];

	/// <summary>
	/// Sorted by Name, also contains ExtraTypes, ReturnType, ReturnUnvType.ExtraTypes, Attributes, ParameterTypes, ParameterNames, ParameterExtraTypes, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static FunctionsList PublicFunctionsList { get; } = new() { { "Abs", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Ceil", (ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Chain", (NoExtraTypes, "list", ["int"], FunctionAttributes.Multiconst, [new("int", "start", NoExtraTypes, ParameterAttributes.None, ""), new("int", "end", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Choose", (NoExtraTypes, "universal", NoExtraTypes, FunctionAttributes.None, [new("universal", "variants", NoExtraTypes, ParameterAttributes.Params, "")]) }, { "Clamp", (ExtraTypesT, "INumber", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "min", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MinValue;\")"), new("real"/*"INumber"*/, "max", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MaxValue;\")")]) }, { "Exp", (ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Fibonacci", (NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.Multiconst, [new("int", "n", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Fill", (ExtraTypesT, "list", ExtraTypesT, FunctionAttributes.Multiconst, [new("T", "element", NoExtraTypes, ParameterAttributes.None, ""), new("int", "count", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Floor", (ExtraTypesT, "int"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Frac", (ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "IntRandom", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [new("int", "max", NoExtraTypes, ParameterAttributes.None, "")]) }, { "IntToReal", (ExtraTypesT, "real", NoExtraTypes, FunctionAttributes.Multiconst, [new("T", "x", NoExtraTypes, ParameterAttributes.None, "")]) }, { "ListWithSingle", (ExtraTypesT, "list", ExtraTypesT, FunctionAttributes.Multiconst, [new("T", "value", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Log", (ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"IRealNumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Max", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Max3", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "z", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "MaxList", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "numbers", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, "")]) }, { "Mean", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Mean3", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "z", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "MeanList", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "numbers", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, "")]) }, { "Min", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Min3", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real"/*"INumber"*/, "z", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "MinList", (ExtraTypesT, "real"/*"INumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "numbers", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.Params, "")]) }, { "Q", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, []) }, { "Random", (NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.None, [new("real", "max", NoExtraTypes, ParameterAttributes.None, "")]) }, { "RealRemainder", (ExtraTypesT, "real"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("real", "y", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "RGB", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.Multiconst, [new("byte", "red", NoExtraTypes, ParameterAttributes.None, ""), new("byte", "green", NoExtraTypes, ParameterAttributes.None, ""), new("byte", "blue", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Round", (ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, ""), new("int", "digits_after_dot", NoExtraTypes, ParameterAttributes.Optional, "0")]) }, { "Sign", (ExtraTypesT, "short int", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Sqrt", (ExtraTypesT, "short int", NoExtraTypes, FunctionAttributes.Multiconst, [new("real"/*"INumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) }, { "Truncate", (ExtraTypesT, "int"/*"IRealNumber"*/, NoExtraTypes/*ExtraTypesT*/, FunctionAttributes.Multiconst, [new("real"/*"IRealNumber"*/, "x", NoExtraTypes/*ExtraTypesT*/, ParameterAttributes.None, "")]) } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ExtraTypes, ReturnType, ReturnUnvType.ExtraTypes, Attributes, ParameterTypes, ParameterNames, ParameterExtraTypes, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static SortedDictionary<String, FunctionsList> MethodsList { get; } = new() { { "DateTime", new() { { "AddDays", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [ParameterRealValue]) }, { "AddHours", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [ParameterRealValue]) }, { "AddMilliseconds", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [ParameterRealValue]) }, { "AddMinutes", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [ParameterRealValue]) }, { "AddMonths", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [new("int", "value", NoExtraTypes, ParameterAttributes.None, "")]) }, { "AddSeconds", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [ParameterRealValue]) }, { "AddTicks", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [new("long int", "value", NoExtraTypes, ParameterAttributes.None, "")]) }, { "AddYears", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [new("int", "value", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Compare", (NoExtraTypes, "short int", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new("DateTime", "left", NoExtraTypes, ParameterAttributes.None, ""), new("DateTime", "right", NoExtraTypes, ParameterAttributes.None, "")]) }, { "CompareTo", (NoExtraTypes, "short int", NoExtraTypes, FunctionAttributes.None, [new("DateTime", "value", NoExtraTypes, ParameterAttributes.None, "")]) }, { "DaysInMonth", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new("int", "year", NoExtraTypes, ParameterAttributes.None, ""), new("int", "month", NoExtraTypes, ParameterAttributes.None, "")]) }, { "IsLeapYear", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new("int", "year", NoExtraTypes, ParameterAttributes.None, "")]) }, { "IsSummertime", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, []) }, { "SpecifyKind", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, [new("DateTimeKind", "kind", NoExtraTypes, ParameterAttributes.None, "")]) }, { "ToBinary", (NoExtraTypes, "long int", NoExtraTypes, FunctionAttributes.None, []) }, { "ToLocalTime", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, []) }, { "ToUniversalTime", (NoExtraTypes, "DateTime", NoExtraTypes, FunctionAttributes.None, []) } } }, { "IChar", new() { { "IsDigit", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsLetter", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsLetterOrDigit", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsLower", (NoExtraTypes, "bool", ExtraTypesT, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsSeparator", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsUpper", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "IsWhiteSpace", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "ToLower", (NoExtraTypes, "IChar", ExtraTypesT, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) }, { "ToUpper", (NoExtraTypes, "IChar", ExtraTypesT, FunctionAttributes.Static | FunctionAttributes.Multiconst, [ParameterICharT]) } } }, { "IComparable", new() { { "CompareTo", (NoExtraTypes, "short int", NoExtraTypes, FunctionAttributes.None, [new("T", "value", NoExtraTypes, ParameterAttributes.None, "")]) } } }, { "list", new() { { "Add", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterListT]) }, { "Clear", (NoExtraTypes, "null", NoExtraTypes, FunctionAttributes.None, []) }, { "Contains", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, [new("T", "item", NoExtraTypes, ParameterAttributes.None, "")]) }, { "Exists", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, [ParameterPredicate]) }, { "Find", (NoExtraTypes, "T", NoExtraTypes, FunctionAttributes.None, [ParameterPredicate]) }, { "FindAll", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterPredicate]) }, { "FindLast", (NoExtraTypes, "T", NoExtraTypes, FunctionAttributes.None, [ParameterPredicate]) }, { "GetRange", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterIndex, ParameterLength]) }, { "Insert", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterIndex, ParameterListT]) }, { "Remove", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterIndex, ParameterLength]) }, { "RemoveAll", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [ParameterPredicate]) }, { "RemoveValue", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, [new("T", "item", NoExtraTypes, ParameterAttributes.None, "")]) }, { "RemoveLast", (NoExtraTypes, "list", ExtraTypesT, FunctionAttributes.None, []) }, { "TrueForAll", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, [ParameterPredicate]) } } }, { "object", new() { { "GetHashCode", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, []) } } }, { "string", new() { { "Contains", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "ContainsAny", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [ParameterChars]) }, { "ContainsAnyExcluding", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [ParameterChars]) }, { "Length", (NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "EndsWith", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "GetAfter", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "GetBefore", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "Insert", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, [ParameterIndex, ParameterSubstring]) }, { "StartsWith", (NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None, [ParameterSubstring, ParameterIgnoreCase]) }, { "ToLower", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, []) }, { "ToUpper", (NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, []) } } } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ArrayParameters, ReturnType, ReturnArrayParameters, Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeSortedList<GeneralMethods> GeneralMethodsList { get; } = new() { { new(), new() { { "ExecuteString", new() { ([], (new([new(BlockType.Primitive, "universal", 1)]), NoGeneralExtraTypes), FunctionAttributes.Multiconst, [GeneralParameterStringS, new(new([new(BlockType.Primitive, "universal", 1)]), "parameters", NoGeneralExtraTypes, ParameterAttributes.Params, "")], null) } } } }, { GeneralTypeList, new() { { "BinarySearch", new() { ([], IntType, FunctionAttributes.None, [GeneralParameterTValue], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterStartIndex, GeneralParameterLength], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterStartIndex, GeneralParameterLength, GeneralParameterStartIndex, GeneralParameterComparer], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterComparer], null) } }, { "CopyTo", new() { ([], (GeneralTypeList, GeneralExtraTypesT), FunctionAttributes.None, [new(GeneralTypeList, "dest_list", GeneralExtraTypesT, ParameterAttributes.None, ""), GeneralParameterLength], null), ([], (GeneralTypeList, GeneralExtraTypesT), FunctionAttributes.None, [new(GeneralTypeInt, "src_index", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeList, "dest_list", GeneralExtraTypesT, ParameterAttributes.None, ""), new(GeneralTypeInt, "dest_index", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterLength], null) } }, { "FindIndex", new() { ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate], null), ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate, GeneralParameterIndex], null), ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate, GeneralParameterIndex, GeneralParameterLength], null) } }, { "FindLastIndex", new() { ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate], null), ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate, GeneralParameterIndex], null), ([], IntType, FunctionAttributes.None, [GeneralParameterPredicate, GeneralParameterIndex, GeneralParameterLength], null) } }, { "IndexOf", new() { ([], IntType, FunctionAttributes.None, [GeneralParameterTValue], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterIndex], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterIndex, GeneralParameterLength], null) } }, { "LastIndexOf", new() { ([], IntType, FunctionAttributes.None, [GeneralParameterTValue], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterIndex], null), ([], IntType, FunctionAttributes.None, [GeneralParameterTValue, GeneralParameterIndex, GeneralParameterLength], null) } }, { "Reverse", new() { ([], (GeneralTypeList, NoGeneralExtraTypes), FunctionAttributes.None, [], null), ([], (GeneralTypeList, NoGeneralExtraTypes), FunctionAttributes.None, [GeneralParameterIndex, GeneralParameterLength], null) } } } }, { GetPrimitiveBlockStack("object"), new() { { "ToString", new() { ([], StringType, FunctionAttributes.Multiconst, [], null) } } } },
		{ GeneralTypeString, new() { { "CompareTo", new() { ([], (new([new(BlockType.Primitive, "short int", 1)]), NoGeneralExtraTypes), FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, new(GeneralTypeInt, "index1", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterString2, new(GeneralTypeInt, "index2", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterLength, GeneralParameterIgnoreCase], null), ([], (new([new(BlockType.Primitive, "short int", 1)]), NoGeneralExtraTypes), FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, new(GeneralTypeInt, "index1", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterString2, new(GeneralTypeInt, "index2", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterLength, GeneralParameterStringComparer], null), ([], (new([new(BlockType.Primitive, "short int", 1)]), NoGeneralExtraTypes), FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, GeneralParameterString2, GeneralParameterIgnoreCase], null), ([], (new([new(BlockType.Primitive, "short int", 1)]), NoGeneralExtraTypes), FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, GeneralParameterString2, GeneralParameterStringComparer], null) } }, { "Concat", new() { ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, GeneralParameterString2], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterString1, GeneralParameterString2, GeneralParameterString3], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new(GeneralTypeString, "strings", NoGeneralExtraTypes, ParameterAttributes.Params, "")], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterStringCollection], null), ([(false, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "T")], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterTCollection], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("universal"), "object1", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object2", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("universal"), "object1", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object2", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object3", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("universal"), "objects", NoGeneralExtraTypes, ParameterAttributes.Params, "")], null) } }, { "IndexOf", new() { ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterLength], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterIgnoreCase], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterIgnoreCase], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterLength, GeneralParameterIgnoreCase], null) } }, { "IndexOfAny", new() { ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "IndexOfAnyExcluding", new() { ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "Join", new() { ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterSeparator, GeneralParameterStrings, GeneralParameterStartIndex, GeneralParameterLength], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterSeparator, new(GeneralTypeString, "strings", NoGeneralExtraTypes, ParameterAttributes.Params, "")], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterSeparator, GeneralParameterStringCollection], null), ([(false, NoGeneralExtraTypes, GetPrimitiveBlockStack("typename"), "T")], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [GeneralParameterSeparator, GeneralParameterTCollection], null), ([], StringType, FunctionAttributes.Static | FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("universal"), "objects", NoGeneralExtraTypes, ParameterAttributes.Params, "")], null) } }, { "LastIndexOf", new() { ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterLength], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterIgnoreCase], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterIgnoreCase], null), ([], IntType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "substring", NoGeneralExtraTypes, ParameterAttributes.None, ""), GeneralParameterStartIndex, GeneralParameterLength, GeneralParameterIgnoreCase], null) } }, { "LastIndexOfAny", new() { ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "LastIndexOfAnyExcluding", new() { ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex], null), ([], IntType, FunctionAttributes.Multiconst, [GeneralParameterChars, GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "RemoveValue", new() { ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterStartIndex], null), ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "Replace", new() { ([], StringType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "old_char", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("char"), "new_char", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], StringType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "old_string", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeString, "new_string", NoGeneralExtraTypes, ParameterAttributes.None, "")], null) } }, { "ReplaceFirst", new() { ([], StringType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "old_char", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("char"), "new_char", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], StringType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "old_string", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeString, "new_string", NoGeneralExtraTypes, ParameterAttributes.None, "")], null) } }, { "ReplaceLast", new() { ([], StringType, FunctionAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "old_char", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("char"), "new_char", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], StringType, FunctionAttributes.Multiconst, [new(GeneralTypeString, "old_string", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeString, "new_string", NoGeneralExtraTypes, ParameterAttributes.None, "")], null) } }, { "Split", new() { ([], (GeneralTypeList, [new(StringType)]), FunctionAttributes.Multiconst, [new(GeneralTypeList, "separators", [new(GetPrimitiveType("char"))], ParameterAttributes.None, "")], null), ([], (GeneralTypeList, [new(StringType)]), FunctionAttributes.Multiconst, [new(GeneralTypeList, "separators", [new(GetPrimitiveType("char"))], ParameterAttributes.None, ""), new(GeneralTypeInt, "count", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), ([], (GeneralTypeList, [new(StringType)]), FunctionAttributes.Multiconst, [new(GeneralTypeList, "separators", [new(StringType)], ParameterAttributes.None, "")], null), ([], (GeneralTypeList, [new(StringType)]), FunctionAttributes.Multiconst, [new(GeneralTypeList, "separators", [new(StringType)], ParameterAttributes.None, ""), new(GeneralTypeInt, "count", NoGeneralExtraTypes, ParameterAttributes.None, "")], null) } }, { "Substring", new() { ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterStartIndex], null), ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "ToCharList", new() { ([], (GeneralTypeList, [new(GetPrimitiveType("char"))]), FunctionAttributes.Multiconst, [], null), ([], (GeneralTypeList, [new(GetPrimitiveType("char"))]), FunctionAttributes.Multiconst, [GeneralParameterStartIndex, GeneralParameterLength], null) } }, { "Trim", new() { ([], StringType, FunctionAttributes.Multiconst, [], null), ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterChars], null) } }, { "TrimEnd", new() { ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterChars], null) } }, { "TrimStart", new() { ([], StringType, FunctionAttributes.Multiconst, [GeneralParameterChars], null) } } } } };

	/// <summary>
	/// Sorted by Container, then by Name, also contains ArrayParameters, ReturnType, ReturnArrayParameters, Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<UserDefinedMethods> UserDefinedFunctionsList { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains Attributes, ParameterTypes, ParameterNames, ParameterArrayParameters, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeSortedList<ConstructorOverloads> ConstructorsList { get; } = new() { { GetPrimitiveBlockStack("DateTime"), new() { (ConstructorAttributes.Multiconst, [new(GeneralTypeInt, "year", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "month", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "day", NoGeneralExtraTypes, ParameterAttributes.None, "")], null), (ConstructorAttributes.Multiconst, [new(GeneralTypeInt, "year", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "month", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "day", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "hours", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "minutes", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "seconds", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), "kind", NoGeneralExtraTypes, ParameterAttributes.Optional, "DateTimeKind.Unspecified")], null), (ConstructorAttributes.Multiconst, [new(GeneralTypeInt, "year", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "month", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "day", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "hours", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "minutes", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "seconds", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "milliseconds", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), "kind", NoGeneralExtraTypes, ParameterAttributes.Optional, "DateTimeKind.Unspecified")], null), (ConstructorAttributes.Multiconst, [new(new([new(BlockType.Primitive, "long int", 1)]), "ticks", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), "kind", NoGeneralExtraTypes, ParameterAttributes.Optional, "DateTimeKind.Unspecified")], null) } }, { GeneralTypeList, new() { (ConstructorAttributes.Multiconst, [GeneralParameterTCollection], null) } }, { GeneralTypeString, new() { (ConstructorAttributes.Multiconst, [new(GetPrimitiveBlockStack("char"), "c", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GeneralTypeInt, "count", NoGeneralExtraTypes, ParameterAttributes.None, "")], null) } } };

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
	public static TypeSortedList<ImplicitConversions> ImplicitConversionsList { get; } = new() { { GeneralTypeBool, new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (ByteType, false) } } } }, { GetPrimitiveBlockStack("byte"), new() { { NoGeneralExtraTypes, new() { (GetPrimitiveType("short char"), false), (UnsignedIntType, false), (IntType, false), (UnsignedShortIntListType, false), (ShortIntType, false), (BoolType, true) } } } }, { GetPrimitiveBlockStack("char"), new() { { NoGeneralExtraTypes, new() { (UnsignedShortIntType, false), (StringType, false) } } } }, { GeneralTypeInt, new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (UnsignedIntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true) } } } }, { GeneralTypeList, new() { { new() { GetPrimitiveType("char") }, new() { (StringType, false) } } } }, { GetPrimitiveBlockStack("long char"), new() { { NoGeneralExtraTypes, new() { (UnsignedIntType, false) } } } }, { GetPrimitiveBlockStack("long int"), new() { { NoGeneralExtraTypes, new() { (UnsignedLongIntType, false), (BoolType, true), (UnsignedShortIntListType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("real"), new() { { NoGeneralExtraTypes, new() { (BoolType, true), (UnsignedLongIntType, true), (LongIntType, true), (UnsignedIntType, true), (IntType, true) } } } }, { GetPrimitiveBlockStack("short char"), new() { { NoGeneralExtraTypes, new() { (ByteType, false) } } } }, { GetPrimitiveBlockStack("short int"), new() { { NoGeneralExtraTypes, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (UnsignedShortIntType, false), (BoolType, true), (ByteType, true) } } } }, { GeneralTypeString, new() { { NoGeneralExtraTypes, new() { ((GeneralTypeList, [new(GetPrimitiveType("char"))]), false) } } } }, { GetPrimitiveBlockStack("unsigned int"), new() { { NoGeneralExtraTypes, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (IntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true) } } } }, { GetPrimitiveBlockStack("unsigned long int"), new() { { NoGeneralExtraTypes, new() { (LongIntType, false), (BoolType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } } } }, { GetPrimitiveBlockStack("unsigned short int"), new() { { NoGeneralExtraTypes, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (ShortIntType, false), (BoolType, true), (ByteType, true) } } } } };

	/// <summary>
	/// Sorted by tuple, contains DestType and DestUnvType.ExtraTypes.
	/// </summary>
	public static List<UniversalType> ImplicitConversionsFromAnythingList { get; } = [(GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), (GeneralTypeString, NoGeneralExtraTypes), (GetPrimitiveBlockStack("null"), NoGeneralExtraTypes), (GeneralTypeList, [new(new BlockStack([new(BlockType.Primitive, "[this]", 1)]), NoGeneralExtraTypes)])];

	public static G.SortedSet<String> NotImplementedNamespacesList { get; } = ["System.Diagnostics", "System.Globalization", "System.IO", "System.Runtime", "System.Text", "System.Threading", "System.Windows", "System.Windows.Forms"];

	/// <summary>
	/// Sorted by Namespace, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedNamespacesList { get; } = new() { { "System.Collections.Generic", "System.Collections" }, { "System.Linq", "Hawaytenn operators" } };

	public static G.SortedSet<String> ReservedNamespacesList { get; } = ["Microsoft", "System.Activities", "System.AddIn", "System.CodeDom", "Concurrent", "ObjectModel", "Specialized", "System.ComponentModel", "System.Configuration", "System.Data", "System.Deployment", "System.Device", "System.Diagnostics.CodeAnalysis", "System.Diagnostics.Contracts", "System.Diagnostics.Design", "System.Diagnostics.Eventing", "System.Diagnostics.PerformanceData", "System.Diagnostics.SymbolStore", "System.Diagnostics.Tracing", "System.DirectoryServices", "System.Drawing", "System.Dynamic", "System.EnterpriseServices", "System.IdentityModel", "System.IO.Compression", "System.IO.IsolatedStorage", "System.IO.Log", "System.IO.MemoryMappedFiles", "System.IO.Packaging", "System.IO.Pipes", "System.IO.Ports", "System.Management", "System.Media", "System.Messaging", "System.Net", "System.Numerics", "System.Printing", "System.Reflection", "System.Resources", "System.Runtime.Caching", "System.Runtime.CompilerServices", "System.Runtime.ConstrainedExecution", "System.Runtime.DesignerServices", "System.Runtime.ExceptionServices", "System.Runtime.Hosting", "System.Runtime.InteropServices", "System.Runtime.Remoting", "System.Runtime.Serialization", "System.Runtime.Versioning", "System.Security", "System.ServiceModel", "System.ServiceProcess", "System.Speech", "System.StubHelpers", "System.Text.RegularExpressions", "System.Threading.Tasks", "System.Timers", "System.Transactions", "System.Web", "System.Windows.Annotations", "System.Windows.Automation", "System.Windows.Baml2006", "System.Windows.Controls", "System.Windows.Data", "System.Windows.Documents", "System.Windows.Forms.ComponentModel", "System.Windows.Forms.DataVisualization", "System.Windows.Forms.Design", "System.Windows.Forms.Interaction", "System.Windows.Forms.Layout", "System.Windows.Forms.PropertyGridInternal", "System.Windows.Forms.VisualStyles", "System.Windows.Ink", "System.Windows.Input", "System.Windows.Interop", "System.Windows.Markup", "System.Windows.Media", "System.Windows.Navigation", "System.Windows.Resources", "System.Windows.Shapes", "System.Windows.Threading", "System.Windows.Xps", "System.Workflow", "System.Xaml", "System.Xml", "Windows", "XamlGeneratedNamespace"];

	public static G.SortedSet<(String Namespace, String Type)> NotImplementedTypesList { get; } = [("", "complex"), ("", "long complex"), ("", "long long"), ("", "long real"), ("System", "Delegate"), ("System", "Enum"), ("System", "Environment"), ("System", "OperatingSystem")];

	/// <summary>
	/// Sorted by Namespace and Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), string> OutdatedTypesList { get; } = new() { { ("", "*Exception"), "\"if error ...\"" }, { ("", "double"), "real or long real" }, { ("", "float"), "real or long real" }, { ("", "uint"), "unsigned int" }, { ("", "ulong"), "unsigned long int" }, { ("", "ushort"), "unsigned short int" }, { ("System", "Array"), "list" }, { ("System", "Boolean"), "bool" }, { ("System", "Byte"), "byte (from the small letter)" }, { ("System", "Char"), "char (from the small letter), short char or long char" }, { ("System", "Console"), "labels and textboxes" }, { ("System", "ConsoleCancelEventArgs"), "TextBox.KeyDown, TextBox.KeyPress and TextBox.KeyUp" }, { ("System", "ConsoleCancelEventHandler"), "TextBox keyboard events" }, { ("System", "ConsoleColor"), "RichTextBox text color" }, { ("System", "ConsoleKey"), "other item enums" }, { ("System", "ConsoleKeyInfo"), "other item info classes" }, { ("System", "ConsoleModifiers"), "other item modifiers enums" }, { ("System", "ConsoleSpecialKey"), "other item enums" }, { ("System", "Double"), "real or long real" }, { ("System", "Int16"), "short int" }, { ("System", "Int32"), "int" }, { ("System", "Int64"), "long int" }, { ("System", "Object"), "object (from the small letter)" }, { ("System", "Random"), "Random(), IntRandom() etc." }, { ("System", "SByte"), "byte or short int" }, { ("System", "Single"), "real or long real" }, { ("System", "String"), "string (from the small letter)" }, { ("System", "Type"), "typename" }, { ("System", "UInt16"), "unsigned short int" }, { ("System", "UInt32"), "unsigned int" }, { ("System", "UInt64"), "unsigned long int" }, { ("System", "Void"), "null" }, { ("System.Collections", "G.HashSet"), "G.HashSet" }, { ("System.Collections", "Hashtable"), "HashTable" }, { ("System.Collections", "G.KeyValuePair"), "tuples" }, { ("System.Collections", "G.SortedSet"), "G.SortedSet" } };

	public static G.SortedSet<(String Namespace, String Type)> ReservedTypesList { get; } = [("", "*Attribute"), ("", "*Comparer"), ("", "*Enumerator"), ("", "*UriParser"), ("", "decimal"), ("System", "ActivationContext"), ("System", "ActivationContext.ContextForm"), ("System", "Activator"), ("System", "AppContext"), ("System", "AppDomain"), ("System", "AppDomainInitializer"), ("System", "AppDomainManager"), ("System", "AppDomainManagerInitializationOptions"), ("System", "AppDomainSetup"), ("System", "ApplicationId"), ("System", "ApplicationIdentity"), ("System", "ArgIterator"), ("System", "ArraySegment"), ("System", "AssemblyLoadEventArgs"), ("System", "AsyncCallback"), ("System", "AttributeTargets"), ("System", "Base64FormattingOptions"), ("System", "BitConverter"), ("System", "Buffer"), ("System", "Comparison"), ("System", "ContextBoundObject"), ("System", "ContextStaticAttribute"), ("System", "Convert"), ("System", "Converter"), ("System", "CrossAppDomainDelegate"), ("System", "DateTimeOffset"), ("System", "DBNull"), ("System", "Decimal"), ("System", "Environment.SpecialFolder"), ("System", "Environment.SpecialFolderOption"), ("System", "EnvironmentVariableTarget"), ("System", "EventArgs"), ("System", "EventHandler"), ("System", "FormattableString"), ("System", "GC"), ("System", "GCCollectionMode"), ("System", "GCNotificationStatus"), ("System", "GenericUriParserOptions"), ("System", "Guid"), ("System", "IAppDomainSetup"), ("System", "IAsyncResult"), ("System", "ICloneable"), ("System", "ICustomFormattable"), ("System", "IDisposable"), ("System", "IFormatProvider"), ("System", "IFormattable"), ("System", "IObservable"), ("System", "IObserver"), ("System", "IProgress"), ("System", "IServiceProvider"), ("System", "Lazy"), ("System", "LoaderOptimization"), ("System", "LocalDataStoreSlot"), ("System", "MarshalByRefObject"), ("System", "Math"), ("System", "MidpointRounding"), ("System", "ModuleHandle"), ("System", "MulticastDelegate"), ("System", "Nullable"), ("System", "PlatformID"), ("System", "Progress"), ("System", "ResolveEventArgs"), ("System", "ResolveEventHandler"), ("System", "RuntimeArgumentHandle"), ("System", "RuntimeFieldHandle"), ("System", "RuntimeMethodHandle"), ("System", "RuntimeTypeHandle"), ("System", "StringComparer"), ("System", "StringComparison"), ("System", "StringSplitOptions"), ("System", "TimeZone"), ("System", "TimeZoneInfo"), ("System", "TimeZoneInfo.AdjustmentRule"), ("System", "TimeZoneInfo.TransitionTime"), ("System", "Tuple"), ("System", "TupleExtensions"), ("System", "TypeCode"), ("System", "TypedReference"), ("System", "UIntPtr"), ("System", "Uri"), ("System", "UriBuilder"), ("System", "UriComponents"), ("System", "UriFormat"), ("System", "UriHostNameType"), ("System", "UriIdnScope"), ("System", "UriKind"), ("System", "UriPartial"), ("System", "UriTemplate"), ("System", "UriTemplateEquivalenceComparer"), ("System", "UriTemplateMatch"), ("System", "UriTemplateTable"), ("System", "UriTypeConverter"), ("System", "ValueTuple"), ("System", "ValueType"), ("System", "Version"), ("System", "WeakReference"), ("System", "_AppDomain"), ("System.Collections", "ArrayList"), ("System.Collections", "CaseInsensitiveHashCodeProvider"), ("System.Collections", "CollectionBase"), ("System.Collections", "Dictionary.KeyCollection"), ("System.Collections", "Dictionary.ValueCollection"), ("System.Collections", "DictionaryBase"), ("System.Collections", "DictionaryEntry"), ("System.Collections", "IHashCodeProvider"), ("System.Collections", "IReadOnlyCollection"), ("System.Collections", "IReadOnlyDictionary"), ("System.Collections", "IReadOnlyList"), ("System.Collections", "ISet"), ("System.Collections", "IStructuralComparable"), ("System.Collections", "IStructuralEquatable"), ("System.Collections", "KeyedByTypeCollection"), ("System.Collections", "ReadOnlyCollectionBase"), ("System.Collections", "StructuralComparisons"), ("System.Collections", "SynchronizedCollection"), ("System.Collections", "SynchronizedKeyedCollection"), ("System.Collections", "SynchronizedReadOnlyCollection")];

	public static G.SortedSet<(String Namespace, String Type)> EmptyTypesList { get; } = [("", "BaseClass"), ("", "IntPtr"), ("", "TimeSpan"), ("System.Collections", "BitArray"), ("System.Collections", "Dictionary"), ("System.Collections", "G.HashSet"), ("System.Collections", "HashTable"), ("System.Collections", "G.LinkedList"), ("System.Collections", "LinkedListNode"), ("System.Collections", "Queue"), ("System.Collections", "G.SortedSet"), ("System.Collections", "Stack")];

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
	public static TypeSortedList<OutdatedMethods> OutdatedMethodOverloadsList { get; } = new() { { new([new(BlockType.Interface, "IChar", 1)]), new() { { "IsDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLetter", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLetterOrDigit", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsLower", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsSeparator", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsUpper", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } }, { "IsWhiteSpace", new() { ([GeneralParameterStringS, GeneralParameterIndex], "info[index] as a parameter") } } } }, { GeneralTypeString, new() { { "Concat", new() { ([GeneralParameterString1, GeneralParameterString2, GeneralParameterString3, new(GeneralTypeString, "string4", NoGeneralExtraTypes, ParameterAttributes.None, "")], "concatenation in pairs, triples or in an array"), ([new(GetPrimitiveBlockStack("universal"), "object1", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object2", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object3", NoGeneralExtraTypes, ParameterAttributes.None, ""), new(GetPrimitiveBlockStack("universal"), "object4", NoGeneralExtraTypes, ParameterAttributes.None, "")], "concatenation in pairs, triples or in an array") } } } } };

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

	public static G.SortedSet<String> AutoCompletionList { get; } = new(new List<String>("abstract", "break", "case", "Class", "closed", "const", "Constructor", "continue", "default", "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst", "Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref", "repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while", "and", "or", "xor", "is", "typeof", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "Infty", "Uncty", "Pi", "E", "CombineWith", "CloseOnReturnWith", "pow", "tetra", "penta", "hexa").AddRange(PrimitiveTypesList).AddRange(ExtraTypesList.Convert(x => x.Namespace.Concat(".").AddRange(x.Type))).AddRange(CompositeTypesList.Keys.Convert(x => x.Namespace.Concat(".").AddRange(x.Type))).AddRange(GeneralTypesList.Keys.Convert(x => ((String)x.Container.ToString()).Concat(".").AddRange(x.Type))).AddRange(PropertiesList.Values.ConvertAndJoin(x => x.Keys)).AddRange(PublicFunctionsList.Keys).AddRange(MethodsList.Values.ConvertAndJoin(x => x.Keys)).AddRange(GeneralMethodsList.Values.ConvertAndJoin(x => x.Keys)));

	public static void Add<T>(ref IList<T>? list, T item)
	{
		list ??= typeof(bool).IsAssignableTo(typeof(T)) ? (new BitList() is IList<T> list_ ? list_ : throw new ArgumentException(null, nameof(list))) : new List<T>();
		list.Add(item);
	}

	public static void Add<T>(ref List<T>? list, T item)
	{
		list ??= [];
		list.Add(item);
	}

	public static void AddRange<T>(ref List<T>? list, G.IEnumerable<T> collection)
	{
		if (collection is not null)
		{
			list ??= [];
			list.AddRange(collection);
		}
	}

	public static void Add<T>(List<List<T>?> list, Index index, T item)
	{
		if (list[index] is null)
			list[index] = [];
		list[index]?.Add(item);
	}

	public static void AddRange<T>(List<List<T>?> list, Index index, G.IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			if (list[index] is null)
				list[index] = [];
			list[index]?.AddRange(collection);
		}
	}

	public static int BinarySearch<TKey, TValue>(this SortedDictionary<TKey, TValue> list, TKey item) where TKey : notnull where TValue : notnull => BinarySearch(list, item, list.Comparer);

	public static int BinarySearch<TKey, TValue>(this SortedDictionary<TKey, TValue> list, TKey item, G.IComparer<TKey> comparer) where TKey : notnull where TValue : notnull
	{
		int start = 0, end = list.Length - 1;
		while (start < end)
		{
			var i = (start + end) >> 1;
			var order = comparer.Compare(list.Keys[i], item);
			if (order == 0)
				return i;
			else if (order < 0)
				start = i + 1;
			else
				end = i - 1;
		}
		return ~start;
	}

	public static bool TypeEqualsToPrimitive(UniversalType type, String primitive, bool noExtra = true) => TypeIsPrimitive(type.MainType) && type.MainType.Peek().Name == primitive && (!noExtra || type.ExtraTypes.Length == 0);

	public static bool TypeIsPrimitive(BlockStack type) => type.Length == 1 && type.Peek().Type == BlockType.Primitive;

	public static UniversalType GetPrimitiveType(String primitive) => (new([new(BlockType.Primitive, primitive, 1)]), NoGeneralExtraTypes);

	public static BlockStack GetPrimitiveBlockStack(String primitive) => new([new(BlockType.Primitive, primitive, 1)]);

	public static UniversalType GetListType(UniversalTypeOrValue InnerType)
	{
		if (InnerType.MainType.IsValue || !TypeEqualsToPrimitive((InnerType.MainType.Type, InnerType.ExtraTypes), "list", false))
			return new(ListBlockStack, new([InnerType]));
		else if (InnerType.ExtraTypes.Length >= 2 && InnerType.ExtraTypes[0].MainType.IsValue && int.TryParse(InnerType.ExtraTypes[0].MainType.Value.ToString(), out var number))
			return new(ListBlockStack, new([new((TypeOrValue)(number + 1).ToString(), []), InnerType.ExtraTypes[^1]]));
		else
			return new(ListBlockStack, new([new((TypeOrValue)"2", []), InnerType.ExtraTypes[^1]]));
	}

	public static String TakeIntoQuotes(this String input, bool oldStyle = false)
	{
		String list = new((int)Ceiling(input.Length * 1.1), '\"');
		for (var i = 0; i < input.Length; i++)
		{
			list.AddRange(input[i] switch
			{
				'\0' => new String('\\', '0'),
				'\a' => new String('\\', 'a'),
				'\b' => new String('\\', 'b'),
				'\f' => new String('\\', 'f'),
				'\n' => new String('\\', 'n'),
				'\r' => new String('\\', 'r'),
				'\t' => new String('\\', 't'),
				'\v' => new String('\\', 'v'),
				'\"' => new String('\\', oldStyle ? '\"' : 'q'),
				'\\' => new String('\\', oldStyle ? '\\' : '!'),
				_ => new String(input[i]),
			});
		}
		list.Add('\"');
		return list;
	}

	public static String TakeIntoVerbatimQuotes(this String input) => ((String)"@\"").AddRange(input.Replace("\"", "\"\"")).Add('\"');

	public static bool TryTakeIntoRawQuotes(this String input, out String output)
	{
		if (IsRawStringContent(input))
		{
			output = ((String)"/\"").AddRange(input).AddRange("\"/");
			return true;
		}
		else
		{
			output = [];
			return false;
		}
	}

	public static String RemoveQuotes(this String input)
	{
		if (input.Length >= 3 && input[0] == '@' && input[1] == '\"' && input[^1] == '\"')
			return input[2..^1].Replace("\"\"", "\"");
		if (!(input.Length >= 2 && (input[0] == '\"' && input[^1] == '\"' || input[0] == '\'' && input[^1] == '\'')))
		{
			if (input.IsRawString(out var input2))
				return input2;
			else
				return input;
		}
		String list = new(input.Length);
		var state = 0;
		var hex_left = 0;
		var n = 0;
		for (var i = 1; i < input.Length - 1; i++)
		{
			var c = input[i];
			if (state == 0 && c == '\\')
			{
				if (state == 1)
				{
					list.Add('\\');
					state = 0;
				}
				else
					state = 1;
			}
			else if (state == 2)
				State2(c);
			else if (state == 1)
			{
				if (c == 'x')
				{
					state = 2;
					hex_left = 2;
				}
				else if (c == 'u')
				{
					state = 2;
					hex_left = 4;
				}
				else
				{
					State1(c);
					state = 0;
				}
			}
			else
				list.Add(c);
		}
		return list;
		void State1(char c) => list.Add(c switch
		{
			'0' => '\0',
			'a' => '\a',
			'b' => '\b',
			'f' => '\f',
			'n' => '\n',
			'q' => '\"',
			'r' => '\r',
			't' => '\t',
			'v' => '\v',
			'\'' => '\'',
			'\"' => '\"',
			'!' => '\\',
			_ => c,
		});
		void State2(char c)
		{
			if (c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f')
			{
				n = n * 16 + ((c is >= '0' and <= '9') ? c - '0' : (c is >= 'A' and <= 'F') ? c - 'A' + 10 : c - 'a' + 10);
				if (hex_left == 1)
				{
					list.Add((char)n);
					state = 0;
				}
				hex_left--;
			}
			else
			{
				state = c == '\\' ? 1 : 0;
				hex_left = 0;
			}
		}
	}

	public static bool IsRawString(this String input, out String output)
	{
		if (!(input.Length >= 4 && input[0] == '/' && input[1] == '\"' && input[^2] == '\"' && input[^1] == '/'))
		{
			output = [];
			return false;
		}
		var content = input[2..^2];
		var b = IsRawStringContent(content);
		output = b ? content : [];
		return b;
	}

	private static bool IsRawStringContent(this String input)
	{
		int depth = 0, state = 0;
		for (var i = 0; i < input.Length; i++)
		{
			var c = input[i];
			if (c == '/')
			{
				if (state != 2)
					state = 1;
				else if (depth == 0)
					return false;
				else
				{
					depth--;
					state = 0;
				}
			}
			else if (c == '\"')
			{
				if (state == 1)
				{
					depth++;
					state = 0;
				}
				else
					state = 2;
			}
		}
		return true;
	}

	[GeneratedRegex("[0-9]+")]
	public static partial Regex IntRegex();

	[GeneratedRegex("""\G([0-9]+(?:\.[0-9]+)?(?:[Ee][+-][0-9]+)?)|((?:pow=?|and|or|xor|CombineWith|is|typeof|sin|cos|tan|asin|acos|atan|ln|_|this|null|Infty|-Infty|Uncty|Pi|E|I|G)(?![0-9A-Za-z_]))|((?:abstract|break|case|Class|closed|const|Constructor|continue|default|Delegate|delete|Destructor|else|Enum|Event|Extent|extern|false|for|foreach|Function|if|Interface|internal|lock|loop|multiconst|Namespace|new|null|Operator|out|override|params|protected|public|readonly|ref|repeat|return|sealed|static|Struct|switch|this|throw|true|using|while)(?![0-9A-Za-z_]))|([A-Za-z_][0-9A-Za-z_]*)|(\^[\^=]?|\|[\|=]?|&[&=]|>(?:>>?)?=?|<(?:<<?)?=?|![!=]?|\?(?:!?=|>=?|<=?|\?|\.|\[)?|,|\:|$|~|\+[+=]?|-[\-=]?|\*=?|/=?|%=?|=[=>]?|\.(?:\.\.?)?)|((?:"(?:[^\\"]|\\[0abfnqrtv'"!]|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4})*?")|(?:@"(?:[^"]|"")*?"))|('(?:[^\\'"]|\\[0abfnqrtv'"!]|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4})?')|([;()[\]{}])|([ \t\r\n\xA0]+)|(.)""")]
	public static partial Regex LexemRegex();
}

public sealed class BlockComparer : G.IComparer<Block>
{
	public int Compare(Block? x, Block? y)
	{
		if (x is null || y is null)
			return (x is null ? 1 : 0) - (y is null ? 1 : 0);
		if (x.Type < y.Type)
			return 1;
		else if (x.Type > y.Type)
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
	public bool Equals(Block? x, Block? y) => x is null && y is null || x is not null && y is not null && x.Type == y.Type && x.Name == y.Name;

	public int GetHashCode(Block x) => x.Type.GetHashCode() ^ x.Name.GetHashCode();
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

public sealed class FullTypeEComparer : G.IEqualityComparer<UniversalType>
{
	public bool Equals(UniversalType x, UniversalType y) => new BlockStackEComparer().Equals(x.MainType, y.MainType) && new GeneralExtraTypesEComparer().Equals(x.ExtraTypes, y.ExtraTypes);

	public int GetHashCode(UniversalType x) => new BlockStackEComparer().GetHashCode(x.MainType) ^ new GeneralExtraTypesEComparer().GetHashCode(x.ExtraTypes);
}

public sealed class StringComparer : G.IEqualityComparer<String>
{
	public bool Equals(String? x, String? y) => x is null && y is null || x == y;

	public int GetHashCode(String x)
	{
		try
		{
			return (x[0] << 4) ^ (x[1] << 2) ^ x[^1];
		}
		catch
		{
			return -123456789;
		}
	}
}

public record struct UniversalType(BlockStack MainType, GeneralExtraTypes ExtraTypes)
{
	public override readonly string ToString() => TypeEqualsToPrimitive(this, "list", false) ? "list(" + (ExtraTypes.Length == 2 ? ExtraTypes[0].ToString() : "") + ") " + ExtraTypes[^1].ToString() : MainType.ToString() + (ExtraTypes.Length == 0 ? "" : "[" + ExtraTypes.ToString() + "]");
	public static implicit operator UniversalType((BlockStack MainType, GeneralExtraTypes ExtraTypes) value) => new(value.MainType, value.ExtraTypes);
}

public record struct UniversalTypeOrValue(TypeOrValue MainType, GeneralExtraTypes ExtraTypes)
{
	public UniversalTypeOrValue(UniversalType value) : this(value.MainType, value.ExtraTypes) { }
	public override readonly string ToString() => MainType.IsValue ? MainType.Value.ToString() : new UniversalType(MainType.Type, ExtraTypes).ToString();
	public static implicit operator UniversalTypeOrValue((TypeOrValue MainType, GeneralExtraTypes ExtraTypes) value) => new(value.MainType, value.ExtraTypes);
	public static implicit operator UniversalTypeOrValue(UniversalType value) => new(value);
}

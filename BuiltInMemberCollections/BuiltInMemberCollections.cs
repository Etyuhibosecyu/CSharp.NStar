global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using static CSharp.NStar.NStarType;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Mpir.NET;
using NStar.BufferLib;
using NStar.ParallelHS;
using NStar.RemoveDoubles;
using NStar.SortedSets;
using NStar.SumCollections;
using NStar.TreeSets;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using ReactiveUI;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
	private static readonly BlockStack ExtendedTypeBool = new([new(BlockType.Primitive, "bool", 1)]);
	private static readonly BlockStack ExtendedTypeIFloatNumber = new([new(BlockType.Interface, "IFloatNumber", 1)]);
	private static readonly BlockStack ExtendedTypeIIncreasable = new([new(BlockType.Interface, "IIncreasable", 1)]);
	private static readonly BlockStack ExtendedTypeIIntegerNumber = new([new(BlockType.Interface, "IIntegerNumber", 1)]);
	private static readonly BlockStack ExtendedTypeINumber = new([new(BlockType.Interface, "INumber", 1)]);
	private static readonly BlockStack ExtendedTypeISignedIntegerNumber = new([new(BlockType.Interface, "ISignedIntegerNumber", 1)]);
	private static readonly BlockStack ExtendedTypeInt = new([new(BlockType.Primitive, "int", 1)]);
	private static readonly BlockStack ExtendedTypeList = new([new(BlockType.Primitive, "list", 1)]);
	private static readonly BlockStack ExtendedTypeString = new([new(BlockType.Primitive, "string", 1)]);
	private static readonly BranchCollection BranchCollectionT = [new("type", 0, []) { Extra = NStarTypeT }];
	private static readonly NStarType NStarTypeT = new(new([new(BlockType.Extra, "T", 1)]), NoBranches);
	private static readonly NStarType CharListType = GetListType(CharType);
	private static readonly NStarType NStarTypeIFloatNumberT = new(ExtendedTypeIFloatNumber, BranchCollectionT);
	private static readonly NStarType NStarTypeIIncreasableT = new(ExtendedTypeIIncreasable, BranchCollectionT);
	private static readonly NStarType NStarTypeIIntegerNumberT = new(ExtendedTypeIIntegerNumber, BranchCollectionT);
	private static readonly NStarType NStarTypeINumberT = new(ExtendedTypeINumber, BranchCollectionT);
	private static readonly NStarType NStarTypeISignedIntegerNumberT = new(ExtendedTypeISignedIntegerNumber, BranchCollectionT);
	private static readonly ExtendedMethodParameter ExtendedParameterIndex = new(IntType, "index", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterStringS = new(StringType, "s", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString1 = new(StringType, "string1", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString2 = new(StringType, "string2", ParameterAttributes.None, []);
	private static readonly ExtendedMethodParameter ExtendedParameterString3 = new(StringType, "string3", ParameterAttributes.None, []);

	public static SortedSet<String> Keywords { get; } = new(
		"_", "abstract", "break", "case", "Class", "const", "Constructor", "continue",
		"Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern",
		"false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", 
		"Megaclass", "multiconst", "Namespace", "new", "null", "Operator", "out", "override",
		"params", "private", "protected", "public", "readonly", "ref", "repeat", "return",
		"sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while"
	);

	public static SortedSet<String> EscapedKeywords { get; } = new(
		"abstract", "as", "base", "bool", "break", "byte",
		"case", "catch", "char", "checked", "class", "const", "continue",
		"decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
		"false", "finally", "fixed", "float", "for", "foreach", "goto",
		"if", "implicit", "in", "int", "interface", "internal", "is",
		"lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
		"params", "private", "protected", "public", "readonly", "ref", "return",
		"sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
		"this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
		"virtual", "void", "volatile", "while"
	);

	public static ImmutableArray<string> AssignmentOperators { get; } = ImmutableArray.Create("=", "+=", "-=", "*=", "/=", "%=", "pow=", "&=", "|=", "^=", ">>=", "<<=");

	public static ImmutableArray<string> TernaryOperators { get; } = ImmutableArray.Create("?", "?=", "?>", "?<", "?>=", "?<=", "?!=");

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type and ExtraTypes.
	/// </summary>
	public static TypeSortedList<TypeVariables> Variables { get; } = [];

	public static SortedSet<String> Namespaces { get; } = new("System", "System.Collections", "System.GUI", "System.IO", "System.Threading");

	public static G.HashSet<String> ImportedNamespaces { get; } = [];

	public static G.HashSet<String> UserDefinedNamespaces { get; } = [];

	public static SortedSet<String> ExplicitlyConnectedNamespaces { get; } = [];

	public static SortedDictionary<String, Type> PrimitiveTypes { get; } = new() 
	{
		{ "null", typeof(void) }, { "object", typeof(object) }, { "bool", typeof(bool) }, { "byte", typeof(byte) },
		{ "short char", typeof(byte) }, { "short int", typeof(short) }, { "unsigned short int", typeof(ushort) },
		{ "char", typeof(char) }, { "int", typeof(int) }, { "unsigned int", typeof(uint) }, 
		{ "long char", typeof(uint) }, { "long int", typeof(long) }, 
		{ "DateTime", typeof(DateTime) }, { "TimeSpan", typeof(TimeSpan) }, { "unsigned long int", typeof(long) },
		{ "real", typeof(double) }, { "long long", typeof(MpzT) }, { "complex", typeof(Complex) },
		{ "typename", typeof(Type) }, { "string", typeof(String) }, { "index", typeof(Index) }, { "range", typeof(Range) },
		{ "nint", typeof(nint) }, { "list", typeof(List<>) }, { "dynamic", typeof(void) }, { "var", typeof(void) }
	};

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static Mirror<(String Namespace, String Type), Type> ExtraTypes { get; } = new()
	{
		{ ("", nameof(IComparable<>)), typeof(IComparable<>) },
		{ ("", nameof(IEquatable<>)), typeof(IEquatable<>) },
		{ ("Environment", nameof(Environment.SpecialFolder)), typeof(Environment.SpecialFolder) },
		{ ("Environment", nameof(Environment.SpecialFolderOption)), typeof(Environment.SpecialFolderOption) },
		{ ("System", nameof(Convert)), typeof(Convert) },
		{ ("System", nameof(DateTimeKind)), typeof(DateTimeKind) },
		{ ("System", nameof(DayOfWeek)), typeof(DayOfWeek) },
		{ ("System", nameof(Environment)), typeof(Environment) },
		{ ("System", nameof(EventArgs)), typeof(EventArgs) },
		{ ("System", nameof(EventHandler)), typeof(EventHandler<>) },
		{ ("System", "IFloatNumber"), typeof(IFloatingPointConstants<>) },
		{ ("System", "IIntegerNumber"), typeof(IBinaryInteger<>) },
		{ ("System", "INumber"), typeof(INumberBase<>) },
		{ ("System", nameof(ISignedNumber<>)), typeof(ISignedNumber<>) },
		{ ("System", nameof(IUnsignedNumber<>)), typeof(IUnsignedNumber<>) },
		{ ("System", nameof(Predicate<>)), typeof(Predicate<>) },
		{ ("System", nameof(ReadOnlySpan<>)), typeof(ReadOnlySpan<>) },
		{ ("System", nameof(RedStarLinq)), typeof(RedStarLinq) },
		{ ("System", nameof(RedStarLinqDictionaries)), typeof(RedStarLinqDictionaries) },
		{ ("System", nameof(RedStarLinqExtras)), typeof(RedStarLinqExtras) },
		{ ("System", nameof(RedStarLinqParallel)), typeof(RedStarLinqParallel) },
		{ ("System", nameof(RedStarLinqMath)), typeof(RedStarLinqMath) },
		{ ("System", nameof(RedStarLinqRemoveDoubles)), typeof(RedStarLinqRemoveDoubles) },
		{ ("System", nameof(Span<>)), typeof(Span<>) },
		{ ("System.Collections", nameof(BaseDictionary<,,>)), typeof(BaseDictionary<,,>) },
		{ ("System.Collections", nameof(BaseHashSet<,>)), typeof(BaseHashSet<,>) },
		{ ("System.Collections", nameof(BaseIndexable<,>)), typeof(BaseIndexable<,>) },
		{ ("System.Collections", nameof(BaseList<,>)), typeof(BaseList<,>) },
		{ ("System.Collections", nameof(BaseSet<,>)), typeof(BaseSet<,>) },
		{ ("System.Collections", nameof(BaseSortedSet<,>)), typeof(BaseSortedSet<,>) },
		{ ("System.Collections", nameof(BaseSumList<,>)), typeof(BaseSumList<int, SumList>) },
		{ ("System.Collections", nameof(Buffer)), typeof(Buffer<>) },
		{ ("System.Collections", nameof(Chain)), typeof(Chain) },
		{ ("System.Collections", nameof(Comparer<>)), typeof(Comparer<>) },
		{ ("System.Collections", nameof(Dictionary<,>)), typeof(Dictionary<,>) },
		{ ("System.Collections", nameof(EComparer<>)), typeof(EComparer<>) },
		{ ("System.Collections", nameof(Extents)), typeof(Extents) },
		{ ("System.Collections", nameof(FastDelHashSet<>)), typeof(FastDelHashSet<>) },
		{ ("System.Collections", nameof(Group<,>)), typeof(Group<,>) },
		{ ("System.Collections", nameof(G.LinkedList<>)), typeof(G.LinkedList<>) },
		{ ("System.Collections", nameof(G.LinkedListNode<>)), typeof(G.LinkedListNode<>) },
		{ ("System.Collections", nameof(LimitedQueue<>)), typeof(LimitedQueue<>) },
		{ ("System.Collections", nameof(ListEComparer<>)), typeof(ListEComparer<>) },
		{ ("System.Collections", nameof(ListHashSet<>)), typeof(ListHashSet<>) },
		{ ("System.Collections", nameof(ListOfBigSums)), typeof(ListOfBigSums) },
		{ ("System.Collections", nameof(Mirror<,>)), typeof(Mirror<,>) },
		{ ("System.Collections", nameof(ParallelHashSet<>)), typeof(ParallelHashSet<>) },
		{ ("System.Collections", nameof(Queue<>)), typeof(Queue<>) },
		{ ("System.Collections", nameof(Slice<>)), typeof(Slice<>) },
		{ ("System.Collections", nameof(SortedDictionary<,>)), typeof(SortedDictionary<,>) },
		{ ("System.Collections", nameof(SortedSet<>)), typeof(SortedSet<>) },
		{ ("System.Collections", nameof(Stack<>)), typeof(Stack<>) },
		{ ("System.Collections", nameof(SumList)), typeof(SumList) },
		{ ("System.Collections", nameof(SumSet<>)), typeof(SumSet<>) },
		{ ("System.Collections", nameof(TreeHashSet<>)), typeof(TreeHashSet<>) },
		{ ("System.Collections", nameof(TreeSet<>)), typeof(TreeSet<>) },
		{ ("System.GUI", nameof(Animatable)), typeof(Animatable) },
		{ ("System.GUI", nameof(AutoCompleteBox)), typeof(AutoCompleteBox) },
		{ ("System.GUI", nameof(AvaloniaObject)), typeof(AvaloniaObject) },
		{ ("System.GUI", nameof(Bitmap)), typeof(Bitmap) },
		{ ("System.GUI", nameof(Border)), typeof(Border) },
		{ ("System.GUI", nameof(Brush)), typeof(Brush) },
		{ ("System.GUI", nameof(Button)), typeof(Button) },
		{ ("System.GUI", nameof(Canvas)), typeof(Canvas) },
		{ ("System.GUI", nameof(CheckBox)), typeof(CheckBox) },
		{ ("System.GUI", nameof(Color)), typeof(Color) },
		{ ("System.GUI", nameof(ColumnDefinition)), typeof(ColumnDefinition) },
		{ ("System.GUI", nameof(ColumnDefinitions)), typeof(ColumnDefinitions) },
		{ ("System.GUI", nameof(ComboBox)), typeof(ComboBox) },
		{ ("System.GUI", nameof(Control)), typeof(Control) },
		{ ("System.GUI", nameof(Controls)), typeof(Controls) },
		{ ("System.GUI", nameof(Decorator)), typeof(Decorator) },
		{ ("System.GUI", nameof(DefinitionBase)), typeof(DefinitionBase) },
		{ ("System.GUI", nameof(DockPanel)), typeof(DockPanel) },
		{ ("System.GUI", nameof(Expander)), typeof(Expander) },
		{ ("System.GUI", nameof(Grid)), typeof(Grid) },
		{ ("System.GUI", nameof(GridLength)), typeof(GridLength) },
		{ ("System.GUI", nameof(GridSplitter)), typeof(GridSplitter) },
		{ ("System.GUI", nameof(GridUnitType)), typeof(GridUnitType) },
		{ ("System.GUI", nameof(GUIWindow)), typeof(GUIWindow) },
		{ ("System.GUI", nameof(HeaderedContentControl)), typeof(HeaderedContentControl) },
		{ ("System.GUI", nameof(HorizontalAlignment)), typeof(HorizontalAlignment) },
		{ ("System.GUI", nameof(IBrush)), typeof(IBrush) },
		{ ("System.GUI", nameof(IImage)), typeof(IImage) },
		{ ("System.GUI", nameof(Image)), typeof(Image) },
		{ ("System.GUI", nameof(ImageBrush)), typeof(ImageBrush) },
		{ ("System.GUI", nameof(IResourceDictionary)), typeof(IResourceDictionary) },
		{ ("System.GUI", nameof(ItemsControl)), typeof(ItemsControl) },
		{ ("System.GUI", nameof(KeyEventArgs)), typeof(KeyEventArgs) },
		{ ("System.GUI", nameof(Panel)), typeof(Panel) },
		{ ("System.GUI", nameof(Point)), typeof(Point) },
		{ ("System.GUI", nameof(PointerEventArgs)), typeof(PointerEventArgs) },
		{ ("System.GUI", nameof(PointerPressedEventArgs)), typeof(PointerPressedEventArgs) },
		{ ("System.GUI", nameof(PointerReleasedEventArgs)), typeof(PointerReleasedEventArgs) },
		{ ("System.GUI", nameof(RadioButton)), typeof(RadioButton) },
		{ ("System.GUI", nameof(RangeBase)), typeof(RangeBase) },
		{ ("System.GUI", nameof(RelativePanel)), typeof(RelativePanel) },
		{ ("System.GUI", nameof(RoutedEventArgs)), typeof(RoutedEventArgs) },
		{ ("System.GUI", nameof(RowDefinition)), typeof(RowDefinition) },
		{ ("System.GUI", nameof(RowDefinitions)), typeof(RowDefinitions) },
		{ ("System.GUI", nameof(ScrollViewer)), typeof(ScrollViewer) },
		{ ("System.GUI", nameof(SelectingItemsControl)), typeof(SelectingItemsControl) },
		{ ("System.GUI", nameof(Slider)), typeof(Slider) },
		{ ("System.GUI", nameof(SolidColorBrush)), typeof(SolidColorBrush) },
		{ ("System.GUI", nameof(SplitView)), typeof(SplitView) },
		{ ("System.GUI", nameof(StackPanel)), typeof(StackPanel) },
		{ ("System.GUI", nameof(TabControl)), typeof(TabControl) },
		{ ("System.GUI", nameof(TemplatedControl)), typeof(TemplatedControl) },
		{ ("System.GUI", nameof(TextBlock)), typeof(TextBlock) },
		{ ("System.GUI", nameof(TextBox)), typeof(TextBox) },
		{ ("System.GUI", nameof(Thumb)), typeof(Thumb) },
		{ ("System.GUI", nameof(TileBrush)), typeof(TileBrush) },
		{ ("System.GUI", nameof(ToggleButton)), typeof(ToggleButton) },
		{ ("System.GUI", nameof(ToolTip)), typeof(ToolTip) },
		{ ("System.GUI", nameof(UniformGrid)), typeof(UniformGrid) },
		{ ("System.GUI", nameof(VerticalAlignment)), typeof(VerticalAlignment) },
		{ ("System.GUI", nameof(Window)), typeof(Window) },
		{ ("System.GUI", nameof(WrapPanel)), typeof(WrapPanel) },
		{ ("System.GUI", nameof(ZoomBorder)), typeof(ZoomBorder) },
		{ ("System.IO", nameof(Directory)), typeof(Directory) },
		{ ("System.IO", nameof(DirectoryInfo)), typeof(DirectoryInfo) },
		{ ("System.IO", nameof(DriveInfo)), typeof(DriveInfo) },
		{ ("System.IO", nameof(DriveType)), typeof(DriveType) },
		{ ("System.IO", nameof(File)), typeof(File) },
		{ ("System.IO", nameof(FileAccess)), typeof(FileAccess) },
		{ ("System.IO", nameof(FileAttributes)), typeof(FileAttributes) },
		{ ("System.IO", nameof(FileInfo)), typeof(FileInfo) },
		{ ("System.IO", nameof(FileMode)), typeof(FileMode) },
		{ ("System.IO", nameof(FileOptions)), typeof(FileOptions) },
		{ ("System.IO", nameof(FileShare)), typeof(FileShare) },
		{ ("System.IO", nameof(FileStream)), typeof(FileStream) },
		{ ("System.IO", nameof(FileSystemInfo)), typeof(FileSystemInfo) },
		{ ("System.IO", nameof(MemoryStream)), typeof(MemoryStream) },
		{ ("System.IO", nameof(Path)), typeof(Path) },
		{ ("System.IO", nameof(Stream)), typeof(Stream) },
		{ ("System.IO", nameof(UnixFileMode)), typeof(UnixFileMode) },
		{ ("System.Threading", nameof(CancellationToken)), typeof(CancellationToken) },
		{ ("System.Threading", nameof(CancellationTokenSource)), typeof(CancellationTokenSource) },
		{ ("System.Threading", "Parallel"), typeof(Parallel) },
		{ ("System.Threading", "ParallelLoopResult"), typeof(ParallelLoopResult) },
		{ ("System.Threading", "ParallelLoopState"), typeof(ParallelLoopState) },
		{ ("System.Threading", "Task"), typeof(Task<>) },
		{ ("System.Threading", "TaskAwaiter"), typeof(TaskAwaiter<>) },
		{ ("System.Threading", "ValueTask"), typeof(ValueTask<>) },
		{ ("System.Threading", "ValueTaskAwaiter"), typeof(ValueTaskAwaiter<>) },
		{ ("System.Unsafe", "EmptyTask"), typeof(Task) },
		{ ("System.Unsafe", nameof(FuncDictionary<,>)), typeof(FuncDictionary<,>) },
		{ ("System.Unsafe", "Memory"), typeof(Memory<>) },
		{ ("System.Unsafe", "ReadOnlyMemory"), typeof(ReadOnlyMemory<>) },
		{ ("System.Unsafe", "UnsafeString"), typeof(string) },
		{ ("System.Unsafe", "ValueEmptyTask"), typeof(ValueTask) },
	};

	/// <summary>
	/// Sorted by tuple, contains Namespace and Type.
	/// </summary>
	public static Mirror<(String Namespace, String Type), Type> ImportedTypes { get; } = [];

	/// <summary>
	/// Sorted by Container and Type, also contains RestrictionPackage modifiers, RestrictionTypes, RestrictionNames and Attributes.
	/// </summary>
	public static ExtendedTypesCollection ExtendedTypes { get; } = new(new BlockStackAndStringComparer())
	{
		{
			(new([new(BlockType.Namespace, "System", 1)]), nameof(Action)),
			([new(true, RecursiveType, "Types")], TypeAttributes.Delegate)
		},
		{
			(new([new(BlockType.Namespace, "System", 1)]), nameof(Func<>)),
			new([new(false, RecursiveType, "TReturn"), new(true, RecursiveType, "Types")], TypeAttributes.Delegate)
		}
	};

	/// <summary>
	/// Sorted by Container and Type, also contains Restrictions, Attributes, BaseType and Decomposition.
	/// </summary>
	public static Dictionary<(BlockStack Container, String Type), UserDefinedType> UserDefinedTypes { get; } = new(new BlockStackAndStringEComparer());

	/// <summary>
	/// Sorted by Container, then by Name, also contains Attributes, BaseType, StartPos and EndPos.
	/// </summary>
	public static TypeDictionary<ListHashSet<TempType>> TempTypes { get; } = [];

	public static TypeDictionary<ListHashSet<String>> UnnamedTypeStartIndexes { get; } = [];

	/// <summary>
	/// Sorted by tuple, contains Namespace, Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Interface), (List<String> ExtraTypes, Type DotNetType)> Interfaces { get; } = new()
	{
		{
			([], "IChar"), (ExtraTypesT, typeof(void))
		},
		{
			([], nameof(IComparable<>)), (ExtraTypesT, typeof(IComparable<>))
		},
		{
			([], "IComparableRaw"), (NoExtraTypes, typeof(IComparable))
		},
		{
			([], nameof(IConvertible)), (NoExtraTypes, typeof(IConvertible))
		},
		{
			([], nameof(IEquatable<>)), (ExtraTypesT, typeof(IEquatable<>))
		},
		{
			([], "IIncreasable"), (ExtraTypesT, typeof(IIncrementOperators<>))
		},
		{
			([], "IIntegerNumber"), (ExtraTypesT, typeof(IBinaryInteger<>))
		},
		{
			([], "INumber"), (ExtraTypesT, typeof(INumber<>))
		},
		{
			([], "IFloatNumber"), (ExtraTypesT, typeof(IFloatingPoint<>))
		},
		{
			([], "ISignedIntegerNumber"), (ExtraTypesT, typeof(ISignedNumber<>)) 
		},
		{
			([], "IUnsignedIntegerNumber"), (ExtraTypesT, typeof(IUnsignedNumber<>))
		},
		{
			("System.Collections", nameof(ICollection)), (ExtraTypesT, typeof(ICollection<>)) 
		}, 
		{
			("System.Collections", "ICollectionRaw"), (NoExtraTypes, typeof(System.Collections.ICollection))
		},
		{
			("System.Collections", "IComparer"), (ExtraTypesT, typeof(G.IComparer<>))
		}, 
		{
			("System.Collections", nameof(IDictionary)), (["TKey", "TValue"], typeof(G.IDictionary<,>))
		},
		{
			("System.Collections", "IDictionaryRaw"), (NoExtraTypes, typeof(System.Collections.IDictionary))
		},
		{
			("System.Collections", nameof(G.IEnumerable<>)), (ExtraTypesT, typeof(G.IEnumerable<>)) 
		}, 
		{ 
			("System.Collections", "IEnumerableRaw"), (NoExtraTypes, typeof(System.Collections.IEnumerable)) 
		}, 
		{ 
			("System.Collections", "IEqualityComparer"), (ExtraTypesT, typeof(G.IEqualityComparer<>))
		},
		{
			("System.Collections", nameof(IList)), (ExtraTypesT, typeof(IList<>)) 
		},
		{ 
			("System.Collections", "IListRaw"), (NoExtraTypes, typeof(G.IList<>))
		},
		{
			("System.Collections", nameof(IReadOnlyList<>)), (ExtraTypesT, typeof(IReadOnlyList<>)) 
		},
		{ 
			("System.Collections", "IReadOnlyListRaw"), (NoExtraTypes, typeof(G.IReadOnlyList<>))
		},
	};

	/// <summary>
	/// Sorted by Class, also contains Interface and ExtraTypes.
	/// </summary>
	public static SortedDictionary<String, List<(String Interface, List<String> ExtraTypes)>> UserDefinedImplementedInterfaces { get; } = [];

	/// <summary>
	/// Sorted by Container, then by Name, also contains Type, ExtraTypes and Attributes.
	/// </summary>
	public static TypeDictionary<UserDefinedTypeProperties> UserDefinedProperties { get; } = [];

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
	public static TypeSortedList<TypeIndexers> UserDefinedIndexers { get; } = [];

	/// <summary>
	/// Sorted by Name, also contains ExtraTypes, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterExtraTypes, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static FunctionsList PublicFunctions { get; } = new()
	{
		{
			"Abs", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Ceil", new(ExtraTypesT, "int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IFloatNumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Chain", new(NoExtraTypes, "list", ["int"], FunctionAttributes.Multiconst,
				[new("int", "start", NoExtraTypes, ParameterAttributes.None, []), new("int", "end", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Choose", new(NoExtraTypes, "object", NoExtraTypes, FunctionAttributes.None,
				[new("object", "variants", NoExtraTypes, ParameterAttributes.Params, [])])
		},
		{
			"Clamp", new(ExtraTypesT, "INumber", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, []),
				new("System.INumber", "min", ExtraTypesT, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MinValue;\")"),
				new("System.INumber", "max", ExtraTypesT, ParameterAttributes.Optional, "ExecuteString(\"return \" + ReinterpretCast[string](T) + \".MaxValue;\")")])
		},
		{
			"Exp", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Fibonacci", new(NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("int", "n", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Fill", new(ExtraTypesT, "list", ExtraTypesT, FunctionAttributes.Multiconst,
				[new("T", "element", NoExtraTypes, ParameterAttributes.None, []), new("int", "count", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Floor", new(ExtraTypesT, "int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Frac", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IFloatNumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"IntRandom", new(NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.None,
				[new("int", "max", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"IntToReal", new(ExtraTypesT, "real", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IIntegerNumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"IsPrime", new(NoExtraTypes, "bool", NoExtraTypes, FunctionAttributes.None,
				[new("int", "n", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Log", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("real", "x", NoExtraTypes, ParameterAttributes.None, []),
				new("System.INumber", "y", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Max", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("IComparable", "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Mean", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Min", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("IComparable", "source", ExtraTypesT, ParameterAttributes.Params, [])])
		},
		{
			"Q", new(NoExtraTypes, "string", NoExtraTypes, FunctionAttributes.None, [])
		},
		{
			"Random", new(NoExtraTypes, "real", NoExtraTypes, FunctionAttributes.None,
				[new("real", "max", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"RealRemainder", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IFloatNumber", "x", ExtraTypesT, ParameterAttributes.None, []),
				new("real", "y", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"RGB", new(NoExtraTypes, "int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("byte", "red", NoExtraTypes, ParameterAttributes.None, []),
				new("byte", "green", NoExtraTypes, ParameterAttributes.None, []),
				new("byte", "blue", NoExtraTypes, ParameterAttributes.None, [])])
		},
		{
			"Round", new(ExtraTypesT, "int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IFloatNumber", "x", ExtraTypesT, ParameterAttributes.None, []),
				new("int", "digits_after_dot", NoExtraTypes, ParameterAttributes.Optional, "0")])
		},
		{
			"Sign", new(ExtraTypesT, "short int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Sqrt", new(ExtraTypesT, "T", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.INumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		},
		{
			"Truncate", new(ExtraTypesT, "int", NoExtraTypes, FunctionAttributes.Multiconst,
				[new("System.IFloatNumber", "x", ExtraTypesT, ParameterAttributes.None, [])])
		}
	};

	/// <summary>
	/// Sorted by Container, then by Name, also contains Restrictions, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeSortedList<ExtendedMethods> ExtendedMethods { get; } = new()
	{
		{
			new(), new()
			{
				{ "ExecuteString", new() { new([], ObjectType, FunctionAttributes.Multiconst, [ExtendedParameterStringS, new(ObjectType, "parameters", ParameterAttributes.Params, [])]) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, then by Name, also contains Restrictions, ReturnType, Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<UserDefinedMethods> UserDefinedFunctions { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedFunctionIndexes { get; } = [];

	/// <summary>
	/// Sorted by Container, also contains Attributes, ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes and ParameterDefaultValues.
	/// </summary>
	public static TypeDictionary<ConstructorOverloads> UserDefinedConstructors { get; } = [];

	/// <summary>
	/// Sorted by Container, then by StartPos.
	/// </summary>
	public static TypeDictionary<SortedDictionary<int, int>> UserDefinedConstructorIndexes { get; } = [];

	/// <summary>
	/// Sorted by Operator, also contains Postfix modifiers, ReturnTypes, ReturnNStarType.ExtraTypes, OpdTypes and OpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, UnaryOperatorClasses> UnaryOperators { get; } = new()
	{
		{
			"+", new(new BlockStackComparer())
			{
				{ ExtendedTypeINumber, new() { (false, NStarTypeT, NStarTypeINumberT) } }
			}
		},
		{
			"-", new(new BlockStackComparer())
			{
				{ ExtendedTypeISignedIntegerNumber, new() { (false, NStarTypeT, NStarTypeISignedIntegerNumberT) } },
				{ ExtendedTypeIFloatNumber, new() { (false, NStarTypeT, NStarTypeIFloatNumberT) } }
			}
		},
		{
			"++", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (false, NStarTypeT, NStarTypeIIncreasableT), (true, NStarTypeT, NStarTypeIIncreasableT) } }
			}
		},
		{
			"--", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (false, NStarTypeT, NStarTypeIIncreasableT), (true, NStarTypeT, NStarTypeIIncreasableT) } }
			}
		},
		{
			"!", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (false, BoolType, BoolType) } }
			}
		},
		{
			"!!", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack("bool"), new() { (true, BoolType, BoolType) } }
			}
		},
		{
			"~", new(new BlockStackComparer())
			{
				{ ExtendedTypeISignedIntegerNumber, new() { (false, NStarTypeT, NStarTypeISignedIntegerNumberT) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Operator, also contains ReturnTypes, ReturnNStarType.ExtraTypes, LeftOpdTypes and LeftOpdExtraTypes, RightOpdTypes and RightOpdExtraTypes.
	/// </summary>
	public static SortedDictionary<String, BinaryOperatorClasses> BinaryOperators { get; } = new()
	{
		{
			"+", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (NStarTypeT, NStarTypeIIncreasableT, IntType), (NStarTypeT, IntType, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } },
				{ ExtendedTypeString, new() { (StringType, CharType, StringType), (StringType, CharListType, StringType), (StringType, StringType, CharType), (StringType, StringType, CharListType), (StringType, StringType, StringType) } }
			}
		},
		{
			"-", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (NStarTypeT, NStarTypeIIncreasableT, IntType), (IntType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"*", new(new BlockStackComparer())
			{
				{ ExtendedTypeINumber, new() { (NStarTypeT, NStarTypeINumberT, NStarTypeINumberT) } },
				{ ExtendedTypeString, new() { (StringType, IntType, StringType), (StringType, StringType, IntType) } }
			}
		},
		{
			"/", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } },
				{ ExtendedTypeIFloatNumber, new() { (NStarTypeT, NStarTypeIFloatNumberT, NStarTypeIFloatNumberT) } }
			}
		},
		{
			"pow", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } },
				{ ExtendedTypeIFloatNumber, new() { (NStarTypeT, NStarTypeIFloatNumberT, NStarTypeIFloatNumberT) } } } },
		{
			"==", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack("object"), new() { (BoolType, ObjectType, ObjectType) } }
			}
		},
		{
			">", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"<", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			">=", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } } } },
		{
			"<=", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIncreasable, new() { (BoolType, NStarTypeIIncreasableT, NStarTypeIIncreasableT) } },
				{ ExtendedTypeINumber, new() { (BoolType, NStarTypeINumberT, NStarTypeINumberT) } }
			}
		},
		{
			"!=", new(new BlockStackComparer())
			{
				{ GetPrimitiveBlockStack("object"), new() { (BoolType, ObjectType, ObjectType) } }
			}
		},
		{
			">>", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } }
			}
		},
		{
			"<<", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, IntType) } }
			}
		},
		{
			"&", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"|", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"^", new(new BlockStackComparer())
			{
				{ ExtendedTypeIIntegerNumber, new() { (NStarTypeT, NStarTypeIIntegerNumberT, NStarTypeIIntegerNumberT) } }
			}
		},
		{
			"&&", new(new BlockStackComparer()) { { ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } } }
		},
		{
			"||", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } }
			}
		},
		{
			"^^", new(new BlockStackComparer())
			{
				{ ExtendedTypeBool, new() { (BoolType, BoolType, BoolType) } }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<List<(String Name, int Value)>> EnumConstants { get; } = new()
	{ 
		{
			new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DateTimeKind", 1)]), new() 
			{
				("Local", (int)DateTimeKind.Local), ("Unspecified", (int)DateTimeKind.Unspecified),
				("UTC", (int)DateTimeKind.Utc)
			}
		},
		{
			new([new(BlockType.Namespace, "System", 1), new(BlockType.Enum, "DayOfWeek", 1)]), new()
			{
				("Friday", (int)DayOfWeek.Friday), ("Monday", (int)DayOfWeek.Monday), ("Saturday", (int)DayOfWeek.Saturday), 
				("Sunday", (int)DayOfWeek.Sunday), ("Thursday", (int)DayOfWeek.Thursday),
				("Tuesday", (int)DayOfWeek.Tuesday), ("Wednesday", (int)DayOfWeek.Wednesday)
			} 
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Name and Value.
	/// </summary>
	public static TypeSortedList<Dictionary<String, UserDefinedConstant>> UserDefinedConstants { get; } = [];

	/// <summary>
	/// Sorted by SrcType, also contains SrcNStarType.ExtraTypes, DestTypes and their DestNStarType.ExtraTypes.
	/// </summary>
	public static TypeSortedList<ImplicitConversions> ImplicitConversions { get; } = new()
	{
		{
			ExtendedTypeBool, new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedIntType, false), (IntType, false), (ShortIntType, false), (ByteType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("byte"), new()
			{
				{ NoBranches, new() { (UnsignedIntType, false), (IntType, false), (ShortIntType, false), (GetPrimitiveType("short char"), true), (BoolType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("char"), new() {
				{ NoBranches, new() { (UnsignedShortIntType, false), (StringType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("complex"), new()
			{
				{ NoBranches, new() { } }
			}
		},
		{
			ExtendedTypeInt, new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (IndexType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true) } }
			}
		},
		{
			ExtendedTypeList, new()
			{
				{ [new("type", 0, []) { Extra = GetPrimitiveType("char") }], new() { (StringType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("long char"), new()
			{
				{ NoBranches, new() { (UnsignedIntType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("long int"), new()
			{
				{ NoBranches, new() { (LongLongType, false), (UnsignedLongIntType, true), (BoolType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("long long"), new()
			{
				{ NoBranches, new() { (BoolType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (UnsignedLongIntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("real"), new()
			{
				{ NoBranches, new() { (ComplexType, false), (BoolType, true), (UnsignedLongIntType, true), (LongIntType, true), (UnsignedIntType, true), (IntType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("short char"), new()
			{
				{ NoBranches, new() { (ByteType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("short int"), new()
			{
				{ NoBranches, new() { (LongIntType, false), (RealType, false), (IntType, false), (UnsignedShortIntType, false), (BoolType, true), (ByteType, true), (UnsignedIntType, true), (UnsignedLongIntType, true) } }
			}
		},
		{
			ExtendedTypeString, new()
			{
				{ NoBranches, new() { (CharListType, false) } }
			}
		},
		{
			GetPrimitiveBlockStack("unsigned int"), new()
			{
				{ NoBranches, new() { (RealType, false), (UnsignedLongIntType, false), (LongIntType, false), (BoolType, true), (ByteType, true), (UnsignedShortIntType, true), (ShortIntType, true), (IntType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("unsigned long int"), new()
			{
				{ NoBranches, new() { (BoolType, true), (UnsignedShortIntType, true), (ShortIntType, true), (UnsignedIntType, true), (IntType, true), (LongIntType, true), (RealType, true) } }
			}
		},
		{
			GetPrimitiveBlockStack("unsigned short int"), new()
			{
				{ NoBranches, new() { (UnsignedLongIntType, false), (LongIntType, false), (RealType, false), (UnsignedIntType, false), (IntType, false), (BoolType, true), (ByteType, true), (ShortIntType, true) } }
			}
		},
	};

	/// <summary>
	/// Sorted by tuple, contains DestType and DestNStarType.ExtraTypes.
	/// </summary>
	public static List<NStarType> ImplicitConversionsFromAnything { get; } = [(GetPrimitiveBlockStack("object"), NoBranches), (GetPrimitiveBlockStack("null"), NoBranches), GetListType(GetPrimitiveType("[this]"))];

	public static G.SortedSet<String> NotImplementedNamespaces { get; } = ["System.Diagnostics", "System.Globalization", "System.Runtime", "System.Text"];

	/// <summary>
	/// Sorted by Namespace, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedNamespaces { get; } = new() 
	{
		{ "System.Collections.Generic", "System.Collections" }, { "System.Linq", "RedStarLinq" }, 
		{ "System.Windows", "System.GUI" }, { "System.Windows.Forms", "System.GUI" }
	};

	public static G.SortedSet<String> ReservedNamespaces { get; } = [
		"Microsoft",
		"System.Activities", "System.AddIn", "System.CodeDom",
		"System.Collections.Concurrent", "System.Collections.ObjectModel", "System.Collections.Specialized",
		"System.ComponentModel", "System.Configuration",
		"System.Data", "System.Deployment", "System.Device",
		"System.Diagnostics.CodeAnalysis", "System.Diagnostics.Contracts", "System.Diagnostics.Design",
		"System.Diagnostics.Eventing", "System.Diagnostics.PerformanceData",
		"System.Diagnostics.SymbolStore", "System.Diagnostics.Tracing",
		"System.DirectoryServices", "System.Drawing", "System.Dynamic",
		"System.EnterpriseServices", "System.IdentityModel",
		"System.IO.Compression", "System.IO.IsolatedStorage", "System.IO.Log", "System.IO.MemoryMappedFiles",
		"System.IO.Packaging", "System.IO.Pipes", "System.IO.Ports",
		"System.Management", "System.Media", "System.Messaging", "System.Net", "System.Numerics",
		"System.Printing", "System.Reflection", "System.Resources",
		"System.Runtime.Caching", "System.Runtime.CompilerServices", "System.Runtime.ConstrainedExecution",
		"System.Runtime.DesignerServices", "System.Runtime.ExceptionServices", "System.Runtime.Hosting",
		"System.Runtime.InteropServices", "System.Runtime.Remoting",
		"System.Runtime.Serialization", "System.Runtime.Versioning",
		"System.Security", "System.ServiceModel", "System.ServiceProcess", "System.Speech", "System.StubHelpers",
		"System.Text.RegularExpressions", "System.Threading.Tasks",
		"System.Timers", "System.Transactions", "System.Web",
		"System.Windows.Annotations", "System.Windows.Automation", "System.Windows.Baml2006", "System.Windows.Controls",
		"System.Windows.Data", "System.Windows.Documents",
		"System.Windows.Forms.ComponentModel", "System.Windows.Forms.DataVisualization", "System.Windows.Forms.Design",
		"System.Windows.Forms.Interaction", "System.Windows.Forms.Layout",
		"System.Windows.Forms.PropertyGridInternal", "System.Windows.Forms.VisualStyles",
		"System.Windows.Ink", "System.Windows.Input", "System.Windows.Interop", "System.Windows.Markup", "System.Windows.Media",
		"System.Windows.Navigation", "System.Windows.Resources", "System.Windows.Shapes",
		"System.Windows.Threading", "System.Windows.Xps",
		"System.Workflow", "System.Xaml", "System.Xml", "Windows", "XamlGeneratedNamespace"
	];

	public static G.SortedSet<(String Namespace, String Type)> NotImplementedTypes { get; } = [
		([], "long complex"), ([], "long real"),
		("System", "Delegate"), ("System", "Enum"), ("System", "Environment"), ("System", "OperatingSystem")
	];

	/// <summary>
	/// Sorted by Namespace and Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<(String Namespace, String Type), String> OutdatedTypes { get; } = new()
	{
		{ ([], "*Exception"), "\"if error ...\"" },
		{ ([], "double"), "real or long real" }, { ([], "float"), "real or long real" },
		{ ([], "uint"), "unsigned int" }, { ([], "ulong"), "unsigned long int" }, { ([], "ushort"), "unsigned short int" },
		{ ("System", "Action"), "System.Func[null, ...]" }, { ("System", "Array"), "list" }, { ("System", "Boolean"), "bool" },
		{ ("System", "Byte"), "byte (from the small letter)" },
		{ ("System", "Char"), "char (from the small letter), short char or long char" },
		{ ("System", "Console"), "labels and textboxes" },
		{ ("System", "ConsoleCancelEventArgs"), "TextBox.KeyDown, TextBox.KeyPress and TextBox.KeyUp" },
		{ ("System", "ConsoleCancelEventHandler"), "TextBox keyboard events" },
		{ ("System", "ConsoleColor"), "RichTextBox text color" }, { ("System", "ConsoleKey"), "other item enums" },
		{ ("System", "ConsoleKeyInfo"), "other item info classes" },
		{ ("System", "ConsoleModifiers"), "other item modifiers enums" },
		{ ("System", "ConsoleSpecialKey"), "other item enums" },
		{ ("System", "Double"), "real or long real" },
		{ ("System", "Int16"), "short int" }, { ("System", "Int32"), "int" }, { ("System", "Int64"), "long int" },
		{ ("System", "Object"), "object (from the small letter)" },
		{ ("System", "Random"), "Random(), IntRandom() etc." },
		{ ("System", "SByte"), "byte or short int" }, { ("System", "Single"), "real or long real" },
		{ ("System", "String"), "string (from the small letter)" },
		{ ("System", "Type"), "typename" },
		{ ("System", "UInt16"), "unsigned short int" }, { ("System", "UInt32"), "unsigned int" },
		{ ("System", "UInt64"), "unsigned long int" }, { ("System", "Void"), "null" },
		{ ("System.Collections", "BitArray"), "BitList" },
		{ ("System.Collections", "HashSet"), "ListHashSet" }, { ("System.Collections", "Hashtable"), "Dictionary" },
		{ ("System.Collections", "KeyValuePair"), "tuples" }, { ("System.Collections", "SortedSet"), "SortedSet" }
	};

	public static G.SortedSet<(String Namespace, String Type)> ReservedTypes { get; } = [
		([], "*Attribute"), ([], "*Comparer"), ([], "*Enumerator"), ([], "*UriParser"), ([], "decimal"),
		("System", "ActivationContext"), ("System", "ActivationContext.ContextForm"), ("System", "Activator"),
		("System", "AppContext"), ("System", "AppDomain"), ("System", "AppDomainInitializer"),
		("System", "AppDomainManager"), ("System", "AppDomainManagerInitializationOptions"), ("System", "AppDomainSetup"),
		("System", "ApplicationId"), ("System", "ApplicationIdentity"), ("System", "ArgIterator"), ("System", "ArraySegment"),
		("System", "AssemblyLoadEventArgs"), ("System", "AsyncCallback"), ("System", "AttributeTargets"),
		("System", "Base64FormattingOptions"), ("System", "BitConverter"), ("System", "Buffer"),
		("System", "Comparison"), ("System", "ContextBoundObject"), ("System", "ContextStaticAttribute"),
		("System", "Convert"), ("System", "Converter"), ("System", "CrossAppDomainDelegate"),
		("System", "DateTimeOffset"), ("System", "DBNull"), ("System", "Decimal"),
		("System", "EnvironmentVariableTarget"), ("System", "FormattableString"),
		("System", "GC"), ("System", "GCCollectionMode"), ("System", "GCNotificationStatus"),
		("System", "GenericUriParserOptions"), ("System", "Guid"),
		("System", "IAppDomainSetup"), ("System", "IAsyncResult"), ("System", "ICloneable"), ("System", "ICustomFormattable"),
		("System", "IDisposable"), ("System", "IFormatProvider"), ("System", "IFormattable"),
		("System", "IObservable"), ("System", "IObserver"), ("System", "IProgress"), ("System", "IServiceProvider"),
		("System", "Lazy"), ("System", "LoaderOptimization"), ("System", "LocalDataStoreSlot"),
		("System", "MarshalByRefObject"), ("System", "Math"), ("System", "MidpointRounding"), ("System", "ModuleHandle"),
		("System", "MulticastDelegate"), ("System", "Nullable"),
		("System", "PlatformID"), ("System", "Progress"),
		("System", "ResolveEventArgs"), ("System", "ResolveEventHandler"),
		("System", "RuntimeArgumentHandle"), ("System", "RuntimeFieldHandle"),
		("System", "RuntimeMethodHandle"), ("System", "RuntimeTypeHandle"),
		("System", "StringComparer"), ("System", "StringComparison"), ("System", "StringSplitOptions"),
		("System", "TimeZone"), ("System", "TimeZoneInfo"),
		("System", "TimeZoneInfo.AdjustmentRule"), ("System", "TimeZoneInfo.TransitionTime"),
		("System", "Tuple"), ("System", "TupleExtensions"), ("System", "TypeCode"), ("System", "TypedReference"),
		("System", "UIntPtr"), ("System", "Uri"), ("System", "UriBuilder"), ("System", "UriComponents"),
		("System", "UriFormat"), ("System", "UriHostNameType"), ("System", "UriIdnScope"),
		("System", "UriKind"), ("System", "UriPartial"),
		("System", "UriTemplate"), ("System", "UriTemplateEquivalenceComparer"),
		("System", "UriTemplateMatch"), ("System", "UriTemplateTable"), ("System", "UriTypeConverter"),
		("System", "ValueTuple"), ("System", "ValueType"), ("System", "Version"),
		("System", "WeakReference"), ("System", "_AppDomain"),
		("System.Collections", "ArrayList"), ("System.Collections", "CaseInsensitiveHashCodeProvider"),
		("System.Collections", "CollectionBase"),
		("System.Collections", "Dictionary.KeyCollection"), ("System.Collections", "Dictionary.ValueCollection"),
		("System.Collections", "DictionaryBase"), ("System.Collections", "DictionaryEntry"),
		("System.Collections", "IHashCodeProvider"), ("System.Collections", "IReadOnlyCollection"),
		("System.Collections", "IReadOnlyDictionary"), ("System.Collections", "IReadOnlyList"), ("System.Collections", "ISet"),
		("System.Collections", "IStructuralComparable"), ("System.Collections", "IStructuralEquatable"),
		("System.Collections", "KeyedByTypeCollection"), ("System.Collections", "ReadOnlyCollectionBase"),
		("System.Collections", "StructuralComparisons"), ("System.Collections", "SynchronizedCollection"),
		("System.Collections", "SynchronizedKeyedCollection"), ("System.Collections", "SynchronizedReadOnlyCollection")
	];

	public static G.SortedSet<String> NotImplementedTypeEnds { get; } = [];

	/// <summary>
	/// Sorted by Type, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedTypeEnds { get; } = new() { { "Exception", "\"if error ...\"" } };

	public static G.SortedSet<String> ReservedTypeEnds { get; } = ["Attribute", "Comparer", "Enumerator", "UriParser"];

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> NotImplementedMembers { get; } = new() { { new([new(BlockType.Interface, "DateTime", 1)]), new() { "AddRange", "Subtract" } } };

	/// <summary>
	/// Sorted by Container, then by Member, also contains UseInstead.
	/// </summary>
	public static TypeSortedList<SortedDictionary<String, String>> OutdatedMembers { get; } = new()
	{
		{
			ExtendedTypeBool, new()
			{
				{ "FalseString", "literal \"false\"" }, { "Parse", "implicit conversion" }, 
				{ "TrueString", "literal \"true\"" }, { "TryParse", "implicit conversion" }
			}
		},
		{ 
			GetPrimitiveBlockStack("DateTime"), new()
			{ 
				{ "IsDaylightSavingTime", "IsSummertime" },
				{ "Parse", "implicit conversion" }, { "TryParse", "implicit conversion" }
			} 
		}, 
		{ 
			ExtendedTypeINumber, new()
			{ 
				{ "Parse", "implicit conversion" }, { "TryParse", "implicit conversion" }
			} 
		},
		{ 
			ExtendedTypeList, new()
			{ 
				{ "Length", "Length" }
			} 
		},
		{ 
			GetPrimitiveBlockStack("object"), new()
			{ 
				{ "Equals", "==" }
			}
		}
	};

	/// <summary>
	/// Sorted by Container, also contains Members.
	/// </summary>
	public static TypeSortedList<G.SortedSet<String>> ReservedMembers { get; } = new()
	{
		{
			GetPrimitiveBlockStack("DateTime"), new() 
			{
				"FromBinary", "FromFileTime", "FromFileTimeUtc", "FromOADate", "GetDateTimeFormats", "ParseExact",
				"ToFileTime", "ToFileTimeUtc", "ToLongDateString", "ToLongTimeString", "ToOADate",
				"ToShortDateString", "ToShortTimeString", "TryParseExact"
			}
		},
		{
			FuncBlockStack, new() { "BeginInvoke", "EndInvoke", "Invoke" }
		},
		{
			new([new(BlockType.Interface, "IChar", 1)]), new()
			{
				"ConvertFromUtf32", "ConvertToUtf32", "GetNumericValue", "GetUnicodeCategory",
				"IsControl", "IsHighSurrogate", "IsLowSurrogate", "IsNumber", "IsPunctuation",
				"IsSurrogate", "IsSurrogatePair", "IsSymbol",
				"ToLowerInvariant", "ToUpperInvariant" 
			}
		},
		{
			ExtendedTypeList, new()
			{
				"AsReadOnly", "ConvertAll", "GetEnumerator" 
			}
		},
		{
			GetPrimitiveBlockStack("object"), new()
			{
				"GetType", "GetTypeCode", "ReferenceEquals"
			}
		},
		{
			new([new(BlockType.Namespace, "System", 1), new(BlockType.Class, "Predicate", 1)]), new() 
			{ 
				"BeginInvoke", "EndInvoke", "Invoke"
			}
		}, 
		{ 
			ExtendedTypeString, new() 
			{
				"Clone", "Copy", "CopyTo", "Empty", "Format", "GetEnumerator", "Intern",
				"IsInterned", "IsNormalized", "IsNullOrEmpty", "IsNullOrWhiteSpace", 
				"Normalize", "ToLowerInvariant", "ToUpperInvariant" 
			}
		}
	};

	public static OutdatedMethods OutdatedStringMethodOverloads { get; } = new()
	{
		{
			"Concat", new()
			{
				([
					ExtendedParameterString1, ExtendedParameterString2, ExtendedParameterString3,
					new(StringType, "string4", ParameterAttributes.None, [])
				], "concatenation in pairs, triples or in an array"),
				([
					new(ObjectType, "object1", ParameterAttributes.None, []),
					new(ObjectType, "object2", ParameterAttributes.None, []),
					new(ObjectType, "object3", ParameterAttributes.None, []),
					new(ObjectType, "object4", ParameterAttributes.None, [])
				], "concatenation in pairs, triples or in an array")
			}
		}
	};

	/// <summary>
	/// Sorted by Container, then by Name, also contains ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes, ParameterDefaultValues and UseInstead suggestions.
	/// </summary>
	public static TypeSortedList<OutdatedMethods> OutdatedMethodOverloads { get; } = new()
	{
		{
			new([new(BlockType.Interface, "IChar", 1)]),
			new()
			{ 
				{ "IsDigit", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "string[index] as a parameter") } },
				{ "IsLetter", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } },
				{ "IsLetterOrDigit", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } },
				{ "IsLower", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } },
				{ "IsSeparator", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } },
				{ "IsUpper", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } },
				{ "IsWhiteSpace", new() { ([ExtendedParameterStringS, ExtendedParameterIndex], "info[index] as a parameter") } }
			} 
		}, 
		{ 
			ExtendedTypeString, OutdatedStringMethodOverloads
		}
	};

	/// <summary>
	/// Sorted by Container, also contains ParameterTypes, ParameterNames, ParameterRestrictions, ParameterAttributes, ParameterDefaultValues and UseInstead suggestions.
	/// </summary>
	public static SortedDictionary<String, OutdatedMethodOverloads> OutdatedConstructors { get; } = [];

	public static G.SortedSet<String> NotImplementedOperators { get; } = ["<<<", ">>>"];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	/// <summary>
	/// Sorted by OperandsCount and Operator, also contains UseInstead.
	/// </summary>
	public static SortedDictionary<String, String> OutdatedOperators { get; } = [];

	public static G.SortedSet<String> ReservedOperators { get; } = ["#", "G", "I", "K", "_", "g", "hexa", "hexa=", "penta", "penta=", "tetra", "tetra="];
	// To specify non-associative N-ary operator, set OperandsCount to -1. To specify postfix unary operator, set it to -2.

	public static BlockStack GetBlockStack(String basic)
	{
		var typeName = basic.Copy();
		var namespace_ = typeName.GetBeforeSetAfterLast(".");
		var split = namespace_.Split('.');
		if (PrimitiveTypes.ContainsKey(basic))
			return GetPrimitiveBlockStack(basic);
		else if (ExtraTypes.TryGetValue((namespace_, typeName), out var netType)
			|| ImportedTypes.TryGetValue((namespace_, typeName), out netType))
			return new([.. split.Convert(x => new Block(BlockType.Namespace, x, 1)),
				new(typeof(Delegate).IsAssignableFrom(netType) ? BlockType.Delegate
				: netType.IsInterface ? BlockType.Interface
				: netType.IsClass ? BlockType.Class : netType.IsValueType
				? BlockType.Struct : throw new InvalidOperationException(), typeName, 1)]);
		else if (Interfaces.TryGetValue((namespace_, typeName), out var value) && value.DotNetType.IsInterface)
			return new([.. split.Convert(x => new Block(BlockType.Namespace, x, 1)),
				new(BlockType.Interface, typeName, 1)]);
		else if (basic.AsSpan() is nameof(Action) or nameof(Func<>))
			return new([new(BlockType.Delegate, basic, 1)]);
		else
			return new([new(BlockType.Extra, basic, 1)]);
	}

	public static async Task<List<string>> DownloadPackage(string packageId)
	{
		var downloadDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloaded");
		var extractDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Extracted");
		if (Directory.Exists(extractDir))
			Directory.Delete(extractDir, true);
		var source = new PackageSource("https://api.nuget.org/v3/index.json");
		var sourceRepository = new SourceRepository(source, Repository.Provider.GetCoreV3());
		var metadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
		var metadata = await metadataResource.GetMetadataAsync(packageId, includePrerelease: false, includeUnlisted: false,
			new(), new NullLogger(), CancellationToken.None);
		var maxVersion = metadata.Max(x => x.Identity.Version);
		var packageIdentity = metadata.FindAll(x => x.Identity.Version == maxVersion).Convert(x => x.Identity).FirstOrDefault()
			?? throw new NonExistentPackageException();
		var downloadResource = await sourceRepository.GetResourceAsync<DownloadResource>();
		using var downloadResult = await downloadResource.GetDownloadResourceResultAsync(packageIdentity,
			new PackageDownloadContext(new SourceCacheContext()), downloadDir, new NullLogger(), CancellationToken.None);
		if (downloadResult.Status != DownloadResourceResultStatus.Available)
			throw new NonExistentPackageException();
		await VerifyPackageSignature(downloadResult.PackageStream);
		var nupkgStream = downloadResult.PackageStream;
		Directory.CreateDirectory(extractDir);
		ZipFile.ExtractToDirectory(nupkgStream, extractDir, true);
		var dllFiles = Directory.GetFiles(extractDir, "*.dll", SearchOption.AllDirectories)
			.Filter(f => f.Contains("/lib/") || f.Contains(@"\lib\")).ToList();
		if (!dllFiles.Any())
			throw new NonExistentPackageException();
		var loadedAssemblies = new ParallelHashSet<Assembly>(new EComparer<Assembly>((x, y) => x.FullName == y.FullName,
			x => x.FullName?.GetHashCode() ?? 0));
		Parallel.ForEach(dllFiles, dllPath =>
		{
			try
			{
				var assembly = Assembly.LoadFrom(dllPath);
				loadedAssemblies.Add(assembly);
			}
			catch
			{
			}
		});
		foreach (var assembly in loadedAssemblies)
		{
			try
			{
				loadedAssemblies.Add(assembly);
				foreach (var type in assembly.GetTypes())
				{
					ImportedNamespaces.Add(type.Namespace);
					ImportedTypes.TryAdd((type.Namespace, ((String)type.Name).GetBefore('`')), type);
				}
			}
			catch
			{
			}
		}
		return loadedAssemblies.ToList(a => a.FullName ?? "netstandard");
	}

	private static async Task VerifyPackageSignature(Stream packageStream)
	{
		using var package = new PackageArchiveReader(packageStream);
		var signaturePath = package.GetFiles()
			.Find(f => f.EndsWith(".signature.p7s"));
		if (string.IsNullOrEmpty(signaturePath))
			throw new WrongSignatureException();
		var signature = File.ReadAllBytes(signaturePath) ?? throw new WrongSignatureException();
		try
		{
			SignedCms signedCms = new();
			signedCms.Decode(signature);
			signedCms.CheckSignature(true);
			var certificates = signedCms.SignerInfos[0].Certificate;
			if (!(certificates?.Verify() ?? false))
				throw new WrongSignatureException();
		}
		catch (CryptographicException)
		{
			throw new WrongSignatureException();
		}
	}
}

public class NonExistentPackageException : Exception
{
	public NonExistentPackageException() : base("Ошибка, такой NuGet-пакет не существует.") { }

	public NonExistentPackageException(string? message) : base(message) { }

	public NonExistentPackageException(string? message, Exception? innerException) : base(message, innerException) { }
}

public class WrongSignatureException : Exception
{
	public WrongSignatureException() : base("Ошибка, нельзя использовать этот пакет из-за неправильной подписи.") { }

	public WrongSignatureException(string? message) : base(message) { }

	public WrongSignatureException(string? message, Exception? innerException) : base(message, innerException) { }
}

public class ViewModelBase : ReactiveObject
{
}

public class MainViewModel : ViewModelBase
{
}

public class GUIWindow : Window
{
	public GUIWindow() => InitializeComponent();

	public void InitializeComponent() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(this, """
		<Window xmlns="https://github.com/avaloniaui"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:vm="using:CSharp.NStar"
				xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
				xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
				xmlns:views="clr-namespace:CSharp.NStar"
				x:Class="CSharp.NStar.GUIWindow"
				x:DataType="vm:MainViewModel"
				Title="CSharp.NStar"
				Width="1024"
				Height="768"
				MinWidth="1024"
				MinHeight="768">
			<Design.DataContext>
				<!-- This only sets the DataContext for the previewer in an IDE,
						to set the actual DataContext for runtime, set the DataContext property in code (look at App.axaml.cs) -->
				<vm:MainViewModel />
			</Design.DataContext>

			<ContentControl
				x:Name="Content"
				HorizontalAlignment="Stretch"
				VerticalAlignment="Stretch" />
		</Window>

		"""));
}

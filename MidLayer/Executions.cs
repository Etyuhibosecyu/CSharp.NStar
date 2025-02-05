global using Corlib.NStar;
global using System;
global using System.Drawing;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.Constructions;
global using static CSharp.NStar.Executions;
global using static System.Math;
global using String = Corlib.NStar.String;
using Newtonsoft.Json;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Diagnostics;

namespace CSharp.NStar;
public struct DelegateParameters
{
	public TreeBranch? Location { get; private set; }
	public object? Function { get; private set; }
	public Universal? ContainerValue { get; private set; }

	public DelegateParameters(TreeBranch? location, (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, Universal? containerValue = null)
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

public static partial class Executions
{
	public static readonly String[] operators = ["or", "and", "^^", "||", "&&", "==", "!=", ">=", "<=", ">", "<", "^=", "|=", "&=", ">>=", "<<=", "+=", "-=", "*=", "/=", "%=", "pow=", "=", "^", "|", "&", ">>", "<<", "+", "-", "*", "/", "%", "pow", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "!", "~", "++", "--", "!!"];
	public static readonly bool[] areOperatorsInverted = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false];
	public static readonly bool[] areOperatorsAssignment = [false, false, false, false, false, false, false, false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, true];

	private static int random_calls;
	private static readonly double random_initializer = DateTime.Now.ToBinary() / 1E+9;

	public static JsonSerializerSettings SerializerSettings { get; } = new() { Converters = [new StringConverter(), new IEnumerableConverter(), new TupleConverter(), new UniversalConverter(), new IClassConverter(), new DoubleConverter()] };

	public static String TypeMapping(String type)
	{
		var after = type.GetAfter(((String)"System.Collections.").AddRange(nameof(G.LinkedList<bool>)));
		if (after != "")
			return "G.LinkedList" + after;
		after = type.GetAfter("System.Collections.");
		if (after != "")
			return after;
		return type;
	}

	public static String TypeMappingBack(Type type)
	{
		if (type.IsSZArray)
			type = typeof(List<>);
		if (type.IsGenericType)
			type = type.GetGenericTypeDefinition();
		if (CreateVar(PrimitiveTypesList.Find(x => x.Value == type).Key, out var typename) != null)
			return typename;
		else if (CreateVar(ExtraTypesList.Find(x => x.Value == type).Key, out var type2) != default)
			return type2.Namespace.Copy().Add('.').AddRange(type2.Type);
		else if (CreateVar(InterfacesList.Find(x => x.Value.Exists(y => y.DotNetType == type)), out var type3).Key != default)
			return type3.Key.Namespace.Copy().Add('.').AddRange(type3.Key.Interface);
		else if (type == typeof(string))
			return "string";
		else
			throw new InvalidOperationException();
	}

	public static String FunctionMapping(String function) => function.ToString() switch
	{
		"Add" => nameof(function.AddRange),
		"Ceil" => "(int)" + nameof(Ceiling),
		nameof(Ceiling) => throw new NotSupportedException(),
		"Chain" => ((String)nameof(Executions)).Add('.').AddRange(nameof(Chain)),
		nameof(RedStarLinq.Fill) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)),
		"FillList" => throw new NotSupportedException(),
		nameof(Floor) => "(int)" + nameof(Floor),
		"IntRandom" => nameof(IntRandomNumber),
		nameof(IntRandomNumber) => throw new NotSupportedException(),
		"IntToReal" => "(double)",
		"IsSummertime" => nameof(DateTime.IsDaylightSavingTime),
		nameof(DateTime.IsDaylightSavingTime) => throw new NotSupportedException(),
		"Log" => ((String)nameof(Executions)).Add('.').AddRange(nameof(Log)),
		nameof(RedStarLinq.Max) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Max)),
		"Max3" => throw new NotSupportedException(),
		nameof(RedStarLinq.Mean) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Mean)),
		"Mean3" => throw new NotSupportedException(),
		nameof(RedStarLinq.Min) => ((String)nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Min)),
		"Min3" => throw new NotSupportedException(),
		"Random" => nameof(RandomNumber),
		nameof(RandomNumber) => throw new NotSupportedException(),
		nameof(Round) => "(int)" + nameof(Round),
		nameof(Truncate) => "(int)" + nameof(Truncate),
		_ => function.Copy(),
	};

	public static String PropertyMapping(String property) => property.ToString() switch
	{
		"UTCNow" => nameof(DateTime.UtcNow),
		nameof(DateTime.UtcNow) => throw new NotSupportedException(),
		_ => property.Copy(),
	};

	private static double RandomNumberBase(int calls, double initializer, double max)
	{
		var a = initializer * 5.29848949848415968;
		var b = Abs(a - Floor(a / 100000) * 100000 + Sin(calls / 1.597486513 + 2.5845984) * 45758.479849894 - 489.498489641984);
		var c = Tan((b - Floor(b / 179.999) * 179.999 - 90) * PI / 180);
		var d = Pow(Abs(Sin(Cos(Tan(calls) * 3.0362187913025793 + 0.10320655487900326) * PI - 2.032198747013) * 146283.032478491032657 - 2903.0267951604) + 0.000001, 2.3065479615036587) + Pow(Abs(Math.Log(Abs(Pow(Pow((double)calls * 123 + 64.0657980165456, 2) + Pow(max - 21.970264984615, 2), 0.5) * 648.0654731649 - 47359.03197931073648) + 0.000001)) + 0.000001, 0.60265497063473049);
		var e = Math.Log(Abs(Pow(Abs(Atan((a - Floor(a / 1000) * 1000 - max) / 169.340493) * 1.905676152049703) + 0.000001, 12.206479803657304) - 382.0654987304) + 0.000001);
		var f = Pow(Abs(c * 1573.06546157302 + d / 51065574.32761504 + e * 1031.3248941027032) + 0.000001, 2.30465546897032);
		return RealRemainder(f, max);
	}

	public static List<int> Chain(int start, int end) => new Chain(start, end - start + 1).ToList();

	public static T Choose<T>(params List<T> variants) => variants.Random();

	public static double Factorial(uint x)
	{
		if (x <= 1)
			return 1;
		else if (x > 170)
			return (double)1 / 0;
		else
		{
			double n = 1;
			for (var i = 2; i <= x; i++)
			{
				n *= i;
			}
			return n;
		}
	}

	public static double Fibonacci(uint x)
	{
		if (x <= 1)
		{
			return x;
		}
		else if (x > 1476)
		{
			return 0;
		}
		else
		{
			var a = new double[] { 0, 1, 1 };
			for (var i = 2; i <= (int)x - 1; i++)
			{
				a[0] = a[1];
				a[1] = a[2];
				a[2] = a[0] + a[1];
			}
			return a[2];
		}
	}

	public static double Frac(double x) => x - Truncate(x);

	public static int IntRandomNumber(int max)
	{
		var a = (int)Floor(RandomNumberBase(random_calls, random_initializer, max) + 1);
		random_calls++;
		return a;
	}

	public static List<T> ListWithSingle<T>(T item) => new(item);

	public static double Log(double a, double x) => Math.Log(x, a);

	public static double RandomNumber(double max)
	{
		var a = RandomNumberBase(random_calls, random_initializer, max);
		random_calls++;
		return a;
	}

	public static double RealRemainder(double x, double y) => x - Floor(x / y) * y;

	public static int RGB(int r, int g, int b) => Color.FromArgb(r, g, b).ToArgb();

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

	public static bool PropertyExists(BlockStack container, String name, out (UniversalType UnvType, PropertyAttributes Attributes)? property)
	{
		if (UserDefinedPropertiesList.TryGetValue(container, out var list) && list.TryGetValue(name, out var a))
		{
			property = a;
			return true;
		}
		if (PropertiesList.TryGetValue(container, out var list2) && list2.TryGetValue(name, out a))
		{
			property = a;
			return true;
		}
		property = null;
		return false;
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
		var index = PublicFunctionsList.IndexOfKey(name);
		if (index != -1)
		{
			function = PublicFunctionsList.Values[index];
			return true;
		}
		function = null;
		return false;
	}

	public static bool MethodExists(UniversalType container, String name, out (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type) || ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)))
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
		function = (type.GetGenericArguments().ToList(TypeMappingBack), TypeMappingBack(method.ReturnType),
			method.ReturnType.GetGenericArguments().ToList(TypeMappingBack), (method.IsAbstract
			? FunctionAttributes.Abstract : 0) | (method.IsStatic ? FunctionAttributes.Static : 0),
			new(method.GetParameters().ToList(x => new MethodParameter(TypeMappingBack(x.ParameterType), x.Name ?? "x",
			x.ParameterType.GetGenericArguments().ToList(TypeMappingBack), (x.IsOptional ? ParameterAttributes.Optional : 0)
			| (x.ParameterType.IsByRef ? ParameterAttributes.Ref : 0)
			| (x.IsOut ? ParameterAttributes.Out : 0), x.DefaultValue?.ToString() ?? "null"))));
		return true;
	}

	public static bool GeneralMethodExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out bool user)
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

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function) => UserDefinedFunctionExists(container, name, out function, out _);

	public static bool UserDefinedFunctionExists(BlockStack container, String name, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out BlockStack matchingContainer)
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

	public static TreeBranch WrapFunctionWithDelegate((List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters) function, String functionName, int pos, int endPos, BlockStack container, UniversalType? containerType)
	{
		var extra = PartialTypeToGeneralType(function.ReturnType, function.ReturnExtraTypes);
		TreeBranch branch, branch2 = new("Call", pos, endPos, container) { Extra = extra, Elements = { new("kernel", pos, endPos, container) { Extra = new List<object> { "Function " + functionName, containerType.HasValue ? "method" : "public", function } } } };
		if (function.Parameters is MethodParameters parameters)
		{
			branch = new("Parameters", pos, endPos, container);
			foreach (var x in parameters)
			{
				TreeBranch branch3 = new("Parameter", pos, endPos, container) { Elements = { new("type", pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes) }, new(x.Name, pos, endPos, container) } };
				if ((x.Attributes & ParameterAttributes.Optional) != 0)
				{
					branch3.Add(new("optional", pos, endPos, container));
					branch3.Add(new("Expr", pos, endPos, container) { Elements = { new(Universal.TryParse(x.DefaultValue.ToString(), out _) ? x.DefaultValue : "null", pos, endPos, container) } });
				}
				else
				{
					branch3.Add(new("no optional", pos, endPos, container));
				}
				branch.Add(branch3);
				branch2.Add(new("Hypername", pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes), Elements = { new(x.Name, pos, endPos, container) { Extra = PartialTypeToGeneralType(x.Type, x.ExtraTypes) } } });
			}
		}
		else
		{
			branch = new("no parameters", pos, endPos, container);
		}
		if (containerType.HasValue && branch2.Elements[0].Extra is List<object> list)
			branch2.Elements[0].Extra = list.Append(new Universal(containerType.Value, GetPrimitiveType("typename")));
		TreeBranch branch4 = new("Main", pos, endPos, container) { Elements = { new("return", pos, endPos, container) { Elements = { new("Expr", pos, endPos, container) { Elements = { new("Hypername", pos, endPos, container) { Elements = { new(functionName + " (function)", pos, endPos, container) { Extra = extra }, branch2 }, Extra = extra } }, Extra = extra } } } } };
		return new("Function", pos, endPos, container) { Elements = { new("_", pos, endPos, container), new("type", pos, endPos, container) { Extra = extra }, branch, branch4 } };
	}

	public static TreeBranch WrapFunctionWithDelegate((GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch Location) function, String functionName, int pos, int endPos, BlockStack container, UniversalType? containerType)
	{
		var extra = function.ReturnUnvType;
		TreeBranch branch, branch2 = new("Call", pos, endPos, container) { Extra = extra, Elements = { new("kernel", pos, endPos, container) { Extra = new List<object> { "Function " + functionName, "general", function } } } };
		if (function.Parameters is GeneralMethodParameters parameters)
		{
			branch = new("Parameters", pos, endPos, container);
			foreach (var x in parameters)
			{
				TreeBranch branch3 = new("Parameter", pos, endPos, container) { Elements = { new("type", pos, endPos, container) { Extra = (x.Type, x.ExtraTypes) }, new(x.Name, pos, endPos, container) } };
				if ((x.Attributes & ParameterAttributes.Optional) != 0)
				{
					branch3.Add(new("optional", pos, endPos, container));
					branch3.Add(new("Expr", pos, endPos, container) { Elements = { new(Universal.TryParse(x.DefaultValue.ToString(), out _) ? x.DefaultValue : "null", pos, endPos, container) } });
				}
				else
				{
					branch3.Add(new("no optional", pos, endPos, container));
				}
				branch.Add(branch3);
				branch2.Add(new("Hypername", pos, endPos, container) { Extra = (x.Type, x.ExtraTypes), Elements = { new(x.Name, pos, endPos, container) { Extra = (x.Type, x.ExtraTypes) } } });
			}
		}
		else
		{
			branch = new("no parameters", pos, endPos, container);
		}
		if (containerType.HasValue && branch2.Elements[0].Extra is List<object> list)
			branch2.Elements[0].Extra = list.Append(new Universal(containerType.Value, GetPrimitiveType("typename")));
		TreeBranch branch4 = new("Main", pos, endPos, container) { Elements = { new("return", pos, endPos, container) { Elements = { new("Expr", pos, endPos, container) { Elements = { new("Hypername", pos, endPos, container) { Elements = { new(functionName + " (function)", pos, endPos, container) { Extra = extra }, branch2 }, Extra = extra } }, Extra = extra } } } } };
		return new("Function", pos, endPos, container) { Elements = { new("_", pos, endPos, container), new("type", pos, endPos, container) { Extra = extra }, branch, branch4 } };
	}

	public static bool ConstructorsExist(UniversalType container, out GeneralConstructorOverloads? constructors)
	{
		var containerType = SplitType(container.MainType);
		if (!(PrimitiveTypesList.TryGetValue(containerType.Type, out var type) || ExtraTypesList.TryGetValue((containerType.Container.ToShortString(), containerType.Type), out type)))
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
			new GeneralMethodParameter(CreateVar(PartialTypeToGeneralType(TypeMappingBack(y.ParameterType),
			y.ParameterType.GetGenericArguments().ToList(TypeMappingBack)), out var UnvType).MainType, y.Name ?? "x",
			UnvType.ExtraTypes, (y.IsOptional ? ParameterAttributes.Optional : 0)
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

	public static UniversalType GetSubtype(UniversalType type, int levels = 1)
	{
		if (levels <= 0)
		{
			return type;
		}
		else if (levels == 1)
		{
			if (TypeIsPrimitive(type.MainType))
			{
				if (type.MainType.Peek().Name == "list")
				{
					return GetListSubtype(type);
				}
				else
				{
					return NullType;
				}
			}
			else
			{
				return NullType;
			}
		}
		else
		{
			var t = type;
			for (var i = 0; i < levels; i++)
			{
				t = GetSubtype(t);
			}
			return t;
		}
	}

	private static UniversalType GetListSubtype(UniversalType type)
	{
		if (type.ExtraTypes.Length == 1)
		{
			return (type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes);
		}
		else if (!(type.ExtraTypes[0].MainType.IsValue && int.TryParse(type.ExtraTypes[0].MainType.Value.ToString(), out var n)))
		{
			return NullType;
		}
		else if (n <= 2)
		{
			return GetListType(type.ExtraTypes[1]);
		}
		else
		{
			return (ListBlockStack, new GeneralExtraTypes { ((TypeOrValue)(n - 1).ToString(), NoGeneralExtraTypes), type.ExtraTypes[1] });
		}
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
			else if (LeafType.MainType.Length != 0 && LeafType.MainType.Peek().Type == BlockType.Class && ListTypesList.Contains(LeafType.MainType.Peek().Name))
			{
				Depth++;
				LeafType = (LeafType.ExtraTypes[^1].MainType.Type, LeafType.ExtraTypes[^1].ExtraTypes);
			}
			else
			{
				return (Depth, LeafType);
			}
		}
	}

	public static UniversalType GetResultType(UniversalType type1, UniversalType type2)
	{
		try
		{
			if (TypesAreEqual(type1, type2))
			{
				return type1;
			}
			if (TypeIsPrimitive(type1.MainType) && TypeIsPrimitive(type2.MainType))
			{
				var left_type = type1.MainType.Peek().Name;
				var right_type = type2.MainType.Peek().Name;
				if (type1.ExtraTypes.Length == 0 && type2.ExtraTypes.Length == 0)
				{
					return GetPrimitiveType(GetPrimitiveResultType(left_type, right_type));
				}
				else if (left_type == "list" || right_type == "list")
				{
					return GetListResultType(type1, type2, left_type, right_type);
				}
				else
				{
					return NullType;
				}
			}
			else
			{
				return NullType;
			}
		}
		catch (StackOverflowException)
		{
			return NullType;
		}
	}

	private static String GetPrimitiveResultType(String left_type, String right_type)
	{
		if (left_type == "dynamic" || right_type == "dynamic")
		{
			return "dynamic";
		}
		else if (left_type == "string" || right_type == "string")
		{
			return "string";
		}
		else if (left_type == "long complex" || right_type == "long complex")
		{
			return "long complex";
		}
		else if (left_type == "long real" || right_type == "long real")
		{
			return "long real";
		}
		else if (left_type == "long long" || right_type == "long long")
		{
			if (left_type == "complex" || right_type == "complex")
			{
				return "long complex";
			}
			else if (left_type == "real" || right_type == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (left_type == "unsigned long long" || right_type == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan", "real", "complex" }.Contains(right_type))
			{
				return "long long";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (left_type == "complex" || right_type == "complex")
		{
			return "complex";
		}
		else if (left_type == "real" || right_type == "real")
		{
			return "real";
		}
		else if (left_type == "unsigned long int" || right_type == "unsigned long int")
		{
			if (new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(left_type) || new List<String> { "short int", "int", "long int", "DateTime", "TimeSpan" }.Contains(right_type))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (left_type == "TimeSpan" || right_type == "TimeSpan")
		{
			return "TimeSpan";
		}
		else if (left_type == "DateTime" || right_type == "DateTime")
		{
			return "DateTime";
		}
		else if (left_type == "long int" || right_type == "long int")
		{
			return "long int";
		}
		else if (left_type == "long char" || right_type == "long char")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (left_type == "unsigned int" || right_type == "unsigned int")
		{
			if (left_type == "short int" || right_type == "short int" || left_type == "int" || right_type == "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (left_type == "int" || right_type == "int")
		{
			return "int";
		}
		else if (left_type == "char" || right_type == "char")
		{
			if (left_type == "short int" || right_type == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (left_type == "unsigned short int" || right_type == "unsigned short int")
		{
			if (left_type == "short int" || right_type == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (left_type == "short int" || right_type == "short int")
		{
			return "short int";
		}
		else if (left_type == "short char" || right_type == "short char")
		{
			return "short char";
		}
		else if (left_type == "byte" || right_type == "byte")
		{
			return "byte";
		}
		else if (left_type == "bool" || right_type == "bool")
		{
			return "bool";
		}
		else if (left_type == "BaseClass" || right_type == "BaseClass")
		{
			return "BaseClass";
		}
		else
		{
			return "null";
		}
	}

	public static String GetQuotientType(String leftType, Universal right, String rightType)
	{
		if (leftType == "long real" || rightType == "long real")
		{
			return "long real";
		}
		else if (leftType == "long long" || rightType == "long long")
		{
			if (leftType == "real" || rightType == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (leftType == "unsigned long long" || rightType == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "real" }.Contains(leftType) || new List<String> { "short int", "int", "long int", "real" }.Contains(rightType))
			{
				return "long real";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (leftType == "real" || rightType == "real")
		{
			return "real";
		}
		else if (rightType == "bool")
		{
			return "byte";
		}
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() >= (ulong)1 << 56)
			{
				return "byte";
			}
			else if (right.ToUnsignedLongInt() >= (ulong)1 << 48)
			{
				return "unsigned short int";
			}
			else if (right.ToUnsignedLongInt() >= 4294967296)
			{
				return "unsigned int";
			}
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() >= (long)1 << 48)
			{
				return "short int";
			}
			else if (right.ToLongInt() >= 4294967296)
			{
				return "int";
			}
			else if (rightType == "unsigned long int")
			{
				return "long long";
			}
			else
			{
				return "long int";
			}
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() >= 16777216)
			{
				return "byte";
			}
			else if (right.ToUnsignedInt() >= 65536)
			{
				return "unsigned short int";
			}
			else if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
			{
				return "long int";
			}
			else if (right.ToInt() >= 65536)
			{
				return "short int";
			}
			else
			{
				return "int";
			}
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (leftType == "unsigned short int")
		{
			if (right.ToUnsignedShortInt() >= 256)
			{
				return "byte";
			}
			else if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (leftType == "short int")
		{
			if (rightType == "unsigned short int")
			{
				return "int";
			}
			else
			{
				return "short int";
			}
		}
		else if (leftType == "short char" || rightType == "short char")
		{
			return "short char";
		}
		else if (leftType.ToString() is "byte" or "bool")
		{
			return "byte";
		}
		else
		{
			return "null";
		}
	}

	public static String GetRemainderType(String leftType, Universal right, String rightType)
	{
		if (leftType == "long real" || rightType == "long real")
		{
			return "long real";
		}
		else if (leftType == "long long" || rightType == "long long")
		{
			if (leftType == "real" || rightType == "real")
			{
				return "long real";
			}
			else
			{
				return "long long";
			}
		}
		else if (leftType == "unsigned long long" || rightType == "unsigned long long")
		{
			if (new List<String> { "short int", "int", "long int", "real" }.Contains(leftType) || new List<String> { "short int", "int", "long int", "real" }.Contains(rightType))
			{
				return "long real";
			}
			else
			{
				return "unsigned long long";
			}
		}
		else if (leftType == "real" || rightType == "real")
		{
			return "real";
		}
		else if (rightType == "bool")
		{
			return "byte";
		}
		if (leftType == "unsigned long int")
		{
			if (right.ToUnsignedLongInt() <= 256)
			{
				return "byte";
			}
			else if (right.ToUnsignedLongInt() <= 65536)
			{
				return "unsigned short int";
			}
			else if (right.ToUnsignedLongInt() <= 4294967296)
			{
				return "unsigned int";
			}
			else if (new List<String> { "short int", "int", "long int" }.Contains(rightType))
			{
				return "long long";
			}
			else
			{
				return "unsigned long int";
			}
		}
		else if (leftType == "long int")
		{
			if (right.ToLongInt() <= 32768)
			{
				return "short int";
			}
			else if (right.ToLongInt() <= 2147483648)
			{
				return "int";
			}
			else if (rightType == "unsigned long int")
			{
				return "long long";
			}
			else
			{
				return "long int";
			}
		}
		else if (leftType == "long char" || rightType == "long char")
		{
			if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "long char";
			}
		}
		else if (leftType == "unsigned int")
		{
			if (right.ToUnsignedInt() <= 256)
			{
				return "byte";
			}
			else if (right.ToUnsignedInt() <= 65536)
			{
				return "unsigned short int";
			}
			else if (rightType.ToString() is "short int" or "int")
			{
				return "long int";
			}
			else
			{
				return "unsigned int";
			}
		}
		else if (leftType == "int")
		{
			if (rightType == "unsigned int")
			{
				return "long int";
			}
			else if (right.ToInt() <= 32768)
			{
				return "short int";
			}
			else
			{
				return "int";
			}
		}
		else if (leftType == "char" || rightType == "char")
		{
			if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "char";
			}
		}
		else if (leftType == "unsigned short int")
		{
			if (right.ToUnsignedShortInt() <= 256)
			{
				return "byte";
			}
			else if (rightType == "short int")
			{
				return "int";
			}
			else
			{
				return "unsigned short int";
			}
		}
		else if (leftType == "short int")
		{
			if (rightType == "unsigned short int")
			{
				return "int";
			}
			else
			{
				return "short int";
			}
		}
		else if (leftType == "short char" || rightType == "short char")
		{
			return "short char";
		}
		else if (leftType.ToString() is "byte" or "bool")
		{
			return "byte";
		}
		else
		{
			return "null";
		}
	}

	private static UniversalType GetListResultType(UniversalType type1, UniversalType type2, String left_type, String right_type)
	{
		if (ListTypesList.Contains(left_type) || ListTypesList.Contains(right_type))
			return GetListType(GetResultType(GetSubtype(type1), GetSubtype(type2)));
		else if (left_type == "list")
			return GetListType(GetResultType(GetSubtype(type1), (right_type == "list") ? GetSubtype(type2) : type2));
		else
			return GetListType(GetResultType(type1, GetSubtype(type2)));
	}

	private static String ListTypeToString(UniversalType type, String basic_type)
	{
		if (type.ExtraTypes.Length == 1)
		{
			return basic_type + "() " + (type.ExtraTypes[0].MainType.IsValue ? type.ExtraTypes[0].MainType.Value : (type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes));
		}
		else
		{
			return basic_type + "(" + type.ExtraTypes[0].MainType.Value + ") " + (type.ExtraTypes[1].MainType.IsValue ? type.ExtraTypes[1].MainType.Value : (type.ExtraTypes[1].MainType.Type, type.ExtraTypes[1].ExtraTypes));
		}
	}

	public static UniversalType PartialTypeToGeneralType(String mainType, List<String> extraTypes) => (GetPrimitiveBlockStack(mainType), GetGeneralExtraTypes(extraTypes));

	public static GeneralExtraTypes GetGeneralExtraTypes(List<String> partialBlockStack) => new(partialBlockStack.Convert(x => (UniversalTypeOrValue)((TypeOrValue)new BlockStack([new Block(BlockType.Primitive, x, 1)]), NoGeneralExtraTypes)));

	public static (BlockStack Container, String Type) SplitType(BlockStack blockStack) => (new(blockStack.ToList().SkipLast(1)), blockStack.TryPeek(out var block) ? block.Name : "");

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
		if (TypeEqualsToPrimitive(destinationType, "list", false))
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
				destExpr = srcExpr ?? null;
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
				{
					return false;
				}
			}
			catch (StackOverflowException)
			{
				return false;
			}
		}
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
			new_types_list = new(new_types2_list);
			types_list.AddRange(new_types2_list);
			if (new_types2_list.Length == 0)
			{
				break;
			}
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

	public static List<(UniversalType Type, bool Warning)> GetCompatibleTypes((UniversalType Type, bool Warning) source, List<(UniversalType Type, bool Warning)> blackList)
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
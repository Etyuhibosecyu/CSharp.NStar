global using Corlib.NStar;
global using System;
global using G = System.Collections.Generic;
global using static CSharp.NStar.Constructions;
global using static CSharp.NStar.Executions;
global using static System.Math;
global using String = Corlib.NStar.String;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using EasyEvalLib;

namespace CSharp.NStar;

public sealed class SemanticTree
{
	private readonly List<Lexem> lexems;
	private readonly String input;
	private bool wreckOccurred;
	private readonly TreeBranch topBranch = TreeBranch.DoNotAdd();

	private readonly List<String>? errorsList = null;

	public SemanticTree(List<Lexem> lexems, String input, TreeBranch topBranch, List<String>? errorsList, bool wreckOccurred)
	{
		this.lexems = lexems;
		this.input = input;
		this.topBranch = topBranch;
		this.wreckOccurred = wreckOccurred;
		this.errorsList = errorsList;
		//_BranchStack[0] = topBranch;
	}

	public SemanticTree((List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) x) : this(x.Lexems, x.String, x.TopBranch, x.ErrorsList, x.WreckOccurred)
	{
	}

	public SemanticTree(LexemStream lexemStream) : this(lexemStream.Parse())
	{
	}

	public static String ExecuteStringPrefix { get; } = "list() dynamic args = null;";

	public static String ExecuteStringPrefixCompiled { get; } = new SemanticTree((LexemStream)new CodeSample(ExecuteStringPrefix)).Parse(out _);

	public String Parse(out List<String> errorsList)
	{
		List<String> innerErrorsList = [];
		try
		{
			var result = CalculationParseAction(topBranch.Info)(topBranch, out innerErrorsList);
			errorsList = innerErrorsList;
			return result;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			innerErrorsList.Add("Wreck in unknown line at unknown position: execution failed because of internal error");
			errorsList = innerErrorsList;
			wreckOccurred = true;
			return "";
		}
	}

	private delegate String ParseAction(TreeBranch branch, out List<String> errorsList);

	private ParseAction CalculationParseAction(String info) => info.ToString() switch
	{
		"Main" => Main,
		"Function" => Function,
		"Constructor" => Constructor,
		"if" or "else if" or "if!" or "else if!" => Condition,
		"loop" => Loop,
		"while" => While,
		"repeat" => Repeat,
		"for" => For,
		"Declaration" => Declaration,
		"Hypername" => Hypername,
		"Expr" or "Indexes" or "Call" or "ConstructorCall" or "Ternary" or "PMExpr" or "MulDivExpr" or "StringConcatenation" or "Assignment" or "UnaryAssignment" => Expr,
		nameof(List) => List,
		"return" => Return,
		_ => Default,
	};

	private String Main(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		foreach (var x in branch.Elements)
		{
			var s = CalculationParseAction(x.Info)(x, out var innerErrorsList);
			if (s != "")
			{
				if (branch.Info == "Main" && x.Info == "Main" && !s.EndsWith('}') && s[..^1].Contains(';'))
					result.Add('{');
				result.AddRange(s);
				if (s[^1] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
					result.Add(' ');
				if (ExprTypesList.Contains(x.Info) && !s.EndsWith(';') || x.Info.ToString() is "continue" or "break")
					result.Add(';');
				if (branch.Info == "Main" && x.Info == "Main" && !s.EndsWith('}') && s[..^1].Contains(';'))
					result.Add('}');
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private String Function(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		var name = branch[0].Info;
		var t = UserDefinedFunctionsList[branch.Container][name][0];
		t.Location = branch;
		UserDefinedFunctionsList[branch.Container][name][0] = t;
		var (_, ReturnUnvType, Attributes, Parameters, _) = UserDefinedFunctionsList[branch.Container][name][0];
		if ((Attributes & FunctionAttributes.Closed) != 0)
			result.AddRange("private ");
		if ((Attributes & FunctionAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & FunctionAttributes.Internal) != 0)
			result.AddRange("internal ");
		if ((Attributes & FunctionAttributes.Static) != 0)
			result.AddRange("static ");
		if ((Attributes & FunctionAttributes.Abstract) != 0)
			result.AddRange("abstract ");
		result.AddRange(Type(ReturnUnvType)).Add(' ').AddRange(name).Add('(');
		result.AddRange(SemanticTree.Parameters(Parameters, out var parametersErrorsList));
		errorsList.AddRange(parametersErrorsList);
		result.AddRange("){").AddRange(ParametersCreate(Parameters, out var pcErrorsList)).AddRange(CalculationParseAction(branch[^1].Info)(branch[^1], out var coreErrorsList).Add('}'));
		errorsList.AddRange(pcErrorsList).AddRange(coreErrorsList);
		return result;
	}

	private String Constructor(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		foreach (var x in branch.Elements)
		{
			var s = CalculationParseAction(x.Info)(x, out var innerErrorsList);
			if (s != "")
			{
				result.AddRange(s);
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private String Condition(TreeBranch branch, out List<String> errorsList)
	{
		String result = branch.Info.ToString() switch
		{
			"if" => "if (((Universal)",
			"else if" => "else if (((Universal)",
			"if!" => "if ((!((Universal)",
			"else if!" => "else if ((!((Universal)",
			_ => throw new InvalidOperationException(),
		};
		errorsList = [];
		var s = CalculationParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s != "")
		{
			result.AddRange(s);
			errorsList.AddRange(innerErrorsList);
		}
		if (branch.Info.EndsWith('!'))
			result.Add(')');
		return result.AddRange(").ToBool())");
	}

	private String Loop(TreeBranch branch, out List<String> errorsList)
	{
		errorsList = [];
		return "while (true)";
	}

	private String While(TreeBranch branch, out List<String> errorsList)
	{
		String result = "while (";
		errorsList = [];
		var s = CalculationParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s != "")
		{
			result.AddRange(s);
			errorsList.AddRange(innerErrorsList);
		}
		return result.Add(')');
	}

	private String Repeat(TreeBranch branch, out List<String> errorsList)
	{
		String result = "var ";
		var lengthName = RedStarLinq.NFill(32, _ => (char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
		result.AddRange(lengthName);
		result.AddRange(" = ");
		errorsList = [];
		var s = CalculationParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s != "")
		{
			result.AddRange(s);
			errorsList.AddRange(innerErrorsList);
		}
		var counterName = RedStarLinq.NFill(32, _ => (char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
		result.AddRange(";for (var ").AddRange(counterName).AddRange(" = 0; ").AddRange(counterName).AddRange(" < ");
		result.AddRange(lengthName).AddRange("; ").AddRange(counterName).AddRange("++)");
		return result;
	}

	private String For(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		foreach (var x in branch.Elements)
		{
			var s = CalculationParseAction(x.Info)(x, out var innerErrorsList);
			if (s != "")
			{
				result.AddRange(s);
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private String Declaration(TreeBranch branch, out List<String> errorsList)
	{
		errorsList = [];
		if (!(branch.Length == 2 && branch[0].Info == "type"))
		{
			var otherPos = branch[0].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return "_";
		}
		var s = branch[1].Info;
		if (VariableExists(branch, s, ref errorsList!))
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))] = new("_", branch[0].Pos, branch[0].EndPos, branch.Container)
			{
				Extra = NullType
			};
		else
		{
			if (branch[0].Extra is not UniversalType UnvType)
				branch.Extra = NullType;
			else if (TypeEqualsToPrimitive(UnvType, "var"))
			{
				var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
				if (prevIndex >= 2 && branch.Parent[prevIndex - 1].Extra is UniversalType AssigningUnvType && branch.Parent.Length >= 3 && branch.Parent[prevIndex + 1].Info == "=")
					branch.Extra = branch[1 - 1].Extra = AssigningUnvType;
				else
				{
					var otherPos = branch[1 - 1].Pos;
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": variable declared with the keyword \"var\" must be assigned explicitly and in the same expression");
					branch.Parent[prevIndex] = new("_", branch[0].Pos, branch[0].EndPos, branch.Container) { Extra = NullType };
					return "_";
				}
			}
			else
				branch.Extra = UnvType;
		}
		return Type(branch.Extra is UniversalType ResultType ? ResultType : NullType).Copy().Add(' ').AddRange(s);
	}

	private String Hypername(TreeBranch branch, out List<String> errorsList) => Hypername(branch, out errorsList, null);

	private String Hypername(TreeBranch branch, out List<String> errorsList, object? extra)
	{
		String result = "";
		errorsList = [];
		result.AddRange(Hypername1(branch, out var firstErrorsList, ref extra));
		errorsList.AddRange(firstErrorsList);
		for (var i = 1; i < branch.Length; i++)
		{
			if (i == 1 && branch[i].Info.ToString() is "Call" or "ConstructorCall")
				result.Replace(Hypername2(branch, errorsList, ref extra, ref i));
			else
				result.AddRange(Hypername2(branch, errorsList, ref extra, ref i));
		}
		return result;
	}

	private String Hypername1(TreeBranch branch, out List<String> errorsList, ref object? extra)
	{
		String result = "";
		errorsList = [];
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		if (extra == null)
		{
			var info = branch[0].Info;
			if (TryReadValue(info, out var value))
				result.AddRange(value.ToString(true, true));
			else if (branch[0].Length != 0)
			{
				result.AddRange(CalculationParseAction(branch[0].Info)(branch[0], out var innerErrorsList));
				AddRange(ref errorsList!, innerErrorsList);
			}
			else if (info == "type")
				result.AddRange(branch[0].Extra is UniversalType type2 ? Type(type2) : "dynamic");
			else if (info == "new type")
				result.AddRange(info);
			else if (IsVariableDeclared(branch, info, out var innerErrorsList, out var extra2))
			{
				if (extra2 is UniversalType UnvType)
				{
					branch[0].Extra = UnvType;
					extra = new List<object> { (String)"Variable", UnvType };
				}
				else
				{
					branch[0].Extra = NullType;
					extra = new List<object> { (String)"Variable", NullType };
				}
				result.AddRange(info);
				AddRange(ref errorsList!, innerErrorsList!);
			}
			else if (IsPropertyDeclared(branch, info, out innerErrorsList, out var property, out _))
			{
				if (property.HasValue)
				{
					branch[0].Extra = (property?.UnvType.MainType, property?.UnvType.ExtraTypes);
					extra = new List<object> { (String)"Property", (property?.UnvType.MainType, property?.UnvType.ExtraTypes) };
				}
				else
				{
					branch[0].Extra = NullType;
					extra = new List<object> { (String)"Property", NullType };
				}
				result.AddRange(info);
			}
			else if (IsFunctionDeclared(branch, info, out innerErrorsList, out var function, out var functionContainer, out _))
			{
				if (functionContainer.Length == 0)
					HypernamePublicGeneralMethod(branch, info, ref extra, function, "user");
				else if (HypernameGeneralMethod(branch, info, ref extra, errorsList, prevIndex, functionContainer, function, "userMethod") != null)
					return "_";
				result.AddRange(info);
			}
			else if (PublicFunctionExists(info, out var function2))
			{
				if (info == "Q" && !(branch.Length >= 2 && branch[1].Info == "Call"))
				{
					var otherPos = branch[0].Pos;
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + info + "\" cannot be used in the delegate");
					branch.Parent[prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
					return "_";
				}
				HypernamePublicFunction(branch, info, ref extra, function2);
				result.AddRange(info);
			}
			else if (GeneralMethodExists(new(), info, out function, out var user))
			{
				if (info == "ExecuteString" && !(branch.Length >= 2 && branch[1].Info == "Call"))
				{
					var otherPos = branch[0].Pos;
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + info + "\" cannot be used in the delegate");
					branch.Parent[prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
					return "_";
				}
				HypernamePublicGeneralMethod(branch, info, ref extra, function, user ? "user" : "general");
				result.AddRange(info);
			}
			else
			{
				var otherPos = branch[0].Pos;
				if (innerErrorsList != null && innerErrorsList.Length != 0)
					AddRange(ref errorsList!, innerErrorsList);
				else
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": identifier \"" + info + "\" is not defined in this location");
				branch.Parent[prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				return "_";
			}
		}
		return result;
	}

	private String Hypername2(TreeBranch branch, List<String> errorsList, ref object? extra, ref int index)
	{
		if (branch[index].Info == "Call" && extra is List<object> list)
		{
			if (list.Length == 2 && list[0] is String delegateElem1 && (delegateElem1 == "Variable" || delegateElem1 == "Property") && list[1] is UniversalType DelegateUnvType && new BlockStackEComparer().Equals(DelegateUnvType.MainType, FuncBlockStack) && DelegateUnvType.ExtraTypes.Length != 0 && !DelegateUnvType.ExtraTypes[0].MainType.IsValue)
			{
				var result = branch[index - 1].Info.Copy().AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			if (!(list.Length >= 3 && list.Length <= 5 && list[0] is String elem1 && elem1.StartsWith("Function ") && list[1] is String elem2))
			{
				var otherPos = branch[index].Pos;
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
			var s = elem1["Function ".Length..];
			if (s == "ExecuteString")
			{
				var @string = CalculationParseAction(branch[index][0].Info)(branch[index][0], out var innerErrorsList);
				errorsList.AddRange(innerErrorsList);
				var addParameters = branch[index].Length != 1;
				String? parameters;
				if (addParameters)
				{
					parameters = ((String)", ").AddRange(CalculationParseAction(nameof(List))(new(nameof(List), branch[index].Elements[1..], branch.Container), out var parametersErrorsList));
					errorsList.AddRange(parametersErrorsList);
				}
				else
					parameters = (String)"";
				if (parameters.StartsWith(", (") && parameters.EndsWith(')'))
				{
					parameters[2] = '[';
					parameters[^1] = ']';
				}
				var result = ((String)nameof(EasyEval.Eval)).AddRange("(new SemanticTree((LexemStream)new CodeSample(").Add('\"').AddRange(ExecuteStringPrefix).AddRange("\" + ").AddRange(@string);
				result.AddRange(")).Parse(out var errorsList)[").AddRange(nameof(SemanticTree)).Add('.').AddRange(nameof(ExecuteStringPrefixCompiled));
				result.AddRange(""".Length..], ["HighLevelAnalysis.Debug", "LowLevelAnalysis", "MidLayer", "Core", "EasyEval"], ["CSharp.NStar", "static EasyEvalLib.EasyEval"]""");
				result.AddRange(parameters).Add(')');
				return result;
			}
			else if (s == "Q")
			{
				return ((String)"@\"").AddRange(input.Replace("\"", "\"\"")).Add('\"');
			}
			else if (elem2 == "public")
			{
				var result = FunctionMapping(s).AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			else if (!elem2.StartsWith("user") && list.Length >= 4 && list[3] is Universal extraValue)
			{
				var result = FunctionMapping(s).AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			else if (elem2 == "user" && UserDefinedFunctionExists(new(), s, out var function) && function.HasValue && function?.Location != null)
			{
				var result = s.Copy().AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			else if (elem2 == "userMethod" && UserDefinedFunctionExists(branch.Container, s, out function) && function.HasValue && function?.Location != null)
			{
				var result = s.Copy().AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			else if (elem2 == "userMethod" && list.Length >= 4 && list[3] is Universal containerValue && containerValue.InnerType is UniversalType ContainerUnvType)
			{
				if (TypeEqualsToPrimitive(ContainerUnvType, "typename") && containerValue.GetCustomObject() is UniversalType UnvType && UserDefinedFunctionExists(UnvType.MainType, s, out function, out _) && function.HasValue && function?.Location != null)
				{
					var result = s.Copy().AddRange(CalculationParseAction(nameof(List))(branch[index], out var innerErrorsList));
					errorsList.AddRange(innerErrorsList);
					return result;
				}
				else if (UserDefinedFunctionExists(ContainerUnvType.MainType, s, out function, out _) && function.HasValue && function?.Location != null)
				{
					var result = s.Copy().AddRange(CalculationParseAction(branch[index].Info)(branch[index], out var innerErrorsList));
					errorsList.AddRange(innerErrorsList);
					return result;
				}
			}
			else
			{
				var otherPos = branch[index].Pos;
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
		}
		else if (branch[index].Info == "ConstructorCall" && extra is List<object> list2)
		{
			if (!(list2.Length >= 3 && list2.Length <= 5 && list2[0] is String elem1 && elem1 == "Constructor" && list2[1] is UniversalType ConstructingUnvType && list2[2] is String elem3 && list2[3] is ConstructorOverloads constructors && constructors.Length != 0))
			{
				var otherPos = branch[index].Pos;
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
			if (elem3 == "typical" || constructors[^1].Location == null)
			{
				var result = (elem3 == "typical" ? (String)nameof(ExecuteTypicalConstructor) : nameof(ExecuteDefaultConstructor)).Add('(').AddRange(ConstructingUnvType.ToString()).AddRange(CalculationParseAction(branch[index].Info)(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
			else
			{
				var result = ((String)"new ").AddRange(Type(ConstructingUnvType)).AddRange(CalculationParseAction(branch[index].Info)(branch[index], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
				return result;
			}
		}
		if (index >= branch.Length)
		{
			return "";
		}
		else if (branch[index].Info == "Indexes")
		{
			String result = "";
			if (branch[index - 1].Extra is not UniversalType CollectionUnvType)
				return "";
			foreach (var x in branch[index].Elements)
			{
				result.AddRange("[(").AddRange(CalculationParseAction(x.Info)(x, out var innerErrorsList)).AddRange(TypeEqualsToPrimitive(CollectionUnvType, "list", false) ? ") - 1]" : ")]");
				errorsList.AddRange(innerErrorsList);
			}
			branch.Extra = GetSubtype(CollectionUnvType, branch[index].Length);
			return result;
		}
		else if (branch[index].Info == "Call")
		{
			var otherPos = branch[index].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": this call is forbidden");
			return "default!";
		}
		else if (branch[index].Info == ".")
		{
			var result = ((String)'.').AddRange(Hypername(branch[index], out var innerErrorsList));
			errorsList.AddRange(innerErrorsList);
			return result;
		}
		else
		{
			var otherPos = branch[index].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
			return "default!";
		}
	}

	private static bool? HypernamePublicFunction(TreeBranch branch, String s, ref object? refExtra, (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		var extraTypes = GetGeneralExtraTypes(function?.ReturnExtraTypes ?? []);
		UniversalType extra;
		object extra2;
		if (!function.HasValue)
		{
			extra = NullType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), (String)"public", default! };
		}
		else
		{
			extra = (GetPrimitiveBlockStack(function?.ReturnType ?? "null"), extraTypes);
			extra2 = new List<object> { ((String)"Function ").AddRange(s), (String)"public", function!.Value };
		}
		HypernameAddExtra(branch, extra, extra2, ref refExtra, new(function?.Parameters?.Convert(x => (UniversalTypeOrValue)((TypeOrValue)GetPrimitiveBlockStack(x.Type), GetGeneralExtraTypes(x.ExtraTypes)))?.Prepend(extra).ToList() ?? [extra]));
		return null;
	}

	private bool? HypernameMethod(TreeBranch branch, String s, ref object? refExtra, List<String> errorsList, int prevIndex, BlockStack ContainerMainType, (List<String> ExtraTypes, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes Attributes, MethodParameters Parameters)? function)
	{
		var extraTypes = GetGeneralExtraTypes(function?.ReturnExtraTypes ?? []);
		UniversalType extra;
		object extra2;
		if (!function.HasValue)
		{
			extra = NullType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), (String)"method", default! };
		}
		else if ((function?.Attributes & FunctionAttributes.Closed) != 0 ^ (function?.Attributes & FunctionAttributes.Protected) != 0 && !ListStartsWith(new List<Block>(branch.Container), new(ContainerMainType)))
		{
			var otherPos = branch[0].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name.ToString()), .. s]) + "\" is inaccessible from here");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else if ((function?.Attributes & FunctionAttributes.Static) == 0 && !(branch.Length >= 2 && branch[1].Info == "Call"))
		{
			var otherPos = branch[0].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name.ToString()), .. s]) + "\" is linked with object instance so it cannot be used in delegate");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else
		{
			extra = (GetPrimitiveBlockStack(function?.ReturnType ?? "null"), extraTypes);
			extra2 = new List<object> { ((String)"Function ").AddRange(s), (String)"method", function!.Value };
		}
		HypernameAddExtra(branch, extra, extra2, ref refExtra, new(function?.Parameters?.Convert(x => (UniversalTypeOrValue)((TypeOrValue)GetPrimitiveBlockStack(x.Type), GetGeneralExtraTypes(x.ExtraTypes)))?.Prepend(extra).ToList() ?? [extra]));
		return null;
	}

	private bool? HypernameGeneralMethod(TreeBranch branch, String s, ref object? refExtra, List<String> errorsList, int prevIndex, BlockStack ContainerMainType, (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, String category)
	{
		UniversalType extra;
		object extra2;
		if (!function.HasValue)
		{
			extra = NullType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), category, default! };
		}
		else if ((function?.Attributes & FunctionAttributes.Closed) != 0 ^ (function?.Attributes & FunctionAttributes.Protected) != 0 && !ListStartsWith(new List<Block>(branch.Container), new(ContainerMainType)))
		{
			var otherPos = branch[0].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name.ToString()), .. s]) + "\" is inaccessible from here");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else if ((function?.Attributes & FunctionAttributes.Static) == 0 && !(branch.Length >= 2 && branch[1].Info == "Call"))
		{
			var otherPos = branch[0].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name.ToString()), .. s]) + "\" is linked with object instance so it cannot be used in delegate");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else
		{
			extra = function!.Value.ReturnUnvType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), category, function.Value };
		}
		GeneralExtraTypes parameterTypes = new(function?.Parameters?.Convert(x => (UniversalTypeOrValue)((TypeOrValue)x.Type, x.ExtraTypes))?.Prepend(extra).ToList() ?? [extra]);
		HypernameAddExtra(branch, extra, extra2, ref refExtra, parameterTypes);
		return null;
	}

	private static bool? HypernamePublicGeneralMethod(TreeBranch branch, String s, ref object? refExtra, (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, String category)
	{
		UniversalType extra;
		object extra2;
		if (!function.HasValue)
		{
			extra = NullType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), category, default! };
		}
		else
		{
			extra = function.Value.ReturnUnvType;
			extra2 = new List<object> { ((String)"Function ").AddRange(s), category, function.Value };
		}
		GeneralExtraTypes parameterTypes = new(function?.Parameters?.Convert(x => (UniversalTypeOrValue)((TypeOrValue)x.Type, x.ExtraTypes))?.Prepend(extra).ToList() ?? [extra]);
		HypernameAddExtra(branch, extra, extra2, ref refExtra, parameterTypes);
		return null;
	}

	private static void HypernameAddExtra(TreeBranch branch, UniversalType extra, object extra2, ref object? refExtra, GeneralExtraTypes extraTypes)
	{
		if (branch.Length >= 2 && branch[1].Info == "Call")
		{
			branch[0].Info += " (function)";
			branch[0].Extra = extra;
			refExtra = extra2;
		}
		else
		{
			branch[0].Info += " (delegate)";
			branch[0].Extra = new UniversalType(FuncBlockStack, extraTypes);
			branch[0].Insert(0, (TreeBranch)new("data", branch.Pos, branch.EndPos, branch.Container) { Extra = extra2 });
		}
	}

	private String Expr(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		if (branch.Info == "Call")
			return ExprCall(branch, out errorsList);
		if (branch.Info == "ConstructorCall")
			return ExprConstructorCall(branch, out errorsList);
		var innerResults = new List<String>();
		int i;
		for (i = 0; i < branch.Length; i++)
		{
			if (branch[i].Info == "type")
				innerResults.SetOrAdd(i, "typeof(" + (branch[0].Extra is UniversalType type2 ? Type(type2) : "dynamic") + ")");
			else if (ExprTypesList.Contains(branch[i].Info))
			{
				innerResults.SetOrAdd(i, CalculationParseAction(branch[i].Info)(branch[i], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
			}
			else
			{
				if (TryReadValue(branch[i].Info, out var value))
				{
					branch[i].Extra = value.InnerType;
					innerResults.SetOrAdd(i, value.ToString(true, true));
				}
				else if (i == 1 && TryReadValue(branch[0].Info, out value))
					innerResults.SetOrAdd(i, ExprValue(value, branch, errorsList, i--));
				else if (i > 0 && i % 2 == 0)
				{
					if (!TryReadValue(branch[i].Info, out _) && branch[i].Info.ToString() is not ("pow" or "tetra" or "penta"
						or "hexa" or "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "pow=" or "tetra=" or "penta=" or "hexa="
						or "&=" or "|=" or "^=" or ">>=" or "<<=") && TryReadValue(branch[Max(i - 3, 0)].Info, out var value1)
						&& TryReadValue(branch[i - 1].Info, out var value2))
					{
						var innerResult = ExprTwoValues(value1, value2, branch, errorsList, ref i);
						innerResults.SetOrAdd(i, innerResult);
					}
					else
						innerResults.SetOrAdd(i, branch[i].Info.ToString() switch
						{
							"*" or "/" or "%" => ExprMulDiv(branch, innerResults, errorsList, ref i),
							"+" or "-" => ExprPM(branch, innerResults, errorsList, ref i),
							"pow" or "tetra" or "penta" or "hexa" => ExprPow(branch, innerResults, i),
							"==" or ">" or "<" or ">=" or "<=" or "!=" or "&&" or "||" or "^^" => ExprBool(branch, innerResults, i),
							"=" or "+=" or "-=" or "*=" or "/=" or "%=" or "pow=" or "tetra=" or "penta=" or "hexa=" or "&=" or "|=" or "^=" or ">>=" or "<<=" => ExprAssignment(branch, innerResults, errorsList, i),
							"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" or ":" => ExprTernary(branch, i),
							"CombineWith" => ExprCombineWith(branch, innerResults, i),
							not nameof(List) => ExprBinaryNotList(branch, innerResults, i),
							_ => ExprDefault(branch, errorsList, i),
						});
				}
				else
					return branch.Length == 2 && i == 1 ? ExprUnary(branch, errorsList, i) : ExprDefault(branch, errorsList, i);
			}
		}
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		if (branch.Info == "StringConcatenation")
		{
			branch.Elements = branch.Elements.Filter(x => x.Info != "+");
			branch.Extra = GetPrimitiveType("string");
		}
		else if (branch.Info == nameof(List))
			branch.Extra = branch.Elements.Progression(GetListType(NullType), (x, y) => GetResultType(x, GetListType(y.Extra is UniversalType UnvType ? UnvType : NullType)));
		else if (branch.Info == "Indexes")
		{
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is UniversalType UnvType)
				branch.Extra = GetSubtype(UnvType, branch.Length);
			else
				branch.Extra = NullType;
		}
		else if (branch.Length == 1 && new List<String> { "Expr", "PMExpr", "MulDivExpr", "StringConcatenation" }.Contains(branch.Parent.Info))
		{
			branch.Parent[prevIndex] = branch[0];
			branch.Extra = branch[0].Extra is UniversalType UnvType ? UnvType : (object)NullType;
		}
		else
			branch.Extra = branch[^1].Extra is UniversalType UnvType ? UnvType : (object)NullType;
		//if (branch.Parent.Info == "Hypername" && branch.Extra is object obj)
		//	_ExtraStack[_Stackpos - 1] = new List<object> { "Expr", obj };
		return innerResults[i - 1];
	}

	private String ExprCall(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		for (var i = 0; i < branch.Length; i++)
		{
			if (!ExprCallCheck(branch, errorsList, i))
				return "";
			var s = CalculationParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
			if (s != "")
			{
				if (result != "")
					result.AddRange(", ");
				result.AddRange(s);
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private bool ExprCallCheck(TreeBranch branch, List<String> errorsList, int i)
	{
		var otherPos = branch[i].Pos;
		if (!(branch[i].Extra is UniversalType CallParameterUnvType && branch[0].Extra is List<object> list))
		{
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return false;
		}
		if (list.Length == 3)
		{
			if (list[1] is not String elem2)
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
				return false;
			}
			else if ((elem2 == "public" || elem2 == "method") && list[2] is (List<String>, String ReturnType, List<String> ReturnExtraTypes, FunctionAttributes, MethodParameters Parameters))
			{
				if (Parameters.Length < i - 1)
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
				else if (!TypesAreCompatible(CallParameterUnvType, PartialTypeToGeneralType(Parameters[i - 2].Type, Parameters[i - 2].ExtraTypes), out var warning))
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incompatibility between type of parameter of the call and type of parameter of the function");
				else if (warning)
					errorsList.Add("Warning in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": type of parameter of the call and type of parameter of the function are badly compatible, you may lost data");
				if (i >= branch.Length || Parameters.Length < i - 1)
				{
					branch.Extra = PartialTypeToGeneralType(ReturnType, ReturnExtraTypes);
					return true;
				}
			}
			else if (list[2] is (GeneralArrayParameters, UniversalType GeneralReturnUnvType, FunctionAttributes, GeneralMethodParameters GeneralParameters, TreeBranch))
			{
				if (GeneralParameters.Length < i - 1)
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
				else if (!TypesAreCompatible(CallParameterUnvType, (GeneralParameters[i - 2].Type, GeneralParameters[i - 2].ExtraTypes), out var warning))
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incompatibility between type of parameter of the call and type of parameter of the function");
				else if (warning)
					errorsList.Add("Warning in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": type of parameter of the call and type of parameter of the function are badly compatible, you may lost data");
				if (i >= branch.Length || GeneralParameters.Length < i - 1)
				{
					branch.Extra = GeneralReturnUnvType;
					return true;
				}
			}
			else
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
		}
		else if (list.Length == 2)
		{
			if (!(list[0] is String elem1 && (elem1 == "Variable" || elem1 == "Property") && list[1] is UniversalType UnvType && new BlockStackEComparer().Equals(UnvType.MainType, FuncBlockStack) && UnvType.ExtraTypes.Length != 0 && !UnvType.ExtraTypes[0].MainType.IsValue && UnvType.ExtraTypes[0] is UniversalTypeOrValue ReturnUnvType))
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
				return false;
			}
			else
			{
				if (UnvType.ExtraTypes.Length < i)
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
				else if (UnvType.ExtraTypes[i - 1].MainType.IsValue || !TypesAreCompatible(CallParameterUnvType, (UnvType.ExtraTypes[i - 1].MainType.Type, UnvType.ExtraTypes[i - 1].ExtraTypes), out var warning))
					errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incompatibility between type of parameter of the call and type of parameter of the function");
				else if (warning)
					errorsList.Add("Warning in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": type of parameter of the call and type of parameter of the function are badly compatible, you may lost data");
				if (i >= branch.Length || UnvType.ExtraTypes.Length < i)
				{
					branch.Extra = ReturnUnvType;
					return true;
				}
			}
		}
		return true;
	}

	private String ExprConstructorCall(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		for (var i = 0; i < branch.Length; i++)
		{
			if (!ExprConstructorCallCheck(branch, errorsList, i))
				return "";
			var s = CalculationParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
			if (s != "")
			{
				if (result != "")
					result.AddRange(", ");
				result.AddRange(s);
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private bool ExprConstructorCallCheck(TreeBranch branch, List<String> errorsList, int i)
	{
		ConstructorOverloads constructors = default!, shortConstructors = [], incompatibleConstructors = [], badlyCompatibleConstructors = [];
		var otherPos = branch[i].Pos;
		if (branch[i].Extra is UniversalType CallParameterUnvType && branch[0].Extra is List<object> list && list[1] is UniversalType ConstructingUnvType)
		{
			try
			{
				constructors = (ConstructorOverloads)list[3];
			}
			catch
			{
			}
		}
		else
		{
			CallParameterUnvType = NullType;
			ConstructingUnvType = NullType;
		}
		if (constructors == null || constructors.Length == 0)
		{
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			branch.Extra = NullType;
			return false;
		}
		for (var j = 0; j < constructors.Length; j++)
		{
			if (constructors[j].Parameters.Length < i)
			{
				shortConstructors.Add(constructors[j]);
				constructors.RemoveAt(j--);
			}
			else if (!TypesAreCompatible(CallParameterUnvType, (constructors[j].Parameters[i - 1].Type, constructors[j].Parameters[i - 1].ExtraTypes), out var warning))
			{
				incompatibleConstructors.Add(constructors[j]);
				constructors.RemoveAt(j--);
			}
			else if (warning)
				badlyCompatibleConstructors.Add(constructors[j]);
		}
		if (constructors.Length == 0)
		{
			if (incompatibleConstructors.Length == 0)
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
			else
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incompatibility between type of parameter of the call (" + TypeToString(CallParameterUnvType) + ") and type of parameter of the nearest overload of the constructor (" + TypeToString((incompatibleConstructors[^1].Parameters[i - 1].Type, incompatibleConstructors[^1].Parameters[i - 1].ExtraTypes)) + ")");
			branch.Extra = NullType;
			return false;
		}
		else if (badlyCompatibleConstructors.Length >= constructors.Length)
			errorsList.Add("Warning in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": type of parameter of the call (" + TypeToString(CallParameterUnvType) + ") and type of parameter of the nearest overload of the constructor (" + TypeToString((badlyCompatibleConstructors[^1].Parameters[i - 1].Type, badlyCompatibleConstructors[^1].Parameters[i - 1].ExtraTypes)) + ") are badly compatible, you may lost data");
		else if (i + 1 >= branch.Length)
		{
			for (var j = 0; j < constructors.Length; j++)
			{
				if (constructors[j].Parameters.Length >= i + 1 && (constructors[j].Parameters[i].Attributes & ParameterAttributes.Optional) == 0)
					constructors.RemoveAt(j--);
			}
			if (constructors.Length == 0)
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
				branch.Extra = NullType;
				return false;
			}
			branch.Extra = ConstructingUnvType;
			return false;
		}
		return true;
	}

	private String ExprValue(Universal value, TreeBranch branch, List<String> errorsList, int i)
	{
		var otherPos = branch[i].Pos;
		Universal result;
		switch (branch[i].Info.ToString())
		{
			case "+" when !branch[0].Info.EndsWith('r'):
			result = +value;
			return result.ToString(true, true);
			case "-" when !branch[0].Info.EndsWith('r'):
			result = -value;
			return result.ToString(true, true);
			case "!":
			result = !value;
			return result.ToString(true, true);
			case "~":
			result = ~value;
			return result.ToString(true, true);
			case "sin":
			try
			{
				result = Sin(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "cos":
			try
			{
				result = Cos(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "tan":
			try
			{
				result = Tan(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "asin":
			try
			{
				result = Asin(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "acos":
			try
			{
				result = Acos(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "atan":
			try
			{
				result = Atan(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "ln":
			try
			{
				result = Log(value.ToReal());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
				return "default!";
			}
			case "postfix !":
			try
			{
				result = Factorial(value.ToUnsignedInt());
				return result.ToString(true, true);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute factorial of this constant");
				return "default!";
			}
		}
		return "default!";
	}

	private String ExprTwoValues(Universal value1, Universal value2, TreeBranch branch, List<String> errorsList, ref int i)
	{
		var otherPos = branch[i].Pos;
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		switch (branch[i].Info.ToString())
		{
			case "?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=":
			var s = branch[i].Info;
			if ((s == "?" ? value1 : s == "?=" ? Universal.Eq(value1, value2) : s == "?>" ? Universal.Gt(value1, value2) : s == "?<" ? Universal.Lt(value1, value2) : s == "?>=" ? Universal.Goe(value1, value2) : s == "?<=" ? Universal.Loe(value1, value2) : Universal.Neq(value1, value2)).ToBool())
			{
				branch[Max(i - 3, 0)] = new((s == "?" ? value2 : value1).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
				branch.RemoveEnd(i - 1);
			}
			else if (i + 2 >= branch.Length)
			{
				branch[Max(i - 3, 0)] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				branch.RemoveEnd(i - 1);
			}
			else
			{
				branch[Max(i - 3, 0)] = branch[i + 1];
				branch.Remove(i - 1, 4);
			}
			i--;
			return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			case ":":
			if (i + 2 >= branch.Length)
			{
				var i2 = i;
				branch[i].Extra = branch.Elements.Filter((_, index) => index == i2 - 1 || index % 4 == 1).Convert(x => x.Extra is UniversalType ElemType ? ElemType : NullType).Progression(GetResultType);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			else
			{
				branch[i].Extra = UnvType2;
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			case "pow":
			try
			{
				branch[Max(i - 3, 0)] = new((value1.IsNull || value2.IsNull ? Universal.Null : Pow(value2.ToReal(), value1.ToReal())).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			}
			catch
			{
				errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this expression");
				branch[Max(i - 3, 0)] = new("null", branch.Pos, branch.EndPos, branch.Container);
			}
			break;
			case "*":
			if (TypeEqualsToPrimitive(UnvType1, "string") && TypeEqualsToPrimitive(UnvType2, "string"))
				errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the string cannot be multiplied by string; one of them can be converted to number but this is not recommended and can cause data loss");
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 * value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && branch[i - 2].Info == "*")
				branch[Max(i - 3, 0)] = new((value1 * value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "/":
			if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
				errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss");
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 / value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && branch[i - 2].Info == "*")
				branch[Max(i - 3, 0)] = new((value1 / value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "%":
			if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
				errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss");
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 % value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && branch[i - 2].Info == "*")
				branch[Max(i - 3, 0)] = new((value1 % value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "+":
			if (i == 2 && TypeEqualsToPrimitive(UnvType1, "string") == false && TypeEqualsToPrimitive(UnvType2, "string") == false)
				branch[Max(i - 3, 0)] = new((value1 + value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "-":
			if (TypeEqualsToPrimitive(UnvType1, "string") || TypeEqualsToPrimitive(UnvType2, "string"))
				errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss");
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 - value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else if (i >= 4 && TypeEqualsToPrimitive(PrevUnvType, "string") && branch[i - 2].Info == "+")
				branch[Max(i - 3, 0)] = new((value1 - value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case ">>":
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 >> value2.ToInt()).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "<<":
			if (i == 2)
				branch[Max(i - 3, 0)] = new((value1 << value2.ToInt()).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			else
			{
				branch[i].Extra = GetResultType(UnvType1, UnvType2);
				i++;
				return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
			}
			break;
			case "&":
			branch[Max(i - 3, 0)] = new((value1 & value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "|":
			branch[Max(i - 3, 0)] = new((value1 | value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "^":
			branch[Max(i - 3, 0)] = new((value1 ^ value2).ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "==":
			var result = Universal.Eq(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case ">":
			result = Universal.Gt(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "<":
			result = Universal.Lt(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case ">=":
			result = Universal.Goe(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "<=":
			result = Universal.Loe(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			case "!=":
			result = Universal.Neq(value1, value2);
			branch[Max(i - 3, 0)] = new(result.ToString(true, true), branch.Pos, branch.EndPos, branch.Container);
			break;
			default:
			branch[i].Extra = GetResultType(UnvType1, UnvType2);
			i++;
			return new String(branch[i - 2].Info).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(branch[i - 1].Info);
		}
		branch[Max(i - 3, 0)].Extra = GetResultType(UnvType1, UnvType2);
		branch.Remove(i - 1, 2);
		i -= 2;
		return branch[i].Info;
	}

	private String ExprMulDiv(TreeBranch branch, List<String> innerResults, List<String> errorsList, ref int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		var isString1 = TypeEqualsToPrimitive(UnvType1, "string");
		var isString2 = TypeEqualsToPrimitive(UnvType2, "string");
		if (branch[i].Info == "*" && isString1 && isString2)
			errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the string cannot be multiplied by string; one of them can be converted to number but this is not recommended and can cause data loss");
		else if (branch[i].Info != "*" && (isString1 || isString2))
			errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss");
		var resultType = (branch[i].Info == "/" && TypeIsPrimitive(UnvType1.MainType) && TypeIsPrimitive(UnvType2.MainType)) ? GetPrimitiveType(GetQuotientType(UnvType1.MainType.Peek().Name, TryReadValue(branch[i - 1].Info, out var value) ? value : 5, UnvType2.MainType.Peek().Name)) : (branch[i].Info == "%" && TypeIsPrimitive(UnvType1.MainType) && TypeIsPrimitive(UnvType2.MainType)) ? GetPrimitiveType(GetRemainderType(UnvType1.MainType.Peek().Name, TryReadValue(branch[i - 1].Info, out var value2) ? value2 : new(12345678901234567890, UnsignedLongIntType), UnvType2.MainType.Peek().Name)) : GetResultType(UnvType1, UnvType2);
		if (TypeEqualsToPrimitive(PrevUnvType, "string") && isString2 == false)
		{
			if (branch[Max(i - 3, 0)].Info == "MulDivExpr")
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new("MulDivExpr", [tempBranch, branch[i - 1], branch[i]], branch[i].Container) { Extra = resultType };
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		branch[i].Extra = resultType;
		return i < 2 ? branch[i].Info : new String(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private String ExprPM(TreeBranch branch, List<String> innerResults, List<String> errorsList, ref int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!(i >= 4 && branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		bool isString1 = TypeEqualsToPrimitive(UnvType1, "string"), isString2 = TypeEqualsToPrimitive(UnvType2, "string"), isStringPrev = TypeEqualsToPrimitive(PrevUnvType, "string");
		if (branch[i].Info == "-" && (isString1 || isString2))
			errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss");
		if (isStringPrev && isString2 == false)
		{
			if (branch[Max(i - 3, 0)].Info == "PMExpr")
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new("PMExpr", [tempBranch, branch[i - 1], branch[i]], branch[i].Container) { Extra = GetResultType(UnvType1, UnvType2) };
			}
			branch[Max(i - 3, 0)][^1].Extra = GetResultType(UnvType1, UnvType2);
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (branch[i].Info == "-" && (isString1 || isString2))
		{
			if (branch[Max(i - 3, 0)].Info == "PMExpr")
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new("PMExpr", [tempBranch, branch[i - 1], branch[i]], branch[i].Container) { Extra = GetResultType(UnvType1, UnvType2) };
			}
			branch[Max(i - 3, 0)][^1].Extra = GetResultType(UnvType1, UnvType2);
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (i >= 4 && isString1 == false && isString2)
		{
			TreeBranch tempBranch = new("PMExpr", branch.GetRange(0, i - 1), branch[i - 2].Container);
			branch[0] = tempBranch;
			branch.Remove(1, i - 2);
			i = 2;
		}
		else if (branch.Info == "Expr" && isString1 && isString2)
			branch.Info = "StringConcatenation";
		branch[i].Extra = GetResultType(UnvType1, UnvType2);
		return i < 2 ? branch[i][^1].Info : new String(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private static String ExprPow(TreeBranch branch, List<String> innerResults, int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		branch[i].Extra = GetResultType(UnvType2, UnvType1);
		return i < 2 ? branch[i].Info : ((String)"Pow(").AddRange(innerResults[^1]).AddRange(", ").AddRange(innerResults[^2]).Add(')');
	}

	private String ExprAssignment(TreeBranch branch, List<String> innerResults, List<String> errorsList, int i)
	{
		if (branch[i].Info == "=" && TryReadValue(branch[Max(0, i - 3)].Info, out _) && branch.Parent != null && (branch.Parent.Info == "if" || branch.Parent.Info == "XorList" || branch.Parent.Info == "Expr" && new List<String> { "xor", "or", "and", "^^", "||", "&&", "!" }.Contains(branch.Parent[Min(Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(x, branch)) + 1, 2), branch.Parent.Length - 1)].Info)))
			errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": this expression, used with conditional constructions, is constant; maybe you wanted to check equality of these values? - it is done with the operator \"==\"");
		else if (branch[i].Info == "=" && branch[i - 1].Info == "Hypername" && branch[Max(0, i - 3)] == branch[i - 1])
			errorsList.Add("Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the variable is assigned to itself - are you sure this is not a mistake?");
		branch.Info = "Assignment";
		if (branch[i - 2].Extra is not UniversalType SrcUnvType)
			SrcUnvType = NullType;
		if (branch[i - 1].Extra is not UniversalType DestUnvType)
			DestUnvType = NullType;
		if (!TypesAreCompatible(SrcUnvType, DestUnvType, out var warning))
		{
			var otherPos = branch[i].Pos;
			errorsList.Add("Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot convert from type \"" + TypeToString(SrcUnvType) + "\" to type \"" + TypeToString(DestUnvType) + "\"");
			branch.Info = "default!";
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return "default!";
		}
		else if (warning)
		{
			var otherPos = branch[i].Pos;
			errorsList.Add("Warning in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": conversion from type \"" + TypeToString(SrcUnvType) + "\" to type \"" + TypeToString(DestUnvType) + "\" is possible but not recommended, you may lost data");
		}
		branch[i].Extra = DestUnvType;
		return i < 2 ? branch[i].Info : new String(innerResults[^1]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^2]);
	}

	private static String ExprTernary(TreeBranch branch, int i)
	{
		branch.Info = "Ternary";
		if (branch[i].Info == ":" && i + 2 >= branch.Length)
			branch[i].Extra = branch.Elements.Filter((_, index) => index == i - 1 || index % 4 == 1).Convert(x => x.Extra is UniversalType ElemType ? ElemType : NullType).Progression(GetResultType);
		else
			branch[i].Extra = branch[i - 1].Extra is UniversalType UnvType ? UnvType : (object)NullType;
		return branch[i].Info;
	}

	private static String ExprCombineWith(TreeBranch branch, List<String> innerResults, int i)
	{
		if (branch[i - 1].Extra is not UniversalType UnvType)
			UnvType = NullType;
		branch[i].Extra = UnvType;
		return innerResults[^1];
	}

	private String ExprBool(TreeBranch branch, List<String> innerResults, int i)
	{
		branch[i].Extra = BoolType;
		return i < 2 ? branch[i].Info : ((String)nameof(Universal)).Add('.').AddRange(branch[i].Info.ToString() switch
		{
			"==" => nameof(Universal.Eq),
			">" => nameof(Universal.Gt),
			"<" => nameof(Universal.Lt),
			">=" => nameof(Universal.Goe),
			"<=" => nameof(Universal.Loe),
			"!=" => nameof(Universal.Neq),
			"&&" => nameof(Universal.And),
			"||" => nameof(Universal.Or),
			"^^" => nameof(Universal.Xor),
			_ => throw new InvalidOperationException(),
		}).Add('(').AddRange(innerResults[^2]).AddRange(", ").AddRange(innerResults[^1]).AddRange(")");
	}

	private static String ExprBinaryNotList(TreeBranch branch, List<String> innerResults, int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		branch[i].Extra = GetResultType(UnvType1, UnvType2);
		return i < 2 ? branch[i].Info : new String(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private String ExprUnary(TreeBranch branch, List<String> errorsList, int i)
	{
		if (branch[i].Info.ToString() is "++" or "--" or "postfix ++" or "postfix --" or "!!")
			branch.Info = "UnaryAssignment";
		if (branch[i - 1].Extra is not UniversalType UnvType)
			UnvType = NullType;
		branch[i].Extra = UnvType;
		var valueString = CalculationParseAction(branch[i - 1].Info)(branch[i - 1], out var innerErrorsList);
		if (valueString == "")
			return "default!";
		errorsList.AddRange(innerErrorsList);
		return branch[i].Info.ToString() switch
		{
			"+" => valueString.Insert(0, "+"),
			"-" => valueString.Insert(0, "-"),
			"!" => valueString.Insert(0, "!"),
			"~" => valueString.Insert(0, "~"),
			"sin" => valueString.Insert(0, "Sin(").Add(')'),
			"cos" => valueString.Insert(0, "Cos(").Add(')'),
			"tan" => valueString.Insert(0, "Tan(").Add(')'),
			"asin" => valueString.Insert(0, "Asin(").Add(')'),
			"acos" => valueString.Insert(0, "Acos(").Add(')'),
			"atan" => valueString.Insert(0, "Atan(").Add(')'),
			"ln" => valueString.Insert(0, "Log(").Add(')'),
			"postfix !" => valueString.Insert(0, "Factorial(").Add(')'),
			"++" => valueString.Insert(0, "++"),
			"--" => valueString.Insert(0, "--"),
			"postfix ++" => valueString.AddRange("++"),
			"postfix --" => valueString.AddRange("--"),
			"!!" => valueString.Copy().Insert(0, '(').AddRange(" = !(").AddRange(valueString).AddRange("))"),
			_ => "default!",
		};
	}

	private String ExprDefault(TreeBranch branch, List<String> errorsList, int i)
	{
		var result = CalculationParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
		errorsList.AddRange(innerErrorsList);
		return result;
	}

	private String List(TreeBranch branch, out List<String> errorsList)
	{
		String result = "(";
		errorsList = [];
		for (var i = 0; i < branch.Length; i++)
		{
			if (i > 0)
				result.AddRange(", ");
			if (TryReadValue(branch[i].Info, out var value))
			{
				branch[i].Extra = value.InnerType;
				result.AddRange(value.ToString(true, true));
			}
			else
			{
				result.AddRange(CalculationParseAction(branch[i].Info)(branch[i], out var innerErrorsList));
				errorsList.AddRange(innerErrorsList);
			}
		}
		branch.Extra = branch.Elements.Progression(GetListType(BoolType), (x, y) => GetResultType(x, GetListType(y.Extra is UniversalType UnvType ? UnvType : NullType)));
		return result.Add(')');
	}

	private String Return(TreeBranch branch, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		result.AddRange("return ");
		result.AddRange(Expr(branch[0], out var innerErrorsList));
		result.Add(';');
		errorsList.AddRange(innerErrorsList);
		return result;
	}

	private String Default(TreeBranch branch, out List<String> errorsList)
	{
		errorsList = [];
		if (Universal.TryParse(branch.Info.ToString(), out var value))
			return value.ToString(true, true);
		if (branch.Length == 0)
			return branch.Info;
		String result = "";
		foreach (var x in branch.Elements)
		{
			var s = CalculationParseAction(x.Info)(x, out var innerErrorsList);
			if (s != "")
			{
				result.AddRange(s);
				errorsList.AddRange(innerErrorsList);
			}
		}
		return result;
	}

	private static String Type(UniversalType type)
	{
		String result = "";
		if (TypeEqualsToPrimitive(type, "list", false))
		{
			var levelsCount = type.ExtraTypes.Length == 1 ? 1 : int.TryParse(type.ExtraTypes[0].ToString(), out var n) ? n : 0;
			result.AddRange(((String)"List<").Repeat(levelsCount));
			result.AddRange(Type(new(type.ExtraTypes[^1].MainType.Type, type.ExtraTypes[^1].ExtraTypes)));
			result.AddRange(((String)">").Repeat(levelsCount));
		}
		else if (TypeIsPrimitive(type.MainType))
		{
			return type.MainType.Peek().Name.ToString() switch
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
				_ => type.MainType.Peek().Name,
			};
		}
		else if (new BlockStackEComparer().Equals(type.MainType, FuncBlockStack))
		{
			var noReturn = TypeEqualsToPrimitive((type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes), "null");
			result.AddRange(noReturn ? "Action<" : "Func<");
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].MainType.IsValue ? type.ExtraTypes[i].MainType.Value : Type(new(type.ExtraTypes[i].MainType.Type, type.ExtraTypes[i].ExtraTypes)));
				if (!(noReturn && i == type.ExtraTypes.Length - 1))
					result.AddRange(", ");
			}
			if (!noReturn)
				result.AddRange(Type(new(type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes)));
			result.Add('>');
		}
		else
		{
			result.AddRange(type.MainType.ToShortString()).Add('<');
			for (var i = 0; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].MainType.IsValue ? type.ExtraTypes[i].MainType.Value : Type(new(type.ExtraTypes[i].MainType.Type, type.ExtraTypes[i].ExtraTypes)));
				if (i != type.ExtraTypes.Length - 1)
					result.AddRange(", ");
			}
			result.Add('>');
		}
		return result;
	}

	private static String Parameters(GeneralMethodParameters parameters, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		for (var i = 0; i < parameters.Length; i++)
		{
			result.AddRange(Type((parameters[i].Type, parameters[i].ExtraTypes))).Add(' ').AddRange(parameters[i].Name);
			if (i != parameters.Length - 1)
				result.AddRange(", ");
		}
		return result;
	}

	private static String ParametersCreate(GeneralMethodParameters parameters, out List<String> errorsList)
	{
		String result = "";
		errorsList = [];
		return result;
	}

	private bool VariableExists(TreeBranch branch, String s, ref List<String>? errorsList)
	{
		NList<int> indexes = [];
		List<TreeBranch> branches = [branch];
		while (branch.Parent != null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Info == "Function")
				break;
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if ((branches[i][j].Info == "Declaration" || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					var otherPos = branches[i][j][0].Pos;
					Add(ref errorsList, "Error in line " + lexems[branch.Pos].LineN.ToString() + " at position " + lexems[branch.Pos].Pos.ToString() + ": variable \"" + s + "\" is already defined in this location or in the location that contains this: line " + lexems[otherPos].LineN.ToString() + ", position " + lexems[otherPos].Pos.ToString());
					return true;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", "return", "Expr", nameof(List), "Indexes", "Call", "Ternary", "PMExpr", "MulDivExpr", "StringConcatenation", "Assignment", "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out var otherPos, out _) && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": variable \"" + s + "\" is already defined in this location or in the location that contains this in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
					return true;
				}
			}
		}
		return false;
	}

	private bool IsVariableDeclared(TreeBranch branch, String s, out List<String>? errorsList, out object? extra)
	{
		errorsList = default!;
		NList<int> indexes = [];
		List<TreeBranch> branches = [branch];
		while (branch.Parent != null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedFunctionExists(branches[i].Container, branches[i][0].Info, out var function) && (function?.Attributes & FunctionAttributes.Multiconst) != 0)
			{
				Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": variable \"" + s + "\" is not defined in this location; multiconst functions cannot use variables that are outside of the function");
				extra = null;
				return false;
			}
			for (var j = 0; j < indexes[i] - 1; j++)
			{
				if ((branches[i][j].Info == "Declaration" || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s)
				{
					extra = branches[i][j][0].Extra;
					return true;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", "Expr", nameof(List), "Indexes", "Call", "Ternary", "PMExpr", "MulDivExpr", "StringConcatenation", "Assignment", "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out _, out var innerExtra))
				{
					extra = innerExtra;
					return true;
				}
			}
			for (var j = indexes[i]; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Info == "Declaration" || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s)
				{
					var otherPos = branches[i][j][0].Pos;
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": one cannot use the local variable \"" + s + "\" before it is declared or inside such declaration in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
					extra = null;
					return false;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", "return", "Expr", nameof(List), "Indexes", "Call", "Ternary", "PMExpr", "MulDivExpr", "StringConcatenation", "Assignment", "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out var otherPos, out _))
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": one cannot use the local variable \"" + s + "\" before it is declared or inside such declaration in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
					extra = null;
					return false;
				}
			}
		}
		if (errorsList == null || errorsList.Length == 0)
			Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
		extra = null;
		return false;
	}

	private static bool VariableExistsInsideExpr(TreeBranch branch, String s, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Info == "Declaration" || branch[i].Info == "Parameter") && branch[i][1].Info == s)
				{
					pos = branch[i][0].Pos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (new List<String> { "Expr", nameof(List), "Indexes", "Call", "Ternary", "PMExpr", "MulDivExpr", "StringConcatenation", "Assignment", "UnaryAssignment" }.Contains(branch[i].Info) && VariableExistsInsideExpr(branch[i], s, out pos, out extra))
					return true;
			}
		}
		catch (StackOverflowException)
		{
		}
		pos = -1;
		extra = null;
		return false;
	}

	private bool IsPropertyDeclared(TreeBranch branch, String s, out List<String>? errorsList, out (UniversalType UnvType, PropertyAttributes Attributes)? property, out object? extra)
	{
		errorsList = default!;
		if (UserDefinedPropertyExists(branch.Container, s, out property, out _) == false)
		{
			if (errorsList == null || errorsList.Length == 0)
				Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
			extra = null;
			return false;
		}
		NList<int> indexes = [];
		List<TreeBranch> branches = [branch];
		while (branch.Parent != null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedFunctionExists(branches[i].Container, branches[i][0].Info, out var function))
			{
				if ((function?.Attributes & FunctionAttributes.Multiconst) != 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": property \"" + s + "\" is not defined in this location; multiconst functions cannot use external properties");
					extra = null;
					return false;
				}
				else if ((function?.Attributes & FunctionAttributes.Static) != 0 && (property?.Attributes & PropertyAttributes.Static) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": property \"" + s + "\" cannot be used from this location; static functions cannot use non-static properties");
					extra = null;
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Info == "Property" && branches[i][j].Length == 3 && branches[i][j][1].Info == s)
				{
					extra = branches[i][j];
					return true;
				}
				else if (new List<String> { "ClassMain", "Properties" }.Contains(branches[i][j].Info) && PropertyExistsInsideExpr(branches[i][j], s, out _, out var innerExtra))
				{
					extra = innerExtra;
					return true;
				}
			}
		}
		if (errorsList == null || errorsList.Length == 0)
			Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
		extra = null;
		return false;
	}

	private static bool PropertyExistsInsideExpr(TreeBranch branch, String s, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Info == "Property" && branch[i].Length == 3 && branch[i][1].Info == s)
				{
					pos = branch[i][0].Pos;
					extra = branch[i][0].Extra;
					return true;
				}
			}
		}
		catch (StackOverflowException)
		{
		}
		pos = -1;
		extra = null;
		return false;
	}

	private bool IsFunctionDeclared(TreeBranch branch, String s, out List<String>? errorsList, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out BlockStack matchingContainer, out object? extra)
	{
		errorsList = default!;
		if (UserDefinedFunctionExists(branch.Container, s, out function, out matchingContainer) == false)
		{
			if (errorsList == null || errorsList.Length == 0)
				Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
			extra = null;
			return false;
		}
		NList<int> indexes = [];
		List<TreeBranch> branches = [branch];
		while (branch.Parent != null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedFunctionExists(branches[i].Container, branches[i][0].Info, out var function2))
			{
				if ((function2?.Attributes & FunctionAttributes.Multiconst) != 0 && (function?.Attributes & FunctionAttributes.Multiconst) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": function \"" + s + "\" is not defined in this location; multiconst functions cannot call external non-multiconst functions");
					extra = null;
					return false;
				}
				else if ((function2?.Attributes & FunctionAttributes.Static) != 0 && (function?.Attributes & FunctionAttributes.Static) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": function \"" + s + "\" cannot be called from this location; static functions cannot call non-static functions");
					extra = null;
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Info == "Function" && branches[i][j].Length == 4 && branches[i][j][0].Info == s)
				{
					extra = branches[i][j];
					return true;
				}
			}
		}
		if (errorsList == null || errorsList.Length == 0)
			Add(ref errorsList, "Error in line " + lexems[branch[0].Pos].LineN.ToString() + " at position " + lexems[branch[0].Pos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
		extra = null;
		return false;
	}

	private static String GetActualFunction(TreeBranch branch, out (GeneralArrayParameters ArrayParameters, UniversalType ReturnUnvType, FunctionAttributes Attributes, GeneralMethodParameters Parameters, TreeBranch? Location)? function, out BlockStack? matchingContainer)
	{
		NList<int> indexes = [];
		List<TreeBranch> branches = [branch];
		while (branch.Parent != null)
		{
			indexes.Add(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) + 1);
			branches.Add(branch = branch.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedFunctionExists(branch.Container, branches[i][0].Info, out function, out matchingContainer))
				return branches[i][0].Info;
		function = ([], (GetPrimitiveBlockStack("universal"), NoGeneralExtraTypes), FunctionAttributes.None, [], null);
		matchingContainer = null;
		return "";
	}

	private static bool TryReadValue(String s, out Universal value) => Universal.TryParse(s.ToString(), out value) || s.StartsWith("(String)") && Universal.TryParse(s["(String)".Length..].ToString(), out value);

	public override string ToString() => $"({String.Join(", ", lexems.ToArray(x => (String)x.ToString())).TakeIntoVerbatimQuotes()}, {input.TakeIntoVerbatimQuotes()}, {((String)topBranch.ToString()).TakeIntoVerbatimQuotes()}, ({(errorsList != null && errorsList.Length != 0 ? String.Join(", ", errorsList.ToArray(x => x.TakeIntoVerbatimQuotes())) : "NoErrors")}), {wreckOccurred})";
}

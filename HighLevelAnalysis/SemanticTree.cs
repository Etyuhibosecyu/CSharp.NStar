﻿global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Diagnostics;
global using G = System.Collections.Generic;
global using static CSharp.NStar.DeclaredConstructions;
global using static CSharp.NStar.TypeHelpers;
global using static System.Math;
global using String = NStar.Core.String;
using NStar.EasyEvalLib;
using NStar.MathLib.Extras;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Reflection;
using static CSharp.NStar.DeclaredConstructionChecks;
using static CSharp.NStar.DeclaredConstructionMappings;
using static CSharp.NStar.IntermediateFunctions;
using static NStar.Core.Extents;

namespace CSharp.NStar;

public sealed class SemanticTree
{
	private readonly List<Lexem> lexems;
	private readonly String input, compiledClasses = [];
	private bool wreckOccurred;
	private GeneralMethodOverload? currentFunction;
	private readonly TreeBranch topBranch = TreeBranch.DoNotAdd();
	private readonly List<String>? errorsList = null;

	private static readonly string AlphanumericCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.";
	private static readonly List<String> TypesListExpr = [nameof(Expr), nameof(List), "Indexes", nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), nameof(XorList), "StringConcatenation", nameof(Assignment), "UnaryAssignment", nameof(Declaration), nameof(Hypername)];
	private static readonly List<String> CycleTypesList = ["loop", "while", "while!", "repeat", "for", "loop_while", "for_while", "repeat_while"];

	public SemanticTree(List<Lexem> lexems, String input, TreeBranch topBranch, List<String>? errorsList, bool wreckOccurred)
	{
		this.lexems = lexems;
		this.input = input;
		this.topBranch = topBranch;
		this.errorsList = errorsList;
		this.wreckOccurred = wreckOccurred;
		this.errorsList = errorsList;
	}

	public SemanticTree((List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) x) : this(x.Lexems, x.String, x.TopBranch, x.ErrorsList, x.WreckOccurred)
	{
	}

	public SemanticTree(LexemStream lexemStream) : this(lexemStream.Parse())
	{
	}

	public static String ExecuteStringPrefix { get; } = "list() dynamic args = null;";

	public static String ExecuteStringPrefixCompiled { get; } = new SemanticTree((LexemStream)new CodeSample(ExecuteStringPrefix)).Parse(out _, out _);

	public String Parse(out List<String>? errorsList, out String compiledClasses)
	{
		List<String>? innerErrorsList = [];
		try
		{
			var result = ParseAction(topBranch.Info)(topBranch, out innerErrorsList);
			errorsList = [];
			if (this.errorsList != null && innerErrorsList != null)
				errorsList = this.errorsList.AddRange(innerErrorsList);
			compiledClasses = this.compiledClasses;
			return wreckOccurred ? [] : result;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			Add(ref innerErrorsList, "Wreck in unknown line at unknown position: translation failed because of internal error");
			errorsList = innerErrorsList;
			compiledClasses = [];
			wreckOccurred = true;
			return [];
		}
	}

	private delegate String ParseActionDelegate(TreeBranch branch, out List<String>? errorsList);

	private ParseActionDelegate ParseAction(String info) => wreckOccurred ? Wreck : info.ToString() switch
	{
		nameof(Main) => Main,
		nameof(Class) => Class,
		nameof(Function) => Function,
		nameof(Constructor) => Constructor,
		nameof(Properties) => Properties,
		"if" or "else if" or "if!" or "else if!" => Condition,
		"loop" => Loop,
		"while" => While,
		"repeat" => Repeat,
		"for" => For,
		nameof(Declaration) => Declaration,
		nameof(Hypername) => Hypername,
		nameof(Expr) or "Indexes" or nameof(Call) or nameof(ConstructorCall) or nameof(Ternary) or nameof(PMExpr) or nameof(MulDivExpr)
			or "StringConcatenation" or nameof(Assignment) or "UnaryAssignment" => Expr,
		nameof(List) => List,
		"xorList" => XorList,
		"return" => Return,
		_ => Default,
	};

	private String Main(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		for (var i = 0; i < branch.Length; i++)
		{
			var x = branch[i];
			if (i != 0 && branch[i - 1].Info == "return" && !(i >= 2 && branch[i - 2].Info.ToString()
				is "if" or "else if" or "if!" or "else if!"))
			{
				Add(ref errorsList, "Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
					+ lexems[branch[i].Pos].Pos.ToString() + ": unreachable code detected");
				break;
			}
			var s = ParseAction(x.Info)(x, out var innerErrorsList);
			if (x.Length == 0 || s.Length != 0)
			{
				if (branch.Info == "Main" && x.Info == "Main" && !s.EndsWith('}') && s.Length != 0 && s[..^1].Contains(';'))
					result.Add('{');
				if (s.ToString() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
					s = [];
				if (s.StartsWith('(') && TypesListExpr.Contains(x.Info) && x.Info != nameof(Assignment))
					s.Insert(0, "_ = ");
				result.AddRange(s);
				if (s.Length != 0 && s[^1] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
					result.Add(' ');
				if (s.Length == 0 || TypesListExpr.Contains(x.Info) && !s.EndsWith(';')
					|| x.Info.ToString() is "continue" or "break")
					result.Add(';');
				if (branch.Info == "Main" && x.Info == "Main" && !s.EndsWith('}') && s.Length != 0 && s[..^1].Contains(';'))
					result.Add('}');
			}
			if (innerErrorsList != null)
				AddRange(ref errorsList, innerErrorsList);
		}
		return result;
	}

	private String Class(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		var name = branch[0].Info;
		var (_, Attributes, BaseType, _) = UserDefinedTypesList[(branch.Container, name)];
		if ((Attributes & TypeAttributes.Closed) != 0)
			result.AddRange("private ");
		if ((Attributes & TypeAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & TypeAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			Add(ref errorsList, "Warning in line " + lexems[branch.Pos].LineN.ToString() + " at position "
				+ lexems[branch.Pos].Pos.ToString() + ": at present time the word \"internal\""
				+ " does nothing because C#.NStar does not have multiple assemblies");
		}
		if ((Attributes & (TypeAttributes.Closed | TypeAttributes.Protected | TypeAttributes.Internal)) == 0)
			result.AddRange("public ");
		if ((Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			result.AddRange("static ");
		else if ((Attributes & TypeAttributes.Abstract) != 0)
			result.AddRange("abstract ");
		else if ((Attributes & TypeAttributes.Sealed) != 0)
			result.AddRange("sealed ");
		result.AddRange("class ");
		if (EscapedKeywordsList.Contains(name))
			result.Add('@');
		result.AddRange(name);
		if ((Attributes & TypeAttributes.Static) != TypeAttributes.Static)
			result.AddRange(" : ").AddRange(TypeIsPrimitive(BaseType.MainType) ? "IClass" : Type(BaseType));
		result.Add('{');
		if ((Attributes & TypeAttributes.Static) != TypeAttributes.Static
			&& !(branch[^1].Info == "ClassMain" && branch[^1].Length != 0
			&& branch[^1].Elements.Any(x => x.Info == "Properties")))
		{
			var propertiesList = GetAllProperties(branch[^1].Container);
			String paramsResult = [], baseResult = [];
			foreach (var property in propertiesList)
				PropertiesConstructor(paramsResult, baseResult, property);
			result.AddRange("public ").AddRange(name).Add('(').AddRange(paramsResult);
			if (!TypeEqualsToPrimitive(BaseType, "null"))
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){}");
		}
		result.AddRange(ParseAction(branch[^1].Info)(branch[^1], out var coreErrorsList).Add('}'));
		AddRange(ref errorsList, coreErrorsList);
		if (IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private static void PropertiesConstructor(String paramsResult, String coreResult, G.KeyValuePair<String, UserDefinedProperty> property)
	{
		if (coreResult.Length != 0)
		{
			paramsResult.AddRange(", ");
			coreResult.AddRange(", ");
		}
		var typeName = Type(property.Value.UnvType);
		paramsResult.AddRange(typeName).Add(' ');
		if (EscapedKeywordsList.Contains(property.Key))
		{
			paramsResult.Add('@');
			coreResult.Add('@');
		}
		paramsResult.AddRange(property.Key).AddRange(" = default!");
		coreResult.AddRange(property.Key);
	}

	private String Function(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		var name = branch[0].Info;
		var t = UserDefinedFunctionsList[branch.Container][name][0];
		var (_, ReturnUnvType, Attributes, Parameters) = UserDefinedFunctionsList[branch.Container][name][0];
		if ((Attributes & FunctionAttributes.Closed) != 0)
			result.AddRange("private ");
		if ((Attributes & FunctionAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & FunctionAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			Add(ref errorsList, "Warning in line " + lexems[branch.Pos].LineN.ToString() + " at position "
				+ lexems[branch.Pos].Pos.ToString()
				+ ": at present time the word \"internal\" does nothing because C#.NStar does not have multiple assemblies");
		}
		if (IsTypeContext(branch) && (Attributes & (FunctionAttributes.Closed | FunctionAttributes.Protected | FunctionAttributes.Internal)) == 0)
			result.AddRange("public ");
		if ((Attributes & FunctionAttributes.Static) != 0)
			result.AddRange("static ");
		else if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
		{
			if (UserDefinedTypesList.TryGetValue(SplitType(branch.Container), out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Abstract) == 0)
			{
				Add(ref errorsList, "Error in line " + lexems[branch.Pos].LineN.ToString() + " at position "
					+ lexems[branch.Pos].Pos.ToString()
					+ ": abstract members can be located only inside the abstract classes");
				return [];
			}
			result.AddRange("abstract ");
		}
		else if (branch.Container.Length == 0 || branch.Container.Peek().BlockType
			is not (BlockType.Class or BlockType.Struct or BlockType.Interface)) { }
		else if (!(UserDefinedTypesList.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& !TypeEqualsToPrimitive(userDefinedType.BaseType, "null")
			&& UserDefinedFunctionExists(userDefinedType.BaseType.MainType, name,
			[.. Parameters.Convert(x => x.Type)], out var baseFunctions) && baseFunctions.Length != 0
			&& CreateVar(baseFunctions.Find(x => (Parameters, x.Parameters).Combine().All(y =>
			TypesAreEqual(y.Item1.Type, y.Item2.Type))), out var baseFunction) != default!))
			result.AddRange("virtual ");
		else if (TypesAreEqual(ReturnUnvType, baseFunction.ReturnUnvType)
			&& (Attributes & (FunctionAttributes.Static | FunctionAttributes.Closed | FunctionAttributes.Protected
			| FunctionAttributes.Internal | FunctionAttributes.Const | FunctionAttributes.Multiconst))
			== (baseFunction.Attributes & (FunctionAttributes.Static | FunctionAttributes.Closed
			| FunctionAttributes.Protected | FunctionAttributes.Internal | FunctionAttributes.Const
			| FunctionAttributes.Multiconst)) && (Parameters, baseFunction.Parameters).Combine().All(x =>
			(x.Item1.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out))
			== (x.Item2.Attributes & (ParameterAttributes.Ref | ParameterAttributes.Out)))
			&& (Attributes & FunctionAttributes.New) != FunctionAttributes.New
			&& (baseFunction.Attributes & FunctionAttributes.New) != FunctionAttributes.Sealed)
			result.AddRange("override ");
		else
		{
			if ((Attributes & FunctionAttributes.New) != FunctionAttributes.New)
				Add(ref errorsList, "Warning in line " + lexems[branch.Pos].LineN.ToString() + " at position "
					+ lexems[branch.Pos].Pos.ToString() + ": the method \"" + name
					+ "\" has the same parameter types as its base method with the same name but it also" +
					" has the other significant differences such as the access modifier or the return type," +
					" so it cannot override that base method and creates a new one;" +
					" if this is intentional, and the \"new\" keyword, otherwise fix the differences");
			result.AddRange("new virtual ");
		}
		result.AddRange(Type(ReturnUnvType)).Add(' ');
		if (EscapedKeywordsList.Contains(name))
			result.Add('@');
		result.AddRange(name).Add('(');
		result.AddRange(SemanticTree.Parameters(Parameters, out var parametersErrorsList)).Add(')');
		if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
			return result.Add(';');
		result.Add('{');
		AddRange(ref errorsList, parametersErrorsList);
		var currentFunction = this.currentFunction;
		this.currentFunction = t;
		if (branch.Length == 4)
		{
			result.AddRange(ParseAction(branch[3].Info)(branch[3], out var coreErrorsList));
			AddRange(ref errorsList, coreErrorsList);
		}
		result.Add('}');
		this.currentFunction = currentFunction;
		return result;
	}

	private String Constructor(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		var parameterTypes = GetParameterTypes(branch[0]);
		if (parameterTypes.Length != 0 && UserDefinedTypesList.TryGetValue(SplitType(branch.Container),
			out var userDefinedType) && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			return [];
		var (Attributes, Parameters) = UserDefinedConstructorsList[branch.Container].FindLast(x => x.Parameters.Equals(parameterTypes));
		if ((Attributes & ConstructorAttributes.Closed) != 0)
			result.AddRange("private ");
		if ((Attributes & ConstructorAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & ConstructorAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			Add(ref errorsList, "Warning in line " + lexems[branch.Pos].LineN.ToString() + " at position "
				+ lexems[branch.Pos].Pos.ToString() + ": at present time the word \"internal\""
				+ " does nothing because C#.NStar does not have multiple assemblies");
		}
		if ((Attributes & (ConstructorAttributes.Closed | ConstructorAttributes.Protected | ConstructorAttributes.Internal)) == 0)
			result.AddRange("public ");
		if ((Attributes & ConstructorAttributes.Static) != 0)
			result.AddRange("static ");
		if ((Attributes & ConstructorAttributes.Abstract) != 0)
		{
			result.AddRange("abstract ");
			Add(ref errorsList, "Wreck in line " + lexems[branch.Pos].LineN.ToString() + " at position "
				+ lexems[branch.Pos].Pos.ToString() + ": at present time the word \"abstract\" is forbidden");
			wreckOccurred = true;
		}
		var name = branch.Container.Peek().Name;
		if (EscapedKeywordsList.Contains(name))
			result.Add('@');
		result.AddRange(name).Add('(');
		result.AddRange(SemanticTree.Parameters(parameterTypes, out var parametersErrorsList));
		AddRange(ref errorsList, parametersErrorsList);
		var currentFunction = this.currentFunction;
		this.currentFunction = new([], NullType, FunctionAttributes.None, parameterTypes);
		result.AddRange("){");
		if (branch[^1].Info == "Main")
			result.AddRange(ParseAction(branch[^1].Info)(branch[^1], out var coreErrorsList));
		result.Add('}');
		this.currentFunction = currentFunction;
		return result;
	}

	private GeneralMethodParameters GetParameterTypes(TreeBranch branch) => [.. branch.Elements.Convert(GetParameterData)];

	private GeneralMethodParameter GetParameterData(TreeBranch branch)
	{
		if (!(branch.Length == 3 && branch[0].Info == "type" && branch[0].Extra is UniversalType ParameterUnvType && (branch[2].Info == "no optional" || TypesListExpr.Contains(branch[2].Info)) && branch.Extra is ParameterAttributes Attributes))
			throw new InvalidOperationException();
		return new(ParameterUnvType, branch[1].Info, Attributes, ParseAction(branch[2].Info)(branch[2], out _));
	}

	private static String Parameters(GeneralMethodParameters parameters, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		for (var i = 0; i < parameters.Length; i++)
		{
			result.AddRange((ReadOnlySpan<char>)((parameters[i].Attributes & ParameterAttributes.Params) switch
			{
				ParameterAttributes.None => [],
				ParameterAttributes.Ref => "ref ",
				ParameterAttributes.Out => "out ",
				ParameterAttributes.Params => "params List<",
				_ => [],
			}));
			result.AddRange(Type(parameters[i].Type));
			if ((parameters[i].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params)
				result.Add('>');
			result.Add(' ');
			var name = parameters[i].Name;
			if (EscapedKeywordsList.Contains(name))
				result.Add('@');
			result.AddRange(name);
			if (parameters[i].DefaultValue.ToString() is not ("" or "no optional"))
				result.AddRange(" = ").AddRange(parameters[i].DefaultValue == "null"
					? "default!" : parameters[i].DefaultValue == double.PositiveInfinity.ToString()
					? "double.PositiveInfinity" : parameters[i].DefaultValue == double.NegativeInfinity.ToString()
					? "double.NegativeInfinity" : parameters[i].DefaultValue == double.NaN.ToString()
					? "double.NaN" : parameters[i].DefaultValue);
			if (i != parameters.Length - 1)
				result.AddRange(", ");
		}
		return result;
	}

	private String Properties(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		if (Universal.TryParse(branch.Info.ToString(), out var value))
			return value.ToString(true, true);
		if (branch.Length == 0)
			return branch.Info;
		String result = [], paramsResult = [], baseResult = [], coreResult = [];
		if (UserDefinedTypesList.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& CreateVar(GetAllProperties(userDefinedType.BaseType.MainType), out var propertiesList).Length != 0)
			foreach (var property in propertiesList)
				PropertiesConstructor(paramsResult, baseResult, property);
		foreach (var x in branch.Elements)
		{
			var s = Property(x, out var innerErrorsList, out var innerResult2, out var innerResult3);
			if (s.Length != 0)
			{
				result.AddRange(s);
				AddRange(ref errorsList, innerErrorsList);
			}
			if (innerResult2.Length != 0)
			{
				if (paramsResult.Length != 0)
					paramsResult.AddRange(", ");
				paramsResult.AddRange(innerResult2);
			}
			coreResult.AddRange(innerResult3);
		}
		if ((userDefinedType.Attributes & TypeAttributes.Static) != TypeAttributes.Static
			&& !(UserDefinedConstructorsList.TryGetValue(branch.Container, out var constructors)
			&& (propertiesList = GetAllProperties(branch.Container)).Length != 0
			&& constructors.FindAll(x => x.Parameters.Length != 0 && (x.Parameters, propertiesList).Combine().All(x =>
			TypesAreEqual(x.Item1.Type, x.Item2.Value.UnvType))).Length > 1))
		{
			result.AddRange("public ").AddRange(branch.Container.Peek().Name).Add('(').AddRange(paramsResult);
			if (baseResult.Length != 0)
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){").AddRange(coreResult).Add('}');
		}
		return result;
	}

	private String Property(TreeBranch branch, out List<String>? errorsList, out String result2, out String result3)
	{
		errorsList = [];
		result2 = [];
		result3 = [];
		if (branch[0].Extra is not UniversalType UnvType)
			return [];
		var name = branch[1].Info;
		var (UnvType2, Attributes, DefaultValue) = UserDefinedPropertiesList[branch.Container][name];
		if (!UnvType.Equals(UnvType2))
			return [];
		String result = [];
		if ((Attributes & PropertyAttributes.Closed) != 0)
			result.AddRange("private ");
		if ((Attributes & PropertyAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & PropertyAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			Add(ref errorsList, "Warning in line " + lexems[branch.Pos].LineN.ToString() + " at position "
				+ lexems[branch.Pos].Pos.ToString() + ": at present time the word \"internal\""
				+ " does nothing because C#.NStar does not have multiple assemblies");
		}
		if (IsTypeContext(branch) && (Attributes & (PropertyAttributes.Closed | PropertyAttributes.Protected | PropertyAttributes.Internal)) == 0)
			result.AddRange("public ");
		if ((Attributes & PropertyAttributes.Static) != 0)
			result.AddRange("static ");
		var typeName = Type(UnvType);
		result.AddRange(typeName).Add(' ');
		result2.AddRange(typeName).Add(' ');
		if (EscapedKeywordsList.Contains(name))
		{
			result.Add('@');
			result2.Add('@');
		}
		result.AddRange(name).AddRange(" { get; set; } = ");
		result2.AddRange(name).AddRange(" = default!");
		var expr = ParseAction(branch[^1].Info)(branch[^1], out var innerErrorsList);
		result.AddRange(expr);
		result3.AddRange("if (").AddRange(name).AddRange(" is default(");
		result3.AddRange(typeName).AddRange("))this.").AddRange(name).AddRange(" = ").AddRange(expr);
		result3.AddRange(";else this.").AddRange(name).AddRange(" = ").AddRange(name).Add(';');
		AddRange(ref errorsList, innerErrorsList);
		result.Add(';');
		return result;
	}

	private String Condition(TreeBranch branch, out List<String>? errorsList)
	{
		String result = branch.Info.ToString() switch
		{
			"if" => "if (",
			"else if" => "else if (",
			"if!" => "if (!(",
			"else if!" => "else if (!(",
			_ => throw new InvalidOperationException(),
		};
		errorsList = [];
		var s = ParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s.Length != 0)
		{
			result.AddRange(s);
			AddRange(ref errorsList, innerErrorsList);
		}
		if (branch.Info.EndsWith('!'))
			result.Add(')');
		return result.Add(')');
	}

	private String Loop(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		return "while (true)";
	}

	private String While(TreeBranch branch, out List<String>? errorsList)
	{
		String result = "while (";
		errorsList = [];
		var s = ParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s.Length != 0)
		{
			result.AddRange(s);
			AddRange(ref errorsList, innerErrorsList);
		}
		return result.Add(')');
	}

	private String Repeat(TreeBranch branch, out List<String>? errorsList)
	{
		String result = "var ";
		var lengthName = RedStarLinq.NFill(32, _ => (char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
		result.AddRange(lengthName);
		result.AddRange(" = ");
		errorsList = [];
		var s = ParseAction(branch[0].Info)(branch[0], out var innerErrorsList);
		if (s.Length != 0)
		{
			result.AddRange(s);
			AddRange(ref errorsList, innerErrorsList);
		}
		var counterName = RedStarLinq.NFill(32, _ => (char)(globalRandom.Next(2) == 1 ? globalRandom.Next('A', 'Z' + 1) : globalRandom.Next('a', 'z' + 1)));
		result.AddRange(";for (var ").AddRange(counterName).AddRange(" = 0; ").AddRange(counterName).AddRange(" < ");
		result.AddRange(lengthName).AddRange("; ").AddRange(counterName).AddRange("++)");
		return result;
	}

	private String For(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		if (!(branch.Length == 2 && branch[0].Info == nameof(Declaration)))
			return [];
		var result = ((String)"foreach (").AddRange(Declaration(branch[0], out var innerErrorsList));
		AddRange(ref errorsList, innerErrorsList);
		result.AddRange(" in ").AddRange(ParseAction(branch[1].Info)(branch[1], out innerErrorsList)).Add(')');
		AddRange(ref errorsList, innerErrorsList);
		return result;
	}

	private String Declaration(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		if (!(branch.Length == 2 && branch[0].Info == "type"))
		{
			var otherPos = branch.FirstPos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return "_";
		}
		var s = branch[1].Info;
		if (VariableExists(branch, s, ref errorsList!))
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))] = new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
			{
				Extra = NullType
			};
			return "_";
		}
		if (branch[0].Extra is not UniversalType UnvType)
			branch.Extra = NullType;
		else if (TypeEqualsToPrimitive(UnvType, "var"))
		{
			var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is UniversalType AssigningUnvType && branch.Parent.Length >= 3 && branch.Parent[prevIndex + 1].Info == "=")
				branch.Extra = branch[1 - 1].Extra = AssigningUnvType;
			else
			{
				var otherPos = branch[1 - 1].Pos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": variable declared with the keyword \"var\" must be assigned explicitly and in the same expression");
				branch.Parent[prevIndex] = new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType };
				return "_";
			}
		}
		else if (UserDefinedTypesList.TryGetValue(SplitType(UnvType.MainType),
			out var userDefinedType) && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))] = new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
			{
				Extra = NullType
			};
			return "_";
		}
		else
			branch.Extra = UnvType;
		if (branch.Extra is UniversalType UnvType2 && TypesAreEqual(UnvType2, NullType))
			return "_";
		return Type(branch.Extra is UniversalType ResultType ? ResultType : NullType).Copy().Add(' ').AddRange(EscapedKeywordsList.Contains(s) ? ((String)"@").AddRange(s) : s);
	}

	private String Hypername(TreeBranch branch, out List<String>? errorsList) => Hypername(branch, out errorsList, null);

	private String Hypername(TreeBranch branch, out List<String>? errorsList, object? extra)
	{
		String result = [];
		errorsList = [];
		result.AddRange(Hypername1(branch, out var firstErrorsList, ref extra));
		AddRange(ref errorsList, firstErrorsList);
		for (var i = 1; i < branch.Length; i++)
		{
			if (i == 1 && branch[i].Info.ToString() is nameof(Call) or nameof(ConstructorCall))
				result.Replace(Hypername2(branch, ref errorsList, ref extra, ref i));
			else
			{
				var innerResult = Hypername2(branch, ref errorsList, ref extra, ref i);
				if (innerResult.ToString() is "default" or "default!")
					return "default!";
				if (innerResult.StartsWith('(') && innerResult.Length > 2)
				{
					innerResult.RemoveAt(0);
					result.Insert(0, '(');
				}
				result.AddRange(innerResult);
			}
		}
		return result;
	}

	private String Hypername1(TreeBranch branch, out List<String>? errorsList, ref object? extra)
	{
		String result = [];
		errorsList = [];
		var info = (branch.Length == 0 ? branch.Info : branch[0].Info).GetBefore(" (function)");
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		var innerErrorsLists = branch.Length <= 1 || branch[0].Info.EndsWith(" (delegate)")
			? [] : new List<String>?[branch[1].Length];
		var innerResults = branch.Length <= 1 || branch[0].Info.EndsWith(" (delegate)")
			? [] : branch[1].Elements.ToList((x, index) =>
			ParseAction(x.Info)(x, out innerErrorsLists[index]));
		var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList(x =>
			x.Extra is UniversalType UnvType ? UnvType : throw new InvalidOperationException());
		foreach (var innerErrorsList in innerErrorsLists)
			AddRange(ref errorsList, innerErrorsList);
		if (extra is null)
		{
			if (Universal.TryParse(branch[0].Info.ToString(), out var value))
			{
				branch[0].Extra = value.InnerType;
				extra = new List<object> { (String)"Constant", value.InnerType };
				return value.ToString(true, true);
			}
			if (TryReadValue(info, out value))
				result.AddRange(value.ToString(true, true));
			else if (branch[0].Length != 0)
			{
				result.AddRange(ParseAction(branch[0].Info)(branch[0], out var innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
				branch.Extra = branch[0].Extra;
			}
			else if (info == "type")
			{
				if (branch[0].Extra is not UniversalType UnvType)
					UnvType = NullType;
				extra = new List<object> { (String)"Static", UnvType };
				result.AddRange(Type(UnvType));
				branch.Extra = branch[0].Extra;
			}
			else if (info == "new type")
			{
				if (branch[0].Extra is not UniversalType UnvType)
					extra = new List<object> { (String)"Constructor", NullType };
				else if (UserDefinedConstructorsExist(UnvType, out var constructors) && constructors != null)
					extra = new List<object> { (String)"Constructor", UnvType, (String)"user", constructors };
				else if (ConstructorsExist(UnvType, out constructors) && constructors != null)
					extra = new List<object> { (String)"Constructor", UnvType, (String)"typical", constructors };
				else
					extra = new List<object> { (String)"Constructor", NullType };
				result.AddRange("new ").AddRange(branch[0].Extra is UniversalType type3 ? Type(type3) : "dynamic");
				return result;
			}
			else if (IsVariableDeclared(branch, info, out var variableErrorsList, out var extra2))
			{
				if (extra2 is UniversalType UnvType)
				{
					branch.Extra = branch[0].Extra = UnvType;
					extra = new List<object> { (String)"Variable", UnvType };
				}
				else
				{
					branch.Extra = branch[0].Extra = UnvType = NullType;
					extra = new List<object> { (String)"Variable", UnvType };
				}
				if (EscapedKeywordsList.Contains(info))
					result.Add('@');
				result.AddRange(TypesAreEqual(UnvType, NullType) ? "default(dynamic)" : info);
				AddRange(ref errorsList, variableErrorsList!);
			}
			else if (IsPropertyDeclared(branch, info, out var propertyErrorsList, out var property))
			{
				if (property.HasValue)
				{
					branch.Extra = branch[0].Extra = new UniversalType(property.Value.UnvType.MainType, property.Value.UnvType.ExtraTypes);
					extra = new List<object> { (String)"Property", new UniversalType(property!.Value.UnvType.MainType, property.Value.UnvType.ExtraTypes) };
				}
				else
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)"Property", NullType };
				}
				if (EscapedKeywordsList.Contains(info))
					result.Add('@');
				result.AddRange(info);
				AddRange(ref errorsList, propertyErrorsList!);
			}
			else if (IsFunctionDeclared(branch, info, out var functionErrorsList,
				out var functions, out var functionContainer, out _) && functions.Length != 0)
			{
				if (functionContainer.Length == 0)
					HypernamePublicGeneralMethod(branch, info, innerResults, ref extra, functions[^1], "user");
				else if (HypernameGeneralMethod(branch, info, innerResults, ref extra, ref errorsList,
					prevIndex, functionContainer, functions[^1], "userMethod") != null)
					return "_";
				result.AddRange(info);
				branch.Extra = new UniversalType(FuncBlockStack,
					new([functions[^1].ReturnUnvType, .. functions[^1].Parameters.Convert(x => x.Type)]));
				AddRange(ref errorsList, functionErrorsList!);
			}
			else if (GeneralMethodExists(new(), info, parameterTypes, out functions, out var user) && functions.Length != 0)
			{
				if (info.ToString() is "ExecuteString" or "Q" && !(branch.Length >= 2 && branch[1].Info == nameof(Call)))
				{
					var otherPos = branch.FirstPos;
					Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + info + "\" cannot be used in the delegate");
					branch.Parent[prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
					return "_";
				}
				HypernamePublicGeneralMethod(branch, info, innerResults, ref extra, functions[^1], user ? "user" : "general");
				result.AddRange(info);
				branch.Extra = new UniversalType(FuncBlockStack, new([functions[^1].ReturnUnvType, .. functions[^1].Parameters.Convert(x => x.Type)]));
			}
			else
			{
				var otherPos = branch.FirstPos;
				if (variableErrorsList != null && variableErrorsList.Length != 0)
					AddRange(ref errorsList, variableErrorsList);
				else if (propertyErrorsList != null && propertyErrorsList.Length != 0)
					AddRange(ref errorsList, propertyErrorsList);
				else if (functionErrorsList != null && functionErrorsList.Length != 0)
					AddRange(ref errorsList, functionErrorsList);
				else
					Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": identifier \"" + info + "\" is not defined in this location");
				branch.Parent[prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				return prevIndex == 0 || branch.Parent.Info == nameof(List) ? "default!" : "_";
			}
		}
		else
		{
			if (!(extra is List<object> list && list.Length is >= 2 and <= 4 && list[0] is String Category && list[1] is UniversalType ContainerUnvType))
			{
				var otherPos = branch.FirstPos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
			if (PropertyExists(ContainerUnvType, PropertyMapping(info), out var property))
			{
				if (!property.HasValue)
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)"Property", NullType };
					return "_";
				}
				else if ((property.Value.Attributes & PropertyAttributes.Closed) != 0 ^ (property.Value.Attributes & PropertyAttributes.Protected) != 0 && !new List<Block>(branch.Container).StartsWith([.. ContainerUnvType.MainType]))
				{
					var otherPos = branch.FirstPos;
					Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": property \"" + String.Join(".", ContainerUnvType.MainType.Convert(x => x.Name).Append(info).ToArray()) + "\" is inaccessible from here");
					branch.Parent[branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
					return "_";
				}
				else
				{
					branch.Extra = branch[0].Extra = new UniversalType(property.Value.UnvType.MainType, property.Value.UnvType.ExtraTypes);
					extra = new List<object> { (String)"Property", branch.Extra };
				}
				result.AddRange(PropertyMapping(info));
			}
			else if (UserDefinedFunctionExists(ContainerUnvType.MainType, info, parameterTypes, out var functions) && functions.Length != 0)
			{
				if (HypernameGeneralMethod(branch, info, innerResults, ref extra, ref errorsList, prevIndex, ContainerUnvType.MainType, functions[^1], "userMethod") != null)
					return "_";
				result.AddRange(info);
				branch.Extra = new UniversalType(FuncBlockStack, new([functions[^1].ReturnUnvType, .. functions[^1].Parameters.Convert(x => x.Type)]));
			}
			else if (MethodExists(ContainerUnvType, FunctionMapping(info, null), parameterTypes, out functions) && functions.Length != 0)
			{
				if (HypernameMethod(branch, info, innerResults, ref extra, ref errorsList, prevIndex, ContainerUnvType.MainType, functions[^1]) != null)
					return "_";
				result.AddRange(info);
				branch.Extra = new UniversalType(FuncBlockStack, new([functions[^1].ReturnUnvType, .. functions[^1].Parameters.Convert(x => x.Type)]));
			}
			else
			{
				var otherPos = branch.FirstPos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": type \"" + String.Join(".", ContainerUnvType.MainType.ToArray(x => x.Name)) + "\" does not contain member \"" + info + "\"");
				branch.Parent[branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				return "_";
			}
		}
		Debug.Assert(branch.Extra != null);
		return result;
	}

	private String Hypername2(TreeBranch branch, ref List<String>? errorsList, ref object? extra, ref int index)
	{
		String result = [];
		if (branch[index].Info == nameof(Call) && extra is List<object> list)
		{
			if (list.Length == 2 && list[0] is String delegateElem1 && delegateElem1.ToString() is "Variable" or "Property" or nameof(Expr) && list[1] is UniversalType DelegateUnvType && new BlockStackEComparer().Equals(DelegateUnvType.MainType, FuncBlockStack) && DelegateUnvType.ExtraTypes.Length != 0 && !DelegateUnvType.ExtraTypes[0].MainType.IsValue)
			{
				if (index <= 1)
					result.AddRange(branch[index - 1].Info);
				result.AddRange(List(branch[index], out var innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
				branch.Extra = new UniversalType(DelegateUnvType.ExtraTypes[0].MainType.Type, DelegateUnvType.ExtraTypes[0].ExtraTypes);
				return result;
			}
			if (!(list.Length >= 4 && list.Length <= 5 && list[0] is String elem1 && elem1.StartsWith("Function ") && list[1] is String elem2 && list[3] is List<String> innerResults))
			{
				var otherPos = branch[index].Pos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
			var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList(x =>
				x.Extra is UniversalType UnvType ? UnvType : throw new InvalidOperationException());
			var s = elem1["Function ".Length..];
			if (s == "ExecuteString")
			{
				var @string = innerResults[0];
				var addParameters = branch[index].Length != 1;
				String? parameters;
				if (addParameters)
				{
					parameters = ((String)", ").AddRange(List(new(nameof(List), branch[index].Elements[1..], branch.Container), out var parametersErrorsList));
					AddRange(ref errorsList, parametersErrorsList);
				}
				else
					parameters = [];
				if (parameters.StartsWith(", (") && parameters.EndsWith(')'))
				{
					parameters[2] = '[';
					parameters[^1] = ']';
				}
				result.AddRange(nameof(ExecuteProgram)).Add('(').AddRange(nameof(TranslateProgram));
				result.AddRange("(((String)\"").AddRange(ExecuteStringPrefix).AddRange("\").AddRange(").AddRange(@string);
				result.AddRange(")).Wrap(x => (x.Item1.Remove(x.Item1.IndexOf(").AddRange(nameof(ExecuteStringPrefixCompiled));
				result.AddRange("), ").AddRange(nameof(ExecuteStringPrefixCompiled));
				result.AddRange(""".Length), x.Item2, x.Item3)), out _, out _""").AddRange(parameters).AddRange(").");
				result.AddRange(nameof(Quotes.RemoveQuotes)).AddRange("()");
			}
			else if (s == "Q")
			{
				branch.Extra = StringType;
				return ((String)"((String)@\"").AddRange(input.Replace("\"", "\"\"")).AddRange("\")");
			}
			else if (elem2 == "public" && s == nameof(RedStarLinq.Fill) && branch[index].Length == 2
				&& branch[index][0].Extra is UniversalType FirstParameterType
				&& TypeEqualsToPrimitive(FirstParameterType, "bool"))
			{
				result.AddRange("new ").AddRange(nameof(BitList)).Add('(');
				result.AddRange(ParseAction(branch[index][1].Info)(branch[index][1], out var innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
				result.AddRange(", ");
				result.AddRange(ParseAction(branch[index][0].Info)(branch[index][0], out innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
				result.Add(')');
				branch.Extra = BitListType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra };
			}
			else if (!elem2.StartsWith("user") && branch.Parent?[0].Extra is UniversalType ContainerUnvType)
			{
				if (MethodExists(ContainerUnvType, FunctionMapping(s, null), parameterTypes, out var functions)
					&& functions.Length != 0)
				{
					result.AddRange(FunctionMapping(s, Call(branch[index], innerResults, out var innerErrorsList, extra)));
					AddRange(ref errorsList, innerErrorsList);
					branch.Extra = functions[^1].ReturnUnvType;
					extra = new List<object> { (String)nameof(Expr), branch.Extra };
				}
				else if (GeneralMethodExists(ContainerUnvType.MainType, s, branch[index].Elements.ToList(x =>
					x.Extra is UniversalType UnvType ? UnvType : throw new InvalidOperationException()),
					out functions, out _) && functions.Length != 0)
				{
					result.AddRange(FunctionMapping(s, Call(branch[index], innerResults, out var innerErrorsList, extra)));
					AddRange(ref errorsList, innerErrorsList);
					branch.Extra = functions[^1].ReturnUnvType;
					extra = new List<object> { (String)nameof(Expr), branch.Extra };
				}
				if (!result.EndsWith(')') && !result.EndsWith(") + 1"))
					return "default!";
			}
			else if ((elem2 == "user" && UserDefinedFunctionExists(new(), s, parameterTypes,
				out var functions, out _, out var derived) || elem2 == "userMethod"
				&& UserDefinedFunctionExists(branch.Container, s, parameterTypes, out functions, out _, out derived))
				&& functions.Length != 0)
			{
				List<String>? innerErrorsList;
				if (derived)
					result.AddRange(FunctionMapping(s, Call(branch[index], innerResults, out innerErrorsList, extra)));
				else
				{
					result.AddRange(index > 1 ? [] : EscapedKeywordsList.Contains(s) ? ((String)"@").AddRange(s) : s);
					result.AddRange(CallUser(branch[index], innerResults, out innerErrorsList, extra));
				}
				AddRange(ref errorsList, innerErrorsList);
				if (!result.EndsWith(')'))
					return "default!";
				branch.Extra = functions[^1].ReturnUnvType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra };
			}
			else if (elem2 == "userMethod" && branch.Parent?[0].Extra is UniversalType ContainerUnvType2)
			{
				if (TypeEqualsToPrimitive(ContainerUnvType2, "typename") && list.Length == 5 && list[4] is String elem4
					&& elem4 == "static" && UserDefinedFunctionExists(ContainerUnvType2.MainType, s,
					parameterTypes, out functions) && functions.Length != 0)
				{
					result.AddRange(index > 1 ? [] : EscapedKeywordsList.Contains(s) ? ((String)"@").AddRange(s) : s).AddRange(CallUser(branch[index], innerResults, out var innerErrorsList, extra));
					AddRange(ref errorsList, innerErrorsList);
					branch.Extra = functions[^1].ReturnUnvType;
					extra = new List<object> { (String)nameof(Expr), branch.Extra };
				}
				else if (UserDefinedFunctionExists(ContainerUnvType2.MainType, s, parameterTypes,
					out functions, out _, out derived) && functions.Length != 0)
				{
					result.AddRange(derived ? FunctionMapping(s, Call(branch[index], innerResults, out var innerErrorsList, extra)) : (index > 1 ? [] : EscapedKeywordsList.Contains(s) ? ((String)"@").AddRange(s) : s.Copy()).AddRange(CallUser(branch[index], innerResults, out innerErrorsList, extra)));
					AddRange(ref errorsList, innerErrorsList);
					branch.Extra = functions[^1].ReturnUnvType;
					extra = new List<object> { (String)nameof(Expr), branch.Extra };
				}
				if (!result.EndsWith(')'))
					return "default!";
			}
			else
			{
				var otherPos = branch[index].Pos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
		}
		else if (branch[index].Info == nameof(ConstructorCall) && extra is List<object> list2)
		{
			if (!(list2.Length >= 4 && list2.Length <= 5 && list2[0] is String elem1 && elem1 == "Constructor" && list2[1] is UniversalType ConstructingUnvType && list2[2] is String elem3 && list2[3] is ConstructorOverloads constructors && constructors.Length != 0))
			{
				var otherPos = branch[index].Pos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
				return "default!";
			}
			if (elem3 == "typical")
			{
				result.AddRange("new ").AddRange(Type(ConstructingUnvType)).AddRange(ConstructorCall(branch[index], out var innerErrorsList, extra));
				AddRange(ref errorsList, innerErrorsList);
				branch.Extra = branch[0].Extra;
				if (innerErrorsList != null && innerErrorsList.Any(x => x.StartsWith("Error")))
					return "default!";
			}
			else
			{
				result.AddRange("new ").AddRange(Type(ConstructingUnvType)).AddRange(ConstructorCall(branch[index], out var innerErrorsList, extra));
				AddRange(ref errorsList, innerErrorsList);
				branch.Extra = branch[0].Extra;
				if (innerErrorsList != null && innerErrorsList.Any(x => x.StartsWith("Error")))
					return "default!";
			}
		}
		else if (branch[index].Info == "Indexes")
		{
			if (branch[index - 1].Extra is not UniversalType CollectionUnvType)
				return [];
			foreach (var x in branch[index].Elements)
			{
				List<String>? innerErrorsList;
				if (!TypeEqualsToPrimitive(CollectionUnvType, "tuple", false))
				{
					var trivialIndex = IsTrivialIndexType(CollectionUnvType);
					result.AddRange("[(").AddRange(ParseAction(x.Info)(x, out innerErrorsList)).AddRange(trivialIndex ? ") - 1]" : ")]");
					AddRange(ref errorsList, innerErrorsList);
					CollectionUnvType = GetSubtype(CollectionUnvType);
					continue;
				}
				if (!int.TryParse(ParseAction(x.Info)(x, out innerErrorsList).ToString(), out var value))
				{
					var otherPos = branch[index].Pos;
					Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": at present time index in the tuple must be compilation-time constant");
					return "default!";
				}
				result.AddRange(".Item").AddRange(value.ToString());
				AddRange(ref errorsList, innerErrorsList);
				CollectionUnvType = (CollectionUnvType.ExtraTypes[value - 1].MainType.Type, CollectionUnvType.ExtraTypes[value - 1].ExtraTypes);
			}
			branch.Extra = CollectionUnvType;
		}
		else if (branch[index].Info == nameof(Call))
		{
			var otherPos = branch[index].Pos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": this call is forbidden");
			return "default!";
		}
		else if (branch[index].Info == ".")
		{
			using var innerResult = Hypername(branch[++index], out var innerErrorsList, extra);
			if (errorsList != null && errorsList.Length != 0)
				return "default!";
			AddRange(ref errorsList, innerErrorsList);
			if (innerResult.ToString() is "default" or "default!")
				return "default!";
			if (innerResult.StartsWith('('))
			{
				innerResult.RemoveAt(0);
				result.Insert(0, '(');
			}
			result.Add('.').AddRange(innerResult);
			extra = branch.Extra = branch[index].Extra;
		}
		else
		{
			var otherPos = branch[index].Pos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal error");
			return "default!";
		}
		Debug.Assert(branch.Extra != null);
		return result;
		static bool IsTrivialIndexType(UniversalType CollectionUnvType)
		{
			if (TypeEqualsToPrimitive(CollectionUnvType, "list", false))
				return true;
			if (CollectionUnvType.ExtraTypes.Length == 1 && TypesAreCompatible(CollectionUnvType,
				new(new([new(BlockType.Namespace, "System", 0), new(BlockType.Namespace, "Collections", 0),
				new(BlockType.Interface, nameof(G.IEnumerable<bool>), 0)]), CollectionUnvType.ExtraTypes),
				out var warning, null, out _, out _) && !warning)
				return true;
			if (CollectionUnvType.ExtraTypes.Length != 2)
				return false;
			if (!new BlockStackEComparer().Equals(CollectionUnvType.MainType, CollectionUnvType.ExtraTypes[1].MainType.Type))
				return false;
			if (CollectionUnvType.ExtraTypes[1].ExtraTypes.Length != 1)
				return false;
			if (!TypesAreEqual(new(CollectionUnvType.ExtraTypes[0].MainType.Type, CollectionUnvType.ExtraTypes[0].ExtraTypes),
				new(CollectionUnvType.ExtraTypes[1].ExtraTypes[0].MainType.Type,
				CollectionUnvType.ExtraTypes[1].ExtraTypes[0].ExtraTypes)))
				return false;
			return TypesAreCompatible(CollectionUnvType,
				new(new([new(BlockType.Namespace, "System", 0), new(BlockType.Namespace, "Collections", 0),
				new(BlockType.Interface, nameof(BaseIndexable<bool>), 0)]), CollectionUnvType.ExtraTypes),
				out warning, null, out _, out _) && !warning;
		}
	}

	private bool? HypernameMethod(TreeBranch branch, String s, List<String> innerResults, ref object? refExtra, ref List<String>? errorsList, int prevIndex, BlockStack ContainerMainType, GeneralMethodOverload function)
	{
		UniversalType extra;
		object extra2;
		if ((function.Attributes & FunctionAttributes.Closed) != 0 ^ (function.Attributes & FunctionAttributes.Protected) != 0 && !new List<Block>(branch.Container).StartsWith([.. ContainerMainType]))
		{
			var otherPos = branch.FirstPos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name), s]) + "\" is inaccessible from here");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else if ((function.Attributes & FunctionAttributes.Static) == 0 && !(branch.Length >= 2 && branch[1].Info == nameof(Call)))
		{
			var otherPos = branch.FirstPos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name), s]) + "\" is linked with object instance so it cannot be used in delegate");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else
		{
			extra = function.ReturnUnvType;
			var list = new List<object> { ((String)"Function ").AddRange(s), (String)"method", function, innerResults };
			extra2 = (function!.Attributes & FunctionAttributes.Static) != 0 ? list.Add("static") : list;
		}
		HypernameAddExtra(branch, extra, extra2, ref refExtra, new(function.Parameters.Convert(x => (UniversalTypeOrValue)x.Type).Prepend(extra).ToList() ?? [extra]));
		return null;
	}

	private bool? HypernameGeneralMethod(TreeBranch branch, String s, List<String> innerResults, ref object? refExtra, ref List<String>? errorsList, int prevIndex, BlockStack ContainerMainType, GeneralMethodOverload function, String category)
	{
		UniversalType extra;
		object extra2;
		if ((function.Attributes & FunctionAttributes.Closed) != 0 ^ (function.Attributes & FunctionAttributes.Protected) != 0 && !new List<Block>(branch.Container).StartsWith([.. ContainerMainType]))
		{
			var otherPos = branch.FirstPos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name), s]) + "\" is inaccessible from here");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else if ((function.Attributes & FunctionAttributes.Static) == 0 && !new BlockStackEComparer().Equals(branch.Container, ContainerMainType) && !(branch.Length >= 2 && branch[1].Info == nameof(Call)))
		{
			var otherPos = branch.FirstPos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": function \"" + String.Join(".", [.. ContainerMainType.ToList().Convert(x => x.Name), s]) + "\" is linked with object instance so it cannot be used in delegate");
			branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
			return false;
		}
		else
		{
			extra = function!.ReturnUnvType;
			var list = new List<object> { ((String)"Function ").AddRange(s), category, function, innerResults };
			extra2 = (function.Attributes & FunctionAttributes.Static) != 0 ? list.Add("static") : list;
		}
		GeneralExtraTypes parameterTypes = new(function.Parameters.Convert(x => (UniversalTypeOrValue)x.Type).Prepend(extra).ToList() ?? [extra]);
		HypernameAddExtra(branch, extra, extra2, ref refExtra, parameterTypes);
		return null;
	}

	private static bool? HypernamePublicGeneralMethod(TreeBranch branch, String s, List<String> innerResults, ref object? refExtra, GeneralMethodOverload function, String category)
	{
		UniversalType extra;
		object extra2;
		extra = function.ReturnUnvType;
		extra2 = new List<object> { ((String)"Function ").AddRange(s), category, function, innerResults };
		GeneralExtraTypes parameterTypes = new(function.Parameters.Convert(x => (UniversalTypeOrValue)x.Type).Prepend(extra).ToList() ?? [extra]);
		HypernameAddExtra(branch, extra, extra2, ref refExtra, parameterTypes);
		return null;
	}

	private static void HypernameAddExtra(TreeBranch branch, UniversalType extra, object extra2, ref object? refExtra, GeneralExtraTypes extraTypes)
	{
		if (branch.Length >= 2 && branch[1].Info == nameof(Call))
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

	private List<String>? Call(TreeBranch branch, List<String> innerResults, out List<String>? errorsList, object? extra = null)
	{
		List<String> result = [];
		errorsList = [];
		for (var i = 0; i < branch.Length; i++)
		{
			var innerResult = innerResults[i];
			if (innerResult.Length != 0)
				result.Add(innerResult);
		}
		if (!CallCheck(branch, ref errorsList, innerResults, extra))
			return null;
		if (branch.Length != 0 && branch[0].Length == 1 && branch[0][0].Info.EndsWith(" (delegate)"))
			return branch[0][0].Info[..^" (delegate)".Length];
		return result;
	}

	private String CallUser(TreeBranch branch, List<String> innerResults, out List<String>? errorsList, object? extra = null)
	{
		var callResult = Call(branch, innerResults, out errorsList, extra);
		if (callResult == null)
			return [];
		var joined = String.Join(", ", callResult);
		return joined.Insert(0, '(').Add(')');
	}

	private bool CallCheck(TreeBranch branch, ref List<String>? errorsList, List<String> innerResults, object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<UniversalType> CallParameterUnvTypes = [];
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is UniversalType type)
				CallParameterUnvTypes.Add(type);
			else
			{
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": internal compiler error");
				return false;
			}
		if (!(extra is List<object> list
			&& list.Length >= 3 && list.Length <= 5 && list[0] is String elem1 && elem1.StartsWith("Function ")
			&& list[2] is GeneralMethodOverload function))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return false;
		}
		var warnings = new bool[CallParameterUnvTypes.Length];
		var FunctionParameterUnvTypes = new UniversalType[CallParameterUnvTypes.Length];
		var adaptedInnerResults = new String[CallParameterUnvTypes.Length];
		var extraMessages = new String[CallParameterUnvTypes.Length];
		int index = 0, index2 = 0;
		if (!(list.Length is 3 or 4 && list[^1] is List<String> || list.Length == 5 && list[3] is List<String>))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString()
				+ " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return false;
		}
		var (_, ReturnUnvType, Attributes, Parameters) = function;
		if (Parameters.Length == 0 && innerResults.Length != 0)
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": the function \"" + elem1["Function ".Length..].ToString()
				+ "\" does not have overloads with parameters");
			return false;
		}
		else if (Parameters.Length == 0)
			return true;
		else if (CallParameterUnvTypes.Length > Parameters.Length
			|| CallParameterUnvTypes.Length < Parameters.Count(x => (x.Attributes & ParameterAttributes.Optional) == 0))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": incorrect number of parameters of the call");
			return false;
		}
		else if (Parameters.Any((x, i) => (index = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
			== ParameterAttributes.Ref && !innerResults[index].StartsWith("ref ")))
		{
			Add(ref errorsList, "Wreck in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": this parameter must pass with the \"ref\" keyword");
			wreckOccurred = true;
			return true;
		}
		else if (Parameters.Any((x, i) => (index = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
			== ParameterAttributes.Out && !innerResults[index].StartsWith("out ")))
		{
			Add(ref errorsList, "Wreck in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": this parameter must pass with the \"out\" keyword");
			wreckOccurred = true;
			return true;
		}
		else if (!(CallParameterUnvTypes.Length >= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			&& CallParameterUnvTypes.Combine(Parameters).All((x, i) => TypesAreCompatible(x.Item1,
			FunctionParameterUnvTypes[i] = x.Item2.Type, out warnings[index2 = index = i],
			innerResults[i], out adaptedInnerResults[i]!, out extraMessages[i]!) && adaptedInnerResults[i] != null))
			&& !((Parameters[^1].Attributes & ParameterAttributes.Params) != 0 && index == Parameters.Length - 1
			&& CallParameterUnvTypes.Skip(index2 = index).All((x, i) => TypesAreCompatible(x,
			Parameters[^1].Type, out warnings[index = index2 + i], innerResults[index],
			out adaptedInnerResults[index]!, out extraMessages[index]!) && adaptedInnerResults[index] != null)))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": " + (extraMessages[index]
				?? "incompatibility between type of the parameter of the call \"" + CallParameterUnvTypes[index]
				+ "\" and type of the parameter of the function \"" + FunctionParameterUnvTypes[index] + "\""
				+ (TypeEqualsToPrimitive(FunctionParameterUnvTypes[index], "string")
				? " - use an addition of zero-length string for this" : "")));
			return false;
		}
		else if (warnings[index])
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": " + (extraMessages[index] ?? "conversion from type \""
				+ CallParameterUnvTypes[index] + "\" to type \"" + FunctionParameterUnvTypes[index]
				+ "\" is possible only in function return," + " not in the direct assignment and not in the call"));
			return false;
		}
		_ = innerResults.ToList((x, i) => x != adaptedInnerResults[i]
			? x.Replace(adaptedInnerResults[i] ?? "default!") : "");
		branch.Extra = ReturnUnvType;
		return true;
	}

	private String ConstructorCall(TreeBranch branch, out List<String>? errorsList, object? extra = null)
	{
		String result = "(";
		errorsList = [];
		List<String> innerResults = [];
		for (var i = 0; i < branch.Length; i++)
		{
			var innerResult = ParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
			if (innerResult.Length != 0)
			{
				innerResults.AddRange(innerResult);
				AddRange(ref errorsList, innerErrorsList);
			}
		}
		if (!ConstructorCallCheck(branch, ref errorsList, innerResults, extra))
			return [];
		return result.AddRange(String.Join(", ", innerResults)).Add(')');
	}

	private bool ConstructorCallCheck(TreeBranch branch, ref List<String>? errorsList, List<String> innerResults, object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<UniversalType> CallParameterUnvTypes = [];
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is UniversalType type)
				CallParameterUnvTypes.Add(type);
			else
			{
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": internal compiler error");
				return false;
			}
		if (!(extra is List<object> list
			&& list.Length >= 4 && list.Length <= 5 && list[0] is String elem1 && elem1 == "Constructor"
			&& list[1] is UniversalType ConstructingUnvType && list[2] is String elem3 &&
			list[3] is ConstructorOverloads constructors && constructors.Length != 0))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": internal compiler error");
			return false;
		}
		var max = constructors.Any(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
		& ParameterAttributes.Params) != 0) ? int.MaxValue : constructors.Max(x => x.Parameters.Length);
		var min = constructors.Min(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0));
		if (CallParameterUnvTypes.Length > max || CallParameterUnvTypes.Length < min)
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": constructor of the type \"" + ConstructingUnvType.ToString()
				+ "\" must have " + (max == min ? max.ToString()
				: "from " + min.ToString() + " to " + max.ToString()) + " parameters");
			return false;
		}
		constructors.FilterInPlace(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
			& ParameterAttributes.Params) != 0 || x.Parameters.Length >= CallParameterUnvTypes.Length)
			.FilterInPlace(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			<= CallParameterUnvTypes.Length);
		var warnings = new bool[CallParameterUnvTypes.Length];
		var FunctionParameterUnvTypes = new UniversalType[CallParameterUnvTypes.Length];
		var adaptedInnerResults = new String[CallParameterUnvTypes.Length];
		var extraMessages = new String[CallParameterUnvTypes.Length];
		int index = 0, index2 = 0;
		if (constructors.Length == 1)
		{
			var (Attributes, Parameters) = constructors[0];
			if (Parameters.Length == 0 && innerResults.Length != 0)
			{
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": the type \"" + ConstructingUnvType.ToString()
					+ "\" does not have constructors with parameters");
				return false;
			}
			else if (Parameters.Length == 0)
				return true;
			else if (!(CallParameterUnvTypes.Length >= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterUnvTypes.Combine(Parameters).All((x, i) => TypesAreCompatible(x.Item1,
				FunctionParameterUnvTypes[i] = x.Item2.Type, out warnings[index2 = index = i],
				innerResults[i], out adaptedInnerResults[i]!, out extraMessages[i]!) && adaptedInnerResults[i] != null))
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) != 0 && index == Parameters.Length - 1
				&& CallParameterUnvTypes.Skip(index2 = index).All((x, i) => TypesAreCompatible(x,
				Parameters[^1].Type, out warnings[index = index2 + i], innerResults[index],
				out adaptedInnerResults[index]!, out extraMessages[index]!) && adaptedInnerResults[index] != null)))
			{
				Add(ref errorsList, "Error in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": " + (extraMessages[index]
					?? "incompatibility between type of the parameter of the call \"" + CallParameterUnvTypes[index]
					+ "\" and type of the parameter of the constructor \""
					+ FunctionParameterUnvTypes[index2] + "\""
					+ (TypeEqualsToPrimitive(FunctionParameterUnvTypes[index], "string")
					? " - use an addition of zero-length string for this" : "")));
				return false;
			}
			else if (warnings[index])
				Add(ref errorsList, "Warning in line " + lexems[otherPos = branch[index].Pos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": " + (extraMessages[index] ?? "type of the parameter of the call \""
					+ CallParameterUnvTypes[index] + "\" and type of the parameter of the constructor \""
					+ FunctionParameterUnvTypes[index2] + "\" are badly compatible, you may lost data"));
			_ = innerResults.ToList((x, i) => x != adaptedInnerResults[i]
				? x.Replace(adaptedInnerResults[i] ?? "default!") : "");
			branch.Extra = ConstructingUnvType;
			return true;
		}
		ListHashSet<int> IncompatibleConstructors = [];
		ListHashSet<(int ConstructorIndex, int ParameterIndex)> BadlyCompatibleConstructors = [];
		var indexes = new int[constructors.Length];
		var indexes2 = new int[constructors.Length];
		for (var j = 0; j < constructors.Length; j++)
		{
			var (Attributes, Parameters) = constructors[j];
			if (Parameters.Length == 0)
				continue;
			else if (!(CallParameterUnvTypes.Length >= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterUnvTypes.Combine(Parameters).All((x, i) =>
				TypesAreCompatible(x.Item1, FunctionParameterUnvTypes[i] = x.Item2.Type,
				out warnings[indexes2[j] = indexes[j] = i], innerResults[i],
				out adaptedInnerResults[i]!, out extraMessages[i]!) && adaptedInnerResults[i] != null)
				&& CallParameterUnvTypes.Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& indexes[j] == Parameters.Length - 1
				&& CallParameterUnvTypes.Skip(indexes2[j] = indexes[j]).All((x, i) => TypesAreCompatible(x,
				Parameters[^1].Type, out warnings[indexes[j] = indexes2[j] + i],
				innerResults[indexes[j]], out adaptedInnerResults[indexes[j]]!, out extraMessages[indexes[j]]!)
				&& adaptedInnerResults[indexes[j]] != null)))
				IncompatibleConstructors.Add(j);
			else if (warnings.Any(x => x))
				_ = warnings.ToList((x, i) => x ? BadlyCompatibleConstructors.Add((j, i)) : default);
		}
		var thresholdIndexes = indexes.IndexesOfMax();
		var incompatibleLength = IncompatibleConstructors.Length;
		IncompatibleConstructors.IntersectWith(thresholdIndexes);
		if (incompatibleLength == constructors.Length)
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos = branch[indexes[thresholdIndexes[0]]].Pos].LineN.ToString()
				+ " at position " + lexems[otherPos].Pos.ToString() + ": "
				+ "incompatibility between type of the parameter of the call \""
				+ CallParameterUnvTypes[indexes[thresholdIndexes[0]]]
				+ "\" and all possible types of the parameter of the constructor (\""
				+ String.Join("\", \"", IncompatibleConstructors.Convert(j =>
				constructors[j].Parameters[indexes2[thresholdIndexes[0]]].Type.ToString()).ToHashSet()) + "\")"
				+ (IncompatibleConstructors.Length == 1 && TypeEqualsToPrimitive(constructors[IncompatibleConstructors[0]]
				.Parameters[indexes2[thresholdIndexes[0]]].Type, "string")
				? " - use an addition of zero-length string for this" : ""));
			return false;
		}
		BadlyCompatibleConstructors.FilterInPlace(x => !IncompatibleConstructors.Contains(x.ConstructorIndex));
		var groups = BadlyCompatibleConstructors.NGroup(x => x.ParameterIndex);
		var WellCompatibleConstructors = new Chain(constructors.Length).ToHashSet()
			.ExceptWith(IncompatibleConstructors).ExceptWith(groups.ConvertAndJoin(x => x).Convert(x => x.ConstructorIndex));
		if (WellCompatibleConstructors.Length != 0)
		{
			branch.Extra = ConstructingUnvType;
			return true;
		}
		foreach (var group in groups)
			Add(ref errorsList, "Warning in line " + lexems[otherPos = branch[group.Key].Pos].LineN.ToString()
				+ " at position " + lexems[otherPos].Pos.ToString() + ": " + "type of the parameter of the call \""
				+ CallParameterUnvTypes[group.Key] + "\" and all possible types of the parameter of the constructor (\""
				+ String.Join("\", \"", group.Convert(item =>
				constructors[item.ConstructorIndex].Parameters.Wrap(x => x[index = Min(group.Key,
				x.Length - 1)].Type.ToString())).ToHashSet())
				+ "\") are badly compatible, you may lost data");
		_ = innerResults.ToList((x, i) => x != adaptedInnerResults[i]
			? x.Replace(adaptedInnerResults[i] ?? "default!") : "");
		branch.Extra = ConstructingUnvType;
		return true;
	}

	private String Expr(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		var innerResults = new List<String>();
		int i;
		for (i = 0; i < branch.Length; i++)
		{
			if (branch[i].Info == "type")
			{
				innerResults.SetOrAdd(i, "typeof(" + (branch[0].Extra is UniversalType type2 ? Type(type2) : "dynamic") + ")");
				continue;
			}
			else if (TypesListExpr.Contains(branch[i].Info))
			{
				innerResults.SetOrAdd(i, ParseAction(branch[i].Info)(branch[i], out var innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
				continue;
			}
			else if (TryReadValue(branch[i].Info, out var value))
			{
				branch[i].Extra = value.InnerType;
				innerResults.SetOrAdd(i, value.ToString(true, true));
				continue;
			}
			else if (i == 1 && innerResults.Length == 1 && TryReadValue(branch[0].Info, out value))
			{
				innerResults.SetOrAdd(i, ValueExpr(value, branch, ref errorsList, i--));
				continue;
			}
			else if (i == 0 || i % 2 != 0)
				return branch.Length == 2 && i == 1 ? UnaryExpr(branch, ref errorsList, i)
					: ListExpr(branch, ref errorsList, i);
			if (branch[i - 2].Extra is not UniversalType UnvType1)
				UnvType1 = NullType;
			if (branch[i - 1].Extra is not UniversalType UnvType2)
				UnvType2 = NullType;
			var resultType = GetResultType(UnvType1, UnvType2, innerResults[^2].Copy(), innerResults[^1].Copy());
			String @default = "default";
			if (!(branch.Parent?.Info == "return"
				|| branch.Parent?.Info == nameof(List) && branch.Parent?.Parent?.Info == "return"))
			{
				@default.Add('(');
				@default.AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String" : Type(resultType)).Add(')');
			}
			@default.Add('!');
			if (!TryReadValue(branch[i].Info, out _) && branch[i].Info.ToString() is not ("pow" or "tetra" or "penta"
				or "hexa" or "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "pow=" or "tetra=" or "penta=" or "hexa="
				or "&=" or "|=" or "^=" or ">>=" or "<<=") && TryReadValue(branch[Max(i - 3, 0)].Info, out var value1)
				&& TryReadValue(branch[i - 1].Info, out var value2))
			{
				var innerResult = new TwoValuesExpr(value1, value2, branch, lexems, @default).Calculate(ref errorsList, ref i);
				innerResults.SetOrAdd(i, innerResult);
				continue;
			}
			innerResults.SetOrAdd(i, branch[i].Info.ToString() switch
			{
				"+" or "-" => PMExpr(branch, innerResults, ref errorsList, ref i),
				"*" or "/" or "%" => MulDivExpr(branch, innerResults, ref errorsList, ref i),
				"pow" or "tetra" or "penta" or "hexa" => PowExpr(branch, innerResults, ref errorsList, i),
				"==" or ">" or "<" or ">=" or "<=" or "!=" or "&&" or "||" or "^^" =>
					BoolExpr(branch, innerResults, ref errorsList, i),
				"=" or "+=" or "-=" or "*=" or "/=" or "%=" or "pow=" or "tetra=" or "penta=" or "hexa="
					or "&=" or "|=" or "^=" or ">>=" or "<<=" => Assignment(branch, innerResults, ref errorsList, i),
				"?" or "?=" or "?>" or "?<" or "?>=" or "?<=" or "?!=" or ":" => Ternary(branch, i),
				"CombineWith" => CombineWithExpr(branch, innerResults, i),
				nameof(List) => ListExpr(branch, ref errorsList, i),
				_ => BinaryNotListExpr(branch, innerResults, i),
			});
		}
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		if (branch.Info == "StringConcatenation")
		{
			branch.Elements = branch.Elements.Filter(x => x.Info != "+");
			branch.Extra = GetPrimitiveType("string");
		}
		else if (branch.Info == nameof(List))
			branch.Extra = branch.Elements.Progression(GetListType(NullType), (x, y) =>
			GetResultType(x, GetListType(y.Extra is UniversalType UnvType ? UnvType : NullType), "default!", "default!"));
		else if (branch.Info == "Indexes")
		{
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is UniversalType UnvType)
				branch.Extra = GetSubtype(UnvType, branch.Length);
			else
				branch.Extra = NullType;
		}
		else if (branch.Length == 1 && new List<String> { nameof(Expr), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation" }.Contains(branch.Parent.Info))
		{
			branch.Parent[prevIndex] = branch[0];
			branch.Extra = branch[0].Extra is UniversalType UnvType ? UnvType : NullType;
		}
		else
			branch.Extra = branch.Length != 0 && branch[^1].Extra is UniversalType UnvType ? UnvType : NullType;
		return innerResults[i - 1];
	}

	private String ValueExpr(Universal value, TreeBranch branch, ref List<String>? errorsList, int i)
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute this function of this constant");
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
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString() + ": cannot compute factorial of this constant");
				return "default!";
			}
		}
		return "default!";
	}

	private String UnaryExpr(TreeBranch branch, ref List<String>? errorsList, int i)
	{
		if (branch[i].Info.ToString() is "++" or "--" or "postfix ++" or "postfix --" or "!!")
			branch.Info = "UnaryAssignment";
		if (branch[i - 1].Extra is not UniversalType UnvType)
			UnvType = NullType;
		if (!(TypeIsPrimitive(UnvType.MainType) && UnvType.MainType.Peek().Name.ToString() is "bool" or "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": cannot apply the operator \"" + branch[i].Info
				+ "\" to the type \"" + UnvType.ToString() + "\"");
			return "default!";
		}
		branch[i].Extra = UnvType;
		var valueString = ParseAction(branch[i - 1].Info)(branch[i - 1], out var innerErrorsList);
		if (valueString.Length == 0)
			return "default!";
		AddRange(ref errorsList, innerErrorsList);
		branch.Extra = branch[i].Info.ToString() switch
		{
			"+" or "-" or "~" => TypeEqualsToPrimitive(UnvType, "bool") || TypeEqualsToPrimitive(UnvType, "string") ? RealType : TypeEqualsToPrimitive(UnvType, "byte") ? ShortIntType : TypeEqualsToPrimitive(UnvType, "unsigned short int") ? IntType : TypeEqualsToPrimitive(UnvType, "unsigned int") || TypeEqualsToPrimitive(UnvType, "unsigned long int") ? LongIntType : UnvType,
			"!" => BoolType,
			"sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "ln" or "postfix !" => RealType,
			"++" or "--" or "postfix ++" or "postfix --" or "!!" => UnvType,
			_ => NullType,
		};
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
			"++" => TypeEqualsToPrimitive(UnvType, "bool") ? valueString.Insert(0, '(').AddRange(" = true)") : valueString.Insert(0, "++"),
			"--" => TypeEqualsToPrimitive(UnvType, "bool") ? valueString.Insert(0, '(').AddRange(" = false)") : valueString.Insert(0, "--"),
			"postfix ++" => TypeEqualsToPrimitive(UnvType, "bool") ? valueString.Insert(0, '(').AddRange(" = true)") : valueString.AddRange("++"),
			"postfix --" => TypeEqualsToPrimitive(UnvType, "bool") ? valueString.Insert(0, '(').AddRange(" = false)") : valueString.AddRange("--"),
			"!!" => valueString.Copy().Insert(0, '(').AddRange(" = !(").AddRange(valueString).AddRange("))"),
			_ => "default!",
		};
	}

	private String PMExpr(TreeBranch branch, List<String> innerResults, ref List<String>? errorsList, ref int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		var resultType = GetResultType(UnvType1, UnvType2, innerResults[^2], innerResults[^1]);
		String @default = "default";
		if (!(branch.Parent?.Info == "return"
			|| branch.Parent?.Info == nameof(List) && branch.Parent?.Parent?.Info == "return"))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String" : Type(resultType)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": cannot cannot apply the operator \"" + branch[i].Info
				+ "\" to the types \"" + UnvType1.ToString() + "\" and \"" + UnvType2.ToString() + "\"");
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		var isString1 = TypeEqualsToPrimitive(UnvType1, "string");
		var isString2 = TypeEqualsToPrimitive(UnvType2, "string") || TypeEqualsToPrimitive(UnvType2, "char");
		var isStringPrev = TypeEqualsToPrimitive(PrevUnvType, "string");
		var isNull1 = TypeEqualsToPrimitive(UnvType1, "null");
		var isNull2 = TypeEqualsToPrimitive(UnvType2, "null");
		if (isNull1 && !isNull2)
			innerResults[^2].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType2)).Add(')'));
		else if (!isNull1 && isNull2)
			innerResults[^1].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType1)).Add(')'));
		if (branch[i].Info == "-" && (isString1 || isString2))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be subtracted");
			return @default;
		}
		if (isStringPrev && isString2 == false)
		{
			if (branch[Max(i - 3, 0)].Info == nameof(PMExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new(nameof(PMExpr), [tempBranch, branch[i - 1], branch[i]], branch[i].Container)
				{ Extra = resultType };
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (branch[i].Info == "-" && (isString1 || isString2))
		{
			if (branch[Max(i - 3, 0)].Info == nameof(PMExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new(nameof(PMExpr), [tempBranch, branch[i - 1], branch[i]], branch[i].Container)
				{ Extra = resultType };
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (i >= 4 && isString1 == false && isString2)
		{
			TreeBranch tempBranch = new(nameof(PMExpr), branch.GetRange(0, i - 1), branch[i - 2].Container);
			branch[0] = tempBranch;
			branch.Remove(1, i - 2);
			i = 2;
		}
		else if (branch.Info == nameof(Expr) && isString1 && isString2)
			branch.Info = "StringConcatenation";
		branch[i].Extra = resultType;
		if (isString1 && isString2)
		{
			var result = innerResults[^2].Copy();
			if (isStringPrev)
				result.AddRange((String)".Copy()");
			return result.AddRange(".AddRange(").AddRange(innerResults[^1]).Add(')');
		}
		else if (isString1 || isString2)
			return ((String)"((").AddRange(nameof(Universal)).Add(')').AddRange(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]).AddRange(").ToString()");
		else
		{
			if (innerResults[^2].ContainsAnyExcluding(AlphanumericCharacters))
				innerResults[^2].Insert(0, '(').Add(')');
			if (innerResults[^1].ContainsAnyExcluding(AlphanumericCharacters))
				innerResults[^1].Insert(0, '(').Add(')');
			return i < 2 ? branch[i][^1].Info : innerResults[^2].Copy().Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
		}
	}

	private String MulDivExpr(TreeBranch branch, List<String> innerResults, ref List<String>? errorsList, ref int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		var resultType = (branch[i].Info == "/" && TypeIsPrimitive(UnvType1.MainType) && TypeIsPrimitive(UnvType2.MainType))
			? GetPrimitiveType(Universal.GetQuotientType(UnvType1.MainType.Peek().Name,
			TryReadValue(branch[i - 1].Info, out var value) ? value : 5, UnvType2.MainType.Peek().Name))
			: (branch[i].Info == "%" && TypeIsPrimitive(UnvType1.MainType) && TypeIsPrimitive(UnvType2.MainType))
			? GetPrimitiveType(Universal.GetRemainderType(UnvType1.MainType.Peek().Name,
			TryReadValue(branch[i - 1].Info, out var value2) ? value2 : new(12345678901234567890, UnsignedLongIntType),
			UnvType2.MainType.Peek().Name)) : GetResultType(UnvType1, UnvType2, innerResults[^2], innerResults[^1]);
		String @default = "default";
		if (!(branch.Parent?.Info == "return"
			|| branch.Parent?.Info == nameof(List) && branch.Parent?.Parent?.Info == "return"))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String" : Type(resultType)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(UnvType2.MainType) && (UnvType2.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			|| branch[i].Info == "*" && UnvType2.MainType.Peek().Name == "null")))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": cannot cannot apply the operator \"" + branch[i].Info
				+ "\" to the types \"" + UnvType1.ToString() + "\" and \"" + UnvType2.ToString() + "\"");
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is UniversalType PrevUnvType))
			PrevUnvType = NullType;
		var isString1 = TypeEqualsToPrimitive(UnvType1, "string");
		var isString2 = TypeEqualsToPrimitive(UnvType2, "string");
		var isNull1 = TypeEqualsToPrimitive(UnvType1, "null");
		var isNull2 = TypeEqualsToPrimitive(UnvType2, "null");
		if (isNull1 && !isNull2)
			innerResults[^2].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType2)).Add(')'));
		else if (!isNull1 && isNull2)
			innerResults[^1].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType1)).Add(')'));
		if (branch[i].Info == "*" && isString1 && isString2)
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": the string cannot be multiplied by string");
			return @default;
		}
		else if (branch[i].Info != "*" && (isString1 || isString2))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": the strings cannot be divided or give remainder (%)");
			return @default;
		}
		if (TypeEqualsToPrimitive(PrevUnvType, "string") && isString2 == false)
		{
			if (branch[Max(i - 3, 0)].Info == nameof(MulDivExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				var tempBranch = branch[Max(i - 3, 0)];
				branch[Max(i - 3, 0)] = new(nameof(MulDivExpr), [tempBranch, branch[i - 1], branch[i]],
					branch[i].Container)
				{ Extra = resultType };
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		branch[i].Extra = resultType;
		if (branch[i].Info.ToString() is "/" or "%" && !TypeEqualsToPrimitive(UnvType1, "real")
			&& !TypeEqualsToPrimitive(UnvType2, "real") && innerResults[^1].ToString()
			is "0" or "0i" or "0u" or "0L" or "0uL" or "\"0\"")
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": division by integer zero is forbidden");
			branch[Max(i - 3, 0)] = new(@default, branch.Pos, branch.EndPos, branch.Container);
		}
		if (innerResults[^2].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^2].Insert(0, '(').Add(')');
		if (innerResults[^1].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^1].Insert(0, '(').Add(')');
		if (isString1)
			return innerResults[^2].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(innerResults[^1]).Add(')');
		if (isString2)
			return innerResults[^1].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(innerResults[^2]).Add(')');
		if (branch[i].Info.ToString() is "/" or "%" && TypeEqualsToPrimitive(UnvType1, "real")
			&& !TypeEqualsToPrimitive(UnvType2, "real"))
			innerResults[^2].Insert(0, "(double)(").Add(')');
		return i < 2 ? branch[i].Info : new String(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private String PowExpr(TreeBranch branch, List<String> innerResults, ref List<String>? errorsList, int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!(TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": cannot cannot apply the operator \"" + branch[i].Info
				+ "\" to the types \"" + UnvType1.ToString() + "\" and \"" + UnvType2.ToString() + "\"");
			return "default(double)!";
		}
		var isNull1 = TypeEqualsToPrimitive(UnvType1, "null");
		var isNull2 = TypeEqualsToPrimitive(UnvType2, "null");
		if (isNull1 && !isNull2)
			innerResults[^2].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType2)).Add(')'));
		else if (!isNull1 && isNull2)
			innerResults[^1].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType1)).Add(')'));
		branch[i].Extra = GetResultType(UnvType2, UnvType1, innerResults[^2], innerResults[^1]);
		return i < 2 ? branch[i].Info : ((String)"Pow(").AddRange(innerResults[^1]).AddRange(", ").AddRange(innerResults[^2]).Add(')');
	}

	private String BoolExpr(TreeBranch branch, List<String> innerResults, ref List<String>? errorsList, int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		if (!((branch[i].Info.ToString() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			|| branch[i].Info.ToString() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(UnvType1.MainType) && UnvType1.MainType.Peek().Name == "bool")
			&& (branch[i].Info.ToString() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name.ToString() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			|| branch[i].Info.ToString() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(UnvType2.MainType) && UnvType2.MainType.Peek().Name == "bool")))
		{
			Add(ref errorsList, "Error in line " + lexems[branch[i].Pos].LineN.ToString() + " at position "
				+ lexems[branch[i].Pos].Pos.ToString() + ": cannot cannot apply the operator \"" + branch[i].Info
				+ "\" to the types \"" + UnvType1.ToString() + "\" and \"" + UnvType2.ToString() + "\"");
			return "false";
		}
		var isNull1 = TypeEqualsToPrimitive(UnvType1, "null");
		var isNull2 = TypeEqualsToPrimitive(UnvType2, "null");
		if (isNull1 && !isNull2)
			innerResults[^2].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType2)).Add(')'));
		else if (!isNull1 && isNull2)
			innerResults[^1].ReplaceInPlace("(dynamic)", "").Insert(^(innerResults[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(UnvType1)).Add(')'));
		branch[i].Extra = BoolType;
		if (innerResults[^2].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^2].Insert(0, '(').Add(')');
		if (innerResults[^1].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^1].Insert(0, '(').Add(')');
		return i < 2 ? branch[i].Info : innerResults[^2].Copy().Add(' ')
			.AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private String Assignment(TreeBranch branch, List<String> innerResults, ref List<String>? errorsList, int i)
	{
		if (branch[i].Info == "=" && TryReadValue(branch[Max(0, i - 3)].Info, out _) && branch.Parent != null && (branch.Parent.Info == "if" || branch.Parent.Info == nameof(XorList) || branch.Parent.Info == nameof(Expr) && new List<String> { "xor", "or", "and", "^^", "||", "&&", "!" }.Contains(branch.Parent[Min(Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(x, branch)) + 1, 2), branch.Parent.Length - 1)].Info)))
			Add(ref errorsList, "Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": this expression, used with conditional constructions, is constant; maybe you wanted to check equality of these values? - it is done with the operator \"==\"");
		else if (branch[i].Info == "=" && branch[i - 1].Info == nameof(Hypername) && branch[Max(0, i - 3)] == branch[i - 1])
			Add(ref errorsList, "Warning in line " + lexems[branch[i].Pos].LineN.ToString() + " at position " + lexems[branch[i].Pos].Pos.ToString() + ": the variable is assigned to itself - are you sure this is not a mistake?");
		branch.Info = nameof(Assignment);
		if (branch[i - 2].Extra is not UniversalType SrcUnvType)
			SrcUnvType = NullType;
		if (branch[i - 1].Extra is not UniversalType DestUnvType)
			DestUnvType = NullType;
		var warning = false;
		if (branch[i].Info == "pow=" && TypesAreCompatible(SrcUnvType, RealType, out warning, innerResults[^2],
			out var adaptedSource, out _) && adaptedSource != null)
		{
			SrcUnvType = RealType;
			innerResults[^2] = ((String)"Pow(").AddRange(innerResults[^1]).AddRange(", ").AddRange(adaptedSource).Add(')');
		}
		if (!TypesAreCompatible(SrcUnvType, DestUnvType, out var warning2, innerResults[^2], out adaptedSource,
			out var extraMessage) || adaptedSource == null)
		{
			var otherPos = branch[i].Pos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": " + (extraMessage
				?? "cannot convert from type \"" + SrcUnvType + "\" to type \"" + DestUnvType + "\""
				+ (TypeEqualsToPrimitive(DestUnvType, "string")
				? " - use an addition of zero-length string for this" : "")));
			branch.Info = "default!";
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return "default!";
		}
		else if (warning || warning2)
		{
			var otherPos = branch[i].Pos;
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": " + (extraMessage ?? "conversion from type \""
				+ SrcUnvType + "\" to type \"" + DestUnvType + "\" is possible only in function return," +
				" not in the direct assignment and not in the call"));
			branch.Info = "default!";
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return "default!";
		}
		branch[i].Extra = DestUnvType;
		if (branch[i].Info == "pow=")
			return i < 2 ? branch[i].Info : innerResults[^1].Copy().AddRange(" = ").AddRange(adaptedSource);
		else if (branch[i].Info == "+=" && TypeEqualsToPrimitive(DestUnvType, "string"))
			return i < 2 ? branch[i].Info : innerResults[^1].Copy().AddRange(".AddRange(").AddRange(adaptedSource).Add(')');
		else
			return i < 2 ? branch[i].Info : innerResults[^1].Copy().Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(adaptedSource == "_" ? "default!" : adaptedSource);
	}

	private static String Ternary(TreeBranch branch, int i)
	{
		branch.Info = nameof(Ternary);
		if (branch[i].Info == ":" && i + 2 >= branch.Length)
			branch[i].Extra = branch.Elements.Filter((_, index) => index == i - 1 || index % 4 == 1).Convert(x => x.Extra is UniversalType ElemType ? ElemType : NullType).Progression((x, y) => GetResultType(x, y, "default!", "default!"));
		else
			branch[i].Extra = branch[i - 1].Extra is UniversalType UnvType ? UnvType : NullType;
		return branch[i].Info;
	}

	private static String CombineWithExpr(TreeBranch branch, List<String> innerResults, int i)
	{
		if (branch[i - 1].Extra is not UniversalType UnvType)
			UnvType = NullType;
		branch[i].Extra = UnvType;
		return innerResults[^1];
	}

	private String ListExpr(TreeBranch branch, ref List<String>? errorsList, int i)
	{
		var result = ParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
		AddRange(ref errorsList, innerErrorsList);
		return result;
	}

	private static String BinaryNotListExpr(TreeBranch branch, List<String> innerResults, int i)
	{
		if (branch[i - 2].Extra is not UniversalType UnvType1)
			UnvType1 = NullType;
		if (branch[i - 1].Extra is not UniversalType UnvType2)
			UnvType2 = NullType;
		branch[i].Extra = GetResultType(UnvType1, UnvType2, innerResults[^2], innerResults[^1]);
		if (innerResults[^2].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^2].Insert(0, '(').Add(')');
		if (innerResults[^1].ContainsAnyExcluding(AlphanumericCharacters))
			innerResults[^1].Insert(0, '(').Add(')');
		return i < 2 ? branch[i].Info : new String(innerResults[^2]).Add(' ').AddRange(branch[i].Info).Add(' ').AddRange(innerResults[^1]);
	}

	private String List(TreeBranch branch, out List<String>? errorsList)
	{
		String result = "(";
		List<String> innerResults = [];
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
				var innerResult = ParseAction(branch[i].Info)(branch[i], out var innerErrorsList);
				innerResults.Add(innerResult);
				result.AddRange(innerResult);
				AddRange(ref errorsList, innerErrorsList);
			}
		}
		if (branch.Info == nameof(List) && innerResults.Length != 0 && innerResults.All(x => x == "default!"))
		{
			branch.Extra = NullType;
			return "default!";
		}
		branch.Extra = new UniversalType(TupleBlockStack, new(branch.Elements.Convert(x => x.Extra is UniversalType UnvType ? (UniversalTypeOrValue)UnvType : throw new InvalidOperationException())));
		return result.Add(')');
	}

	private String XorList(TreeBranch branch, out List<String>? errorsList)
	{
		String result = "Universal.Xor(";
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
				result.AddRange(ParseAction(branch[i].Info)(branch[i], out var innerErrorsList));
				AddRange(ref errorsList, innerErrorsList);
			}
		}
		branch.Extra = branch.Elements.Progression(GetListType(BoolType), (x, y) =>
			GetResultType(x, GetListType(y.Extra is UniversalType UnvType ? UnvType : NullType), "default!", "default!"));
		return result.Add(')');
	}

	private String Return(TreeBranch branch, out List<String>? errorsList)
	{
		String result = [];
		errorsList = [];
		result.AddRange("return ");
		if (branch[0].Length == 0)
		{
			result.AddRange("default!;");
			return result;
		}
		var expr = Expr(branch[0], out var innerErrorsList);
		var otherPos = branch.FirstPos;
		if (!currentFunction.HasValue || branch[0].Extra is not UniversalType UnvTypeExpr)
			result.AddRange(expr == "_" ? "default!" : expr);
		else if (TypesAreEqual(currentFunction.Value.ReturnUnvType, NullType))
			return "return;";
		else if (!TypesAreCompatible(UnvTypeExpr, currentFunction.Value.ReturnUnvType, out var warning, expr,
			out var adapterExpr, out var extraMessage))
		{
			Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
				+ lexems[otherPos].Pos.ToString() + ": " + (extraMessage
				?? "incompatibility between type of the returning value \"" + UnvTypeExpr
				+ "\" and the function return type \"" + currentFunction.Value.ReturnUnvType + "\""
				+ (TypeEqualsToPrimitive(currentFunction.Value.ReturnUnvType, "string")
				? " - use an addition of zero-length string for this" : "")));
			result.AddRange("default!");
		}
		else
		{
			if (warning)
				Add(ref errorsList, "Warning in line " + lexems[otherPos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": " + (extraMessage ?? "type of the returning value \""
					+ UnvTypeExpr + "\" and the function return type \"" + currentFunction.Value.ReturnUnvType
					+ "\" are badly compatible, you may lost data"));
			result.AddRange(adapterExpr ?? "default!");
		}
		result.Add(';');
		AddRange(ref errorsList, innerErrorsList);
		return result;
	}

	private String Default(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		if (Universal.TryParse(branch.Info.ToString(), out var value))
		{
			branch.Extra = value.InnerType;
			return value.ToString(true, true);
		}
		if (branch.Length == 0)
			return branch.Info == "ClassMain" ? [] : branch.Info;
		String result = [];
		if (branch.Info.ToString() is "ref" or "out")
		{
			if (branch.Length != 1)
			{
				var otherPos = branch.FirstPos;
				Add(ref errorsList, "Error in line " + lexems[otherPos].LineN.ToString() + " at position "
					+ lexems[otherPos].Pos.ToString() + ": incorrect construction is passing with the \""
					+ branch.Info.ToString() + "\" keyword");
				return [];
			}
			result.AddRange(branch.Info).Add(' ').AddRange(Hypername(branch, out var innerErrorsList));
			AddRange(ref errorsList, innerErrorsList);
			return result;
		}
		if (branch.Info.StartsWith("Namespace "))
			result.Add('n').AddRange(branch.Info[1..]).Add('{');
		foreach (var x in branch.Elements)
		{
			var s = ParseAction(x.Info)(x, out var innerErrorsList);
			if (s.Length != 0)
				result.AddRange(s);
			AddRange(ref errorsList, innerErrorsList);
		}
		if (branch.Info.StartsWith("Namespace "))
			result.Add('}');
		if (!branch.Info.StartsWith("Namespace ") || IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private static String Wreck(TreeBranch branch, out List<String>? errorsList)
	{
		errorsList = [];
		return [];
	}

	private static String Type(UniversalType type)
	{
		String result = [];
		if (TypeEqualsToPrimitive(type, "list", false))
		{
			var levelsCount = type.ExtraTypes.Length == 1 ? 1 : int.TryParse(type.ExtraTypes[0].ToString(), out var n) ? n : 0;
			if (levelsCount == 0)
				result.AddRange(Type((type.ExtraTypes[^1].MainType.Type, type.ExtraTypes[^1].ExtraTypes)));
			else
			{
				result.AddRange(((String)"List<").Repeat(levelsCount - 1));
				var DotNetType = DeclaredConstructionMappings.TypeMapping(type.ExtraTypes[^1]);
				if (DotNetType == typeof(bool))
					result.AddRange(nameof(BitList));
				else
				{
					result.AddRange(DotNetType.IsUnmanaged() ? nameof(NList<bool>) : nameof(List<bool>)).Add('<');
					result.AddRange(Type((type.ExtraTypes[^1].MainType.Type, type.ExtraTypes[^1].ExtraTypes)));
					result.Add('>');
				}
				result.AddRange(((String)">").Repeat(levelsCount - 1));
			}
		}
		else if (TypeEqualsToPrimitive(type, "tuple", false))
		{
			if (type.ExtraTypes.Length == 0)
				return "void";
			var first = Type(new(type.ExtraTypes[0].MainType.Type, type.ExtraTypes[0].ExtraTypes));
			if (type.ExtraTypes.Length == 1)
				return first;
			using var innerResult = first.Copy();
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (!type.ExtraTypes[i].MainType.IsValue)
				{
					result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult);
					innerResult.Replace(Type(new(type.ExtraTypes[i].MainType.Type, type.ExtraTypes[i].ExtraTypes)));
					continue;
				}
				using var collection = String.Join(", ", RedStarLinq.FillArray(innerResult, int.TryParse(type.ExtraTypes[i].MainType.Value.ToString(), out var n) ? n : 1));
				if (i >= 2 && type.ExtraTypes[i - 1].MainType.IsValue)
					innerResult.Replace(((String)'(').AddRange(collection).Add(')'));
				else
					innerResult.Replace(collection);
			}
			result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult).Add(')');
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
			result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(type.MainType.FindLastIndex(x => x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface)) + 1)).ToShortString()));
			if (result == nameof(ListHashSet<int>) && type.ExtraTypes.Length == 1 && DeclaredConstructionMappings.TypeMapping(type.ExtraTypes[0]).IsUnmanaged())
				result.Insert(0, 'N');
			if (type.ExtraTypes.Length == 0)
				return result;
			result.Add('<');
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

	private static String TypeMapping(String typeName)
	{
		var after = typeName.GetAfter(((String)"System.Collections.").AddRange(nameof(G.LinkedList<bool>)));
		if (after.Length != 0)
			return "G.LinkedList" + after;
		after = typeName.GetAfter("System.Collections.");
		if (after.Length != 0)
			return after;
		return typeName;
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
				if ((branches[i][j].Info == nameof(Declaration) || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					var otherPos = branches[i][j].FirstPos;
					Add(ref errorsList, "Error in line " + lexems[branch.Pos].LineN.ToString() + " at position " + lexems[branch.Pos].Pos.ToString() + ": variable \"" + s + "\" is already defined in this location or in the location that contains this: line " + lexems[otherPos].LineN.ToString() + ", position " + lexems[otherPos].Pos.ToString());
					return true;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", "return", nameof(Expr), nameof(List), "Indexes", nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment), "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out var otherPos, out _) && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": variable \"" + s + "\" is already defined in this location or in the location that contains this in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
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
		var parent = branch;
		while (parent.Parent != null)
		{
			indexes.Add(parent.Parent.Elements.FindIndex(x => ReferenceEquals(parent, x)) + 1);
			branches.Add(parent = parent.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Info, out var functions, out _) && (functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
			{
				Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": variable \"" + s + "\" is not defined in this location; multiconst functions cannot use variables that are outside of the function");
				extra = null;
				return false;
			}
			for (var j = 0; j < indexes[i] - 1; j++)
			{
				if ((branches[i][j].Info == nameof(Declaration) || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s)
				{
					extra = branches[i][j][0].Extra;
					return true;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", nameof(Expr), nameof(List), "Indexes", nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment), "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out _, out var innerExtra))
				{
					extra = innerExtra;
					return true;
				}
			}
			for (var j = indexes[i]; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Info == nameof(Declaration) || branches[i][j].Info == "Parameter") && branches[i][j][1].Info == s)
				{
					var otherPos = branches[i][j].FirstPos;
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": one cannot use the local variable \"" + s + "\" before it is declared or inside such declaration in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
					extra = null;
					return false;
				}
				else if (new List<String> { "Parameters", "if", "if!", "else if", "else if!", "repeat", "while", "while!", "for", "return", nameof(Expr), nameof(List), "Indexes", nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment), "UnaryAssignment" }.Contains(branches[i][j].Info) && VariableExistsInsideExpr(branches[i][j], s, out var otherPos, out _))
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": one cannot use the local variable \"" + s + "\" before it is declared or inside such declaration in line " + lexems[otherPos].LineN.ToString() + " at position " + lexems[otherPos].Pos.ToString());
					extra = null;
					return false;
				}
			}
		}
		if (errorsList == null || errorsList.Length == 0)
			Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
		extra = null;
		return false;
	}

	private static bool VariableExistsInsideExpr(TreeBranch branch, String s, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Info == nameof(Declaration) || branch[i].Info == "Parameter") && branch[i][1].Info == s)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (new List<String> { nameof(Expr), nameof(List), "Indexes", nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment), "UnaryAssignment" }.Contains(branch[i].Info) && VariableExistsInsideExpr(branch[i], s, out pos, out extra))
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

	private bool IsPropertyDeclared(TreeBranch branch, String s, out List<String>? errorsList, out UserDefinedProperty? property)
	{
		errorsList = default!;
		if (!UserDefinedPropertyExists(branch.Container, s, out property, out _, out var inBase))
		{
			if (errorsList == null || errorsList.Length == 0)
				Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
			return false;
		}
		else if (inBase)
			return true;
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
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Info, out var functions, out _))
			{
				if ((functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": property \"" + s + "\" is not defined in this location; multiconst functions cannot use external properties");
					return false;
				}
				else if ((functions[^1].Attributes & FunctionAttributes.Static) != 0 && (property?.Attributes & PropertyAttributes.Static) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": property \"" + s + "\" cannot be used from this location; static functions cannot use non-static properties");
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Info == "Property" && branches[i][j].Length == 3 && branches[i][j][1].Info == s)
				{
					return true;
				}
				else if (new List<String> { "ClassMain", "Properties" }.Contains(branches[i][j].Info) && PropertyExistsInsideExpr(branches[i][j], s, out _, out var innerExtra))
				{
					return true;
				}
			}
		}
		if (errorsList == null || errorsList.Length == 0)
			Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
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
					pos = branch[i].FirstPos;
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

	private bool IsFunctionDeclared(TreeBranch branch, String s, out List<String>? errorsList,
		[MaybeNullWhen(false)] out GeneralMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out object? extra)
	{
		errorsList = default!;
		if (!UserDefinedNonDerivedFunctionExists(branch.Container, s, out functions, out matchingContainer))
		{
			if (errorsList == null || errorsList.Length == 0)
				Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
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
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Info, out var functions2, out _))
			{
				if ((functions2[^1].Attributes & FunctionAttributes.Multiconst) != 0 && (functions[^1].Attributes & FunctionAttributes.Multiconst) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": function \"" + s + "\" is not defined in this location; multiconst functions cannot call external non-multiconst functions");
					extra = null;
					return false;
				}
				else if ((functions2[^1].Attributes & FunctionAttributes.Static) != 0 && (functions[^1].Attributes & FunctionAttributes.Static) == 0)
				{
					Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": function \"" + s + "\" cannot be called from this location; static functions cannot call non-static functions");
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
			Add(ref errorsList, "Error in line " + lexems[branch.FirstPos].LineN.ToString() + " at position " + lexems[branch.FirstPos].Pos.ToString() + ": identifier \"" + s + "\" is not defined in this location");
		extra = null;
		return false;
	}

	private static String GetActualFunction(TreeBranch branch, out GeneralMethodOverloads functions,
		out BlockStack? matchingContainer)
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
			if (branches[i].Info == "Function" && branches[i].Length == 4 && UserDefinedNonDerivedFunctionExists(branch.Container, branches[i][0].Info, out var functions2, out matchingContainer))
			{
				functions = functions2;
				return branches[i][0].Info;
			}
		functions = [];
		matchingContainer = null;
		return [];
	}

	public static void Add<T>(ref List<T>? list, T item)
	{
		list ??= [];
		list.Add(item);
	}

	public static void AddRange<T>(ref List<T>? list, G.IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			list ??= [];
			list.AddRange(collection);
		}
	}

	private static bool IsTypeContext(TreeBranch branch) => branch.Container.TryPeek(out var nearestBlock) && nearestBlock.BlockType is BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface;

	public static String ExecuteProgram(String program, out String errors, params dynamic?[] args) => TranslateAndExecuteProgram(program, out errors, out _, args);

	public static String TranslateAndExecuteProgram(String program, out String errors, out Assembly? assembly, params dynamic?[] args)
	{
		List<String>? errorsList = [];
		try
		{
			ClearUserDefinedLists();
			var translated = TranslateProgram(program);
			AddRange(ref errorsList, translated.errorsList);
			return ExecuteProgram(translated, out errors, out assembly, args);
		}
		catch (OutOfMemoryException)
		{
			Add(ref errorsList, "Memory limit exceeded during compilation, translation or execution; program has not been executed\r\n");
			errors = String.Join("\r\n", errorsList?.Append([]) ?? []);
			assembly = null;
			return "null";
		}
		catch
		{
			Add(ref errorsList, "A serious error occurred during compilation, translation or execution; program has not been executed\r\n");
			errors = String.Join("\r\n", errorsList?.Append([]) ?? []);
			assembly = null;
			return "null";
		}
	}

	private static void ClearUserDefinedLists()
	{
		ExplicitlyConnectedNamespacesList.Clear();
		UserDefinedConstantsList.Clear();
		UserDefinedConstructorsList.Clear();
		UserDefinedConstructorIndexesList.Clear();
		UserDefinedFunctionsList.Clear();
		UserDefinedImplementedInterfacesList.Clear();
		UserDefinedIndexersList.Clear();
		UserDefinedNamespacesList.Clear();
		UserDefinedPropertiesList.Clear();
		UserDefinedPropertiesMapping.Clear();
		UserDefinedPropertiesOrder.Clear();
		UserDefinedTypesList.Clear();
		VariablesList.Clear();
	}

	public static (String s, List<String>? errorsList, String translatedClasses) TranslateProgram(String program)
	{
		var s = new SemanticTree((LexemStream)new CodeSample(program)).Parse(out var errorsList, out var translatedClasses);
		return (s, errorsList, translatedClasses);
	}

	public static String ExecuteProgram((String s, List<String>? errorsList, String translatedClasses) translated, out String errors, out Assembly? assembly, dynamic?[] args)
	{
		var (bytes, errorsList) = CompileProgram(translated);
		assembly = EasyEval.GetAssembly(bytes);
		var result = assembly?.GetType("Program")?.GetMethod("F")?.Invoke(null, [args]) ?? null;
		errors = errorsList == null || errorsList.Length == 0 ? "Ошибок нет" : String.Join("\r\n", errorsList.Append([]));
		return result is null ? "null" : JsonConvert.SerializeObject(result, SerializerHelpers.SerializerSettings);
	}

	public static byte[] CompileProgram(String program)
	{
		try
		{
			ClearUserDefinedLists();
			var translated = TranslateProgram(program);
			return CompileProgram(translated).Bytes;
		}
		catch
		{
			return [];
		}
	}

	public static List<(string Name, byte[] Bytes)> GetNecessaryDependencies(List<string> assemblyArray)
	{
		//var assemblyArray = new[] { "NStar.Core", "Microsoft.CSharp", "mscorlib", "Mpir.NET", "netstandard",
		//	"System", "System.Console", "System.Core", "System.Linq.essionsExpr", "System.Private.CoreLib",
		//	"System.Runtime", "HighLevelAnalysis.Debug", "LowLevelAnalysis", "MidLayer", "Core", "EasyEval" };
		var thisList = assemblyArray.ToList(GetAssemblyAsBytes);
		var depencencyList = assemblyArray.ConvertAndJoin(x => Assembly.Load(x).GetReferencedAssemblies().ToArray(x => x.Name ?? throw new NotSupportedException())).ToHashSet().ToList();
		if (depencencyList.Length == 0)
			return thisList;
		return thisList.AddRange(GetNecessaryDependencies(depencencyList));
		static (string Name, byte[] Bytes) GetAssemblyAsBytes(string assemblyName) => (assemblyName, File.ReadAllBytes(Assembly.Load(assemblyName).Location));
	}

	private static (byte[] Bytes, List<String> ErrorsList) CompileProgram((String s, List<String>? errorsList, String translatedClasses) translated)
	{
		var sb = new StringBuilder();
		var translateErrors = new StringWriter(sb);
		var (s, errorsList, translatedClasses) = translated;
		var bytes = EasyEval.Compile(((String)@"using Mpir.NET;
using NHashSets;
using NStar.BufferLib;
using NStar.Core;
using NStar.Dictionaries;
using NStar.Linq;
using NStar.MathLib;
using NStar.RemoveDoubles;
using NStar.SumCollections;
using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using G = System.Collections.Generic;
using static NStar.Core.Extents;
using static CSharp.NStar.").AddRange(nameof(Quotes)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(IntermediateFunctions)).AddRange(@";
using static System.Math;
using String = NStar.Core.String;
using CSharp.NStar;
using static NStar.EasyEvalLib.EasyEval;
using static CSharp.NStar.SemanticTree;
").AddRange(translatedClasses).AddRange(@"
public static class Program
{
public static dynamic? F(params dynamic?[] args)
{
").AddRange(s).AddRange(@"
return null;
}

public static void Main(string[] args)
{
Console.WriteLine(F(args));
}
}
"), ["HighLevelAnalysis", "LowLevelAnalysis", "DeclaredConstructionHelpers", "DeclaredConstructions", "QuotesAndTreeBranch", "TypeHelpers", "Universal", "NStar.EasyEval"], translateErrors);
		if (bytes == null || bytes.Length <= 2 || sb.ToString() != "Compilation done without any error.\r\n")
			throw new EvaluationFailedException();
		return (bytes, errorsList ?? []);
	}

	private static bool TryReadValue(String s, out Universal value) => Universal.TryParse(s.ToString(), out value) || s.StartsWith("(String)") && Universal.TryParse(s["(String)".Length..].ToString(), out value);

	public override string ToString() => $"({String.Join(", ", lexems.ToArray(x => (String)x.ToString())).TakeIntoVerbatimQuotes()}, {input.TakeIntoVerbatimQuotes()}, {((String)topBranch.ToString()).TakeIntoVerbatimQuotes()}, ({(errorsList != null && errorsList.Length != 0 ? String.Join(", ", errorsList.ToArray(x => x.TakeIntoVerbatimQuotes())) : "NoErrors")}), {wreckOccurred})";
}

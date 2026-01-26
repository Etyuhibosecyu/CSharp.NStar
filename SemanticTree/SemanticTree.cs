global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.EasyEvalLib;
global using NStar.Linq;
global using NStar.MathLib;
global using NStar.MathLib.Extras;
global using System;
global using System.Diagnostics;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.MemberChecks;
global using static CSharp.NStar.MemberConverters;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.NStarUtilityFunctions;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using Mpir.NET;
using Newtonsoft.Json;
using Nito.AsyncEx;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharp.NStar;

public sealed partial class SemanticTree
{
	private readonly List<Lexem> lexems;
	private readonly String input;
	private readonly TreeBranch topBranch;
	private readonly List<String>? errors;
	private bool wreckOccurred, noAddAsync = true, containsAsync;
	private readonly String compiledClasses = [];
	private UserDefinedMethodOverload? currentFunction;
	private int constantsDepth, indentationUnits;
	private readonly Dictionary<String, String> prepassClasses = [];
	private readonly ListHashSet<String> nestedPrepassClasses = [];
	private readonly Dictionary<NStarType, String> parsedTypes = [];
	private readonly Dictionary<(NStarType, TreeBranch), (bool flowControl, String value)> parsedUserConstructors = [];
	private readonly Dictionary<String, ListHashSet<String>> functionReferences = [];

	private static readonly string AlphanumericCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.";
	private static readonly ImmutableArray<string> ExprTypes = [nameof(Expr), nameof(List), nameof(Lambda), nameof(SwitchExpr),
		nameof(Indexes), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), nameof(XorList), "StringConcatenation",
		nameof(Assignment), "DeclarationAssignment", "UnaryAssignment", nameof(Declaration), nameof(Hypername), nameof(Index),
		nameof(Range)];
	private static readonly ImmutableArray<string> ArithmeticExprTypes = [nameof(Expr), nameof(PMExpr), nameof(MulDivExpr),
		"StringConcatenation"];
	private static readonly ImmutableArray<string> BoolOperators = ["xor", "or", "and", "^^", "||", "&&", "!"];
	private static readonly ImmutableArray<string> BranchOpeners = ["if", "else if", "if!", "else if!", "else",
		"loop", "loop-while", "loop-while!", "while", "while!", "repeat", "for"];
	private static readonly ImmutableArray<string> ExprTypesToSearchDeeper = [nameof(Expr), nameof(List), nameof(Indexes),
		nameof(Call), nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment),
		"DeclarationAssignment", "UnaryAssignment"];
	private static readonly ImmutableArray<string> BranchesToSearchDeeper = [nameof(Parameters), "if", "if!",
		"else if", "else if!", "repeat", "while", "while!", "for", "return", .. ExprTypesToSearchDeeper];
	private static readonly ImmutableArray<string> BranchesToSearchDeeperNoReturn = [nameof(Parameters), "if", "if!", "else if",
		"else if!", "repeat", "while", "while!", "for", nameof(Expr), nameof(List), nameof(Indexes), nameof(Call),
		nameof(Ternary), nameof(PMExpr), nameof(MulDivExpr), "StringConcatenation", nameof(Assignment),
		"DeclarationAssignment", "UnaryAssignment"];
	private static List<Lexem>? lastLexems;

	public SemanticTree(List<Lexem> lexems, String input, TreeBranch topBranch,
		List<String>? errors, bool wreckOccurred)
	{
		lastLexems = this.lexems = lexems;
		this.input = input;
		this.topBranch = topBranch;
		this.errors = errors;
		this.wreckOccurred = wreckOccurred;
	}

	public SemanticTree((List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList,
		bool WreckOccurred) x) : this(x.Lexems, x.String, x.TopBranch, x.ErrorsList, x.WreckOccurred)
	{
	}

	public SemanticTree(LexemStream lexemStream) : this(lexemStream.Parse())
	{
	}

	public static String ExecuteStringPrefix { get; } = "list() dynamic args = null;";

	public static String ExecuteStringPrefixCompiled
	{
		get
		{
			if (field != null)
				return field;
			return field = new SemanticTree((LexemStream)new CodeSample(ExecuteStringPrefix)).Parse(out _, out _);
		}
	}

	public String Parse(out List<String>? errors, out String compiledClasses)
	{
		List<String>? innerErrors = [];
		if (wreckOccurred)
		{
			errors = this.errors;
			compiledClasses = [];
			return [];
		}
		try
		{
			var result = ParseAction(topBranch.Name)(topBranch, out innerErrors);
			errors = this.errors;
			AddRange(ref errors, innerErrors);
			compiledClasses = this.compiledClasses;
			return wreckOccurred ? [] : result;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			const string errorMessage = "Technical wreck F000 in unknown line at unknown position:" +
				" compilation failed because of internal compiler error";
			Add(ref innerErrors, errorMessage + @" (see %TEMP%\CSharp.NStar.log for details)");
			var targetLexem = lexems[Max(TreeBranch.LastTreePos, 0)];
			File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
				+ @"\CSharp.NStar.log", [errorMessage, "The last visited location was: line " + targetLexem.LineN
				+ ", position " + targetLexem.Pos, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? "null",
					"The underlying internal exception message was:", ex.InnerException?.Message ?? "null"]);
			errors = innerErrors;
			compiledClasses = [];
			wreckOccurred = true;
			return [];
		}
	}

	private delegate String ParseActionDelegate(TreeBranch branch, out List<String>? errors);

	private ParseActionDelegate ParseAction(String branchName) => wreckOccurred ? Wreck : branchName.ToString() switch
	{
		nameof(Main) => Main,
		nameof(Class) => Class,
		nameof(Function) => Function,
		nameof(Constructor) => Constructor,
		nameof(Members) => Members,
		nameof(Constant) => Constant,
		"if" or "else if" or "if!" or "else if!" => Condition,
		"loop" or "loop-while" or "loop-while!" => Loop,
		"while" => While,
		"repeat" => Repeat,
		"for" => For,
		nameof(Declaration) => Declaration,
		nameof(Hypername) => Hypername,
		nameof(List) => List,
		"xorList" => XorList,
		nameof(Lambda) => Lambda,
		nameof(SwitchExpr) => SwitchExpr,
		"typeof" => Typeof,
		"return" => Return,
		_ when ExprTypes.Contains(branchName.ToString()) => Expr,
		_ => Default,
	};

	private String Main(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		if (branch.Length != 1 && branch.Parent != null && branch.Parent.Name == nameof(Lambda))
			result.Add('{');
		var initialExtra = branch.Extra != null;
		var thisBlockReturns = false;
		var conditionReturns = false;
		var nestedConditions = 0;
		NStarType? extraToReturn = null;
		for (var i = 0; i < branch.Length; i++)
		{
			var x = branch[i];
			var xName = x.Name.ToString();
			if (thisBlockReturns)
			{
				GenerateMessage(ref errors, 0x8005, branch[i].Pos);
				break;
			}
			var indentationUnits = this.indentationUnits;
			this.indentationUnits += (i != 0 && BranchOpeners.Contains(branch[i - 1].Name.ToString())
				|| xName == nameof(Main) && branch.Length != 1 && x.Container == branch.Container)
				&& !(xName == nameof(Main) && x[0].Name.AsSpan() is "if" or "if!" && lexems[x[0].Pos].String == "while")
				? 1 : 0;
			if (this.indentationUnits > 5)
			{
				this.indentationUnits = indentationUnits;
				GenerateMessage(ref errors, 0x9017, branch.Pos);
				wreckOccurred = true;
				return [];
			}
			else if (CreateVar(this.indentationUnits - lexems[x.Pos].Pos, out var indentsBalance) > 0
				&& !(xName == "break" && lexems[x.Pos].String == "}"))
				GenerateMessage(ref errors, 0x800D, branch.Pos);
			else if (indentsBalance < 0 && !(x.Pos != 0 && lexems[x.Pos - 1].LineN == lexems[x.Pos].LineN))
				GenerateMessage(ref errors, 0x800E, branch.Pos);
			var parsedSubbranch = ParseAction(x.Name)(x, out var innerErrors);
			this.indentationUnits = indentationUnits;
			if (xName is nameof(Main) or "return")
			{
				if (!extraToReturn.HasValue && x.Extra != null)
					extraToReturn ??= (NStarType)x.Extra!;
				else if (!extraToReturn.HasValue || x.Extra is not NStarType ReturnNStarType) { }
				else if (TypesAreCompatible(ReturnNStarType, extraToReturn.Value,
					out var warning, parsedSubbranch, out var destExpr, out _) && !warning && destExpr != null)
					parsedSubbranch = destExpr;
				else if (!initialExtra && TypesAreCompatible(extraToReturn.Value, ReturnNStarType, out warning,
					parsedSubbranch.Copy(), out destExpr, out _) && !warning && destExpr == parsedSubbranch)
					extraToReturn = ReturnNStarType;
				else
				{
					GenerateMessage(ref errors, 0x4015, branch[i].Pos, extraToReturn.Value, ReturnNStarType);
					break;
				}
			}
			if (BranchOpeners.Contains(xName))
				nestedConditions++;
			if (xName is nameof(Main) or "return" && x.Extra is NStarType)
			{
				if (i != 0 && branch[i - 1].Name.AsSpan() is "if" or "if!"
					or "loop-while" or "loop-while!" or "while" or "while!" or "repeat" or "for")
					conditionReturns = true;
				else if (i == 0 || branch[i - 1].Name == "else" && nestedConditions <= 1 && conditionReturns
					|| branch[i - 1].Name == "loop" && nestedConditions <= 1
					|| branch[i - 1].Name.AsSpan() is not ("else if" or "else if!") && nestedConditions <= 0)
					thisBlockReturns = true;
			}
			if (i != 0 && branch[i - 1].Name.AsSpan() is "else if" or "else if!" && x.Extra is not NStarType)
				conditionReturns = false;
			if (i != 0 && BranchOpeners.Contains(branch[i - 1].Name.ToString()))
				nestedConditions--;
			if (branch.Length == 1 && branch.Parent != null && branch.Parent.Name == nameof(Lambda)
				&& parsedSubbranch.StartsWith("return ") && !parsedSubbranch[..^1].Contains(';'))
				parsedSubbranch.Remove(0, "return ".Length).RemoveEnd(^1);
			if (x.Length == 0 || parsedSubbranch.Length != 0)
			{
				if (branch.Name == "Main" && x.Name == "Main" && x.Length != 1 && parsedSubbranch.Length != 0
					&& parsedSubbranch[..^1].Contains(';'))
					result.Add('{');
				if (parsedSubbranch.AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
					parsedSubbranch = [];
				if (parsedSubbranch.StartsWith('(') && ExprTypes.Contains(xName) && xName
					is not (nameof(Assignment) or "DeclarationAssignment"))
					parsedSubbranch.Insert(0, "_ = ");
				result.AddRange(parsedSubbranch);
				if (parsedSubbranch.Length != 0
					&& parsedSubbranch[^1] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_')
					result.Add(' ');
				if (parsedSubbranch.Length == 0 || ExprTypes.Contains(xName) && !parsedSubbranch.EndsWith(';')
					|| xName is "continue" or "break")
					result.Add(';');
				if (branch.Name == "Main" && x.Name == "Main" && x.Length != 1 && parsedSubbranch.Length != 0
					&& parsedSubbranch[..^1].Contains(';'))
					result.Add('}');
			}
			if (i != 0 && branch[i - 1].Name.AsSpan() is "loop-while" or "loop-while!")
			{
				result.AddRange(branch[i - 1].Name == "loop-while" ? "while (" : "while (!(");
				parsedSubbranch = ParseAction(branch[i - 1][0].Name)(branch[i - 1][0], out innerErrors);
				if (parsedSubbranch.Length != 0)
				{
					result.AddRange(parsedSubbranch);
					AddRange(ref errors, innerErrors);
				}
				if (branch[i - 1].Name.EndsWith('!'))
					result.Add(')');
				result.AddRange(");");
			}
			if (innerErrors != null)
				AddRange(ref errors, innerErrors);
		}
		if (thisBlockReturns)
			branch.Extra ??= extraToReturn;
		else if (branch.Parent != null && branch.Parent.Name.AsSpan() is not (nameof(Constructor) or nameof(Main))
			&& !(branch.Extra is NStarType ThisBlockNStarType && ThisBlockNStarType.Equals(NullType))
			&& !(branch.Parent.Name == nameof(Function) && currentFunction.HasValue
			&& currentFunction.Value.ReturnNStarType.Equals(NullType))
			&& !branch.Parent.Name.StartsWith("Namespace "))
		{
			GenerateMessage(ref errors, 0x402A, branch.Pos);
			return branch.Length == 1 && branch.Parent != null && branch.Parent.Name == nameof(Lambda)
				? "default!" : "return default!;";
		}
		if (branch.Length != 1 && branch.Parent != null && branch.Parent.Name == nameof(Lambda))
			result.Add('}');
		return result;
	}

	private String Class(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		var name = branch[0].Name;
		if (prepassClasses.TryGetValue(name, out var pass) && pass.StartsWith("UNPASSED"))
		{
			try
			{
				return pass["UNPASSED".Length..];
			}
			finally
			{
				prepassClasses[name] = "PASSED";
			}
		}
		String result = [];
		var (Restrictions, Attributes, BaseType, _) = UserDefinedTypes[(branch.Container, name)];
		if ((Attributes & TypeAttributes.Private) != 0)
			result.AddRange("private ");
		if ((Attributes & TypeAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & TypeAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if ((Attributes & (TypeAttributes.Private | TypeAttributes.Protected | TypeAttributes.Internal)) == 0)
			result.AddRange("public ");
		var @static = (Attributes & TypeAttributes.Static) == TypeAttributes.Static;
		if (@static)
			result.AddRange("static ");
		else if ((Attributes & TypeAttributes.Abstract) != 0)
			result.AddRange("abstract ");
		else if ((Attributes & TypeAttributes.Sealed) != 0)
			result.AddRange("sealed ");
		result.AddRange("class ");
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name);
		var (TypeIndexes, OtherIndexes) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.Equals(RecursiveType));
		if (TypeIndexes.Length != 0)
			result.Add('<').AddRange(String.Join(", ", TypeIndexes.ToArray(x => Restrictions[x].Name))).Add('>');
		if (!@static)
		{
			result.AddRange(" : ");
			if (TypeIsPrimitive(BaseType.MainType))
				result.AddRange((String)"IClass");
			else
				result.AddRange(Type(ref BaseType, branch[0], ref errors));
		}
		result.Add('{');
		for (var i = 0; i < OtherIndexes.Length; i++)
		{
			var x = OtherIndexes[i];
			var RestrictionType = Restrictions[x].RestrictionType;
			result.AddRange("public ").AddRange(Type(ref RestrictionType, branch[0], ref errors));
			result.Add(' ').AddRange(Restrictions[x].Name).AddRange(" { get; init; }");
		}
		BlockStack fullContainer = new(branch.Container.Append(new(BlockType.Class, name, 1)));
		var properties = GetAllProperties(branch[^1].Container);
		var UnsetRequiredProperties = UserDefinedConstructors[fullContainer]
			.FindLast(x => x.Parameters.Equals(properties,
			(x, y) => x.Type.Equals(y.Value.NStarType) && x.Name == y.Key)).UnsetRequiredProperties;
		UnsetRequiredProperties?.Replace(new Chain(Restrictions.Length)).ExceptWith(TypeIndexes);
		if (!@static && !(branch[^1].Name == "ClassMain" && branch[^1].Length != 0
			&& branch[^1].Elements.Any(x => x.Name == "Members")))
		{
			String paramsResult = [], baseResult = [];
			foreach (var property in properties)
				PropertiesConstructor(branch[^1], paramsResult, baseResult, property, ref errors);
			result.AddRange("public ").AddRange(name).Add('(').AddRange(paramsResult);
			if (!TypeEqualsToPrimitive(BaseType, "null"))
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){}");
		}
		UserDefinedConstructors[fullContainer][0].UnsetRequiredProperties.Replace(new Chain(Restrictions.Length))
			.ExceptWith(TypeIndexes);
		var indentationUnits = this.indentationUnits;
		this.indentationUnits++;
		result.AddRange(ParseAction(branch[^1].Name)(branch[^1], out var coreErrors).Add('}'));
		this.indentationUnits = indentationUnits;
		AddRange(ref errors, coreErrors);
		if (IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private void PrepassClass(TreeBranch branch, out List<String>? errors, List<String> typeNames)
	{
		Debug.Assert(typeNames.Length != 0);
		var fullName = String.Join('.', typeNames);
		if (prepassClasses.TryGetValue(fullName, out var pass) && pass == "PASSED")
		{
			errors = null;
			return;
		}
		if (nestedPrepassClasses.Contains(fullName))
		{
			errors = null;
			GenerateMessage(ref errors, 0x4063, branch.Pos, nestedPrepassClasses[0]);
			return;
		}
		List<int> indexes = [];
		var preservedBranch = branch;
		List<TreeBranch> branches = [branch];
		var parent = branch;
		while (parent.Parent != null)
		{
			indexes.Add(parent.Parent.Elements.FindIndex(x => ReferenceEquals(parent, x)) + 1);
			branches.Add(parent = parent.Parent);
		}
		indexes.Reverse();
		branches.Reverse();
		var typeNamesIndex = 0;
		PrepassClassInitial(ref branch, branches, indexes, typeNames[0]);
		while (typeNamesIndex != typeNames.Length - 1)
		{
			typeNamesIndex++;
			PrepassClassIteration(ref branch, typeNames[typeNamesIndex]);
		}
		if (branch.IsAncestorOf(preservedBranch))
		{
			errors = null;
			GenerateMessage(ref errors, 0x4063, preservedBranch.Pos, nestedPrepassClasses[0]);
			return;
		}
		nestedPrepassClasses.Add(fullName);
		var indentationUnits = this.indentationUnits;
		this.indentationUnits = 0;
		prepassClasses[typeNames[^1]] = Class(branch, out errors).Insert(0, "UNPASSED");
		this.indentationUnits = indentationUnits;
		nestedPrepassClasses.RemoveAt(^1);
	}

	private void PrepassClassInitial(ref TreeBranch branch, List<TreeBranch> branches, List<int> indexes, String typeName)
	{
		for (var i = indexes.Length - 1; i >= 0; i--)
		{
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (branches[i][j].Name == nameof(Class) && branches[i][j][0].Name == typeName)
				{
					branch = branches[i][j];
					return;
				}
			}
		}
		throw new InvalidOperationException();
	}

	private void PrepassClassIteration(ref TreeBranch branch, String typeName)
	{
		for (var i = 0; i < branch[^1].Length; i++)
		{
			if (branch[^1][i].Name == nameof(Class) && branch[^1][i][0].Name == typeName)
			{
				branch = branch[^1][i];
				return;
			}
		}
		throw new InvalidOperationException();
	}

	private void PropertiesConstructor(TreeBranch branch, String paramsResult, String coreResult,
		G.KeyValuePair<String, UserDefinedProperty> property, ref List<String>? errors)
	{
		if (coreResult.Length != 0)
		{
			paramsResult.AddRange(", ");
			coreResult.AddRange(", ");
		}
		var NStarType = property.Value.NStarType;
		var typeName = Type(ref NStarType, branch, ref errors);
		paramsResult.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(property.Key))
		{
			paramsResult.Add('@');
			coreResult.Add('@');
		}
		paramsResult.AddRange(property.Key).AddRange(" = default!");
		coreResult.AddRange(property.Key);
	}

	private String Function(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		var container = branch.Container;
		var name = branch[0].Name;
		var start = branch.Pos;
		var index = UserDefinedFunctionIndexes[container][start];
		var t = UserDefinedFunctions[branch.Container][name][index];
		var (RealName, _, ReturnNStarType, Attributes, Parameters) = UserDefinedFunctions[branch.Container][name][index];
		if ((Attributes & FunctionAttributes.Wrong) != 0 || name.StartsWith('?'))
			return "";
		if ((Attributes & FunctionAttributes.Private) != 0)
			result.AddRange("private ");
		if ((Attributes & FunctionAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & FunctionAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch) && (Attributes
			& (FunctionAttributes.Private | FunctionAttributes.Protected | FunctionAttributes.Internal)) == 0)
			result.AddRange("public ");
		if ((Attributes & FunctionAttributes.Static) != 0)
			result.AddRange("static ");
		else if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
		{
			if (UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Abstract) == 0)
			{
				GenerateMessage(ref errors, 0x400A, branch.Pos);
				return [];
			}
			result.AddRange("abstract ");
		}
		else if (branch.Container.Length == 0 || branch.Container.Peek().BlockType
			is not (BlockType.Class or BlockType.Struct or BlockType.Interface)) { }
		else if (!(UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& !TypeEqualsToPrimitive(userDefinedType.BaseType, "null")
			&& UserDefinedFunctionExists(userDefinedType.BaseType, name,
			[.. Parameters.Convert(x => x.Type)], out var baseFunctions) && baseFunctions.Length != 0
			&& CreateVar(baseFunctions.Find(x => (Parameters, x.Parameters).Combine().All(y =>
			y.Item1.Type.Equals(y.Item2.Type))), out var baseFunction) != default!))
			result.AddRange((userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Sealed ? "" : "virtual ");
		else if (ReturnNStarType.Equals(baseFunction.ReturnNStarType)
			&& (Attributes & (FunctionAttributes.Static | FunctionAttributes.Private | FunctionAttributes.Protected
			| FunctionAttributes.Internal | FunctionAttributes.Const | FunctionAttributes.Multiconst))
			== (baseFunction.Attributes & (FunctionAttributes.Static | FunctionAttributes.Private
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
				GenerateMessage(ref errors, 0x8008, branch.Pos, t.RealName);
			result.AddRange("new " + ((userDefinedType.Attributes & TypeAttributes.Static)
				== TypeAttributes.Sealed ? "" : "virtual "));
		}
		var asyncAdded = false;
		var asyncInsertionPos = result.Length;
		if (TaskBlockStacks.Contains(ReturnNStarType.MainType) && !result.EndsWith("abstract "))
		{
			result.AddRange("async ");
			asyncAdded = true;
		}
		var targetBranch = branch.Length == 4 ? branch[3] : branch;
		result.AddRange(Type(ref ReturnNStarType, targetBranch, ref errors)).Add(' ');
		if (EscapedKeywords.Contains(t.RealName))
			result.Add('@');
		result.AddRange(t.RealName).Add('(');
		result.AddRange(this.Parameters(targetBranch, Parameters, out var parametersErrors)).Add(')');
		if ((Attributes & FunctionAttributes.New) == FunctionAttributes.Abstract)
			return result.Add(';');
		result.Add('{');
		AddRange(ref errors, parametersErrors);
		var currentFunction = this.currentFunction;
		this.currentFunction = t;
		if (branch.Length == 4)
		{
			var indentationUnits = this.indentationUnits;
			this.indentationUnits++;
			var noAddAsync = this.noAddAsync;
			this.noAddAsync = !asyncAdded;
			var containsAsync = this.containsAsync;
			this.containsAsync = false;
			result.AddRange(ParseAction(branch[3].Name)(branch[3], out var coreErrors));
			if (containsAsync && !asyncAdded)
				result.Insert(asyncInsertionPos, "async ");
			this.containsAsync = containsAsync;
			this.noAddAsync = noAddAsync;
			this.indentationUnits = indentationUnits;
			AddRange(ref errors, coreErrors);
		}
		result.Add('}');
		this.currentFunction = currentFunction;
		return result;
	}

	private String Constructor(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		var parameterTypes = GetParameterTypes(branch[0]);
		UserDefinedType userDefinedType = default!;
		if (UserDefinedTypes.TryGetValue(SplitType(branch.Container), out userDefinedType)
			&& parameterTypes.Length != 0 && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			return [];
		var (Attributes, Parameters, UnsetRequiredProperties) = UserDefinedConstructors[branch.Container]
			.FindLast(x => x.Parameters.Equals(parameterTypes, (x, y) => x.Name == y.Name && x.Type.Equals(y.Type)));
		var (TypeIndexes, _) = new Chain(userDefinedType.Restrictions.Length).BreakFilter(index =>
			!userDefinedType.Restrictions[index].Package
			&& userDefinedType.Restrictions[index].RestrictionType.Equals(RecursiveType));
		UnsetRequiredProperties?.Replace(new Chain(userDefinedType.Restrictions?.Length ?? 0)).ExceptWith(TypeIndexes);
		if ((Attributes & ConstructorAttributes.Private) != 0)
			result.AddRange("private ");
		if ((Attributes & ConstructorAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & ConstructorAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if ((Attributes & (ConstructorAttributes.Private | ConstructorAttributes.Protected | ConstructorAttributes.Internal))
			== 0)
			result.AddRange("public ");
		if ((Attributes & ConstructorAttributes.Static) != 0)
			result.AddRange("static ");
		if ((Attributes & ConstructorAttributes.Abstract) != 0)
		{
			result.AddRange("abstract ");
			GenerateMessage(ref errors, 0x9012, branch.Pos);
			wreckOccurred = true;
		}
		var name = branch.Container.Peek().Name;
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name).Add('(');
		result.AddRange(this.Parameters(branch[^1], parameterTypes, out var parametersErrors));
		AddRange(ref errors, parametersErrors);
		var currentFunction = this.currentFunction;
		this.currentFunction = new([], [], NullType, FunctionAttributes.None, parameterTypes);
		result.AddRange("){");
		if (branch[^1].Name == "Main")
		{
			var indentationUnits = this.indentationUnits;
			this.indentationUnits++;
			var noAddAsync = this.noAddAsync;
			this.noAddAsync = true;
			result.AddRange(ParseAction(branch[^1].Name)(branch[^1], out var coreErrors));
			this.noAddAsync = noAddAsync;
			this.indentationUnits = indentationUnits;
			AddRange(ref errors, coreErrors);
		}
		result.Add('}');
		this.currentFunction = currentFunction;
		return result;
	}

	private ExtendedMethodParameters GetParameterTypes(TreeBranch branch) => [.. branch.Elements.Convert(GetParameterData)];

	private ExtendedMethodParameter GetParameterData(TreeBranch branch)
	{
		if (!(branch[0].Name == "type" && branch[0].Extra is NStarType ParameterNStarType
			&& (branch.Length == 3 && branch[2].Name == "no optional"
			|| branch.Length == 4 && branch[2].Name == "optional" && ExprTypes.Contains(branch[3].Name.ToString()))
			&& branch.Extra is ParameterAttributes Attributes))
			throw new InvalidOperationException();
		return new(ParameterNStarType, branch[1].Name, Attributes, ParseAction(branch[^1].Name)(branch[^1], out _));
	}

	private String Parameters(TreeBranch branch, ExtendedMethodParameters parameters, out List<String>? errors)
	{
		String result = [];
		errors = null;
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
			var ParameterNStarType = parameters[i].Type;
			result.AddRange(Type(ref ParameterNStarType, branch, ref errors));
			if ((parameters[i].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params)
				result.Add('>');
			result.Add(' ');
			var name = parameters[i].Name;
			if (EscapedKeywords.Contains(name))
				result.Add('@');
			result.AddRange(name);
			if (parameters[i].DefaultValue.AsSpan() is not ("" or "no optional"))
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

	private String Members(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (NStarEntity.TryParse(branch.Name.ToString(), out var value))
			return value.ToString(true, true);
		if (branch.Length == 0)
			return branch.Name;
		String result = [], paramsResult = [], baseResult = [], coreResult = [];
		if (UserDefinedTypes.TryGetValue(SplitType(branch.Container), out var userDefinedType)
			&& CreateVar(GetAllProperties(userDefinedType.BaseType.MainType), out var properties).Length != 0)
			foreach (var property in properties)
				PropertiesConstructor(branch, paramsResult, baseResult, property, ref errors);
		foreach (var x in branch.Elements)
		{
			List<String>? innerErrors;
			if (x.Name != nameof(Property))
			{
				result.AddRange(ParseAction(x.Name)(x, out innerErrors));
				AddRange(ref errors, innerErrors);
				continue;
			}
			var parsedSubbranch = Property(x, out innerErrors, out var constructorTop, out var constructorCore);
			if (parsedSubbranch.Length != 0)
			{
				result.AddRange(parsedSubbranch);
				AddRange(ref errors, innerErrors);
			}
			if (constructorTop.Length != 0)
			{
				if (paramsResult.Length != 0)
					paramsResult.AddRange(", ");
				paramsResult.AddRange(constructorTop);
			}
			coreResult.AddRange(constructorCore);
		}
		if ((userDefinedType.Attributes & TypeAttributes.Static) != TypeAttributes.Static
			&& (!UserDefinedConstructors.TryGetValue(branch.Container, out var constructors)
			|| (properties = GetAllProperties(branch.Container)).Length >= 0
			&& constructors.FindAll(x => (x.Attributes & ConstructorAttributes.AutoGenerated) != 0
			&& (x.Parameters.Length != 0 || properties.Length == 0) && x.Parameters.Length <= properties.Length
			&& (x.Parameters, properties.GetSlice(^x.Parameters.Length)).Combine()
			.All(x => x.Item1.Type.Equals(x.Item2.Value.NStarType))).Length != 0))
		{
			result.AddRange("public ").AddRange(branch.Container.Peek().Name).Add('(').AddRange(paramsResult);
			if (baseResult.Length != 0)
				result.AddRange(") : base(").AddRange(baseResult);
			result.AddRange("){").AddRange(coreResult).Add('}');
		}
		return result;
	}

	private String Property(TreeBranch branch, out List<String>? errors, out String constructorTop, out String constructorCore)
	{
		errors = null;
		constructorTop = [];
		constructorCore = [];
		if (branch[0].Extra is not NStarType NStarType)
			return [];
		var name = branch[1].Name;
		var (NStarType2, Attributes, DefaultValue) = UserDefinedProperties[branch.Container][name];
		if (!NStarType.Equals(NStarType2))
			return [];
		String result = [];
		if ((Attributes & PropertyAttributes.Private) != 0)
			result.AddRange("private ");
		if ((Attributes & PropertyAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & PropertyAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch)
			&& (Attributes & (PropertyAttributes.Private | PropertyAttributes.Protected | PropertyAttributes.Internal)) == 0)
			result.AddRange("public ");
		var @static = (Attributes & PropertyAttributes.Static) != 0;
		if (@static)
			result.AddRange("static ");
		var typeName = Type(ref NStarType, branch[^1], ref errors);
		result.AddRange(typeName).Add(' ');
		constructorTop.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(name))
		{
			result.Add('@');
			constructorTop.Add('@');
		}
		result.AddRange(name).AddRange(" { get; ");
		if ((Attributes & PropertyAttributes.NoSet) == 0)
		{
			if ((Attributes & PropertyAttributes.PrivateSet) != 0)
				result.AddRange("private ");
			if ((Attributes & PropertyAttributes.ProtectedSet) != 0)
				result.AddRange("protected ");
			result.AddRange((Attributes & PropertyAttributes.SetOnce) != 0 ? "init" : "set").AddRange("; ");
		}
		result.AddRange("} = ");
		constructorTop.AddRange(name).AddRange(" = default!");
		branch[^1].Extra ??= NStarType;
		var expr = ParseAction(branch[^1].Name)(branch[^1], out var innerErrors);
		if (branch[^1].Name != "null" && expr.AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
		{
			AddRange(ref errors, innerErrors);
			branch[^1].Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
			return result.AddRange("default!;");
		}
		else if (TypeEqualsToPrimitive(NStarType, "typename") && name == "typename")
		{
			GenerateMessage(ref errors, 0x4092, branch[1].Pos);
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		else if (branch[^1].Extra is not NStarType ValueNStarType)
		{
			GenerateMessage(ref errors, 0x4014, branch[^1].Pos, null!, NullType, NStarType);
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		else if (!TypesAreCompatible(ValueNStarType, NStarType, out var warning, expr, out _, out var extraMessage) || warning)
		{
			GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, NStarType);
			ValidateStatic(constructorTop, constructorCore);
			return result.AddRange("default!;");
		}
		result.AddRange(expr);
		constructorCore.AddRange("if (").AddRange(name).AddRange(" is ");
		if (TypeIsFullySpecified(NStarType, branch.Container))
			constructorCore.AddRange("default(").AddRange(typeName).Add(')');
		else
			constructorCore.AddRange("null");
		constructorCore.AddRange(")this.").AddRange(name).AddRange(" = ").AddRange(expr);
		constructorCore.AddRange(";else this.").AddRange(name).AddRange(" = ").AddRange(name).Add(';');
		AddRange(ref errors, innerErrors);
		result.Add(';');
		ValidateStatic(constructorTop, constructorCore);
		return result;
		void ValidateStatic(String constructorTop, String constructorCore)
		{
			if (@static)
			{
				constructorTop.Clear();
				constructorCore.Clear();
			}
		}
	}

	private String Constant(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch[0].Extra is not NStarType NStarType)
			return [];
		var name = branch[1].Name;
		var (TestNStarType, Attributes, DefaultValue) = UserDefinedConstants[branch.Container][name];
		if (!NStarType.Equals(TestNStarType))
			return [];
		String result = [];
		if ((Attributes & ConstantAttributes.Private) != 0)
			result.AddRange("private ");
		if ((Attributes & ConstantAttributes.Protected) != 0)
			result.AddRange("protected ");
		if ((Attributes & ConstantAttributes.Internal) != 0)
		{
			result.AddRange("internal ");
			GenerateMessage(ref errors, 0x8006, branch.Pos);
		}
		if (IsTypeContext(branch)
			&& (Attributes & (ConstantAttributes.Private | ConstantAttributes.Protected | ConstantAttributes.Internal)) == 0)
			result.AddRange("public ");
		result.AddRange(TypeIsPrimitive(NStarType.MainType) && NStarType.ExtraTypes.Length == 0
			&& NStarType.MainType.Peek().Name.AsSpan() is "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "real" ? "const " : "static readonly ");
		var typeName = Type(ref NStarType, branch[^1], ref errors);
		result.AddRange(typeName).Add(' ');
		if (EscapedKeywords.Contains(name))
			result.Add('@');
		result.AddRange(name).AddRange(" = ");
		var constantsDepth = this.constantsDepth;
		this.constantsDepth++;
		try
		{
			if (constantsDepth >= 25)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4055, otherPos);
				branch.Parent![branch.Parent.Elements.IndexOf(branch)]
					= new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
				return [];
			}
			branch[^1].Extra ??= NStarType;
			var expr = ParseAction(branch[^1].Name)(branch[^1], out var innerErrors);
			if (expr.AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
			{
				AddRange(ref errors, innerErrors);
				return [];
			}
			else if (TypeEqualsToPrimitive(NStarType, "typename") && name == "typename")
			{
				GenerateMessage(ref errors, 0x4092, branch[1].Pos);
				return [];
			}
			else if (branch[^1].Extra is not NStarType ValueNStarType)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, null!, NullType, NStarType);
				return [];
			}
			else if (!TypesAreCompatible(ValueNStarType, NStarType, out var warning, expr, out _, out var extraMessage)
				|| warning)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, NStarType);
				return [];
			}
			result.AddRange(expr);
			AddRange(ref errors, innerErrors);
		}
		finally
		{
			this.constantsDepth = constantsDepth;
		}
		result.Add(';');
		return [];
	}

	private String Condition(TreeBranch branch, out List<String>? errors)
	{
		String result = branch.Name.ToString() switch
		{
			"if" => "if (",
			"else if" => "else if (",
			"if!" => "if (!(",
			"else if!" => "else if (!(",
			_ => throw new InvalidOperationException(),
		};
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		if (branch.Name.EndsWith('!'))
			result.Add(')');
		return result.Add(')');
	}

	private String Loop(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch.Length == 0)
			return "while (true)";
		else
			return "do";
	}

	private String While(TreeBranch branch, out List<String>? errors)
	{
		String result = "while (";
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		return result.Add(')');
	}

	private String Repeat(TreeBranch branch, out List<String>? errors)
	{
		String result = "var ";
		var lengthName = RandomVarName();
		result.AddRange(lengthName);
		result.AddRange(" = ");
		errors = null;
		var parsedSubbranch = ParseAction(branch[0].Name)(branch[0], out var innerErrors);
		if (parsedSubbranch.Length != 0)
		{
			result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		var counterName = RandomVarName();
		result.AddRange(";for (var ").AddRange(counterName).AddRange(" = 0; ").AddRange(counterName).AddRange(" < ");
		result.AddRange(lengthName).AddRange("; ").AddRange(counterName).AddRange("++)");
		return result;
	}

	private String For(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (!(branch.Length == 2 && branch[0].Name == nameof(Declaration)))
			return [];
		if (branch[0].Length == 2 && VariableExists(branch[0], branch[0][1].Name, ref errors))
			return [];
		var result = ((String)"foreach (").AddRange(Declaration(branch[0], out var innerErrors));
		AddRange(ref errors, innerErrors);
		result.AddRange(" in ").AddRange(ParseAction(branch[1].Name)(branch[1], out innerErrors)).Add(')');
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String Declaration(TreeBranch branch, out List<String>? errors) =>
		Declaration(branch, out errors, false);

	private String Declaration(TreeBranch branch, out List<String>? errors, bool prepass)
	{
		errors = null;
		if (!(branch.Length == 2 && branch[0].Name == "type"))
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4000, otherPos);
			return "_";
		}
		var varName = branch[1].Name;
		if (VariableExists(branch, varName, ref errors!))
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))]
				= new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
				{
					Extra = NullType
				};
			return "_";
		}
		else if (UserDefinedConstantExists(branch.Container, varName, out var constant, out var matchingContainer, out _)
			&& constant.HasValue && constant.Value.DefaultValue == null)
		{
			if (branch.Parent == null || branch.Parent.Name != "DeclarationAssignment")
			{
				GenerateMessage(ref errors, 0x4053, branch.Pos);
				return "default!";
			}
			var prevIndex = branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
			var assignmentIndex = Max(prevIndex + 1, 2);
			if (assignmentIndex != 2 || branch.Parent[assignmentIndex].Name != "=")
			{
				GenerateMessage(ref errors, 0x4054, branch.Parent[assignmentIndex].Pos);
				return "default!";
			}
			UserDefinedConstants[matchingContainer][varName] = new(constant.Value.NStarType,
				constant.Value.Attributes, branch.Parent[0]);
		}
		if (branch[0].Extra is not NStarType NStarType)
			branch.Extra = NullType;
		else if (TypeEqualsToPrimitive(NStarType, "var"))
		{
			var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is NStarType AssigningNStarType
				&& branch.Parent.Length >= 3 && branch.Parent[prevIndex + 1].Name == "=")
			{
				if (TypeEqualsToPrimitive(AssigningNStarType, "typename") && varName == "typename")
				{
					var otherPos = branch[1].Pos;
					GenerateMessage(ref errors, 0x4092, otherPos);
					branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				branch.Extra = branch[prevIndex - 1].Extra = AssigningNStarType;
			}
			else if (branch.Parent[prevIndex - 1].Extra == null && prepass) { }
			else
			{
				var otherPos = branch[0].Pos;
				GenerateMessage(ref errors, 0x4011, otherPos);
				branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
				return "_";
			}
		}
		else if (TypeEqualsToPrimitive(NStarType, "typename") && varName == "typename")
		{
			var otherPos = branch[1].Pos;
			GenerateMessage(ref errors, 0x4092, otherPos);
			branch.Replace(new("_", branch.FirstPos, branch[0].EndPos, branch.Container) { Extra = NullType });
			return "_";
		}
		else if (UserDefinedTypes.TryGetValue(SplitType(NStarType.MainType),
			out var userDefinedType) && (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
		{
			branch.Parent![branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x))]
				= new("_", branch.FirstPos, branch[0].EndPos, branch.Container)
				{
					Extra = NullType
				};
			return "_";
		}
		else
		{
			branch.Extra = NStarType;
			var targetIndex = Max(branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
			branch.Parent[targetIndex].Extra ??= NStarType;
		}
		if (branch.Extra is NStarType NStarType2 && NStarType2.Equals(NullType))
			return "_";
		if (branch.Extra is not NStarType ResultType)
			ResultType = NullType;
		return Type(ref ResultType, branch, ref errors)
			.Copy().Add(' ').AddRange(EscapedKeywords.Contains(varName) ? ((String)"@").AddRange(varName) : varName);
	}

	private String Hypername(TreeBranch branch, out List<String>? errors) => Hypername(branch, out errors, null, false);

	private String Hypername(TreeBranch branch, out List<String>? errors, object? extra, bool prepass)
	{
		String result = [];
		errors = null;
		result.AddRange(Hypername1(branch, out var firstErrors, ref extra, prepass));
		AddRange(ref errors, firstErrors);
		for (var i = 1; i < branch.Length; i++)
		{
			if (i == 1 && branch[i].Name.AsSpan() is nameof(Call) or nameof(ConstructorCall))
				result.Replace(Hypername2(branch, ref errors, ref extra, ref i));
			else
			{
				var innerResult = Hypername2(branch, ref errors, ref extra, ref i);
				if (innerResult.AsSpan() is "default" or "default!")
					return "default!";
				if (result.ContainsAnyExcluding(AlphanumericCharacters)
					&& !(branch.Extra != null && branch.Extra.Equals(NullType)))
					result.Insert(0, '(').Add(')');
				if (innerResult.StartsWith("(.AsyncContext.Run(async () => await "))
				{
					innerResult.ReplaceRange(0, "(.AsyncContext.Run(async () => await ".Length, ".");
					result.Insert(0, "(AsyncContext.Run(async () => await ");
				}
				else if (innerResult.StartsWith(".AsyncContext.Run(async () => await "))
				{
					innerResult.Remove(1, "AsyncContext.Run(async () => await ".Length);
					result.Insert(0, "AsyncContext.Run(async () => await ");
				}
				else if (innerResult.StartsWith("(.await "))
				{
					innerResult.ReplaceRange(0, "(.await ".Length, ".");
					result.Insert(0, "(await ");
				}
				else if (innerResult.StartsWith(".await "))
				{
					innerResult.Remove(1, "await ".Length);
					result.Insert(0, "await ");
				}
				else if (innerResult.StartsWith("(."))
				{
					innerResult.RemoveAt(0);
					result.Insert(0, '(');
				}
				if (innerResult.StartsWith("(await ("))
				{
					innerResult.Remove(0, "(await ".Length);
					result.Insert(0, "(await (").Add(')');
				}
				if (innerResult.StartsWith('.')
					&& TryReadValue(innerResult[1..^(innerResult.EndsWith(')') ? 1 : 0)], out var value))
				{
					branch.Name = value.ToString(true, true);
					branch.Elements.Clear();
					branch.Extra ??= value.InnerType;
					return branch.Name;
				}
				result.AddRange(innerResult);
			}
		}
		return result;
	}

	private String Hypername1(TreeBranch branch, out List<String>? errors, ref object? extra, bool prepass)
	{
		String result = [];
		errors = null;
		if (branch.Name == "Hypername" && branch.Length == 0)
			return "default!";
		var targetBranch = branch.Length == 0 ? branch : branch[0];
		var branchName = targetBranch.Name.GetBefore(" (function)");
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		var innerErrorsLists = branch.Length <= 1 || branch[0].Name.EndsWith(" (delegate)")
			? [] : new List<String>?[branch[1].Length];
		var subbranchValues = branch.Length <= 1 || branch[0].Name.EndsWith(" (delegate)")
			? [] : branch[1].Elements.ToList((x, index) =>
			branch[0].Name == "new" && branch.Extra == null && index != 0 ? "default!"
			: x.Name == nameof(Hypername) ? Hypername(x, out innerErrorsLists[index], null, true)
			: ParseAction(x.Name)(x, out innerErrorsLists[index]));
		var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList((x, index) =>
			branch[0].Name == "new" && branch.Extra == null && index != 0 ? NullType : x.Extra is NStarType NStarType
			? NStarType : NullType);
		for (var i = 0; i < innerErrorsLists.Length; i++)
		{
			var innerErrors = innerErrorsLists[i];
			if (subbranchValues[i].AsSpan() is not ("" or "_" or "default" or "default!" or "_ = default" or "_ = default!"))
				AddRange(ref errors, innerErrors);
		}
		if (extra is null)
		{
			if (NStarEntity.TryParse(branchName.ToString(), out var value))
			{
				targetBranch.Extra = value.InnerType;
				extra = new List<object> { (String)nameof(Constant), value.InnerType };
				return value.ToString(true, true);
			}
			if (TryReadValue(branchName, out value))
				result.AddRange(value.ToString(true, true));
			else if (branch[0].Length != 0)
			{
				result.AddRange(ParseAction(branch[0].Name)(branch[0], out var innerErrors));
				AddRange(ref errors, innerErrors);
				branch.Extra = branch[0].Extra;
				extra = new List<object> { (String)nameof(Expr), branch.Extra! };
			}
			else if (branchName == "type")
				extra = HypernameType(branch, ref errors, result);
			else if (branchName == "new")
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return "default!";
				}
				if (!ImplicitConstructor(branch, ref errors, parameterTypes, prepass))
					return "default!";
				if (branch[0].Extra is not NStarType NStarType)
					NStarType = NullType;
				Type(ref NStarType, branch, ref errors, true);
				extra = HypernameConstructor(branch, subbranchValues, parameterTypes, ref NStarType, true);
				DetectSpaghettiOutOfRecursion(ref errors);
				if (branch[0].Extra is NStarType)
					branch[0].Name = "new type";
				if (NStarType == NullType)
					return "default!";
				else if (TypeIsFullySpecified(NStarType, branch.Container))
					result.AddRange("new ").AddRange(Type(ref NStarType, targetBranch, ref errors));
				else
				{
					result.AddRange("Activator.CreateInstance(");
					result.AddRange(TypeReflected(ref NStarType, targetBranch, ref errors));
					result.AddRange(", (List<object>)");
				}
			}
			else if (branchName == "new type")
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return "default!";
				}
				if (branch[0].Extra is not NStarType NStarType)
					NStarType = NullType;
				extra = HypernameConstructor(branch, subbranchValues, parameterTypes, ref NStarType);
				DetectSpaghettiOutOfRecursion(ref errors);
				result.AddRange("new ").AddRange(branch[0].Extra is NStarType type
					? Type(ref type, targetBranch, ref errors) : "dynamic");
				return result;
			}
			else if (IsConstantDeclared(branch, branchName, out var constantErrors, out var constant))
			{
				if (branch.Parent != null && branch.Parent.Name == nameof(Assignment))
				{
					GenerateMessage(ref errors, 0x4052, branch.Parent[Max(prevIndex + 1, 2)].Pos);
					branch.Parent.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return "default!";
				}
				var constantsDepth = this.constantsDepth;
				this.constantsDepth++;
				List<String>? innerErrors = null;
				if (constantsDepth >= 25)
					return ConstantsDepthExceeded(ref errors, constantsDepth);
				else if (constant.HasValue && constant.Value.DefaultValue != null
					&& TryReadValue(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue,
					out innerErrors).ToString(), out value))
				{
					branchName = branch.Name = value.ToString(true, true);
					branch.Elements.Clear();
					branch.Extra = value.InnerType;
					extra = new List<object> { (String)nameof(Constant), value.InnerType, subbranchValues };
				}
				else
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Constant), NullType, subbranchValues };
				}
				if (prepass && branch.Length == 1 && branch.Parent != null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				result.AddRange(branchName);
				AddRange(ref errors, constantErrors);
				AddRange(ref errors, innerErrors);
				this.constantsDepth = constantsDepth;
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, value.InnerType);
			}
			else if (IsVariableDeclared(branch, branchName, out var variableErrors, out var innerExtra))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return "default!";
				}
				if (innerExtra is NStarType NStarType)
				{
					branch.Extra = branch[0].Extra = NStarType;
					extra = new List<object> { (String)"Variable", NStarType, subbranchValues };
				}
				else
				{
					branch.Extra = branch[0].Extra = NStarType = NullType;
					extra = new List<object> { (String)"Variable", NStarType, subbranchValues };
				}
				if (prepass && branch.Length == 1 && branch.Parent != null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				if (EscapedKeywords.Contains(branchName))
					result.Add('@');
				result.AddRange(NStarType.Equals(NullType) ? "default(dynamic)" : branchName);
				AddRange(ref errors, variableErrors!);
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, NStarType);
			}
			else if (IsPropertyDeclared(branch, branchName, out var propertyErrors, out var property,
				out var inBase, out var actualContainer))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return "default!";
				}
				if (!property.HasValue)
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Property), NullType, subbranchValues };
					return "default!";
				}
				var fullName = String.Join(".", actualContainer.Convert(x => x.Name)
					.Append(branchName).ToArray());
				TreeBranch? assignmentBranch;
				int assignmentIndex;
				if (inBase && (property.Value.Attributes & PropertyAttributes.Private) != 0
					&& (property.Value.Attributes & PropertyAttributes.Protected) == 0
					&& !branch.Container.StartsWith([.. actualContainer]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (inBase && IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					&& (property.Value.Attributes & PropertyAttributes.ProtectedSet) == 0
					&& !branch.Container.StartsWith([.. actualContainer]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& !branch.Container.StartsWith([.. actualContainer,
					new(BlockType.Constructor, "", 1)]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403A, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else
				{
					branch.Extra = branch[0].Extra = property.Value.NStarType;
					extra = new List<object> { (String)nameof(Property), property.Value.NStarType, subbranchValues };
				}
				(BlockStack, String) matchingKey = default!;
				if (!(CheckContainer(branch.Container, x => UserDefinedTypes.ContainsKey(matchingKey = SplitType(x)), out _)
					&& UserDefinedTypes.TryGetValue(matchingKey, out var userDefinedType)))
					throw new InvalidOperationException();
				if (property.HasValue && (property.Value.Attributes & PropertyAttributes.Required) != 0
					&& IsConstructor(branch, out var constructorBranch, out var overloads)
					&& CreateVar(userDefinedType.Restrictions, out var requiredProperties).Length != 0
					&& CreateVar(requiredProperties.FindLastIndex(x => x.RestrictionType.Equals(property.Value.NStarType)
					&& x.Name == branchName), out var foundIndex) >= 0)
					overloads[0].UnsetRequiredProperties.RemoveValue(foundIndex);
				if (prepass && branch.Length == 1 && branch.Parent != null && branch.Parent.Name == nameof(Assignment))
				{
					var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
					branch.Parent[targetIndex].Extra ??= branch.Extra;
				}
				if (EscapedKeywords.Contains(branchName))
					result.Add('@');
				result.AddRange(branchName);
				AddRange(ref errors, propertyErrors!);
				if (!(IsAnyAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, property.Value.NStarType);
			}
			else if (IsFunctionDeclared(branch, branchName, out var functionErrors,
				out var functions, out var functionContainer, out _) && functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return "default!";
				}
				DetectSpaghettiOutOfRecursion(ref errors);
				if (functionContainer.Length == 0)
					HypernamePublicExtendedMethod(branch, branchName, subbranchValues, ref extra,
						ref errors, prevIndex, functions, "user");
				else if (HypernameExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors,
					prevIndex, new(functionContainer, NoBranches), functions, "userMethod") != null)
					return "_";
				result.AddRange(functions[^1].RealName);
				branch.Extra = new NStarType(FuncBlockStack,
					new([new("type", branch.Pos, branch.Container) { Extra = functions[^1].ReturnNStarType },
					.. functions[^1].Parameters.Convert(x =>
					new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type })]));
				AddRange(ref errors, functionErrors!);
			}
			else if (ExtendedMethodExists(new(), branchName, parameterTypes, out functions, out var user)
				&& functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return "default!";
				}
				DetectSpaghettiOutOfRecursion(ref errors);
				if (branchName.AsSpan() is "ExecuteString" or "Q" && !(branch.Length >= 2 && branch[1].Name == nameof(Call)))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4020, otherPos, branchName);
					branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				HypernamePublicExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors,
					prevIndex, functions, user ? "user" : "general");
				result.AddRange(functions[^1].RealName);
				branch.Extra = new NStarType(FuncBlockStack,
					new([new("type", branch.Pos, branch.Container) { Extra = functions[^1].ReturnNStarType },
					.. functions[^1].Parameters.Convert(x =>
					new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type })]));
			}
			else if (branch.Length == 1 && branch.Extra is NStarType RecursiveNStarType
				&& RecursiveNStarType.Equals(RecursiveType) && PrimitiveTypes.ContainsKey(branchName))
			{
				if (branchName == "typename")
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4090, otherPos, branchName);
					branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				NStarType primitiveType = (new([new(PrimitiveTypes.ContainsKey(branchName)
					? BlockType.Primitive : BlockType.Extra, branchName, 1)]), NoBranches);
				branch[0] = new("type", branch.Pos, branch.Container) { Extra = primitiveType };
				result.AddRange("typeof(").AddRange(Type(ref primitiveType, targetBranch, ref errors)).Add(')');
			}
			else if (branchName == "I")
			{
				branch.Extra = ComplexType;
				result.AddRange("new Complex(0, 1)");
			}
			else if (!prepass)
			{
				var otherPos = branch.FirstPos;
				if (variableErrors != null && variableErrors.Length != 0)
					AddRange(ref errors, variableErrors);
				else if (propertyErrors != null && propertyErrors.Length != 0)
					AddRange(ref errors, propertyErrors);
				else if (functionErrors != null && functionErrors.Length != 0)
					AddRange(ref errors, functionErrors);
				else
					GenerateMessage(ref errors, 0x4001, otherPos, branchName);
				branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				return prevIndex == 0 || branch.Parent.Name == nameof(List) ? "default!" : "_";
			}
		}
		else
		{
			if (!(extra is List<object> paramCollection && paramCollection.Length is >= 2 and <= 5
				&& paramCollection[0] is String Category && paramCollection[1] is NStarType ContainerNStarType))
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4000, otherPos);
				return "default!";
			}
			if (ConstantExists(ContainerNStarType, branchName, out var constant))
			{
				if (IsAssignment(branch, out var assignmentBranch, out var assignmentIndex))
				{
					GenerateMessage(ref errors, 0x4052, assignmentBranch[assignmentIndex].Pos);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "default!";
				}
				var constantsDepth = this.constantsDepth;
				this.constantsDepth++;
				NStarEntity value;
				if (constantsDepth >= 25)
					return ConstantsDepthExceeded(ref errors, constantsDepth);
				else if (!(constant.HasValue && constant.Value.DefaultValue != null
					&& TryReadValue(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue,
					out var innerErrors).ToString(), out value)))
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Constant), NullType };
					this.constantsDepth = constantsDepth;
					return "_";
				}
				else if ((constant.Value.Attributes & ConstantAttributes.Private) != 0
					^ (constant.Value.Attributes & ConstantAttributes.Protected) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
					return InaccessibleConstant(ref errors, constantsDepth);
				else
				{
					branchName = branch.Name = value.ToString(true, true);
					branch.Elements.Clear();
					branch.Extra = value.InnerType;
					extra = new List<object> { (String)nameof(Constant), branch.Extra, subbranchValues };
				}
				result.AddRange(branchName);
				this.constantsDepth = constantsDepth;
				WrapIntoAsync(branch, result, value.InnerType);
			}
			else if (PropertyExists(ContainerNStarType, PropertyMapping(branchName), Category == "Static", out var property))
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4050, branch.Pos);
					return "default!";
				}
				if (!property.HasValue)
				{
					branch.Extra = branch[0].Extra = NullType;
					extra = new List<object> { (String)nameof(Property), NullType, subbranchValues };
					return "_";
				}
				var fullName = String.Join(".", ContainerNStarType.MainType.Convert(x => x.Name).Append(branchName).ToArray());
				TreeBranch? assignmentBranch;
				int assignmentIndex;
				if ((property.Value.Attributes & PropertyAttributes.Private) != 0
					^ (property.Value.Attributes & PropertyAttributes.Protected) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Value.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& !branch.Container.StartsWith([.. ContainerNStarType.MainType,
					new(BlockType.Constructor, "", 1)]))
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403A, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				else if (IsAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& (property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					assignmentBranch.Name = "null";
					assignmentBranch.Elements.Clear();
					assignmentBranch.Extra = NullType;
					return "_";
				}
				branch.Extra = branch[0].Extra
					= new NStarType(property.Value.NStarType.MainType, property.Value.NStarType.ExtraTypes);
				extra = new List<object> { (String)nameof(Property), branch.Extra, subbranchValues };
				result.AddRange(PropertyMapping(branchName));
				if (!(prepass && IsAnyAssignment(branch, out assignmentBranch, out assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, property.Value.NStarType);
			}
			else if (UserDefinedFunctionExists(ContainerNStarType, branchName, parameterTypes, out var functions)
				&& functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return "default!";
				}
				DetectSpaghettiOutOfRecursion(ref errors);
				if (HypernameExtendedMethod(branch, branchName, subbranchValues, ref extra, ref errors, prevIndex,
					ContainerNStarType, functions, "userMethod") != null)
					return "_";
				result.AddRange(functions[^1].RealName);
				branch.Extra = functions;
			}
			else if (MethodExists(ContainerNStarType, FunctionMapping(branchName, parameterTypes, null), parameterTypes,
				out functions) && functions.Length != 0)
			{
				if (constantsDepth != 0)
				{
					GenerateMessage(ref errors, 0x4051, branch.Pos);
					return "default!";
				}
				DetectSpaghettiOutOfRecursion(ref errors);
				if (HypernameMethod(branch, branchName, subbranchValues, ref extra, ref errors, prevIndex,
					ContainerNStarType, functions) != null)
					return "_";
				result.AddRange(functions[^1].RealName);
				branch.Extra = functions;
			}
			else
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4033, otherPos,
					String.Join(".", ContainerNStarType.MainType.ToArray(x => x.Name)), branchName);
				branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				return "_";
			}
			String InaccessibleConstant(ref List<String>? errors, int constantsDepth)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4030, otherPos,
					String.Join(".", ContainerNStarType.MainType.Convert(x => x.Name).Append(branchName).ToArray()));
				branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
				this.constantsDepth = constantsDepth;
				return "_";
			}
		}
		Debug.Assert(prepass || branch.Extra != null);
		return result;
		String ConstantsDepthExceeded(ref List<String>? errors, int constantsDepth)
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4055, otherPos);
			branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = NullType
			};
			this.constantsDepth = constantsDepth;
			return "_";
		}
		void DetectSpaghettiOutOfRecursion(ref List<String>? errors)
		{
			var pureBranchName = branchName.AsSpan() is not ("new" or "new type")
				? branchName.Replace(" (function", "") : branch[0].Extra is NStarType ConstructingNStarType
				? "new " + ConstructingNStarType.MainType.ToString() : [];
			if (pureBranchName.Length == 0)
				return;
			var containerFunction = GetFunctionName(branch);
			if (containerFunction.Length != 0)
			{
				functionReferences.TryAdd(pureBranchName, []);
				functionReferences[pureBranchName].TryAdd(containerFunction);
			}
			if (IsSpaghettiOutOfRecursion(pureBranchName))
				GenerateMessage(ref errors, 0x801D, branch.Pos);
		}
	}

	private List<object> HypernameType(TreeBranch branch, ref List<String>? errors, String result)
	{
		var targetBranch = branch.Length == 0 ? branch : branch[0];
		if (branch[0].Extra is not NStarType NStarType)
			NStarType = NullType;
		var extra = new List<object> { (String)"Static", NStarType };
		//if (branch.Extra == null)
		//{
		//	var otherPos = branch.FirstPos;
		//	GenerateMessage(ref errors, 0x4000, otherPos);
		//	branch.Replace(new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType });
		//	result.Replace("_");
		//	return default!;
		//}
		var bTypename = branch.Extra is NStarType OuterNStarType && OuterNStarType.Equals(RecursiveType);
		var bExtraType = !TypeIsFullySpecified(NStarType, branch.Container);
		if (NStarType.Equals(RecursiveType))
		{
			var otherPos = branch.FirstPos;
			GenerateMessage(ref errors, 0x4090, otherPos);
			branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = NullType
			};
			result.Replace("_");
			return default!;
		}
		if (bExtraType)
		{
			var fullName = TypeReflected(ref NStarType, branch, ref errors);
			result.AddRange(fullName);
		}
		else
		{
			if (bTypename)
				result.AddRange("typeof(");
			result.AddRange(Type(ref NStarType, targetBranch, ref errors));
			if (bTypename)
				result.Add(')');
		}
		branch.Extra ??= branch[0].Extra;
		return extra;
	}

	private bool ImplicitConstructor(TreeBranch branch, ref List<String>? errors, List<NStarType> parameterTypes, bool prepass)
	{
		if (branch.Extra is NStarType NStarType)
		{
			var split = SplitType(NStarType.MainType);
			if (!NStarType.MainType.TryPeek(out var block) || block.BlockType is not (BlockType.Primitive or BlockType.Extra
				or BlockType.Class or BlockType.Struct or BlockType.Interface or BlockType.Delegate or BlockType.Enum))
				throw new InvalidOperationException();
			else if (block.BlockType is BlockType.Delegate or BlockType.Enum
				|| block.BlockType == BlockType.Primitive && block.Name.AsSpan() is "null" or "bool" or "byte"
				or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int" or "long char"
				or "long int" or "unsigned long int" or "long long" or "real" or "complex" or "typename"
				or "index" or "range" or "nint" or "dynamic"
				|| block.BlockType is BlockType.Class && TypeExists(split, out var netType)
				&& netType.IsAbstract && netType.IsSealed
				|| UserDefinedTypes.TryGetValue(split, out var userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Static)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4017, otherPos, NStarType.ToString());
				branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos,
					branch.EndPos, branch.Container)
				{
					Extra = NullType
				};
				return false;
			}
			else if (block.BlockType == BlockType.Primitive && block.Name == "object" || block.BlockType == BlockType.Interface
				|| block.BlockType is BlockType.Class && TypeExists(split, out netType)
				&& netType.IsAbstract || UserDefinedTypes.TryGetValue(split, out userDefinedType)
				&& (userDefinedType.Attributes & TypeAttributes.Static) == TypeAttributes.Abstract)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x4018, otherPos, NStarType.ToString());
				branch.Parent![branch.Parent.Elements.IndexOf(branch)] = new("null", branch.Pos,
					branch.EndPos, branch.Container)
				{
					Extra = NullType
				};
				return false;
			}
			branch[0].Extra = NStarType;
		}
		else if (parameterTypes.Length != 0 && parameterTypes[0] is NStarType ParameterNStarType)
		{
			if (prepass)
				return false;
			branch[1].Elements.Skip(1).ForEach(x => x.Extra = ParameterNStarType);
			branch.Extra = branch[0].Extra = GetListType(ParameterNStarType);
		}
		return true;
	}

	private static object HypernameConstructor(TreeBranch branch, List<String> parameters, List<NStarType> parameterTypes,
		ref NStarType NStarType, bool @implicit = false)
	{
		object? extra;
		if (UserDefinedConstructorsExist(NStarType, parameterTypes, out var constructors) && constructors != null)
			extra = new List<object> { (String)"Constructor", NStarType, (String)"user", constructors, parameters };
		else if (ConstructorsExist(NStarType, parameterTypes, out constructors) && constructors != null)
			extra = new List<object> { (String)"Constructor", NStarType, (String)"typical", constructors, parameters };
		else
		{
			if (!@implicit)
				NStarType = NullType;
			extra = new List<object> { (String)"Constructor", NStarType, parameters };
		}
		branch[1].Elements.ToList((x, index) => x.Extra is NStarType SourceType
			&& (!TypesAreCompatible(SourceType, parameterTypes[index], out var warning,
			parameters[index], out _, out _) || warning));
		return extra;
	}

	private String Hypername2(TreeBranch branch, ref List<String>? errors, ref object? extra, ref int index)
	{
		String result = [];
		if (branch[index].Name == nameof(Call) && extra is List<object> paramCollection)
		{
			var (flowControl, value) = HypernameCall(branch, ref errors, ref extra, index, result, paramCollection);
			if (!flowControl)
				return value;
		}
		else if (branch[index].Name == nameof(ConstructorCall) && extra is List<object> processingWay)
		{
			var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList(x =>
				x.Extra is NStarType NStarType ? NStarType : NullType);
			if (processingWay.Length == 3 && processingWay[1] is NStarType ReflectedNStarType
				&& ReflectedNStarType != NullType && processingWay[2] is List<String> reflectedInnerResults)
			{
				result.AddRange("Activator.CreateInstance(");
				result.AddRange(TypeReflected(ref ReflectedNStarType, branch[0], ref errors));
				result.AddRange(", new object[] { ");
				for (var i = 0; i < reflectedInnerResults.Length; i++)
				{
					reflectedInnerResults[i] = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
					AddRange(ref errors, innerErrors);
				}
				result.AddRange(String.Join(", ", reflectedInnerResults)).AddRange(" })");
				Debug.Assert(branch.Extra != null);
				return result;
			}
			if (!(processingWay.Length == 5 && processingWay[0] is String elem1 && elem1 == "Constructor"
				&& processingWay[1] is NStarType ConstructingNStarType && processingWay[2] is String elem3
				&& processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
				&& processingWay[4] is List<String> parameters
				&& (ConstructorsExist(ConstructingNStarType, parameterTypes, out var innerConstructors)
				|| UserDefinedConstructorsExist(ConstructingNStarType, parameterTypes, out innerConstructors))))
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4000, otherPos);
				return "default!";
			}
			ConstructorCall(branch[index], parameters.ToList(x => x.Copy()), out _, extra);
			for (var i = 0; i < parameters.Length; i++)
				if (parameters[i].AsSpan() is "" or "_" or "default" or "default!" or "_ = default" or "_ = default!")
				{
					parameters[i] = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
					AddRange(ref errors, innerErrors);
				}
			if (elem3 == "typical")
			{
				processingWay[3] = innerConstructors;
				result.AddRange("new ").AddRange(Type(ref ConstructingNStarType, branch[index], ref errors));
				result.AddRange(ConstructorCall(branch[index], parameters, out var innerErrors, extra));
				AddRange(ref errors, innerErrors);
				branch.Extra = branch[0].Extra;
				if (innerErrors != null && innerErrors.Any(x => x.StartsWith("Error")))
					return "default!";
			}
			else
			{
				processingWay[3] = innerConstructors;
				var (flowControl, value) = HypernameUserConstructor(branch[index], ref errors, extra);
				branch.Extra = branch.Length == 0 ? NullType : branch[0].Extra;
				result.AddRange(value);
				if (!flowControl)
					return result;
			}
		}
		else if (branch[index].Name == nameof(Indexes))
			result.AddRange(Indexes(branch, ref errors, extra, index));
		else if (branch[index].Name == nameof(Call))
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4038, otherPos);
			return "default!";
		}
		else if (branch[index].Name == ".")
		{
			using var innerResult = Hypername(branch[++index], out var innerErrors, extra, false);
			if (errors != null && errors.Length != 0 && errors.Any(x => x.StartsWith("Error ")))
				return "default!";
			AddRange(ref errors, innerErrors);
			if (innerResult.AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
			{
				branch.Extra = NullType;
				return "default!";
			}
			if (innerResult.StartsWith('('))
			{
				innerResult.RemoveAt(0);
				result.Insert(0, '(');
			}
			result.Add('.').AddRange(innerResult);
			extra = branch.Extra = branch[index].Extra;
			if (branch.Parent != null && branch.Parent.Name == nameof(Assignment))
			{
				var targetIndex = Max(branch.Parent.Elements.FindIndex(x => ReferenceEquals(branch, x)) - 2, 0);
				branch.Parent[targetIndex].Extra ??= branch.Extra;
			}
		}
		else
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos);
			return "default!";
		}
		Debug.Assert(branch.Extra != null);
		return result;
	}

	private (bool flowControl, String value) HypernameCall(TreeBranch branch, ref List<String>? errors, ref object? extra,
		int index, String result, List<object> paramCollection)
	{
		if (paramCollection.Length == 3 && paramCollection[0] is String delegateElem1
			&& delegateElem1.AsSpan() is "Variable" or nameof(Property) or nameof(Expr)
			&& paramCollection[1] is NStarType DelegateNStarType
			&& DelegateNStarType.MainType.Equals(FuncBlockStack)
			&& DelegateNStarType.ExtraTypes.Length != 0 && DelegateNStarType.ExtraTypes[0].Name == "type"
			&& DelegateNStarType.ExtraTypes[0].Extra is NStarType ReturnNStarType)
		{
			if (index <= 1)
				result.AddRange(branch[index - 1].Name);
			if (branch[index].Length != DelegateNStarType.ExtraTypes.Length - 1)
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4045, otherPos, DelegateNStarType.ExtraTypes.Length - 1);
				return (false, "default!");
			}
			NStarType ParameterNStarType = default!, CallNStarType = default!;
			result.AddRange(List(branch[index], out var innerErrors));
			var wrongParameterIndex = branch[index].Elements.Combine(DelegateNStarType.ExtraTypes.Skip(1))
				.FindIndex(x => x.Item1.Extra is not NStarType ParameterNStarType2
				|| !TypesAreCompatible(ParameterNStarType = ParameterNStarType2,
				CallNStarType = x.Item2.Value.Name == "type"
				&& x.Item2.Value.Extra is NStarType NStarType ? NStarType : NullType,
				out var warning, [], out var destExpr, out _) || warning || destExpr != null && destExpr.Length != 0);
			if (wrongParameterIndex >= 0)
			{
				var otherPos = branch[index][wrongParameterIndex].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, null!, ParameterNStarType, CallNStarType);
				return (false, "default!");
			}
			AddRange(ref errors, innerErrors);
			branch.Extra = ReturnNStarType;
			if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
				&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
				&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
				WrapIntoAsync(branch, result, ReturnNStarType);
			return (false, result);
		}
		if (!(paramCollection.Length >= 4 && paramCollection.Length <= 5
			&& paramCollection[0] is String functionName && functionName.StartsWith("Function ")
			&& paramCollection[1] is String processingWay && paramCollection[3] is List<String> parameters))
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos);
			return (false, "default!");
		}
		for (var i = 0; i < parameters.Length; i++)
			if (parameters[i].AsSpan() is "" or "_" or "default" or "default!" or "_ = default" or "_ = default!")
			{
				parameters[i] = ParseAction(branch[index][i].Name)(branch[index][i], out var innerErrors);
				AddRange(ref errors, innerErrors);
			}
		var parameterTypes = branch.Length <= 1 ? [] : branch[1].Elements.ToList(x =>
			x.Extra is NStarType NStarType ? NStarType : throw new InvalidOperationException());
		var name = functionName["Function ".Length..];
		if (name == "ExecuteString")
		{
			var @string = parameters[0];
			var addParameters = branch[index].Length != 1;
			String? joinedParameters;
			if (addParameters)
			{
				joinedParameters = ((String)", ").AddRange(List(new(nameof(List),
					branch[index].Elements[1..], branch.Container), out var parametersErrors));
				AddRange(ref errors, parametersErrors);
			}
			else
				joinedParameters = [];
			if (joinedParameters.StartsWith(", (") && joinedParameters.EndsWith(')'))
			{
				joinedParameters.ReplaceRange(2, 1, "new[]{");
				joinedParameters[^1] = '}';
			}
			result.AddRange(nameof(ExecuteProgram)).Add('(').AddRange(nameof(TranslateProgram));
			result.AddRange("(((String)\"").AddRange(ExecuteStringPrefix).AddRange("\").AddRange(").AddRange(@string);
			result.AddRange(")).Wrap(x => (x.Item1.Remove(x.Item1.IndexOf(").AddRange(nameof(ExecuteStringPrefixCompiled));
			result.AddRange("), ").AddRange(nameof(ExecuteStringPrefixCompiled));
			result.AddRange(""".Length), x.Item2, x.Item3)), out _, out _""").AddRange(joinedParameters).AddRange(").");
			result.AddRange(nameof(Quotes.RemoveQuotes)).AddRange("()");
		}
		else if (name == "Q")
		{
			branch.Extra = StringType;
			return (false, ((String)"((String)@\"").AddRange(input.Replace("\"", "\"\"")).AddRange("\")"));
		}
		else if (processingWay == "public" && name == nameof(RedStarLinq.Fill) && branch[index].Length == 2
			&& branch[index][0].Extra is NStarType FirstParameterType
			&& TypeEqualsToPrimitive(FirstParameterType, "bool"))
		{
			result.AddRange("new ").AddRange(nameof(BitList)).Add('(');
			result.AddRange(ParseAction(branch[index][1].Name)(branch[index][1], out var innerErrors));
			AddRange(ref errors, innerErrors);
			result.AddRange(", ");
			result.AddRange(ParseAction(branch[index][0].Name)(branch[index][0], out innerErrors));
			AddRange(ref errors, innerErrors);
			result.Add(')');
			branch.Extra = BitListType;
			extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
		}
		else if (!processingWay.StartsWith("user") && branch.Parent?[0].Extra is NStarType ContainerNStarType)
		{
			UnwrapParameters();
			if (MethodExists(ContainerNStarType, FunctionMapping(name, parameterTypes, null), parameterTypes, out var functions)
				&& functions.Length != 0
				|| ExtendedMethodExists(ContainerNStarType.MainType, name, branch[index].Elements
				.ToList(x => x.Extra is NStarType NStarType ? NStarType : throw new InvalidOperationException()),
				out functions, out _) && functions.Length != 0)
			{
				paramCollection[2] = functions;
				var convertedParameters = Call(branch[index], parameters, out var innerErrors, extra);
				result.AddRange(FunctionMapping(name, parameterTypes, convertedParameters));
				AddRange(ref errors, innerErrors);
				branch.Extra = functions[^1].ReturnNStarType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			if (!result.EndsWith(')') && !result.EndsWith(") + 1"))
				return (false, "default!");
		}
		else if ((processingWay == "user" && UserDefinedFunctionExists(new(), name, parameterTypes,
			out var functions, out _, out var derived) || processingWay == "userMethod"
			&& UserDefinedFunctionExists(new(branch.Container, NoBranches), name, parameterTypes, out functions, out _,
			out derived)) && functions.Length != 0)
		{
			paramCollection[2] = functions;
			List<String>? innerErrors;
			if (derived)
			{
				UnwrapParameters();
				var convertedParameters = Call(branch[index], parameters, out innerErrors, extra);
				result.AddRange(FunctionMapping(name, parameterTypes, convertedParameters));
			}
			else
			{
				var callResult = CallUser(branch[index], parameters, out innerErrors, extra);
				var realName = functions[^1].RealName;
				if (EscapedKeywords.Contains(realName))
					realName.Insert(0, '@');
				result.AddRange(index > 1 ? [] : realName);
				result.AddRange(callResult);
			}
			AddRange(ref errors, innerErrors);
			if (!result.EndsWith(')'))
				return (false, "default!");
			branch.Extra = functions[^1].ReturnNStarType;
			extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
			if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
				&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
				&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
				WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
		}
		else if (processingWay == "userMethod" && branch.Parent?[0].Extra is NStarType ContainerNStarType2)
		{
			if (TypeEqualsToPrimitive(ContainerNStarType2, "typename")
				&& paramCollection.Length == 5 && paramCollection[4] is String elem4
				&& elem4 == "static" && UserDefinedFunctionExists(ContainerNStarType2, name,
				parameterTypes, out functions) && functions.Length != 0)
			{
				paramCollection[2] = functions;
				var callResult = CallUser(branch[index], parameters, out var innerErrors, extra);
				var realName = functions[^1].RealName;
				if (EscapedKeywords.Contains(realName))
					realName.Insert(0, '@');
				result.AddRange(index > 1 ? [] : realName).AddRange(callResult);
				AddRange(ref errors, innerErrors);
				branch.Extra = functions[^1].ReturnNStarType;
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			else if (UserDefinedFunctionExists(ContainerNStarType2, name, parameterTypes,
				out functions, out _, out derived) && functions.Length != 0)
			{
				paramCollection[2] = functions;
				List<String>? innerErrors;
				if (derived)
				{
					UnwrapParameters();
					var convertedParameters = Call(branch[index], parameters, out innerErrors, extra);
					result.AddRange(FunctionMapping(name, parameterTypes, convertedParameters));
				}
				else
				{
					var callResult = CallUser(branch[index], parameters, out innerErrors, extra);
					ContainerUserDefinedFunction(functions, callResult);
				}
				AddRange(ref errors, innerErrors);
				branch.Extra = functions[^1].ReturnNStarType;
				extra = new List<object> { (String)nameof(Expr), branch.Extra, parameters };
				if (!(IsAnyAssignment(branch, out var assignmentBranch, out var assignmentIndex)
					&& assignmentBranch[assignmentIndex - 1].Extra is NStarType AssignmentNStarType
					&& TaskBlockStacks.Contains(AssignmentNStarType.MainType)))
					WrapIntoAsync(branch, result, functions[^1].ReturnNStarType);
			}
			if (!result.EndsWith(')'))
				return (false, "default!");
		}
		else
		{
			var otherPos = branch[index].Pos;
			GenerateMessage(ref errors, 0x4000, otherPos);
			return (false, "default!");
		}
		return (true, result);
		void UnwrapParameters()
		{
			foreach (var x in parameters)
				if (x.StartsWith("async "))
					x.Remove(0, "async ".Length);
		}
		void ContainerUserDefinedFunction(UserDefinedMethodOverloads functions, String callResult)
		{
			var realName = functions[^1].RealName;
			if (EscapedKeywords.Contains(realName))
				realName.Insert(0, '@');
			result.AddRange((index > 1 ? [] : realName.Copy()).AddRange(callResult));
		}
	}

	private void WrapIntoAsync(TreeBranch branch, String result, NStarType ReturnNStarType)
	{
		if (TaskBlockStacks.Contains(ReturnNStarType.MainType))
		{
			if (noAddAsync)
			{
				var prefix = ((String)nameof(AsyncContext)).Add('.').AddRange(nameof(AsyncContext.Run));
				prefix.AddRange("(async () => await ");
				result.Insert(0, prefix).Add(')');
			}
			else
			{
				result.Insert(0, "await ");
				containsAsync = true;
			}
		}
		if (branch.Extra != null && branch.Extra.Equals(ReturnNStarType)
			&& (ReturnNStarType.MainType.Equals(TaskBlockStack) || ReturnNStarType.MainType.Equals(ValueTaskBlockStack)
			|| ReturnNStarType.MainType.Equals(TaskBlockStackNamespace)
			|| ReturnNStarType.MainType.Equals(ValueTaskBlockStackNamespace))
			&& ReturnNStarType.ExtraTypes.Length == 1 && ReturnNStarType.ExtraTypes[0].Name == "type"
			&& ReturnNStarType.ExtraTypes[0].Extra is NStarType UnderlyingNStarType)
			branch.Extra = UnderlyingNStarType;
	}

	private (bool flowControl, String value) HypernameUserConstructor(TreeBranch branch, ref List<String>? errors,
		object? extra)
	{
		String result = [], requiredProperties = [];
		if (!(extra is List<object> processingWay && processingWay.Length == 5 && processingWay[0] is String elem1
			&& elem1 == "Constructor" && processingWay[1] is NStarType ConstructingNStarType && processingWay[2]
			is String elem3 && processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
			&& processingWay[4] is List<String> parameters))
			return (false, "default!");
		if (parsedUserConstructors.TryGetValue((ConstructingNStarType, branch), out var parsed))
			return parsed;
		var Restrictions = UserDefinedTypes[SplitType(ConstructingNStarType.MainType)].Restrictions;
		var (TypeIndexes, OtherIndexes) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.Equals(RecursiveType));
		TreeBranch? CallRestrictions = null;
		if (ConstructingNStarType.ExtraTypes.Length != 0)
		{
			if (ConstructingNStarType.ExtraTypes.Length != 1)
				throw new InvalidOperationException();
			CallRestrictions = ConstructingNStarType.ExtraTypes[0];
			if (CallRestrictions.Parent == null)
				typeof(TreeBranch).GetProperty("Parent")?.SetValue(CallRestrictions, branch);
			var hs = new ListHashSet<int>(new Chain(ConstructingNStarType.ExtraTypes.Length)).ExceptWith(TypeIndexes);
			ConstructingNStarType.ExtraTypes.Keys.Filter((x, index) => hs.Contains(index))
				.ForEach(x => ConstructingNStarType.ExtraTypes.Remove(x));
		}
		else if (Restrictions.Length != 0)
		{
			var otherPos = branch.Pos;
			GenerateMessage(ref errors, 0x403C, otherPos);
			parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, "default!"));
			return (false, "default!");
		}
		result.AddRange("new ").AddRange(Type(ref ConstructingNStarType, branch, ref errors));
		if (CallRestrictions != null)
		{
			var (flowControl, value) = PolymorphConstructor(branch, ref errors, extra, CallRestrictions);
			if (flowControl)
				requiredProperties.AddRange(value);
			else
			{
				parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, "default!"));
				return (false, "default!");
			}
		}
		result.AddRange(ConstructorCall(branch, parameters, out var innerErrors, extra));
		AddRange(ref errors, innerErrors);
		if (requiredProperties.Length != 0)
			result.AddRange("{ ").AddRange(requiredProperties).AddRange(" }");
		if (innerErrors != null && innerErrors.Any(x => x.StartsWith("Error")))
		{
			parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (false, "default!"));
			return (false, "default!");
		}
		parsedUserConstructors.TryAdd((ConstructingNStarType, branch), (true, result));
		return (true, result);
	}

	private (bool flowControl, String value) PolymorphConstructor(TreeBranch branch, ref List<String>? errors,
		object? extra, TreeBranch CallRestrictions)
	{
		String result = [];
		if (!(extra is List<object> processingWay && processingWay.Length == 5 && processingWay[0] is String elem1
			&& elem1 == "Constructor" && processingWay[1] is NStarType ConstructingNStarType && processingWay[2]
			is String elem3 && processingWay[3] is ConstructorOverloads constructors && constructors.Length != 0
			&& processingWay[4] is List<String> parameters))
			return (false, "default!");
		var Restrictions = UserDefinedTypes[SplitType(ConstructingNStarType.MainType)].Restrictions;
		var (TypeIndexes, OtherIndexes) = new Chain(Restrictions.Length).BreakFilter(index =>
			!Restrictions[index].Package && Restrictions[index].RestrictionType.Equals(RecursiveType));
		var unsetRequiredProperties = constructors[^1].UnsetRequiredProperties;
		if (unsetRequiredProperties.Contains(-1))
		{
			PrepassClass(branch, out var innerErrors,
				ConstructingNStarType.MainType.Skip(ConstructingNStarType.MainType.FindLastIndex(x =>
				x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface)) + 1)
				.ToList(x => x.Name));
			unsetRequiredProperties = constructors[^1].UnsetRequiredProperties;
			if (unsetRequiredProperties.Contains(-1))
			{
				AddRange(ref errors, innerErrors ?? throw new InvalidOperationException());
				return (false, "default!");
			}
		}
		if (CallRestrictions.Name != nameof(List))
		{
			if (unsetRequiredProperties.Length == 0)
			{
				var properties = GetAllProperties(ConstructingNStarType.MainType);
				String propertyName = default!;
				UserDefinedProperty? property = null;
				if (!properties.Any(x => PropertyExists(ConstructingNStarType, propertyName = x.Key, false, out property)
					&& property.HasValue && (property.Value.Attributes & (PropertyAttributes.Private
					| PropertyAttributes.Protected | PropertyAttributes.Internal | PropertyAttributes.NoSet
				| PropertyAttributes.PrivateSet | PropertyAttributes.ProtectedSet)) == 0)
					|| !property.HasValue)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x403F, otherPos);
					return (false, "default!");
				}
				CallRestrictions.Extra ??= property.Value.NStarType;
				var parsedRestriction = ParseAction(CallRestrictions.Name)(CallRestrictions, out var innerErrors);
				AddRange(ref errors, innerErrors);
				var fullName = String.Join(".",
					ConstructingNStarType.MainType.Convert(x => x.Name).Append(propertyName).ToArray());
				if ((property.Value.Attributes & PropertyAttributes.Private) != 0
					^ (property.Value.Attributes & PropertyAttributes.Protected) != 0
					&& !CallRestrictions.Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if ((property.Value.Attributes & PropertyAttributes.NoSet) != 0)
				{
					var otherPos = branch.FirstPos;
					GenerateMessage(ref errors, 0x4070, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if ((property.Value.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Value.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !CallRestrictions.Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if ((property.Value.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Value.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if (CallRestrictions.Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				else if (!TypesAreCompatible(NStarType, property.Value.NStarType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, Restrictions[0].RestrictionType);
					return (false, "default!");
				}
				result.AddRange(propertyName).AddRange(" = ").AddRange(parsedRestriction);
			}
			else if (TypeIndexes.Equals([0]))
			{
				//CallRestrictions.Extra ??= RecursiveType;
				//var parsedRestriction = ParseAction(CallRestrictions.Name)(CallRestrictions, out innerErrors);
				//AddRange(ref errors, innerErrors);
				//var targetBranch = CallRestrictions.Length != 0 && CallRestrictions.Name == nameof(Hypername)
				//	? CallRestrictions[0] : CallRestrictions;
				//if (targetBranch.Name != "type" || targetBranch.Extra is not NStarType PolymorphNStarType)
				//{
				//	var otherPos = targetBranch.Pos;
				//	GenerateMessage(ref errors, 0x403E, otherPos);
				//	return (false, "default!");
				//}
				//result.AddRange(Type(ref PolymorphNStarType, targetBranch, ref innerErrors));
			}
			else if (OtherIndexes.Equals([0]))
			{
				CallRestrictions.Extra ??= Restrictions[0].RestrictionType;
				var parsedRestriction = ParseAction(CallRestrictions.Name)(CallRestrictions, out var innerErrors);
				AddRange(ref errors, innerErrors);
				if (CallRestrictions.Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				if (!TypesAreCompatible(NStarType, Restrictions[0].RestrictionType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions.Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, Restrictions[0].RestrictionType);
					return (false, "default!");
				}
				result.AddRange(Restrictions[0].Name).AddRange(" = ").AddRange(parsedRestriction);
			}
			else
			{
				var otherPos = CallRestrictions.EndPos;
				GenerateMessage(ref errors, 0x403D, otherPos, Restrictions[1].Name);
				return (false, "default!");
			}
		}
		else
		{
			var unsetRequiredPropertiesCount = unsetRequiredProperties.Length == 0 ? 0 : unsetRequiredProperties.Max() + 1;
			//for (var counter = 0; counter < TypeIndexes.Length; counter++)
			//{
			//	var index = TypeIndexes[counter];
			//	if (index >= CallRestrictions.Length && index < unsetRequiredPropertiesCount)
			//	{
			//		var otherPos = CallRestrictions.EndPos;
			//		GenerateMessage(ref errors, 0x403D, otherPos, Restrictions[index].Name);
			//		return (false, "default!");
			//	}
			//	else if (index >= CallRestrictions.Length)
			//		break;
			//	CallRestrictions[index].Extra ??= RecursiveType;
			//	var parsedRestriction = ParseAction(CallRestrictions[index].Name)(CallRestrictions[index], out var innerErrors);
			//	AddRange(ref errors, innerErrors);
			//	if (counter != 0)
			//		result.AddRange(", ");
			//	var targetBranch = CallRestrictions[index].Length != 0 && CallRestrictions[index].Name == nameof(Hypername)
			//		? CallRestrictions[index][0] : CallRestrictions[index];
			//	if (targetBranch.Name != "type" || targetBranch.Extra is not NStarType PolymorphNStarType)
			//	{
			//		var otherPos = targetBranch.Pos;
			//		GenerateMessage(ref errors, 0x403E, otherPos);
			//		return (false, "default!");
			//	}
			//	result.AddRange(Type(ref PolymorphNStarType, targetBranch, ref innerErrors));
			//}
			for (var counter = 0; counter < OtherIndexes.Length; counter++)
			{
				var index = OtherIndexes[counter];
				if (index >= CallRestrictions.Length && index < unsetRequiredPropertiesCount)
				{
					var otherPos = CallRestrictions.EndPos;
					GenerateMessage(ref errors, 0x403D, otherPos, Restrictions[index].Name);
					return (false, "default!");
				}
				else if (index >= CallRestrictions.Length)
					break;
				CallRestrictions[index].Extra ??= Restrictions[index].RestrictionType;
				var parsedRestriction = ParseAction(CallRestrictions[index].Name)(CallRestrictions[index], out var innerErrors);
				AddRange(ref errors, innerErrors);
				if (counter != 0)
					result.AddRange(", ");
				if (CallRestrictions[index].Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				if (!TypesAreCompatible(NStarType, Restrictions[index].RestrictionType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, Restrictions[0].RestrictionType);
					return (false, "default!");
				}
				result.AddRange(Restrictions[index].Name).AddRange(" = ").AddRange(parsedRestriction);
			}
			var properties = CallRestrictions.Length == Restrictions.Length
				? [] : GetAllProperties(ConstructingNStarType.MainType);
			properties.FilterInPlace(property => (property.Value.Attributes & (PropertyAttributes.Private
				| PropertyAttributes.Protected | PropertyAttributes.Internal | PropertyAttributes.NoSet
				| PropertyAttributes.PrivateSet | PropertyAttributes.ProtectedSet)) == 0);
			if (properties.Length < CallRestrictions.Length - Restrictions.Length)
			{
				var otherPos = CallRestrictions[Restrictions.Length + properties.Length].Pos;
				GenerateMessage(ref errors, 0x403F, otherPos);
				return (false, "default!");
			}
			for (var index = Restrictions.Length; index < CallRestrictions.Length; index++)
			{
				var i = index - Restrictions.Length;
				var (propertyName, property) = properties[i];
				CallRestrictions[index].Extra ??= property.NStarType;
				var parsedRestriction = ParseAction(CallRestrictions[index].Name)(CallRestrictions[index], out var innerErrors);
				AddRange(ref errors, innerErrors);
				var fullName = String.Join(".",
					ConstructingNStarType.MainType.Convert(x => x.Name).Append(propertyName).ToArray());
				if ((property.Attributes & PropertyAttributes.Private) != 0
					^ (property.Attributes & PropertyAttributes.Protected) != 0
					&& !CallRestrictions[index].Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4030, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if ((property.Attributes & PropertyAttributes.PrivateSet) != 0
					^ (property.Attributes & PropertyAttributes.ProtectedSet) != 0
					&& !CallRestrictions[index].Container.StartsWith([.. ConstructingNStarType.MainType]))
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4039, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if ((property.Attributes & PropertyAttributes.SetOnce) != 0
					&& (property.Attributes & PropertyAttributes.Static) != 0)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x403B, otherPos, fullName);
					branch.Parent!.Name = "null";
					branch.Parent.Elements.Clear();
					branch.Parent.Extra = NullType;
					return (false, "default!");
				}
				else if (CallRestrictions[index].Extra is not NStarType NStarType)
					throw new InvalidOperationException();
				else if (!TypesAreCompatible(NStarType, property.NStarType,
					out var warning, parsedRestriction, out _, out var extraMessage) || warning)
				{
					var otherPos = CallRestrictions[index].Pos;
					GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, property.NStarType);
					return (false, "default!");
				}
				if (index != 0)
					result.AddRange(", ");
				result.AddRange(propertyName).AddRange(" = ").AddRange(parsedRestriction);
			}
		}
		return (true, result);
	}

	private String Indexes(TreeBranch branch, ref List<String>? errors, object? extra, int index)
	{
		String result = [];
		if (branch[index - 1].Extra is not NStarType CollectionNStarType)
			return "default!";
		if (!(extra is List<object> paramCollection && paramCollection.Length == 3 && paramCollection[0] is String elem1
			&& elem1.AsSpan() is "Variable" or nameof(Property) or nameof(Expr)
			&& paramCollection[1] is NStarType CollectionNStarType2
			&& CollectionNStarType.Equals(CollectionNStarType2)
			&& paramCollection[2] is List<String> indexValues))
			return "default!";
		var OldCollectionNStarType = CollectionNStarType;
		var rangeDepth = 0;
		bool range = false, oldRange = false;
		for (var i = 0; i < indexValues.Length; i++)
		{
			var x = indexValues[i];
			if (oldRange)
			{
				var randomName = RandomVarName();
				result.AddRange(".Convert(").AddRange(randomName).AddRange(" => ").AddRange(randomName);
				rangeDepth++;
			}
			int repeatsCount;
			if (TypeEqualsToPrimitive(CollectionNStarType, "tuple", false))
			{
				if (!int.TryParse(x.ToString(), out repeatsCount))
				{
					var otherPos = branch[index].Pos;
					GenerateMessage(ref errors, 0x400B, otherPos);
					return "default!";
				}
				if (repeatsCount <= 0)
				{
					var otherPos = branch[index].Pos;
					GenerateMessage(ref errors, 0x4016, otherPos);
					return "default!";
				}
				result.AddRange(".Item").AddRange(repeatsCount.ToString());
				CollectionNStarType = CollectionNStarType.ExtraTypes[repeatsCount - 1].Name == "type"
					&& CollectionNStarType.ExtraTypes[repeatsCount - 1].Extra is NStarType InnerNStarType
					? InnerNStarType : NullType;
				oldRange = false;
				continue;
			}
			var trivialIndex = IsTrivialIndexType(CollectionNStarType) && branch[index][i].Extra is NStarType IndexNStarType
				&& !IndexNStarType.Equals(IndexType) && !(range = IndexNStarType.Equals(RangeType));
			if (trivialIndex && int.TryParse(x.ToString(), out repeatsCount) && repeatsCount <= 0)
			{
				var otherPos = branch[index].Pos;
				GenerateMessage(ref errors, 0x4016, otherPos);
				return "default!";
			}
			if (trivialIndex)
				result.AddRange("[(");
			else
				result.Add('[');
			result.AddRange(x);
			if (trivialIndex)
				result.AddRange(") - 1]");
			else
				result.Add(']');
			if (!range)
			{
				if (CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Class,
					nameof(Dictionary<,>), 1)]))
					|| CollectionNStarType.MainType.Equals(new BlockStack([new(BlockType.Namespace, "System", 1),
					new(BlockType.Namespace, "Collections", 1), new(BlockType.Class, nameof(Dictionary<,>), 1)])))
					CollectionNStarType = (NStarType)CollectionNStarType.ExtraTypes[^1].Extra!;
				else
					CollectionNStarType = GetSubtype(CollectionNStarType);
			}
			oldRange = range;
		}
		result.AddRange(new(')', rangeDepth));
		branch.Extra = CollectionNStarType;
		paramCollection[1] = CollectionNStarType;
		return result;
		static bool IsTrivialIndexType(NStarType CollectionNStarType)
		{
			if (TypeEqualsToPrimitive(CollectionNStarType, "list", false))
				return true;
			if (CollectionNStarType.ExtraTypes.Length == 1 && TypesAreCompatible(CollectionNStarType,
				new(IEnumerableBlockStack, CollectionNStarType.ExtraTypes),
				out var warning, null, out _, out _) && !warning)
				return true;
			if (CollectionNStarType.ExtraTypes.Length != 2 || CollectionNStarType.ExtraTypes[0].Name != "type"
				|| CollectionNStarType.ExtraTypes[0].Extra is not NStarType FirstNStarType
				|| CollectionNStarType.ExtraTypes[1].Name != "type"
				|| CollectionNStarType.ExtraTypes[1].Extra is not NStarType SecondNStarType)
				return false;
			if (!CollectionNStarType.MainType.Equals(FirstNStarType.MainType))
				return false;
			if (SecondNStarType.ExtraTypes.Length != 1 || SecondNStarType.ExtraTypes[0].Name != "type"
				|| SecondNStarType.ExtraTypes[0].Extra is not NStarType SecondInnerNStarType)
				return false;
			if (!FirstNStarType.Equals(SecondInnerNStarType))
				return false;
			return TypesAreCompatible(CollectionNStarType,
				new(BaseIndexableBlockStack, CollectionNStarType.ExtraTypes),
				out warning, null, out _, out _) && !warning;
		}
	}

	private bool? HypernameMethod(TreeBranch branch, String name, List<String> parameters, ref object? refExtra,
		ref List<String>? errors, int prevIndex, NStarType ContainerNStarType, UserDefinedMethodOverloads functions)
	{
		NStarType NStarType;
		foreach (var function in functions)
		{
			if ((function.Attributes & FunctionAttributes.Private) != 0
				^ (function.Attributes & FunctionAttributes.Protected) != 0
				&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				continue;
			else if ((function.Attributes & FunctionAttributes.Static) == 0
				&& !(branch.Length >= 2 && branch[1].Name == nameof(Call)))
				continue;
			NStarType = function.ReturnNStarType;
			List<object> paramCollection = [((String)"Function ").AddRange(name), (String)"method", functions, parameters];
			if ((function!.Attributes & FunctionAttributes.Static) != 0)
				paramCollection.Add("static");
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			PropagateParameterTypes(branch, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Params, parameterTypes);
			return null;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos,
			String.Join(".", [.. ContainerNStarType.MainType.ToList().Convert(x => x.Name), name]));
		branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
		return false;
	}

	private bool? HypernameExtendedMethod(TreeBranch branch, String name, List<String> parameters,
		ref object? refExtra, ref List<String>? errors, int prevIndex, NStarType ContainerNStarType,
		UserDefinedMethodOverloads functions, String category)
	{
		NStarType NStarType;
		foreach (var function in functions)
		{
			if ((function.Attributes & FunctionAttributes.Private) != 0
				^ (function.Attributes & FunctionAttributes.Protected) != 0
				&& !branch.Container.StartsWith([.. ContainerNStarType.MainType]))
				continue;
			NStarType = function!.ReturnNStarType;
			List<object> paramCollection = [((String)"Function ").AddRange(name), category, functions, parameters];
			if ((function.Attributes & FunctionAttributes.Static) != 0)
				paramCollection.Add("static");
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			PropagateParameterTypes(branch, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Params, parameterTypes);
			return null;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos,
			String.Join(".", [.. ContainerNStarType.MainType.ToList().Convert(x => x.Name), name]));
		branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
		return false;
	}

	private bool? HypernamePublicExtendedMethod(TreeBranch branch, String name, List<String> parameters,
		ref object? refExtra, ref List<String>? errors, int prevIndex, UserDefinedMethodOverloads functions, String category)
	{
		foreach (var function in functions)
		{
			var NStarType = function.ReturnNStarType;
			List<object> paramCollection = [((String)"Function ").AddRange(name), category, functions, parameters];
			TreeBranch newBranch = new("type", branch.Pos, branch.Container) { Extra = NStarType };
			BranchCollection parameterTypes = new(function.Parameters.Convert(x =>
			new TreeBranch("type", branch.Pos, branch.Container) { Extra = x.Type }).Append(newBranch).ToList()
				?? [newBranch]);
			HypernameAddExtra(branch, function.RealName, NStarType, paramCollection, ref refExtra, parameterTypes);
			PropagateParameterTypes(branch, name, function.Parameters.Length != 0
				&& (function.Parameters[^1].Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Params, parameterTypes);
			return null;
		}
		var otherPos = branch.FirstPos;
		GenerateMessage(ref errors, 0x4021, otherPos, name);
		branch.Parent![prevIndex] = new("null", branch.Pos, branch.EndPos, branch.Container) { Extra = NullType };
		return false;
	}

	private static void HypernameAddExtra(TreeBranch branch, String realName, NStarType extra, List<object> paramCollection,
		ref object? refExtra, BranchCollection extraTypes)
	{
		if (branch.Length >= 2 && branch[1].Name == nameof(Call))
		{
			branch[0].Name.AddRange(" (function)");
			branch[0].Extra = extra;
			refExtra = paramCollection;
		}
		else
		{
			branch[0].Name.Replace(realName).AddRange(" (delegate)");
			branch[0].Extra = new NStarType(FuncBlockStack, extraTypes);
			branch[0].Insert(0, (TreeBranch)new("data", branch.Pos, branch.EndPos, branch.Container)
			{
				Extra = paramCollection
			});
		}
	}

	private void PropagateParameterTypes(TreeBranch branch, String name, bool @params, BranchCollection parameterTypes)
	{
		if (branch.Length < 2)
			return;
		for (var i = 0; i < branch[1].Length; i++)
		{
			var x = branch[1][i];
			if (parameterTypes[@params ? ^1 : i].Extra is NStarType DestinationType && (x.Extra is not NStarType SourceType
				|| i >= parameterTypes.Length - 1 && @params && GetSubtype(SourceType).Equals(DestinationType)
				|| TypesAreCompatible(SourceType, DestinationType, out var warning, [], out _, out _) && !warning
				&& !(name.StartsWith(nameof(branch.Add)) && GetListType(SourceType).Equals(DestinationType))
				&& !(DestinationType.MainType.TryPeek(out var block) && block.Name.Contains("Number"))))
			{
				x.Extra = DestinationType;
				ParseAction(x.Name)(x, out _);
			}
		}
	}

	private List<String>? Call(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		List<String> result = [];
		errors = null;
		for (var i = 0; i < branch.Length; i++)
		{
			var innerResult = parameters[i];
			if (innerResult.Length != 0)
				result.Add(innerResult);
		}
		if (!CallCheck(branch, ref errors, parameters, extra))
			return null;
		if (branch.Length != 0 && branch[0].Length == 1 && branch[0][0].Name.EndsWith(" (delegate)"))
			return branch[0][0].Name[..^" (delegate)".Length];
		return result;
	}

	private String CallUser(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		var callResult = Call(branch, parameters, out errors, extra);
		if (callResult == null)
			return [];
		var joined = String.Join(", ", callResult);
		return joined.Insert(0, '(').Add(')');
	}

	private bool CallCheck(TreeBranch branch, ref List<String>? errors, List<String> parameters, object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<NStarType> CallParameterNStarTypes = [];
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is NStarType type)
				CallParameterNStarTypes.Add(type);
			else
			{
				GenerateMessage(ref errors, 0x4000, otherPos);
				return false;
			}
		if (!(extra is List<object> paramCollection && paramCollection.Length >= 3 && paramCollection.Length <= 5
			&& paramCollection[0] is String elem1 && elem1.StartsWith("Function ")
			&& paramCollection[2] is UserDefinedMethodOverloads functions && functions.Length != 0))
		{
			GenerateMessage(ref errors, 0x4000, otherPos);
			return false;
		}
		elem1 = elem1["Function ".Length..];
		var max = functions.Any(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
			& ParameterAttributes.Params) == ParameterAttributes.Params)
			? int.MaxValue : functions.Max(x => x.Parameters.Length);
		var min = functions.Min(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0));
		if (CallParameterNStarTypes.Length > max || CallParameterNStarTypes.Length < min)
		{
			GenerateMessage(ref errors, 0x4022, otherPos, elem1, max, min);
			return false;
		}
		functions.FilterInPlace(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
			& ParameterAttributes.Params) != 0 || x.Parameters.Length >= CallParameterNStarTypes.Length)
			.FilterInPlace(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			<= CallParameterNStarTypes.Length);
		var warnings = new bool[CallParameterNStarTypes.Length];
		var FunctionParameterNStarTypes = new NStarType[CallParameterNStarTypes.Length];
		var adaptedInnerResults = new String[CallParameterNStarTypes.Length];
		var extraMessages = new String[CallParameterNStarTypes.Length];
		int callIndex = 0, functionIndex = 0;
		if (functions.Length == 1)
		{
			var (_, _, ReturnNStarType, Attributes, Parameters) = functions[0];
			if (Parameters.Length == 0 && parameters.Length != 0)
			{
				GenerateMessage(ref errors, 0x4023, otherPos, elem1);
				return false;
			}
			else if (Parameters.Length == 0)
				return true;
			else if (Parameters.Any((x, i) => (callIndex = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Ref && !parameters[callIndex].StartsWith("ref ")))
			{
				GenerateMessage(ref errors, 0x9013, otherPos = branch[callIndex].Pos, "ref");
				wreckOccurred = true;
				return true;
			}
			else if (Parameters.Any((x, i) => (callIndex = i) >= 0 && (x.Attributes & ParameterAttributes.Params)
				== ParameterAttributes.Out && !parameters[callIndex].StartsWith("out ")))
			{
				GenerateMessage(ref errors, 0x9013, otherPos = branch[callIndex].Pos, "out");
				wreckOccurred = true;
				return true;
			}
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters).All((x, i) =>
				TypesAreCompatible(x.Item1, FunctionParameterNStarTypes[i] = x.Item2.Type,
				out warnings[functionIndex = callIndex = i], parameters[i],
				out adaptedInnerResults[i]!, out extraMessages[i]!) && adaptedInnerResults[i] != null)
				&& CallParameterNStarTypes.Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndex == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(functionIndex = callIndex).All((x, i) => TypesAreCompatible(x,
				Parameters[^1].Type, out warnings[callIndex = functionIndex + i],
				parameters[callIndex], out adaptedInnerResults[callIndex]!, out extraMessages[callIndex]!)
				&& adaptedInnerResults[callIndex] != null)))
			{
				GenerateMessage(ref errors, 0x4026, otherPos = branch[callIndex].Pos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[functionIndex],
					FunctionParameterNStarTypes[callIndex]);
				return false;
			}
			else if (callIndex < warnings.Length && warnings[callIndex])
			{
				GenerateMessage(ref errors, 0x4027, otherPos = branch[callIndex].Pos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[functionIndex]);
				return false;
			}
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[i]
				? x.Replace(adaptedInnerResults[i] ?? "default!") : "");
			branch.Extra = ReturnNStarType;
			return true;
		}
		ListHashSet<int> IncompatibleOverloads = [];
		ListHashSet<(int OverloadIndex, int ParameterIndex)> BadlyCompatibleOverloads = [];
		var callIndexes = new int[functions.Length];
		var functionIndexes = new int[functions.Length];
		for (var j = 0; j < functions.Length; j++)
		{
			var (_, _, ReturnNStarType, Attributes, Parameters) = functions[j];
			if (Parameters.Length == 0)
				continue;
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters).All((x, i) =>
				TypesAreCompatible(x.Item1, FunctionParameterNStarTypes[i] = x.Item2.Type,
				out warnings[functionIndexes[j] = callIndexes[j] = i], parameters[i],
				out adaptedInnerResults[i]!, out extraMessages[i]!) && adaptedInnerResults[i] != null)
				&& CallParameterNStarTypes.Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndexes[j] == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(functionIndexes[j] = callIndexes[j]).All((x, i) => TypesAreCompatible(x,
				Parameters[^1].Type, out warnings[callIndexes[j] = functionIndexes[j] + i],
				parameters[callIndexes[j]], out adaptedInnerResults[callIndexes[j]]!, out extraMessages[callIndexes[j]]!)
				&& adaptedInnerResults[callIndexes[j]] != null)))
				IncompatibleOverloads.Add(j);
			else if (warnings.Any(x => x))
				_ = warnings.ToList((x, i) => x ? BadlyCompatibleOverloads.Add((j, i)) : default);
		}
		var thresholdIndexes = callIndexes.IndexesOfMax();
		var incompatibleLength = IncompatibleOverloads.Length;
		if (incompatibleLength == functions.Length)
		{
			GenerateMessage(ref errors, 0x4028, otherPos = branch[callIndexes[thresholdIndexes[0]]].Pos,
				CallParameterNStarTypes[callIndexes[thresholdIndexes[0]]],
				String.Join("\", \"", IncompatibleOverloads.Convert(j =>
				functions[j].Parameters[functionIndexes[thresholdIndexes[j]]].Type.ToString()).ToHashSet()),
				IncompatibleOverloads.Length, functions[IncompatibleOverloads[0]]
				.Parameters[functionIndexes[thresholdIndexes[0]]].Type);
			return false;
		}
		BadlyCompatibleOverloads.FilterInPlace(x => !IncompatibleOverloads.Contains(x.OverloadIndex));
		var bcoGroups = BadlyCompatibleOverloads.Group(x => x.ParameterIndex);
		var WellCompatibleOverloads = new Chain(functions.Length).ToHashSet()
			.ExceptWith(IncompatibleOverloads).ExceptWith(bcoGroups.ConvertAndJoin(x => x).Convert(x => x.OverloadIndex));
		if (WellCompatibleOverloads.Length != 0)
		{
			branch.Extra = functions[WellCompatibleOverloads[^1]].ReturnNStarType;
			return true;
		}
		foreach (var bcoGroup in bcoGroups)
			GenerateMessage(ref errors, 0x4029, otherPos = branch[bcoGroup.Key].Pos, CallParameterNStarTypes[bcoGroup.Key],
				String.Join("\", \"", bcoGroup.Convert(bco => functions[bco.OverloadIndex].Parameters.Wrap(x =>
				x[callIndex = Min(bcoGroup.Key, x.Length - 1)].Type.ToString())).ToHashSet()));
		_ = parameters.ToList((x, i) => x != adaptedInnerResults[i]
			? x.Replace(adaptedInnerResults[i] ?? "default!") : "");
		branch.Extra = NullType;
		return true;
	}

	private String ConstructorCall(TreeBranch branch, List<String> parameters, out List<String>? errors, object? extra = null)
	{
		String result = "(";
		errors = null;
		if (!ConstructorCallCheck(branch, ref errors, parameters, extra))
			return [];
		return result.AddRange(String.Join(", ", parameters)).Add(')');
	}

	private bool ConstructorCallCheck(TreeBranch branch, ref List<String>? errors, List<String> parameters,
		object? extra = null)
	{
		var otherPos = branch.FirstPos;
		List<NStarType> CallParameterNStarTypes = [];
		for (var i = 0; i < branch.Length; i++)
			if (branch[i].Extra is NStarType type)
				CallParameterNStarTypes.Add(type);
			else
			{
				GenerateMessage(ref errors, 0x4000, otherPos);
				return false;
			}
		if (!(extra is List<object> paramCollection
			&& paramCollection.Length >= 4 && paramCollection.Length <= 5 && paramCollection[0] is String elem1
			&& elem1 == "Constructor" && paramCollection[1] is NStarType ConstructingNStarType
			&& paramCollection[2] is String elem3 && paramCollection[3] is ConstructorOverloads constructors
			&& constructors.Length != 0))
		{
			GenerateMessage(ref errors, 0x4000, otherPos);
			return false;
		}
		var max = constructors.Any(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
		& ParameterAttributes.Params) != 0) ? int.MaxValue : constructors.Max(x => x.Parameters.Length);
		var min = constructors.Min(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0));
		if (CallParameterNStarTypes.Length > max || CallParameterNStarTypes.Length < min)
		{
			GenerateMessage(ref errors, 0x4060, otherPos, ConstructingNStarType, max, min);
			return false;
		}
		constructors.FilterInPlace(x => x.Parameters.Length != 0 && (x.Parameters[^1].Attributes
			& ParameterAttributes.Params) != 0 || x.Parameters.Length >= CallParameterNStarTypes.Length)
			.FilterInPlace(x => x.Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
			<= CallParameterNStarTypes.Length);
		var warnings = new bool[CallParameterNStarTypes.Length];
		var FunctionParameterNStarTypes = new NStarType[CallParameterNStarTypes.Length];
		var adaptedInnerResults = RedStarLinq.FillArray(constructors.Length, _ => new String[CallParameterNStarTypes.Length]);
		var extraMessages = new String[CallParameterNStarTypes.Length];
		int callIndex = 0, constructorIndex = 0;
		if (constructors.Length == 1)
		{
			var (Attributes, Parameters, _) = constructors[0];
			if (Parameters.Length == 0 && parameters.Length != 0)
			{
				GenerateMessage(ref errors, 0x4034, otherPos, ConstructingNStarType);
				return false;
			}
			else if (Parameters.Length == 0)
				return true;
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters).All((x, i) => TypesAreCompatible(x.Item1,
				FunctionParameterNStarTypes[i] = x.Item2.Type, out warnings[constructorIndex = callIndex = i],
				parameters[i], out adaptedInnerResults[0][i]!, out extraMessages[i]!) && adaptedInnerResults[0][i] != null))
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndex == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(constructorIndex = callIndex).All((x, i) =>
				TypesAreCompatible(x, Parameters[^1].Type, out warnings[callIndex = constructorIndex + i],
				parameters[callIndex], out adaptedInnerResults[0][callIndex]!, out extraMessages[callIndex]!)
				&& adaptedInnerResults[0][callIndex] != null)))
			{
				GenerateMessage(ref errors, 0x4061, otherPos = branch[callIndex].Pos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[constructorIndex],
					FunctionParameterNStarTypes[callIndex]);
				return false;
			}
			else if (warnings[callIndex])
			{
				GenerateMessage(ref errors, 0x4027, otherPos = branch[callIndex].Pos, extraMessages[callIndex],
					CallParameterNStarTypes[callIndex], FunctionParameterNStarTypes[constructorIndex]);
				return false;
			}
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[0][i]
				? x.Replace(adaptedInnerResults[0][i] ?? "default!") : "");
			branch.Extra = ConstructingNStarType;
			return true;
		}
		ListHashSet<int> IncompatibleConstructors = [];
		ListHashSet<(int ConstructorIndex, int ParameterIndex)> BadlyCompatibleConstructors = [];
		var callIndexes = new int[constructors.Length];
		var constructorIndexes = new int[constructors.Length];
		for (var j = 0; j < constructors.Length; j++)
		{
			var (Attributes, Parameters, _) = constructors[j];
			if (Parameters.Length == 0)
				continue;
			else if (!(CallParameterNStarTypes.Length
				>= Parameters.Count(y => (y.Attributes & ParameterAttributes.Optional) == 0)
				&& CallParameterNStarTypes.Combine(Parameters).All((x, i) =>
				TypesAreCompatible(x.Item1, FunctionParameterNStarTypes[i] = x.Item2.Type,
				out warnings[constructorIndexes[j] = callIndexes[j] = i], parameters[i].Copy(),
				out adaptedInnerResults[j][i]!, out extraMessages[i]!) && adaptedInnerResults[j][i] != null)
				&& CallParameterNStarTypes.Length <= Parameters.Length)
				&& !((Parameters[^1].Attributes & ParameterAttributes.Params) == ParameterAttributes.Params
				&& callIndexes[j] == Parameters.Length - 1
				&& CallParameterNStarTypes.Skip(constructorIndexes[j] = callIndexes[j]).All((x, i) =>
				TypesAreCompatible(x, Parameters[^1].Type, out warnings[callIndexes[j] = constructorIndexes[j] + i],
				parameters[callIndexes[j]], out adaptedInnerResults[j][callIndexes[j]]!, out extraMessages[callIndexes[j]]!)
				&& adaptedInnerResults[j][callIndexes[j]] != null)))
				IncompatibleConstructors.Add(j);
			else if (warnings.Any(x => x))
				_ = warnings.ToList((x, i) => x ? BadlyCompatibleConstructors.Add((j, i)) : default);
		}
		var thresholdIndexes = callIndexes.IndexesOfMax();
		var incompatibleLength = IncompatibleConstructors.Length;
		if (incompatibleLength == constructors.Length)
		{
			GenerateMessage(ref errors, 0x4062, otherPos = branch[callIndexes[thresholdIndexes[0]]].Pos,
				CallParameterNStarTypes[callIndexes[thresholdIndexes[0]]], String.Join("\", \"",
				IncompatibleConstructors.Convert(j =>
				constructors[j].Parameters[constructorIndexes[thresholdIndexes[0]]].Type.ToString()).ToHashSet()),
				IncompatibleConstructors.Length,
				constructors[IncompatibleConstructors[0]].Parameters[constructorIndexes[thresholdIndexes[0]]].Type);
			return false;
		}
		BadlyCompatibleConstructors.FilterInPlace(x => !IncompatibleConstructors.Contains(x.ConstructorIndex));
		var bccGroups = BadlyCompatibleConstructors.Group(x => x.ParameterIndex);
		var WellCompatibleConstructors = new Chain(constructors.Length).ToHashSet()
			.ExceptWith(IncompatibleConstructors).ExceptWith(bccGroups.ConvertAndJoin(x => x).Convert(x => x.ConstructorIndex));
		if (WellCompatibleConstructors.Length != 0)
		{
			_ = parameters.ToList((x, i) => x != adaptedInnerResults[WellCompatibleConstructors[^1]][i]
				? x.Replace(adaptedInnerResults[WellCompatibleConstructors[^1]][i] ?? "default!") : "");
			branch.Extra = ConstructingNStarType;
			return true;
		}
		foreach (var bccGroup in bccGroups)
		{
			GenerateMessage(ref errors, 0x4029, otherPos = branch[bccGroup.Key].Pos, CallParameterNStarTypes[bccGroup.Key],
				String.Join("\", \"", bccGroup.Convert(item => constructors[item.ConstructorIndex].Parameters.Wrap(x =>
				x[callIndex = Min(bccGroup.Key, x.Length - 1)].Type.ToString())).ToHashSet()));
			return false;
		}
		_ = parameters.ToList((x, i) => x != adaptedInnerResults[BadlyCompatibleConstructors[^1].ConstructorIndex][i]
			? x.Replace(adaptedInnerResults[BadlyCompatibleConstructors[^1].ConstructorIndex][i] ?? "default!") : "");
		branch.Extra = ConstructingNStarType;
		return true;
	}

	private String Expr(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		using List<String> subbranchValues = [];
		int i;
		if (branch.Name.AsSpan() is nameof(Assignment) or "DeclarationAssignment")
		{
			for (i = branch.Length - 2; i > 0; i -= 2)
			{
				if ((branch[i].Name == nameof(Hypername) && branch[i].Length == 0
					|| branch[i].Name == nameof(Declaration)) && branch[i + 1].Name == "=")
					continue;
				i -= 2;
				break;
			}
			List<String>? innerErrors;
			if (branch[i + 2].Name == nameof(Hypername))
				Hypername(branch[i + 2], out innerErrors, null, true);
			else
				Declaration(branch[i + 2], out innerErrors, true);
			AddRange(ref errors, innerErrors);
		}
		if (TryReadValue(branch.Name, out var value))
		{
			branch.Extra = value.InnerType;
			return value.ToString(true, true);
		}
		if (branch.Length == 1)
			branch[0].Extra ??= branch.Extra;
		for (i = 0; i < branch.Length; i++)
		{
			if (branch[i].Name == "type")
			{
				subbranchValues.SetOrAdd(i, "typeof(" + (branch[0].Extra is NStarType type
					? Type(ref type, branch[0], ref errors) : "dynamic") + ")");
				continue;
			}
			else if (branch[i].Name == nameof(Hypername) && branch[i].Length == 1)
			{
				object? none = null;
				subbranchValues.SetOrAdd(i, Hypername1(branch[i], out var innerErrors, ref none, false));
				AddRange(ref errors, innerErrors);
				continue;
			}
			else if (ExprTypes.Contains(branch[i].Name.ToString()))
			{
				subbranchValues.SetOrAdd(i, ParseAction(branch[i].Name)(branch[i], out var innerErrors));
				AddRange(ref errors, innerErrors);
				continue;
			}
			else if (branch[i].Name == "typeof")
			{
				subbranchValues.SetOrAdd(i, Typeof(branch[i], out var innerErrors));
				AddRange(ref errors, innerErrors);
				continue;
			}
			else if (TryReadValue(branch[i].Name, out value))
			{
				branch[i].Extra = value.InnerType;
				subbranchValues.SetOrAdd(i, value.ToString(true, true));
				continue;
			}
			else if (i == 1 && subbranchValues.Length == 1 && TryReadValue(branch[0].Name, out value)
				&& branch[i].Name != "^")
			{
				subbranchValues.SetOrAdd(0, ValueExpr(value, branch, ref errors, i--));
				branch.RemoveAt(0);
				if (branch.Length == 1)
				{
					branch.Name = branch[0].Name;
					branch.Extra = branch[0].Extra;
					branch.RemoveAt(0);
				}
				continue;
			}
			else if (i == 0 || i % 2 != 0)
				return branch.Length == 2 && i == 1 ? UnaryExpr(branch, ref errors, i)
					: ListExpr(branch, ref errors, i);
			if (branch[i - 2].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			var resultType = GetResultType(LeftNStarType, RightNStarType,
				subbranchValues[^2].Copy(), subbranchValues[^1].Copy());
			String @default = "default";
			if (!(branch.Parent?.Name == "return"
				|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == "return"))
			{
				@default.Add('(');
				@default.AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String"
					: Type(ref resultType, branch, ref errors)).Add(')');
			}
			@default.Add('!');
			if (!TryReadValue(branch[i].Name, out _) && branch[i].Name.AsSpan() is not ("pow" or "tetra" or "penta"
				or "hexa" or "..") && !AssignmentOperators.Contains(branch[i].Name.ToString())
				&& !TernaryOperators.Contains(branch[i].Name.ToString()) && branch[i].Name != ":"
				&& TryReadValue(branch[Max(i - 3, 0)].Name, out var leftValue)
				&& TryReadValue(branch[i - 1].Name, out var rightValue))
			{
				var innerResult = new TwoValuesExpr(leftValue, rightValue, branch, lexems, @default)
					.Calculate(ref errors, ref i);
				subbranchValues.SetOrAdd(i, innerResult);
				continue;
			}
			subbranchValues.SetOrAdd(i, branch[i].Name.ToString() switch
			{
				"+" or "-" => PMExpr(branch, subbranchValues, ref errors, ref i),
				"*" or "/" or "%" => MulDivExpr(branch, subbranchValues, ref errors, ref i),
				"pow" or "tetra" or "penta" or "hexa" => PowExpr(branch, subbranchValues, ref errors, i),
				".." => RangeExpr(branch, subbranchValues, ref errors, i),
				"==" or ">" or "<" or ">=" or "<=" or "!=" or "&&" or "||" or "^^" =>
					BoolExpr(branch, subbranchValues, ref errors, i),
				":" => Ternary(branch, subbranchValues, ref errors, i),
				"CombineWith" => CombineWithExpr(branch, subbranchValues, i),
				nameof(List) => ListExpr(branch, ref errors, i),
				_ when AssignmentOperators.Contains(branch[i].Name.ToString()) => Assignment(branch, subbranchValues, ref errors, i),
				_ when TernaryOperators.Contains(branch[i].Name.ToString()) =>
					branch.Length > i + 2 ? branch[i].Name : Ternary(branch, subbranchValues, ref errors, i),
				_ => BinaryNotListExpr(branch, ref errors, subbranchValues, i),
			});
		}
		var prevIndex = branch.Parent!.Elements.FindIndex(x => ReferenceEquals(branch, x));
		if (branch.Name == "StringConcatenation")
		{
			branch.Elements.FilterInPlace(x => x.Name != "+");
			branch.Extra = GetPrimitiveType("string");
		}
		else if (branch.Name == nameof(List))
			branch.Extra = branch.Elements.Progression(GetListType(NullType), (x, y) =>
			GetResultType(x, GetListType(y.Extra is NStarType NStarType ? NStarType : NullType), "default!", "default!"));
		else if (branch.Name == nameof(Indexes))
		{
			if (prevIndex >= 1 && branch.Parent[prevIndex - 1].Extra is NStarType NStarType)
				branch.Extra = GetSubtype(NStarType, branch.Length);
			else
				branch.Extra = NullType;
		}
		else if (branch.Length == 1 && ArithmeticExprTypes.Contains(branch.Parent.Name.ToString()))
			branch.Replace(branch[0]);
		else if (branch.Length != 0)
		{
			if (branch[^1].Extra is not NStarType NStarType)
				branch.Extra = NullType;
			else if (branch.Extra is NStarType BranchNStarType && branch.Parent.Name != "return"
				&& (!TypesAreCompatible(NStarType, BranchNStarType, out var warning,
				[], out _, out var extraMessage)
				|| warning))
			{
				GenerateMessage(ref errors, 0x4014, branch.Pos, extraMessage!, NStarType, BranchNStarType);
				return "default!";
			}
			else
				branch.Extra = NStarType;
		}
		return subbranchValues[i - 1];
	}

	private String ValueExpr(NStarEntity source, TreeBranch branch, ref List<String>? errors, int i)
	{
		var otherPos = branch[i].Pos;
		NStarEntity result;
		double realValue;
		switch (branch[i].Name.ToString())
		{
			case "+":
			result = +source;
			branch[i].Name = result.InnerType.Equals(ComplexType) ? result.ToString(true, true) : result.ToString(true);
			if (branch[0].Name.Length != 0 && branch[0].Name[^1] is 'r' or 'c' or 'I'
				&& double.TryParse(branch[i].Name.ToString(), out _))
				branch[i].Name.Add(branch[0].Name[^1]);
			branch[i].Extra = result.InnerType;
			return result.ToString(true, true);
			case "-":
			result = -source;
			branch[i].Name = result.InnerType.Equals(ComplexType) ? result.ToString(true, true)
				: result.ToString(true).AddRange(branch[0].Name.Length != 0
				&& branch[0].Name[^1] is 'r' or 'c' or 'I' ? branch[0].Name[^1] : []);
			if (branch[0].Name.Length != 0 && branch[0].Name[^1] is 'r' or 'c' or 'I'
				&& double.TryParse(branch[i].Name.ToString(), out _))
				branch[i].Name.Add(branch[0].Name[^1]);
			branch[i].Extra = result.InnerType;
			return result.ToString(true, true);
			case "!":
			result = !source;
			branch[i].Name = result.ToString(true);
			branch[i].Extra = result.InnerType;
			return result.ToString(true, true);
			case "~":
			result = ~source;
			branch[i].Name = result.ToString(true);
			branch[i].Extra = result.InnerType;
			return result.ToString(true, true);
			case "sin":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Sin(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "cos":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Cos(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "tan":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Tan(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "asin":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Asin(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "acos":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Acos(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "atan":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Atan(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "ln":
			realValue = source.ToReal();
			if (source != 0 && realValue != source)
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Log(realValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4002, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			case "postfix !":
			var unsignedIntValue = source.ToUnsignedInt();
			if (source != 0 && unsignedIntValue != source)
			{
				GenerateMessage(ref errors, 0x4003, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
			try
			{
				result = Factorial(unsignedIntValue);
				branch[i].Name = result.ToString(true);
				branch[i].Extra = result.InnerType;
				return result.ToString(true, true);
			}
			catch
			{
				GenerateMessage(ref errors, 0x4003, otherPos);
				branch[i].Name = "null";
				return "default!";
			}
		}
		branch[i].Name = "null";
		return "default!";
	}

	private String UnaryExpr(TreeBranch branch, ref List<String>? errors, int i)
	{
		if (branch[i].Name.AsSpan() is "++" or "--" or "postfix ++" or "postfix --" or "!!")
			branch.Name = "UnaryAssignment";
		if (branch[i - 1].Extra is not NStarType NStarType)
			NStarType = NullType;
		if (!(TypeIsPrimitive(NStarType.MainType) && (branch[i].Name == "^" ? NStarType.MainType.Peek().Name.ToString()
			is "byte" or "short int" or "unsigned short int" or "int"
			: NStarType.MainType.Peek().Name.AsSpan() is "bool" or "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex")))
		{
			GenerateMessage(ref errors, 0x4005, branch[i].Pos, branch[i].Name, NStarType);
			return "default!";
		}
		branch[i].Extra = NStarType;
		var valueString = ParseAction(branch[i - 1].Name)(branch[i - 1], out var innerErrors);
		if (valueString.Length == 0)
			return "default!";
		AddRange(ref errors, innerErrors);
		branch.Extra = branch[i].Name.ToString() switch
		{
			"+" or "-" or "~" => TypeEqualsToPrimitive(NStarType, "bool") || TypeEqualsToPrimitive(NStarType, "string")
				? RealType : TypeEqualsToPrimitive(NStarType, "byte")
				? ShortIntType : TypeEqualsToPrimitive(NStarType, "unsigned short int")
				? IntType : TypeEqualsToPrimitive(NStarType, "unsigned int")
				? LongIntType : TypeEqualsToPrimitive(NStarType, "unsigned long int") ? LongLongType : NStarType,
			"!" => BoolType,
			"^" => IndexType,
			"sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "ln" or "postfix !" =>
				TypeEqualsToPrimitive(NStarType, "complex") ? ComplexType : RealType,
			"++" or "--" or "postfix ++" or "postfix --" or "!!" => NStarType,
			_ => NullType,
		};
		return branch[i].Name.ToString() switch
		{
			"+" => valueString.Insert(0, "(+(").AddRange("))"),
			"-" => valueString.Insert(0, "(-(").AddRange("))"),
			"!" => valueString.Insert(0, "(!(").AddRange("))"),
			"~" => valueString.Insert(0, "(~(").AddRange("))"),
			"^" => valueString.Insert(0, "^(").Add(')'),
			"sin" => valueString.Insert(0, "Sin(").Add(')'),
			"cos" => valueString.Insert(0, "Cos(").Add(')'),
			"tan" => valueString.Insert(0, "Tan(").Add(')'),
			"asin" => valueString.Insert(0, "Asin(").Add(')'),
			"acos" => valueString.Insert(0, "Acos(").Add(')'),
			"atan" => valueString.Insert(0, "Atan(").Add(')'),
			"ln" => valueString.Insert(0, nameof(Log) + '(').Add(')'),
			"postfix !" => valueString.Insert(0, "Factorial(").Add(')'),
			"++" => TypeEqualsToPrimitive(NStarType, "bool")
				? valueString.Insert(0, '(').AddRange(" = true)") : valueString.Insert(0, "++"),
			"--" => TypeEqualsToPrimitive(NStarType, "bool")
				? valueString.Insert(0, '(').AddRange(" = false)") : valueString.Insert(0, "--"),
			"postfix ++" => TypeEqualsToPrimitive(NStarType, "bool")
				? valueString.Insert(0, '(').AddRange(" = true)") : valueString.AddRange("++"),
			"postfix --" => TypeEqualsToPrimitive(NStarType, "bool")
				? valueString.Insert(0, '(').AddRange(" = false)") : valueString.AddRange("--"),
			"!!" => valueString.Copy().Insert(0, '(').AddRange(" = !(").AddRange(valueString).AddRange("))"),
			_ => "default!",
		};
	}

	private String PMExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, ref int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		var resultType = GetResultType(LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		String @default = "default";
		if (!(branch.Parent?.Name == "return"
			|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == "return"))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String"
				: Type(ref resultType, branch, ref errors)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType)
			&& (LeftNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& RightNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			|| LeftNStarType.MainType.Peek().Name.AsSpan() is "DateTime" or "TimeSpan"
			&& RightNStarType.MainType.Peek().Name.AsSpan() is "DateTime" or "TimeSpan"
			&& !(LeftNStarType.MainType.Peek().Name == "DateTime" && RightNStarType.MainType.Peek().Name == "DateTime"
			&& branch[i].Name == "+"))))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is NStarType PrevNStarType))
			PrevNStarType = NullType;
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, "string");
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, "string") || TypeEqualsToPrimitive(RightNStarType, "char");
		var isStringPrev = TypeEqualsToPrimitive(PrevNStarType, "string");
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, "null");
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, "null");
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		if (branch[i].Name == "-" && (isStringLeft || isStringRight))
		{
			GenerateMessage(ref errors, 0x4007, branch[i].Pos);
			return @default;
		}
		if (isStringPrev && !isStringRight || branch[i].Name == "-" && (isStringLeft || isStringRight))
		{
			if (branch[Max(i - 3, 0)].Name == nameof(PMExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				branch[Max(i - 3, 0)] = new(nameof(PMExpr),
					[branch[Max(i - 3, 0)], branch[i - 1], branch[i]], branch[i].Container)
				{
					Extra = resultType
				};
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		else if (i >= 4 && !isStringLeft && isStringRight)
		{
			branch[0] = new TreeBranch(nameof(PMExpr), branch.GetRange(0, i - 1), branch[i - 2].Container);
			branch.Remove(1, i - 2);
			i = 2;
		}
		else if (branch.Name == nameof(Expr) && isStringLeft && isStringRight)
			branch.Name = "StringConcatenation";
		branch[i].Extra = resultType;
		if (isStringLeft && isStringRight)
		{
			var result = subbranchValues[^2].Copy();
			if (isStringPrev)
				result.AddRange((String)".Copy()");
			return result.AddRange(".AddRange(").AddRange(subbranchValues[^1]).Add(')');
		}
		else if (isStringLeft || isStringRight)
		{
			var result = ((String)"((").AddRange(nameof(NStarEntity)).Add(')').AddRange(subbranchValues[^2]).Add(' ');
			result.AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]).AddRange(").");
			result.AddRange(nameof(ToString)).AddRange("()");
			return result;
		}
		else
		{
			if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
				subbranchValues[^2].Insert(0, '(').Add(')');
			if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
				subbranchValues[^1].Insert(0, '(').Add(')');
			if (i < 2)
				return branch[i][^1].Name;
			return subbranchValues[^2].Copy().Add(' ').AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
		}
	}

	private String MulDivExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, ref int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		var resultType = (branch[i].Name == "/"
			&& TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType))
			? GetPrimitiveType(NStarEntity.GetQuotientType(LeftNStarType.MainType.Peek().Name,
			TryReadValue(branch[i - 1].Name, out var value) ? value : 5, RightNStarType.MainType.Peek().Name))
			: (branch[i].Name == "%" && TypeIsPrimitive(LeftNStarType.MainType) && TypeIsPrimitive(RightNStarType.MainType))
			? GetPrimitiveType(NStarEntity.GetRemainderType(LeftNStarType.MainType.Peek().Name,
			TryReadValue(branch[i - 1].Name, out value) ? value : new(12345678901234567890, UnsignedLongIntType),
			RightNStarType.MainType.Peek().Name))
			: GetResultType(LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		String @default = "default";
		if (!(branch.Parent?.Name == "return"
			|| branch.Parent?.Name == nameof(List) && branch.Parent?.Parent?.Name == "return"))
			@default.Add('(').AddRange(TypeEqualsToPrimitive(resultType, "null") ? "String"
				: Type(ref resultType, branch, ref errors)).Add(')');
		@default.Add('!');
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is "null"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			&& TypeIsPrimitive(RightNStarType.MainType) && (RightNStarType.MainType.Peek().Name.AsSpan() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex" or "string"
			|| branch[i].Name == "*" && RightNStarType.MainType.Peek().Name == "null")))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return @default;
		}
		if (!(i >= 4 && branch[i - 4].Extra is NStarType PrevNStarType))
			PrevNStarType = NullType;
		var isStringLeft = TypeEqualsToPrimitive(LeftNStarType, "string");
		var isStringRight = TypeEqualsToPrimitive(RightNStarType, "string");
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, "null");
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, "null");
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		if (branch[i].Name == "*" && isStringLeft && isStringRight)
		{
			GenerateMessage(ref errors, 0x4008, branch[i].Pos);
			return @default;
		}
		else if (branch[i].Name != "*" && (isStringLeft || isStringRight))
		{
			GenerateMessage(ref errors, 0x4009, branch[i].Pos);
			return @default;
		}
		if (TypeEqualsToPrimitive(PrevNStarType, "string") && !isStringRight)
		{
			if (branch[Max(i - 3, 0)].Name == nameof(MulDivExpr))
				branch[Max(i - 3, 0)].AddRange(branch.GetRange(i - 1, 2));
			else
			{
				branch[Max(i - 3, 0)] = new(nameof(MulDivExpr), [branch[Max(i - 3, 0)], branch[i - 1], branch[i]],
					branch[i].Container)
				{
					Extra = resultType
				};
			}
			branch[Max(i - 3, 0)][^1].Extra = resultType;
			branch.Remove(i - 1, 2);
			i -= 2;
		}
		branch[i].Extra = resultType;
		if (branch[i].Name.AsSpan() is "/" or "%" && !TypeEqualsToPrimitive(LeftNStarType, "real")
			&& !TypeEqualsToPrimitive(RightNStarType, "real")
			&& subbranchValues[^1].AsSpan() is "0" or "0i" or "0u" or "0L" or "0uL" or "0LL" or "\"0\"")
		{
			GenerateMessage(ref errors, 0x4004, branch[i].Pos);
			branch[Max(i - 3, 0)] = new(@default, branch.Pos, branch.EndPos, branch.Container);
		}
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		if (isStringLeft)
			return subbranchValues[^2].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(subbranchValues[^1]).Add(')');
		if (isStringRight)
			return subbranchValues[^1].Add('.').AddRange(nameof(Repeat)).Add('(').AddRange(subbranchValues[^2]).Add(')');
		if (branch[i].Name.AsSpan() is "/" or "%" && TypeEqualsToPrimitive(LeftNStarType, "real")
			&& !TypeEqualsToPrimitive(RightNStarType, "real"))
			subbranchValues[^2].Insert(0, "(double)(").Add(')');
		var result = i < 2 ? branch[i].Name : subbranchValues[^2].Copy().Add(' ')
			.AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
		if (branch[i].Name.AsSpan() is "/" or "%" && !LeftNStarType.Equals(resultType))
		{
			if (!TypeIsPrimitive(resultType.MainType) || !resultType.MainType.TryPeek(out var resultBlock))
				return result;
			return result.Insert(0, ((String)"(").AddRange(Type(ref resultType, branch, ref errors)).AddRange(")(")).Add(')');
		}
		return result;
	}

	private String PowExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 1].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 2].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		string leftPrimitiveType;
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && (leftPrimitiveType = LeftNStarType.MainType.Peek().Name.ToString())
			is "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is "byte"
			or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex")
			|| leftPrimitiveType == "long long"
			&& (!TypesAreCompatible(RightNStarType, IntType, out var warning, subbranchValues[^1], out _, out _) || warning))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return "default(double)!";
		}
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, "null");
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, "null");
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		branch[i].Extra = GetResultType(RightNStarType, LeftNStarType, subbranchValues[^2], subbranchValues[^1]);
		if (leftPrimitiveType == "long long")
			return ((String)"(").AddRange(subbranchValues[^1]).AddRange(").").AddRange(nameof(MpzT.One.Power))
				.Add('(').AddRange(subbranchValues[^2]).Add(')');
		return i < 2 ? branch[i].Name : ((String)"Pow(").AddRange(subbranchValues[^1])
			.AddRange(", ").AddRange(subbranchValues[^2]).Add(')');
	}

	private String RangeExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (!(TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is "byte"
			or "short int" or "unsigned short int" or "int" or "index"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is "byte"
			or "short int" or "unsigned short int" or "int" or "index"))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return "default(double)!";
		}
		branch[i].Extra = RangeType;
		String result = [];
		if (subbranchValues[^2].StartsWith('^'))
			result.AddRange(subbranchValues[^2]);
		else if (LeftNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^2]).AddRange(", out var ");
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^").AddRange(varName).AddRange(".Value : (");
			result.AddRange(varName).AddRange(".Value - 1))");
		}
		else
			result.AddRange("((").AddRange(subbranchValues[^2]).AddRange(") - 1)");
		result.AddRange("..");
		if (subbranchValues[^1].StartsWith('^'))
			result.AddRange("^((").AddRange(subbranchValues[^1][1..]).AddRange(") - 1)");
		else if (RightNStarType.Equals(IndexType))
		{
			result.AddRange("(CreateVar(").AddRange(subbranchValues[^1]).AddRange(", out var ");
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(").IsFromEnd ? ^(").AddRange(varName).AddRange(".Value - 1) : ");
			result.AddRange(varName).AddRange(".Value)");
		}
		else
			result.AddRange(subbranchValues[^1]);
		return result;
	}

	private String BoolExpr(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (!((branch[i].Name.AsSpan() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			|| branch[i].Name.AsSpan() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name == "bool")
			&& (branch[i].Name.AsSpan() is "==" or ">" or "<" or ">=" or "<=" or "!="
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool"
			or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
			or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
			or "real" or "long real" or "complex" or "long complex"
			|| branch[i].Name.AsSpan() is "&&" or "||" or "&" or "|" or "^"
			&& TypeIsPrimitive(RightNStarType.MainType) && RightNStarType.MainType.Peek().Name == "bool")))
		{
			GenerateMessage(ref errors, 0x4006, branch[i].Pos, branch[i].Name,
				LeftNStarType.ToString(), RightNStarType.ToString());
			return "false";
		}
		var isNullLeft = TypeEqualsToPrimitive(LeftNStarType, "null");
		var isNullRight = TypeEqualsToPrimitive(RightNStarType, "null");
		if (isNullLeft && !isNullRight)
			subbranchValues[^2].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^2].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref RightNStarType, branch, ref errors)).Add(')'));
		else if (!isNullLeft && isNullRight)
			subbranchValues[^1].ReplaceInPlace("(dynamic)", "").Insert(^(subbranchValues[^1].EndsWith('!') ? 1 : 0),
				((String)'(').AddRange(Type(ref LeftNStarType, branch, ref errors)).Add(')'));
		branch[i].Extra = BoolType;
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		return i < 2 ? branch[i].Name : subbranchValues[^2].Copy().Add(' ')
			.AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
	}

	private String Assignment(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		if (constantsDepth != 0 && !(branch.Length == 3 && branch[1].Name == nameof(Declaration) && branch[2].Name == "="))
		{
			GenerateMessage(ref errors, 0x4052, branch[i].Pos);
			branch.Name = "null";
			branch.Elements.Clear();
			branch.Extra = NullType;
			return "default!";
		}
		if (branch[i].Name == "=" && TryReadValue(branch[Max(0, i - 3)].Name, out _) && branch.Parent != null
			&& (branch.Parent.Name == "if" || branch.Parent.Name == nameof(XorList) || branch.Parent.Name == nameof(Expr)
			&& BoolOperators.Contains(branch.Parent[Min(Max(branch.Parent.Elements.FindIndex(x =>
			ReferenceEquals(x, branch)) + 1, 2), branch.Parent.Length - 1)].Name.ToString())))
			GenerateMessage(ref errors, 0x8009, branch[i].Pos);
		else if (branch[i].Name == "=" && branch[i - 1].Name == nameof(Hypername) && branch[Max(0, i - 3)] == branch[i - 1])
			GenerateMessage(ref errors, 0x8007, branch[i].Pos);
		branch.Name = nameof(Assignment);
		if (branch[i - 2].Extra is not NStarType SrcNStarType)
			SrcNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType DestNStarType)
			DestNStarType = NullType;
		var powWarning = false;
		if (branch[i].Name == "pow=" && TypesAreCompatible(SrcNStarType, RealType, out powWarning, subbranchValues[^2],
			out var adaptedSource, out _) && adaptedSource != null)
		{
			SrcNStarType = RealType;
			subbranchValues[^2] = ((String)"Pow(").AddRange(subbranchValues[^1])
				.AddRange(", ").AddRange(adaptedSource).Add(')');
		}
		var srcBelowInt = TypeIsPrimitive(SrcNStarType.MainType)
			&& SrcNStarType.MainType.Peek().Name.AsSpan() is "byte" or "short char" or "short int" or "unsigned short int";
		if (!TypeIsFullySpecified(DestNStarType, branch.Container) && !DestNStarType.Equals(SrcNStarType))
		{
			branch[i].Extra = DestNStarType;
			return subbranchValues[^1].Copy().AddRange(" = ").AddRange(nameof(CastType)).Add('(')
				.AddRange(TypeReflected(ref DestNStarType, branch, ref errors))
				.AddRange(", ").AddRange(subbranchValues[^2]).Add(')');
		}
		else if (!TypesAreCompatible(branch[i - 2].Length != 0 && srcBelowInt ? IntType : SrcNStarType, DestNStarType,
			out var notPowWarning, subbranchValues[^2], out adaptedSource, out var extraMessage) || adaptedSource == null)
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcNStarType, DestNStarType);
			branch.Name = "default!";
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return "default!";
		}
		else if (!srcBelowInt && (powWarning || notPowWarning))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4027, otherPos, extraMessage!, SrcNStarType, DestNStarType);
			branch.Name = "default!";
			branch.RemoveEnd(0);
			branch.Extra = NullType;
			return "default!";
		}
		branch[i].Extra = DestNStarType;
		if (subbranchValues[^1].AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!")
			return "_ = default!";
		else if (branch[i].Name == "pow=")
			return i < 2 ? branch[i].Name : subbranchValues[^1].Copy().AddRange(" = ").AddRange(adaptedSource);
		else if (branch[i].Name == "+=" && TypeEqualsToPrimitive(DestNStarType, "string"))
			return i < 2 ? branch[i].Name : subbranchValues[^1].Copy().AddRange(".AddRange(").AddRange(adaptedSource).Add(')');
		else
			return i < 2 ? branch[i].Name : subbranchValues[^1].Copy().Add(' ').AddRange(branch[i].Name)
				.Add(' ').AddRange(adaptedSource == "_" ? "default!" : adaptedSource);
	}

	private String Ternary(TreeBranch branch, List<String> subbranchValues, ref List<String>? errors, int i)
	{
		branch.Name = nameof(Ternary);
		if (i == 2 && branch[i].Name == ":")
		{
			if (branch[0].Name == nameof(Declaration) && branch[0].Length == 2
				&& branch[0][0].Name == "type" && branch[0][0].Extra is NStarType LeftNStarType
				&& LeftNStarType.Equals(RecursiveType)
				&& branch[1].Name == "type" && branch[1].Extra is NStarType RightNStarType)
			{
				BranchCollection extraTypes = [new("type", branch[1].Pos, branch[1].Container)
				{
					Extra = RightNStarType
				}];
				LeftNStarType.ExtraTypes.Replace(extraTypes);
				branch.Replace(branch[0]);
				return Declaration(branch, out errors);
			}
			else if (!(branch.Extra is NStarType TupleNStarType && TupleNStarType.MainType.Equals(TupleBlockStack)
				&& TupleNStarType.ExtraTypes.Length == 2
				&& TupleNStarType.ExtraTypes[0].Extra is NStarType DestKeyNStarType
				&& TupleNStarType.ExtraTypes[1].Extra is NStarType DestValueNStarType
				&& branch[0].Extra is NStarType SrcKeyNStarType && branch[1].Extra is NStarType SrcValueNStarType))
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4080, otherPos, branch[i].Name);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else if (!TypesAreCompatible(SrcKeyNStarType, DestKeyNStarType, out var warning, subbranchValues[^2], out _,
				out var extraMessage) || warning)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcKeyNStarType, DestKeyNStarType);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else if (!TypesAreCompatible(SrcValueNStarType, DestValueNStarType, out warning, subbranchValues[^1], out _,
				out extraMessage) || warning)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, SrcValueNStarType, DestValueNStarType);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else
			{
				branch[i].Extra = TupleNStarType;
				return ((String)"(").AddRange(subbranchValues[^2]).AddRange(", ").AddRange(subbranchValues[^1]).Add(')');
			}
		}
		if ((i < 4 || branch.Length <= i + 2) && branch[i].Name != ":")
		{
			if (branch[i].Name != "?")
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x400E, otherPos, branch[i].Name);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else if (i < 2)
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x400F, otherPos, branch[i].Name);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else
				return subbranchValues[^2].Copy().AddRange(" ? ").AddRange(subbranchValues[^1]).AddRange(" : default!");
		}
		if (branch[i - 2].Name == "?")
		{
			if (branch[i - 3].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			NStarType ResultNStarType;
			if (branch.Parent != null && branch.Parent.Name == "return" && branch.Parent.Parent != null
				&& branch.Parent.Parent.Name == nameof(Main) && branch.Parent.Parent.Parent == null)
				branch[i].Extra = ResultNStarType = LeftNStarType;
			else if (TypesAreCompatible(LeftNStarType, RightNStarType, out var warning, subbranchValues[^3],
				out var destExpr, out _)
				&& !warning && destExpr != null)
			{
				branch[i].Extra = ResultNStarType = RightNStarType;
				if (!ReferenceEquals(subbranchValues[^3], destExpr))
					subbranchValues[^3].Replace(destExpr);
			}
			else if (TypesAreCompatible(RightNStarType, LeftNStarType, out warning, subbranchValues[^1], out destExpr, out _)
				&& !warning && destExpr != null)
			{
				branch[i].Extra = ResultNStarType = LeftNStarType;
				if (!ReferenceEquals(subbranchValues[^1], destExpr))
					subbranchValues[^1].Replace(destExpr);
			}
			else
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, LeftNStarType.ToString(), RightNStarType.ToString());
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			var result = subbranchValues[^4].Copy().AddRange(" ? ").AddRange(subbranchValues[^3]);
			result.AddRange(" : ").AddRange(subbranchValues[^1]);
			if (ResultNStarType.Equals(ByteType))
				result.Insert(0, "(byte)(").Add(')');
			else if (ResultNStarType.Equals(ShortIntType))
				result.Insert(0, "(short)(").Add(')');
			else if (ResultNStarType.Equals(UnsignedShortIntType))
				result.Insert(0, "(ushort)(").Add(')');
			else if (ResultNStarType.Equals(CharType))
				result.Insert(0, "(char)(").Add(')');
			return result;
		}
		else
		{
			if (branch[i - 4].Extra is not NStarType LeftNStarType)
				LeftNStarType = NullType;
			if (branch[i - 3].Extra is not NStarType RightNStarType)
				RightNStarType = NullType;
			if (branch[i - 1].Extra is not NStarType NStarType3)
				NStarType3 = NullType;
			var checksEquality = branch[i - 2].Name.AsSpan() is "?=" or "?!=";
			if (!((checksEquality && TypeEqualsToPrimitive(LeftNStarType, "string")
				|| TypeIsPrimitive(LeftNStarType.MainType) && LeftNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool"
				or "byte" or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
				or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
				or "real" or "long real" or "complex" or "long complex")
				&& (checksEquality && TypeEqualsToPrimitive(RightNStarType, "string")
				|| TypeIsPrimitive(RightNStarType.MainType)
				&& RightNStarType.MainType.Peek().Name.AsSpan() is "null" or "bool" or "byte"
				or "short char" or "short int" or "unsigned short int" or "char" or "int" or "unsigned int"
				or "long char" or "long int" or "unsigned long int" or "long long" or "unsigned long long"
				or "real" or "long real" or "complex" or "long complex")))
			{
				var otherPos = branch[i - 2].Pos;
				GenerateMessage(ref errors, 0x4006, otherPos, branch[i - 2].Name,
					LeftNStarType.ToString(), RightNStarType.ToString());
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			if (branch.Parent != null && branch.Parent.Name == "return" && branch.Parent.Parent != null
				&& branch.Parent.Parent.Name == nameof(Main) && branch.Parent.Parent.Parent == null)
				branch[i].Extra = LeftNStarType;
			else if (TypesAreCompatible(LeftNStarType, NStarType3, out var warning, subbranchValues[^3], out var outExpr, out _)
				&& !warning && outExpr != null)
			{
				branch[i].Extra = NStarType3;
				if (!ReferenceEquals(subbranchValues[^3], outExpr))
					subbranchValues[^3].Replace(outExpr);
			}
			else if (TypesAreCompatible(NStarType3, LeftNStarType, out warning, subbranchValues[^1], out outExpr, out _)
				&& !warning && outExpr != null)
			{
				branch[i].Extra = LeftNStarType;
				if (!ReferenceEquals(subbranchValues[^1], outExpr))
					subbranchValues[^1].Replace(outExpr);
			}
			else
			{
				var otherPos = branch[i].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, LeftNStarType.ToString(), NStarType3.ToString());
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			var result = ((String)"NStar.Core.").AddRange(nameof(Extents)).Add('.').AddRange(nameof(CreateVar));
			result.Add('(').AddRange(subbranchValues[^4]).AddRange(", out var ");
			var varName = RandomVarName();
			result.AddRange(varName).AddRange(") ").AddRange(branch[i - 2].Name[1..]);
			if (branch[i - 2].Name == "?=")
				result.Add('=');
			result.Add(' ').AddRange(subbranchValues[^3]).AddRange(" ? ").AddRange(varName);
			result.AddRange(" : ").AddRange(subbranchValues[^1]);
			return result;
		}
	}

	private static String CombineWithExpr(TreeBranch branch, List<String> subbranchValues, int i)
	{
		if (branch[i - 1].Extra is not NStarType NStarType)
			NStarType = NullType;
		branch[i].Extra = NStarType;
		return subbranchValues[^1];
	}

	private String ListExpr(TreeBranch branch, ref List<String>? errors, int i)
	{
		var result = ParseAction(branch[i].Name)(branch[i], out var innerErrors);
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String BinaryNotListExpr(TreeBranch branch, ref List<String>? errors, List<String> subbranchValues, int i)
	{
		if (branch[i - 2].Extra is not NStarType LeftNStarType)
			LeftNStarType = NullType;
		if (branch[i - 1].Extra is not NStarType RightNStarType)
			RightNStarType = NullType;
		if (branch[i].Name.AsSpan() is "<<" or ">>" or ">>>"
			&& (!TypesAreCompatible(RightNStarType, IntType, out var warning, subbranchValues[^1], out _, out _) || warning))
		{
			var otherPos = branch[i].Pos;
			GenerateMessage(ref errors, 0x4081, otherPos, branch[i].Name);
			branch[i].Extra = NullType;
			return "default!";
		}
		branch[i].Extra = GetResultType(LeftNStarType, RightNStarType, subbranchValues[^2], subbranchValues[^1]);
		if (subbranchValues[^2].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^2].Insert(0, '(').Add(')');
		if (subbranchValues[^1].ContainsAnyExcluding(AlphanumericCharacters))
			subbranchValues[^1].Insert(0, '(').Add(')');
		if (i < 2)
			return branch[i].Name;
		return subbranchValues[^2].Copy().Add(' ').AddRange(branch[i].Name).Add(' ').AddRange(subbranchValues[^1]);
	}

	private String List(TreeBranch branch, out List<String>? errors)
	{
		String result = "(";
		List<String> listItemValues = [];
		errors = null;
		if (branch.Extra is NStarType MainNStarType)
		{
			if (TypeEqualsToPrimitive(MainNStarType, "list", false))
			{
				var innerType = GetSubtype(MainNStarType);
				for (var i = 0; i < branch.Length; i++)
					branch[i].Extra = innerType;
			}
			else if (TypeEqualsToPrimitive(MainNStarType, "tuple", false))
			{
				if (MainNStarType.ExtraTypes.Any(x => x.Value.Name != "type" || x.Value.Extra is not NStarType))
					Type(ref MainNStarType, branch, ref errors, true);
				if (MainNStarType.ExtraTypes.Any(x => x.Value.Name != "type" || x.Value.Extra is not NStarType))
					throw new InvalidOperationException();
				for (var i = 0; i < MainNStarType.ExtraTypes.Length && i < branch.Length; i++)
					branch[i].Extra = (NStarType)MainNStarType.ExtraTypes[i].Extra!;
			}
			else if (MainNStarType.MainType.Equals(DictionaryBlockStack))
			{
				if (MainNStarType.ExtraTypes.Length != 2)
					throw new InvalidOperationException();
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					Type(ref MainNStarType, branch, ref errors, true);
				if (MainNStarType.ExtraTypes[0].Name != "type" || MainNStarType.ExtraTypes[0].Extra is not NStarType
					|| MainNStarType.ExtraTypes[1].Name != "type" || MainNStarType.ExtraTypes[1].Extra is not NStarType)
					throw new InvalidOperationException();
				for (var i = 0; i < branch.Length; i++)
					branch[i].Extra = new NStarType(TupleBlockStack, MainNStarType.ExtraTypes);
			}
		}
		for (var i = 0; i < branch.Length; i++)
		{
			if (i > 0)
				result.AddRange(", ");
			if (TryReadValue(branch[i].Name, out var value))
			{
				branch[i].Extra = value.InnerType;
				listItemValues.Add(value.ToString(true, true));
				result.AddRange(listItemValues[^1]);
			}
			else
			{
				var innerResult = ParseAction(branch[i].Name)(branch[i], out var innerErrors);
				listItemValues.Add(innerResult.AsSpan() is "_" or "default" or "default!" or "_ = default" or "_ = default!"
					|| branch[i].Name == nameof(Hypername)
					&& branch[i].Extra is NStarType ExprNStarType && ExprNStarType.Equals(NullType)
					? branch.Parent != null && branch.Parent.Name == nameof(Expr) && branch.Parent.Parent != null
					&& branch.Parent.Parent.Name == "return" && branch.Parent.Parent.Parent != null
					&& branch.Parent.Parent.Parent.Name == nameof(Main)
					&& branch.Parent.Parent.Parent.Parent == null ? "default(object)!" : "default!" : innerResult);
				result.AddRange(listItemValues[^1]);
				AddRange(ref errors, innerErrors);
			}
		}
		if (branch.Name == nameof(List) && listItemValues.Length != 0 && listItemValues.All(x =>
			x.AsSpan() is "default!" or "default(object)!"))
		{
			branch.Extra = NullType;
			return "default!";
		}
		branch.Extra = new NStarType(TupleBlockStack, new(branch.Elements.Convert(x =>
		{
			if (x.Extra is NStarType NStarType)
				return new TreeBranch("type", branch.Pos, branch.Container) { Extra = NStarType };
			else
				throw new InvalidOperationException();
		})));
		return result.Add(')');
	}

	private String XorList(TreeBranch branch, out List<String>? errors)
	{
		String result = "Universal.Xor(";
		errors = null;
		for (var i = 0; i < branch.Length; i++)
		{
			if (i > 0)
				result.AddRange(", ");
			if (TryReadValue(branch[i].Name, out var value))
			{
				branch[i].Extra = value.InnerType;
				result.AddRange(value.ToString(true, true));
			}
			else
			{
				result.AddRange(ParseAction(branch[i].Name)(branch[i], out var innerErrors));
				AddRange(ref errors, innerErrors);
			}
		}
		branch.Extra = branch.Elements.Progression(GetListType(BoolType), (x, y) =>
			GetResultType(x, GetListType(y.Extra is NStarType NStarType ? NStarType : NullType), "default!", "default!"));
		return result.Add(')');
	}

	private String Lambda(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		var otherPos = branch.FirstPos;
		if (branch.Parent == null || branch.Parent.Name.AsSpan() is not (nameof(Call) or nameof(ConstructorCall)))
		{
			if (branch.Extra is not NStarType FunctionType)
				return Default(ref errors);
			else if (FunctionType.MainType.Equals(EventHandlerBlockStack))
			{
				FunctionType = new(FuncBlockStack, new([new("type", 0, []) { Extra = NullType },
					new("type", 0, []) { Extra = ObjectType }, .. FunctionType.ExtraTypes.Values]));
				return LambdaDeterminedType(branch, ref errors);
			}
			else if (!FunctionType.MainType.Equals(FuncBlockStack))
				return Default(ref errors);
			else
				return LambdaDeterminedType(branch, ref errors);
		}
		return LambdaUndeterminedType(branch, ref errors, result, otherPos);
		String Default(ref List<String>? errors)
		{
			GenerateMessage(ref errors, 0x4040, otherPos);
			branch.Extra = NullType;
			return "default!";
		}

		String LambdaUndeterminedType(TreeBranch branch, ref List<String>? errors, String result, int otherPos)
		{
			Debug.Assert(branch.Parent != null);
			var parentIndex = branch.Parent.Elements.FindIndex(x => ReferenceEquals(x, branch));
			if (parentIndex < 0)
				return Default(ref errors);
			var grandParent = branch.Parent.Parent;
			if (grandParent == null)
				return Default(ref errors);
			var grandParentIndex = grandParent.Elements.FindIndex(x => ReferenceEquals(x, branch.Parent));
			if (grandParentIndex < 1 || grandParent.Extra is not UserDefinedMethodOverloads functions)
				return Default(ref errors);
			List<NStarType> parameterTypes = [];
			List<TreeBranch> parameterBranches = [];
			String[] parameterNames;
			int foundIndex;
			var success = false;
			for (var i = 0; i < functions.Length; i++)
			{
				if (functions[i].Parameters.Length <= parentIndex)
					continue;
				var ContainerNStarType = functions[i].Parameters[parentIndex].Type;
				if (!ContainerNStarType.MainType.Equals(FuncBlockStack))
					continue;
				if (ContainerNStarType.ExtraTypes.Skip(1).Any(x => x.Value.Name != "type" || x.Value.Extra is not NStarType))
					continue;
				parameterTypes = ContainerNStarType.ExtraTypes.Skip(1).ToList(x => (NStarType)x.Value.Extra!);
				parameterBranches = ContainerNStarType.ExtraTypes.Skip(1).ToList(x => x.Value);
				if (parameterTypes.Length == 1 && LambdaIsValidParameter(branch[0], out var singleParameterName))
				{
					result.AddRange("async ").AddRange(singleParameterName).AddRange(" => ");
					branch[0].Extra = parameterTypes[0];
					success = true;
					break;
				}
				if (branch[0].Name != "List" || parameterTypes.Length != branch[0].Length)
				{
					GenerateMessage(ref errors, 0x4042, otherPos);
					branch.Extra = NullType;
					return "default!";
				}
				parameterNames = new String[branch[0].Length];
				foundIndex = branch[0].Elements.FindIndex((x, index) => !LambdaIsValidParameter(x, out parameterNames[index]));
				if (foundIndex >= 0)
				{
					GenerateMessage(ref errors, 0x4043, otherPos, foundIndex + 1);
					branch.Extra = NullType;
					return "default!";
				}
				result.AddRange("async (");
				result.AddRange(String.Join(", ", parameterNames));
				result.Add(')').AddRange(" => ");
				for (var j = 0; j < branch[0].Length; j++)
					branch[0][j].Extra = parameterTypes[j];
				success = true;
				break;
			}
			if (!success)
			{
				GenerateMessage(ref errors, 0x4041, otherPos);
				branch.Extra = NullType;
				return "default!";
			}
			return LambdaClosing(branch, ref errors, result, parameterBranches);
		}
	}

	private String LambdaDeterminedType(TreeBranch branch, ref List<String>? errors)
	{
		String result = [];
		var otherPos = branch.FirstPos;
		if (branch.Extra is not NStarType FunctionNStarType)
			throw new InvalidOperationException();
		else if (FunctionNStarType.MainType.Equals(EventHandlerBlockStack))
			FunctionNStarType = new(FuncBlockStack, new([new("type", 0, []) { Extra = NullType },
			new("type", 0, []) { Extra = ObjectType }, .. FunctionNStarType.ExtraTypes.Values]));
		else if (!FunctionNStarType.MainType.Equals(FuncBlockStack))
			throw new InvalidOperationException();
		List<NStarType> parameterTypes = [];
		parameterTypes = [];
		if (FunctionNStarType.ExtraTypes.Skip(1).Any(x => x.Value.Name != "type" || x.Value.Extra is not NStarType))
		{
			GenerateMessage(ref errors, 0x4044, otherPos);
			branch.Extra = NullType;
			return "default!";
		}
		parameterTypes = FunctionNStarType.ExtraTypes.Skip(1).ToList(x => (NStarType)x.Value.Extra!);
		List<TreeBranch> parameterBranches = [];
		parameterBranches = FunctionNStarType.ExtraTypes.Skip(1).ToList(x => x.Value);
		if (parameterTypes.Length == 1 && LambdaIsValidParameter(branch[0], out var singleParameterName))
		{
			result/*.AddRange("async ")*/.AddRange(singleParameterName).AddRange(" => ");
			branch[0].Extra = parameterTypes[0];
			branch[1].Extra = (NStarType)FunctionNStarType.ExtraTypes[0].Extra!;
			return LambdaClosing(branch, ref errors, result, parameterBranches);
		}
		if (branch[0].Name != "List" || parameterTypes.Length != branch[0].Length)
		{
			GenerateMessage(ref errors, 0x4042, otherPos);
			branch.Extra = NullType;
			return "default!";
		}
		var parameterNames = new String[branch[0].Length];
		var foundIndex = branch[0].Elements.FindIndex((x, index) => !LambdaIsValidParameter(x, out parameterNames[index]));
		if (foundIndex >= 0)
		{
			GenerateMessage(ref errors, 0x4043, otherPos, foundIndex + 1);
			branch.Extra = NullType;
			return "default!";
		}
		//result.AddRange("async (");
		result.Add('(');
		result.AddRange(String.Join(", ", parameterNames));
		result.Add(')').AddRange(" => ");
		for (var j = 0; j < branch[0].Length; j++)
			branch[0][j].Extra = parameterTypes[j];
		branch[1].Extra ??= NullType;
		return LambdaClosing(branch, ref errors, result, parameterBranches);
	}
	private static bool LambdaIsValidParameter(TreeBranch branch, out String branchName)
	{
		if (branch.Length == 0)
		{
			branchName = branch.Name;
			return true;
		}
		if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
		{
			branchName = default!;
			return false;
		}
		branchName = branch[0].Name;
		return true;
	}

	private String LambdaClosing(TreeBranch branch, ref List<String>? errors, String result, List<TreeBranch> parameterBranches)
	{
		var indentationUnits = this.indentationUnits;
		this.indentationUnits++;
		var innerResult = ParseAction(branch[1].Name)(branch[1], out var innerErrors);
		this.indentationUnits = indentationUnits;
		if (branch.Extra is NStarType FunctionNStarType && FunctionNStarType.MainType.Equals(FuncBlockStack)
			&& FunctionNStarType.ExtraTypes.Length != 0 && FunctionNStarType.ExtraTypes[0].Name == "type"
			&& FunctionNStarType.ExtraTypes[0].Extra is NStarType ReturnNStarType)
		{
			if (branch[1].Extra is not NStarType ValueNStarType)
			{
				GenerateMessage(ref errors, 0x4014, branch[1].Pos, null!, NullType, ReturnNStarType);
				return result.AddRange("default!");
			}
			else if (!TypesAreCompatible(ValueNStarType, ReturnNStarType,
				out var warning, innerResult, out _, out var extraMessage) || warning)
			{
				GenerateMessage(ref errors, 0x4014, branch[^1].Pos, extraMessage!, ValueNStarType, ReturnNStarType);
				return result.AddRange("default!");
			}
		}
		result.AddRange(innerResult);
		AddRange(ref errors, innerErrors);
		if (branch[1].Extra is not NStarType ReturnNStarType2)
			throw new InvalidOperationException();
		if (!((NStarType)branch.Extra!).MainType.Equals(EventHandlerBlockStack))
			branch.Extra = new NStarType(FuncBlockStack, new([new TreeBranch("type", branch.Pos, branch.Container)
			{
				Extra = ReturnNStarType2
			}, .. parameterBranches]));
		return result;
	}

	private String SwitchExpr(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		if (branch.Length == 0)
			return "default!";
		result.AddRange(ParseAction(branch[0].Name)(branch[0], out errors));
		if (branch.Length == 1 || branch[1].Name != "switch")
			return result;
		if (branch[0].Extra is not NStarType SourceNStarType || !SourceNStarType.MainType.TryPeek(out var sourceBlock)
			|| sourceBlock.BlockType is not BlockType.Primitive || sourceBlock.Name.AsSpan() is not ("byte" or "short int"
			or "unsigned short int" or "int" or "unsigned int" or "long int" or "unsigned long int" or "real" or "string"))
		{
			GenerateMessage(ref errors, 0x4019, branch[0].FirstPos);
			branch.Extra = NullType;
			return "default!";
		}
		if (sourceBlock.Name == "string")
			result.Insert(0, '(').AddRange(").").AddRange(nameof(RedStarLinq.ToString)).AddRange("()");
		result.AddRange(" switch { ");
		String innerResult = [], prevResult = [], caseResult = [], prevCaseResult = [];
		List<String>? innerErrors;
		var ReturnNStarType = NullType;
		for (var i = 0; i < Max(3, branch[1].Length); i++)
		{
			if (i == 1)
			{
				(innerResult, prevResult) = ([], innerResult);
				(caseResult, prevCaseResult) = ([], caseResult);
			}
			else if (i == 2)
				innerResult = result.AddRange(prevResult).AddRange(innerResult);
			if (i >= branch[1].Length)
				continue;
			var x = branch[1][i];
			if (x.Length < 2)
				continue;
			var constantsDepth = this.constantsDepth;
			this.constantsDepth++;
			x[0].Extra ??= SourceNStarType;
			if (x[0].Name == "_")
				innerResult.Add('_');
			else if (!TryReadValue(ParseAction(x[0].Name)(x[0], out innerErrors), out var value))
			{
				AddRange(ref errors, innerErrors);
				GenerateMessage(ref errors, 0x4050, x[0].FirstPos);
				branch.Extra = NullType;
				this.constantsDepth = constantsDepth;
				return "default!";
			}
			else
			{
				innerResult.AddRange(value.ToString(true));
				AddRange(ref errors, innerErrors);
			}
			this.constantsDepth = constantsDepth;
			if (x.Length >= 3)
			{
				innerResult.AddRange(" when ").AddRange(ParseAction(x[^2].Name)(x[^2], out innerErrors));
				AddRange(ref errors, innerErrors);
			}
			if (i != 0)
				x[^1].Extra ??= ReturnNStarType;
			innerResult.AddRange(" => ");
			caseResult = ParseAction(x[^1].Name)(x[^1], out innerErrors);
			AddRange(ref errors, innerErrors);
			if (x[^1].Extra is not NStarType NStarType)
				return "default!";
			if (i == 0)
				ReturnNStarType = NStarType;
			else if (TypesAreCompatible(ReturnNStarType, NStarType,
				out var warning, prevCaseResult.Copy(), out var outExpr, out _)
				&& !warning && outExpr != null && (i == 1 || prevCaseResult == outExpr))
			{
				x[^1].Extra = ReturnNStarType = NStarType;
				if (!ReferenceEquals(prevCaseResult, outExpr))
					prevCaseResult.Replace(outExpr);
				prevResult.AddRange(prevCaseResult).AddRange(", ");
			}
			else if (i == 1)
			{
				var otherPos = x[^1].Pos;
				GenerateMessage(ref errors, 0x4015, otherPos, ReturnNStarType, NStarType);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			else if (TypesAreCompatible(NStarType, ReturnNStarType, out warning, caseResult, out outExpr, out var extraMessage)
				&& !warning && outExpr != null)
			{
				x[^1].Extra = ReturnNStarType;
				if (!ReferenceEquals(caseResult, outExpr))
					caseResult.Replace(outExpr);
			}
			else
			{
				var otherPos = x[^1].Pos;
				GenerateMessage(ref errors, 0x4014, otherPos, extraMessage!, NStarType, ReturnNStarType);
				branch.Name = "default!";
				branch.RemoveEnd(0);
				branch.Extra = NullType;
				return "default!";
			}
			if (i != 0)
				innerResult.AddRange(caseResult).AddRange(", ");
		}
		branch.Extra = ReturnNStarType;
		return result.AddRange(" }");
	}

	private String Typeof(TreeBranch branch, out List<String>? errors)
	{
		branch.Extra = RecursiveType;
		if (branch.Length == 0)
		{
			errors = null;
			return "typeof(dynamic)";
		}
		var parseResult = ParseAction(branch[0].Name)(branch[0], out errors);
		if (branch[0].Extra is NStarType NStarType && NStarType.Equals(RecursiveType))
		{
			GenerateMessage(ref errors, 0x4091, branch.FirstPos);
			branch.Extra = NullType;
			return "default!";
		}
		if (TryReadValue(parseResult, out var value) || TryReadValue(branch[0].Name, out value))
		{
			var InnerNStarType = value.InnerType;
			return ((String)"typeof(").AddRange(Type(ref InnerNStarType, branch, ref errors)).Add(')');
		}
		return parseResult.Insert(0, '(').AddRange(").GetType()");
	}

	private String Return(TreeBranch branch, out List<String>? errors)
	{
		String result = [];
		errors = null;
		branch[0].Extra ??= currentFunction.HasValue ? currentFunction.Value.ReturnNStarType : null;
		var expr = Expr(branch[0], out var innerErrors);
		var otherPos = branch.FirstPos;
		if (!currentFunction.HasValue || branch[0].Extra is not NStarType ExprNStarType)
			result.AddRange("return ").AddRange(expr == "_" || branch[0].Extra is NStarType ExprNStarType2
				&& ExprNStarType2.Equals(NullType) ? "default!" : expr);
		else if (currentFunction.Value.ReturnNStarType.Equals(NullType)
			|| TaskBlockStacks.Contains(currentFunction.Value.ReturnNStarType.MainType)
			&& (currentFunction.Value.ReturnNStarType.ExtraTypes.Length == 0
			|| currentFunction.Value.ReturnNStarType.ExtraTypes[0].Name == "type"
			&& currentFunction.Value.ReturnNStarType.ExtraTypes[0].Extra is NStarType TaskNStarType
			&& TaskNStarType.Equals(NullType)))
		{
			branch.Extra ??= NullType;
			result.Add('{');
			if (expr.AsSpan() is not ("_" or "default" or "default!" or "_ = default" or "_ = default!"))
				result.AddRange(expr).Add(';');
			return result.AddRange("return;}");
		}
		else if (!TypesAreCompatible(ExprNStarType, currentFunction.Value.ReturnNStarType, out var warning, expr,
			out var adapterExpr, out var extraMessage))
		{
			GenerateMessage(ref errors, 0x402B, otherPos, extraMessage!, ExprNStarType, currentFunction.Value.ReturnNStarType);
			result.AddRange("return default!");
		}
		else
		{
			if (warning)
				GenerateMessage(ref errors, 0x800A, otherPos, extraMessage!, ExprNStarType,
					currentFunction.Value.ReturnNStarType);
			result.AddRange("return ").AddRange(adapterExpr ?? "default!");
		}
		result.Add(';');
		branch.Extra ??= branch[0].Extra;
		AddRange(ref errors, innerErrors);
		return result;
	}

	private String Default(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		if (branch.Name == nameof(TreeBranch.DoNotAdd))
			return "default!";
		if (NStarEntity.TryParse(branch.Name.ToString(), out var value))
		{
			branch.Extra = value.InnerType;
			return value.ToString(true, true);
		}
		if (branch.Length == 0)
			return branch.Name == "ClassMain" ? [] : branch.Name;
		String result = [];
		if (branch.Name.AsSpan() is "ref" or "out")
		{
			if (branch.Length != 1)
			{
				var otherPos = branch.FirstPos;
				GenerateMessage(ref errors, 0x400C, otherPos, branch.Name.ToString());
				return [];
			}
			result.AddRange(branch.Name).Add(' ').AddRange(Hypername(branch, out var innerErrors, null, false));
			AddRange(ref errors, innerErrors);
			return result;
		}
		if (branch.Name.StartsWith("Namespace "))
		{
			result.Add('n').AddRange(branch.Name[1..]).Add('{');
			indentationUnits++;
		}
		foreach (var x in branch.Elements)
		{
			var parsedSubbranch = ParseAction(x.Name)(x, out var innerErrors);
			if (parsedSubbranch.Length != 0)
				result.AddRange(parsedSubbranch);
			AddRange(ref errors, innerErrors);
		}
		if (branch.Name.StartsWith("Namespace "))
		{
			indentationUnits--;
			result.Add('}');
		}
		if (!branch.Name.StartsWith("Namespace ") || IsTypeContext(branch))
			return result;
		else
		{
			compiledClasses.AddRange(result);
			return [];
		}
	}

	private static String Wreck(TreeBranch branch, out List<String>? errors)
	{
		errors = null;
		return [];
	}

	private String Type(ref NStarType type, TreeBranch branch, ref List<String>? errors, bool earlyReturn = false)
	{
		if (parsedTypes.TryGetValue(type, out var parsed))
			return parsed;
		String result = [];
		List<String>? innerErrors = null;
		for (var i = 0; i < type.ExtraTypes.Length; i++)
			if (type.ExtraTypes[i].Parent == null)
				typeof(TreeBranch).GetProperty("Parent")?.SetValue(type.ExtraTypes[i], branch);
		if (type.MainType.Peek().BlockType == BlockType.Extra)
		{
			var name = type.MainType.Peek().Name;
			if (UserDefinedPolymorphTypeExists(branch.Container, name, out _))
				return name;
			if (!((ConstantExists(new(new(type.MainType.SkipLast(1)), NoBranches), name, out var constant)
				|| UserDefinedConstantExists(branch.Container, name, out constant, out _, out _))
				&& constant.HasValue && constant.Value.DefaultValue != null))
				return "dynamic";
			result.AddRange(ParseAction(constant.Value.DefaultValue.Name)(constant.Value.DefaultValue, out innerErrors));
			if (result == "default!")
				return result;
			if (!(result.StartsWith("typeof(") && result.EndsWith(')')))
				throw new InvalidOperationException();
			AddRange(ref errors, innerErrors);
			var targetBranch = constant.Value.DefaultValue;
			if (targetBranch.Length != 0)
				targetBranch = targetBranch[0];
			if (targetBranch.Name == "type" && targetBranch.Extra is NStarType NStarType)
				type = NStarType;
			return result["typeof(".Length..^1];
		}
		else if (earlyReturn)
			return [];
		else if (TypeEqualsToPrimitive(type, "list", false))
		{
			var constantsDepth = this.constantsDepth;
			this.constantsDepth++;
			int levelsCount;
			if (type.ExtraTypes.Length == 1)
				levelsCount = 1;
			else if (int.TryParse(ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0],
				out innerErrors).ToString(), out var n))
			{
				levelsCount = n;
				AddRange(ref errors, innerErrors);
			}
			else
			{
				GenerateMessage(ref errors, 0x4057, type.ExtraTypes[0].Pos);
				this.constantsDepth = constantsDepth;
				return "dynamic";
			}
			this.constantsDepth = constantsDepth;
			if (type.ExtraTypes.Length == 2)
			{
				type.ExtraTypes[0].Name = levelsCount.ToString();
				type.ExtraTypes[0].Elements.Clear();
				type.ExtraTypes[0].Extra = IntType;
			}
			if (levelsCount == 0)
			{
				if (type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				result.AddRange(Type(ref InnerNStarType, branch, ref errors));
				type.ExtraTypes[^1].Extra = InnerNStarType;
			}
			else
			{
				result.AddRange(((String)"List<").Repeat(levelsCount - 1));
				if (type.ExtraTypes[^1].Name != "type" || type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
				{
					GenerateMessage(ref errors, 0x4056, type.ExtraTypes[^1].Pos);
					result.AddRange("dynamic");
				}
				else
				{
					var innerTypeName = Type(ref InnerNStarType, branch, ref errors);
					type.ExtraTypes[^1].Extra = InnerNStarType;
					AddListType(innerTypeName, InnerNStarType);
				}
				result.AddRange(((String)">").Repeat(levelsCount - 1));
			}
			parsedTypes.TryAdd(type, result);
			return result;
		}
		else if (TypeEqualsToPrimitive(type, "tuple", false))
		{
			BranchCollection newBranches = [];
			if (type.ExtraTypes.Length == 0)
				return "void";
			if (type.ExtraTypes[0].Extra is not NStarType FirstNStarType)
				throw new InvalidOperationException();
			var first = Type(ref FirstNStarType, branch, ref errors);
			type.ExtraTypes[0].Extra = FirstNStarType;
			if (type.ExtraTypes.Length == 1)
				return first;
			var innerType = type.ExtraTypes[0];
			newBranches.Add(innerType);
			using var innerResult = first.Copy();
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name == "type" && type.ExtraTypes[i].Extra is NStarType InnerNStarType)
				{
					result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult);
					innerType = type.ExtraTypes[i];
					innerResult.Replace(Type(ref InnerNStarType, branch, ref errors));
					type.ExtraTypes[i].Extra = InnerNStarType;
					newBranches.Add(innerType);
					continue;
				}
				if (!int.TryParse(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors).ToString(), out var n))
					n = 1;
				BranchCollection innerTypeCollection = new(RedStarLinq.FillArray(innerType, n));
				innerType = new("type", innerType.Pos, innerType.Container)
				{
					Extra = new NStarType(TupleBlockStack, innerTypeCollection)
				};
				newBranches[^1] = innerType;
				using var innerNameCollection = String.Join(", ", RedStarLinq.FillArray(innerResult, n));
				AddRange(ref errors, innerErrors);
				if (i >= 2 && type.ExtraTypes[i - 1].Name != "type")
					innerResult.Replace(((String)'(').AddRange(innerNameCollection).Add(')'));
				else
					innerResult.Replace(innerNameCollection);
			}
			type.ExtraTypes.Replace(newBranches);
			result.AddRange(result.Length == 0 ? "(" : ", ").AddRange(innerResult).Add(')');
			parsedTypes.TryAdd(type, result);
			return result;
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
				"long long" => nameof(MpzT),
				"complex" => "Complex",
				"string" => nameof(String),
				"index" => nameof(Index),
				"range" => nameof(Range),
				"typename" => "Type",
				"universal" => "object",
				_ => type.MainType.Peek().Name,
			};
		}
		else if (type.MainType.Equals(FuncBlockStack))
		{
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType ReturnNStarType)
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				return "dynamic";
			}
			var noReturn = TypeEqualsToPrimitive(ReturnNStarType, "null");
			if (noReturn && type.ExtraTypes.Length == 1)
			{
				return result.AddRange("Action");
				//return result.AddRange("Func<").AddRange(nameof(ValueTask)).Add('>');
			}
			result.AddRange(noReturn ? "Action<" : "Func<");
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType)
					result.AddRange(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i], out innerErrors));
				else
				{
					result.AddRange(Type(ref InnerNStarType, branch, ref errors));
					type.ExtraTypes[i].Extra = InnerNStarType;
				}
				AddRange(ref errors, innerErrors);
				if (!(noReturn && i == type.ExtraTypes.Length - 1))
					result.AddRange(", ");
			}
			if (!noReturn)
			{
				result.AddRange(Type(ref ReturnNStarType, branch, ref errors));
				type.ExtraTypes[0].Extra = ReturnNStarType;
			}
			//if (noReturn)
			//	result.AddRange(", ").AddRange(nameof(ValueTask));
			//else
			//{
			//	result.AddRange(nameof(ValueTask)).Add('<').AddRange(Type(ref ReturnNStarType, branch, ref errors)).Add('>');
			//	type.ExtraTypes[0].Extra = ReturnNStarType;
			//}
			result.Add('>');
			parsedTypes.TryAdd(type, result);
			return result;
		}
		result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(type.MainType.FindLastIndex(x =>
			x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct
			or BlockType.Interface or BlockType.Delegate)) + 1))
			.ToString()));
		ExplicitlyConnectedNamespaces.Reverse<String>().ForEach(x => result.ReplaceInPlace(x.Copy().Add('.'), ""));
		if (result == nameof(G.IEnumerable<>))
			result.Insert(0, "G.");
		if (result == nameof(ListHashSet<>) && type.ExtraTypes.Length == 1)
		{
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType InnerNStarType)
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				result.AddRange("dynamic");
				return "dynamic";
			}
		}
		if (type.ExtraTypes.Length == 0)
		{
			parsedTypes.TryAdd(type, result);
			return result;
		}
		if (type.ExtraTypes.Length == 1 && type.ExtraTypes[0].Name == nameof(List))
		{
			ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0], out innerErrors);
			AddRange(ref errors, innerErrors);
			String innerResult = [];
			var preservedErrors = errors;
			for (var i = 0; i < type.ExtraTypes[0].Length; i++)
			{
				var x = type.ExtraTypes[0][i];
				if (x.Name == "Hypername" && x.Length == 1)
					x = x[0];
				if (x.Name != "type" || x.Extra is not NStarType NStarType)
					continue;
				if (innerResult.Length != 0)
					innerResult.AddRange(", ");
				innerResult.AddRange(Type(ref NStarType, branch, ref preservedErrors));
			}
			if (innerResult.Length != 0)
				result.Add('<').AddRange(innerResult).Add('>');
			parsedTypes.TryAdd(type, result);
			return result;
		}
		if (type.ExtraTypes.All(x => x.Value.Name == "type" && x.Value.Extra is NStarType NullNStarType
			&& NullNStarType.Equals(NullType)))
			return result;
		result.Add('<');
		for (var i = 0; i < type.ExtraTypes.Length; i++)
		{
			if (type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType)
				result.AddRange(type.ExtraTypes[i].Name);
			else
			{
				result.AddRange(Type(ref InnerNStarType, branch, ref errors));
				type.ExtraTypes[i].Extra = InnerNStarType;
			}
			if (i != type.ExtraTypes.Length - 1)
				result.AddRange(", ");
		}
		result.Add('>');
		parsedTypes.TryAdd(type, result);
		return result;
		void AddListType(String innerTypeName, NStarType NStarType)
		{
			if (!TypeIsFullySpecified(NStarType, branch.Container))
			{
				result.AddRange(nameof(List<>)).Add('<');
				result.AddRange(innerTypeName);
				result.Add('>');
				return;
			}
			var DotNetType = TypeConverters.TypeMapping(NStarType);
			if (DotNetType == typeof(bool))
				result.AddRange(nameof(BitList));
			else
			{
				result.AddRange(nameof(List<>)).Add('<');
				result.AddRange(innerTypeName);
				result.Add('>');
			}
		}
	}

	private static String TypeMapping(String typeName)
	{
		var after = typeName.GetAfter(((String)"System.Collections.").AddRange(nameof(G.LinkedList<>)));
		if (after.Length != 0)
			return "G.LinkedList" + after;
		after = typeName.GetAfter("System.Collections.");
		if (after.Length != 0)
			return after;
		return typeName;
	}

	private String TypeReflected(ref NStarType type, TreeBranch branch, ref List<String>? errors)
	{
		if (TypeIsFullySpecified(type, branch.Container))
			return ((String)"typeof(").AddRange(Type(ref type, branch, ref errors)).Add(')');
		String result = [];
		List<String>? innerErrors = null;
		for (var i = 0; i < type.ExtraTypes.Length; i++)
			if (type.ExtraTypes[i].Parent == null)
				typeof(TreeBranch).GetProperty("Parent")?.SetValue(type.ExtraTypes[i], branch);
		if (type.MainType.Peek().BlockType == BlockType.Extra)
			return type.MainType.ToString();
		if (TypeEqualsToPrimitive(type, "list", false))
		{
			var constantsDepth = this.constantsDepth;
			this.constantsDepth++;
			int levelsCount;
			if (type.ExtraTypes.Length == 1)
				levelsCount = 1;
			else if (int.TryParse(ParseAction(type.ExtraTypes[0].Name)(type.ExtraTypes[0],
				out innerErrors).ToString(), out var n))
			{
				levelsCount = n;
				AddRange(ref errors, innerErrors);
			}
			else
			{
				GenerateMessage(ref errors, 0x4057, type.ExtraTypes[0].Pos);
				this.constantsDepth = constantsDepth;
				return "dynamic";
			}
			this.constantsDepth = constantsDepth;
			if (type.ExtraTypes.Length == 2)
			{
				type.ExtraTypes[0].Name = levelsCount.ToString();
				type.ExtraTypes[0].Elements.Clear();
				type.ExtraTypes[0].Extra = IntType;
			}
			if (levelsCount == 0)
			{
				if (type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
					throw new InvalidOperationException();
				result.AddRange(TypeReflected(ref InnerNStarType, branch, ref errors));
			}
			else
			{
				result.AddRange(((String)"typeof(List<>).MakeGenericType(").Repeat(levelsCount - 1));
				if (type.ExtraTypes[^1].Name != "type" || type.ExtraTypes[^1].Extra is not NStarType InnerNStarType)
				{
					GenerateMessage(ref errors, 0x4056, type.ExtraTypes[^1].Pos);
					result.AddRange("dynamic");
				}
				else
				{
					var innerTypeName = TypeReflected(ref InnerNStarType, branch, ref errors);
					result.AddRange(nameof(ConstructListType)).Add('(').AddRange(innerTypeName).Add(')');
				}
				result.AddRange(((String)")").Repeat(levelsCount - 1));
			}
		}
		else if (TypeEqualsToPrimitive(type, "tuple", false))
		{
			BranchCollection newBranches = [];
			if (type.ExtraTypes.Length == 0)
				return "void";
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType FirstNStarType)
				throw new InvalidOperationException();
			using var prefix = ((String)nameof(ConstructTupleType)).AddRange("(new Type[] { ");
			using var suffix = ((String)" }.").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("())");
			using var singularPrefix = ((String)nameof(ConstructTupleType)).Add('(');
			singularPrefix.AddRange(nameof(RedStarLinq)).Add('.').AddRange(nameof(RedStarLinq.Fill)).Add('(');
			using var singularSuffix = ((String)").").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("())");
			var first = TypeReflected(ref FirstNStarType, branch, ref errors);
			if (type.ExtraTypes.Length == 1)
				return first;
			var innerType = type.ExtraTypes[0];
			newBranches.Add(innerType);
			using var innerResult = first.Copy();
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				if (type.ExtraTypes[i].Name == "type" && type.ExtraTypes[i].Extra is NStarType InnerNStarType)
				{
					result.AddRange(result.Length == 0 ? prefix : ", ").AddRange(innerResult);
					innerType = type.ExtraTypes[i];
					innerResult.Replace(TypeReflected(ref InnerNStarType, branch, ref errors));
					newBranches.Add(innerType);
					continue;
				}
				if (!int.TryParse(ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors).ToString(), out var n))
					n = 1;
				BranchCollection innerTypeCollection = new(RedStarLinq.FillArray(innerType, n));
				innerType = new("type", innerType.Pos, innerType.Container)
				{
					Extra = new NStarType(TupleBlockStack, innerTypeCollection)
				};
				newBranches[^1] = innerType;
				AddRange(ref errors, innerErrors);
				innerResult.Insert(0, singularPrefix).AddRange(", ").AddRange(n.ToString()).AddRange(singularSuffix);
			}
			type.ExtraTypes.Replace(newBranches);
			result.AddRange(result.Length == 0 ? prefix : ", ").AddRange(innerResult).AddRange(suffix);
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
				"long long" => nameof(MpzT),
				"complex" => "Complex",
				"string" => nameof(String),
				"index" => nameof(Index),
				"range" => nameof(Range),
				"typename" => "Type",
				"universal" => "object",
				_ => type.MainType.Peek().Name,
			};
		}
		else if (type.MainType.Equals(FuncBlockStack))
		{
			if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType ReturnNStarType)
			{
				GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
				return "dynamic";
			}
			var noReturn = TypeEqualsToPrimitive(ReturnNStarType, "null");
			result.AddRange(nameof(ConstructFuncType)).Add('(');
			result.AddRange(TypeReflected(ref ReturnNStarType, branch, ref errors));
			if (type.ExtraTypes.Length >= 3)
				result.AddRange(", new Type[] { ");
			else if (type.ExtraTypes.Length == 2)
				result.AddRange(", ");
			for (var i = 1; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType
					? ParseAction(type.ExtraTypes[i].Name)(type.ExtraTypes[i],
					out innerErrors) : TypeReflected(ref InnerNStarType, branch, ref errors));
				AddRange(ref errors, innerErrors);
				if (!(noReturn && i == type.ExtraTypes.Length - 1))
					result.AddRange(", ");
			}
			if (type.ExtraTypes.Length >= 3)
				result.AddRange(" }.").AddRange(nameof(RedStarLinq.GetSlice)).AddRange("()");
			result.Add(')');
		}
		else
		{
			result.AddRange(TypeMapping(new BlockStack(type.MainType.Skip(type.MainType.FindLastIndex(x =>
				x.BlockType is not (BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface)) + 1))
				.ToString()));
			if (result == nameof(G.IEnumerable<>))
				result.Insert(0, "G.");
			if (result == nameof(ListHashSet<>) && type.ExtraTypes.Length == 1)
			{
				if (type.ExtraTypes[0].Name != "type" || type.ExtraTypes[0].Extra is not NStarType InnerNStarType)
				{
					GenerateMessage(ref errors, 0x4056, type.ExtraTypes[0].Pos);
					return "typeof(dynamic)";
				}
			}
			if (type.ExtraTypes.Length == 0)
				return result;
			result.Insert(0, "typeof(").AddRange("<>).MakeGenericType(");
			for (var i = 0; i < type.ExtraTypes.Length; i++)
			{
				result.AddRange(type.ExtraTypes[i].Name != "type" || type.ExtraTypes[i].Extra is not NStarType InnerNStarType
					? type.ExtraTypes[i].Name : TypeReflected(ref InnerNStarType, branch, ref errors));
				if (i != type.ExtraTypes.Length - 1)
					result.AddRange(", ");
			}
			result.Add(')');
		}
		return result;
	}

	private bool VariableExists(TreeBranch branch, String name, ref List<String>? errors)
	{
		List<int> indexes = [];
		var preservedBranch = branch;
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
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == "Parameter")
					&& branches[i][j][1].Name == name && !(i == indexes.Length - 1 && j >= indexes[^1]))
				{
					var otherPos = branches[i][j].FirstPos;
					return Error(ref errors, otherPos);
				}
				else if (BranchesToSearchDeeper.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2)
					&& VariableExistsInsideExpr(branches[i][j], name, out var otherPos, out _)
					&& !(i == indexes.Length - 1 && j >= indexes[^1]))
					return Error(ref errors, otherPos);
			}
			if (branches[i].Name == nameof(Function))
				break;
		}
		return false;
		bool Error(ref List<String>? errors, int otherPos)
		{
			GenerateMessage(ref errors, 0x4013, preservedBranch.Pos, name,
				lexems[otherPos].LineN.ToString(), lexems[otherPos].Pos.ToString());
			return true;
		}
	}

	private bool IsVariableDeclared(TreeBranch branch, String name, out List<String>? errors, out object? extra)
	{
		errors = default!;
		List<int> indexes = [];
		var preservedBranch = branch;
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
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _)
				&& (functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
			{
				GenerateMessage(ref errors, 0x4010, preservedBranch.FirstPos, name);
				extra = null;
				return false;
			}
			for (var j = 0; j < indexes[i] - 1; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == "Parameter")
					&& branches[i][j][1].Name == name)
				{
					extra = branches[i][j][0].Extra;
					return true;
				}
				else if (branches[i].Name == nameof(Lambda) && branches[i].Length == 2
					&& (IsValidLambdaParameter(branches[i][0], name, out var innerExtra) || branches[i][0].Name == nameof(List)
					&& branches[i][0].Elements.Any(x => IsValidLambdaParameter(x, name, out innerExtra))))
				{
					extra = innerExtra;
					return true;
				}
				else if (BranchesToSearchDeeperNoReturn.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2
					|| branches[i].Elements[(j + 1)..(indexes[i] - 1)].All(x => x.Name.AsSpan() is "if" or "if!"))
				&& VariableExistsInsideExpr(branches[i][j], name, out _, out innerExtra))
				{
					extra = innerExtra;
					return true;
				}
			}
			for (var j = indexes[i]; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == "Parameter")
					&& branches[i][j][1].Name == name)
				{
					var otherPos = branches[i][j].FirstPos;
					return Error(ref errors, out extra, otherPos);
				}
				else if (BranchesToSearchDeeper.Contains(branches[i][j].Name.ToString())
					&& (branches[i][j].Name != "for" || j == indexes[i] - 2)
					&& VariableExistsInsideExpr(branches[i][j], name, out var otherPos, out _))
					return Error(ref errors, out extra, otherPos);
			}
		}
		if (errors == null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		extra = null;
		return false;
		static bool IsValidLambdaParameter(TreeBranch branch, String branchName, out object? extra)
		{
			if (branch.Length == 0)
			{
				extra = branch.Extra;
				return branch.Name == branchName;
			}
			if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
			{
				extra = null;
				return false;
			}
			extra = branch.Extra;
			return branch[0].Name == branchName;
		}

		bool Error(ref List<String>? errors, out object? extra, int otherPos)
		{
			GenerateMessage(ref errors, 0x4012, preservedBranch.FirstPos, name,
				lexems[otherPos].LineN.ToString(), lexems[otherPos].Pos.ToString());
			extra = null;
			return false;
		}
	}

	private static bool VariableExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Name == nameof(Declaration) || branch[i].Name == "Parameter") && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (ExprTypesToSearchDeeper.Contains(branch[i].Name.ToString())
					&& VariableExistsInsideExpr(branch[i], name, out pos, out extra))
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

	private bool IsPropertyDeclared(TreeBranch branch, String name, out List<String>? errors,
		out UserDefinedProperty? property, out bool inBase, out BlockStack actualContainer)
	{
		errors = default!;
		(BlockStack Container, String Type) matchingKey = default!;
		if (CheckContainer(branch.Container, x => UserDefinedTypes.ContainsKey(matchingKey = SplitType(x)),
			out _) && CreateVar(CreateVar(UserDefinedTypes[matchingKey].Restrictions, out var restrictions)
			.FindIndex(x => x.Name == name), out var foundIndex) >= 0)
		{
			property = new(restrictions[foundIndex].RestrictionType, PropertyAttributes.Required, []);
			inBase = false;
			actualContainer = branch.Container;
			return true;
		}
		else if (!UserDefinedPropertyExists(branch.Container, name, false, out property, out _, out inBase,
			out actualContainer))
		{
			if (errors == null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			return false;
		}
		else if (inBase)
			return true;
		List<int> indexes = [];
		var preservedBranch = branch;
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
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _))
			{
				if ((functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
				{
					GenerateMessage(ref errors, 0x4031, preservedBranch.FirstPos, name);
					return false;
				}
				else if ((functions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (property?.Attributes & PropertyAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4032, preservedBranch.FirstPos, name);
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Name == nameof(Property) && branches[i][j].Length == 3 && branches[i][j][1].Name == name)
				{
					return true;
				}
				else if ((branches[i][j].Name == "ClassMain" || branches[i][j].Name == "Members")
					&& PropertyExistsInsideExpr(branches[i][j], name, out _, out var innerExtra))
				{
					return true;
				}
			}
		}
		if (errors == null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		return false;
	}

	private bool IsConstantDeclared(TreeBranch branch, String name, out List<String>? errors, out UserDefinedConstant? constant)
	{
		errors = default!;
		if (!UserDefinedConstantExists(branch.Container, name, out constant, out _, out var inBase))
		{
			if (errors == null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			return false;
		}
		else if (inBase)
			return true;
		List<int> indexes = [];
		var preservedBranch = branch;
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
			if (branches[i].Name == nameof(Function) && branches[i].Length == 4
				&& UserDefinedNonDerivedFunctionExists(branches[i].Container, branches[i][0].Name, out var functions, out _))
			{
				if ((functions[^1].Attributes & FunctionAttributes.Multiconst) != 0)
				{
					GenerateMessage(ref errors, 0x4031, preservedBranch.FirstPos, name);
					return false;
				}
				else if ((functions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (constant?.Attributes & ConstantAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4032, preservedBranch.FirstPos, name);
					return false;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if ((branches[i][j].Name == nameof(Declaration) || branches[i][j].Name == "Parameter")
					&& branches[i][j][1].Name == name)
					return true;
				else if (branches[i].Name == nameof(Lambda) && branches[i].Length == 2
					&& (IsValidLambdaParameter(branches[i][0], name) || branches[i][0].Name == nameof(List)
					&& branches[i][0].Elements.Any(x => IsValidLambdaParameter(x, name))))
					return true;
				else if (BranchesToSearchDeeperNoReturn.Contains(branches[i][j].Name.ToString())
					&& ConstantExistsInsideExpr(branches[i][j], name, out _, out _))
					return true;
				if (branches[i][j].Name == nameof(Constant) && branches[i][j].Length == 3 && branches[i][j][1].Name == name)
					return true;
				else if ((branches[i][j].Name == "ClassMain" || branches[i][j].Name == "Members")
					&& ConstantExistsInsideExpr(branches[i][j], name, out _, out _))
					return true;
			}
		}
		if (errors == null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		return false;
		static bool IsValidLambdaParameter(TreeBranch branch, String branchName)
		{
			if (branch.Length == 0)
				return branch.Name == branchName;
			if (branch.Name != nameof(Hypername) || branch.Length != 1 || branch[0].Length != 0)
				return false;
			return branch[0].Name == branchName;
		}
	}

	private static bool PropertyExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == nameof(Property) && branch[i].Length == 3 && branch[i][1].Name == name)
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

	private static bool ConstantExistsInsideExpr(TreeBranch branch, String name, out int pos, out object? extra)
	{
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if (branch[i].Name == nameof(Constant) && branch[i].Length == 3 && branch[i][1].Name == name)
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
		try
		{
			for (var i = 0; i < branch.Length; i++)
			{
				if ((branch[i].Name == nameof(Declaration) || branch[i].Name == "Parameter") && branch[i][1].Name == name)
				{
					pos = branch[i].FirstPos;
					extra = branch[i][0].Extra;
					return true;
				}
				else if (ExprTypesToSearchDeeper.Contains(branch[i].Name.ToString())
					&& ConstantExistsInsideExpr(branch[i], name, out pos, out extra))
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

	private bool IsFunctionDeclared(TreeBranch branch, String name, out List<String>? errors,
		[MaybeNullWhen(false)] out UserDefinedMethodOverloads functions,
		[MaybeNullWhen(false)] out BlockStack matchingContainer, out object? extra)
	{
		errors = default!;
		if (!UserDefinedNonDerivedFunctionExists(branch.Container, name, out functions, out matchingContainer))
		{
			if (errors == null || errors.Length == 0)
				GenerateMessage(ref errors, 0x4001, branch.FirstPos, name);
			extra = null;
			return false;
		}
		List<int> indexes = [];
		var preservedBranch = branch;
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
			if (branches[i].Name == nameof(Function) && branches[i].Length >= 3
				&& UserDefinedNonDerivedFunctionExists(branches[i].Container,
				branches[i][0].Name, out var innerFunctions, out _))
			{
				if ((innerFunctions[^1].Attributes & FunctionAttributes.Multiconst) != 0
					&& (functions[^1].Attributes & FunctionAttributes.Multiconst) == 0)
				{
					GenerateMessage(ref errors, 0x4024, preservedBranch.FirstPos, name);
					extra = null;
					return false;
				}
				else if ((innerFunctions[^1].Attributes & FunctionAttributes.Static) != 0
					&& (functions[^1].Attributes & FunctionAttributes.Static) == 0)
				{
					GenerateMessage(ref errors, 0x4025, preservedBranch.FirstPos, name);
					extra = null;
					return false;
				}
				else if (branches[i][0].Name == name)
				{
					extra = branches[i];
					return true;
				}
			}
			for (var j = 0; j < branches[i].Length; j++)
			{
				if (j == indexes[i] - 1)
					continue;
				if (branches[i][j].Name == nameof(Function) && branches[i][j].Length >= 3 && branches[i][j][0].Name == name)
				{
					extra = branches[i][j];
					return true;
				}
			}
		}
		if (errors == null || errors.Length == 0)
			GenerateMessage(ref errors, 0x4001, preservedBranch.FirstPos, name);
		extra = null;
		return false;
	}

	private static bool IsAnyAssignment(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch assignmentBranch,
		out int assignmentIndex)
	{
		var parent = branch.Parent;
		while (parent != null)
		{
			if (parent.Name.AsSpan() is nameof(Assignment) or "DeclarationAssignment" or nameof(List))
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				assignmentBranch = parent;
				assignmentIndex = Max(prevIndex + 1, 2);
				return true;
			}
			parent = parent.Parent;
		}
		assignmentBranch = null;
		assignmentIndex = -1;
		return false;
	}

	private static bool IsAssignment(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch assignmentBranch,
		out int assignmentIndex)
	{
		var parent = branch.Parent;
		while (parent != null)
		{
			if (parent.Name == nameof(Assignment))
			{
				var prevIndex = parent.Elements.FindIndex(x => ReferenceEquals(branch, x));
				assignmentBranch = parent;
				assignmentIndex = Max(prevIndex + 1, 2);
				return true;
			}
			parent = parent.Parent;
		}
		assignmentBranch = null;
		assignmentIndex = -1;
		return false;
	}

	private static bool IsConstructor(TreeBranch branch, [MaybeNullWhen(false)] out TreeBranch constructorBranch,
		[MaybeNullWhen(false)] out ConstructorOverloads overloads)
	{
		var parent = branch.Parent;
		while (parent != null)
		{
			if (parent.Name != nameof(Constructor))
			{
				parent = parent.Parent;
				continue;
			}
			constructorBranch = parent;
			if (parent.Length == 0 || parent[0].Elements.Any(x => x.Length == 0)
				|| !UserDefinedConstructors.TryGetValue(parent.Container, out overloads))
			{
				constructorBranch = null;
				overloads = null;
				return false;
			}
			overloads = [overloads.FindLast(x => parent[0].Elements.Length == x.Parameters.Length
				&& parent[0].Elements.Combine(x.Parameters)
				.All(x => x.Item1[0].Extra is NStarType NStarType && NStarType.Equals(x.Item2.Type)))];
			return true;
		}
		constructorBranch = null;
		overloads = null;
		return false;
	}

	private bool IsSpaghettiOutOfRecursion(String functionName) =>
		IsSpaghettiOutOfRecursion(functionName, functionName, [], false);

	private bool IsSpaghettiOutOfRecursion(String functionName, String targetName,
		ListHashSet<String> blackList, bool includesDirect)
	{
		if (blackList.Contains(functionName) || !functionReferences.TryGetValue(functionName, out var references))
			return false;
		if ((includesDirect |= references.Contains(functionName))
			&& functionName != targetName && references.Contains(targetName))
			return true;
		foreach (var x in references.Copy().ExceptWith(blackList))
			if (IsSpaghettiOutOfRecursion(x, targetName, blackList.Add(functionName), includesDirect))
				return true;
		return false;
	}

	private static String GetFunctionName(TreeBranch branch)
	{
		var parent = branch.Parent;
		while (parent != null)
		{
			if (parent.Name == nameof(Function) && parent.Length >= 3
				&& UserDefinedNonDerivedFunctionExists(parent.Container, parent[0].Name, out _, out _))
				return parent[0].Name;
			if (parent.Name == nameof(Constructor) && parent.Length >= 2)
				return "new " + parent.Container.ToString();
			parent = parent.Parent;
		}
		return [];
	}

	private void GenerateMessage(ref List<String>? errors, ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, lexems[pos].LineN, lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			wreckOccurred = true;
	}

	private static void Add<T>(ref List<T>? source, T item)
	{
		source ??= [];
		source.Add(item);
	}

	private static void AddRange<T>(ref List<T>? source, G.IEnumerable<T>? collection)
	{
		if (collection is not null)
		{
			source ??= [];
			source.AddRange(collection);
		}
	}

	private static bool IsTypeContext(TreeBranch branch) =>
		branch.Container.TryPeek(out var nearestBlock)
		&& nearestBlock.BlockType is BlockType.Namespace or BlockType.Class or BlockType.Struct or BlockType.Interface;

	public static String ExecuteProgram(String program, out String errors, params dynamic?[] args) =>
		TranslateAndExecuteProgram(program, out errors, out _, args);

	public static String TranslateAndExecuteProgram(String program, out String errors,
		out Assembly? assembly, params dynamic?[] args)
	{
		List<String>? errorsInListForm = null;
		try
		{
			ClearUserDefinedLists();
			var translated = TranslateProgram(program);
			AddRange(ref errorsInListForm, translated.errors);
			return ExecuteProgram(translated, out errors, out assembly, args);
		}
		catch (OutOfMemoryException)
		{
			Add(ref errorsInListForm, "Technical wreck F002 in unknown line at unknown position:" +
				" memory limit exceeded during compilation, translation or execution; program has not been executed\r\n");
			errors = String.Join("\r\n", errorsInListForm?.Append([]) ?? []);
			assembly = null;
			return "null";
		}
		catch (Exception ex)
		{
			const string errorMessage = "Technical wreck F003 in unknown line at unknown position:" +
				" a serious error occurred during compilation, translation or execution; program has not been executed\r\n";
			try
			{
				var targetLexem = TreeBranch.LastTreePos < 0 || lastLexems == null
					|| TreeBranch.LastTreePos >= lastLexems.Length
					? new([], LexemType.Int, 0, 0) : lastLexems[TreeBranch.LastTreePos];
				File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
					+ @"\CSharp.NStar.log", [errorMessage, "The last visited location was: line " + targetLexem.LineN
				+ ", position " + targetLexem.Pos, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? "null",
					"The underlying internal exception message was:", ex.InnerException?.Message ?? "null"]);
				Add(ref errorsInListForm, errorMessage + @" (see %TEMP%\CSharp.NStar.log for details)");
			}
			catch
			{
				Add(ref errorsInListForm,
					errorMessage + " (also could not write to the log, check your environment TEMP variable)");
			}
			errors = String.Join("\r\n", errorsInListForm?.Append([]) ?? []);
			assembly = null;
			return "null";
		}
	}

	private static void ClearUserDefinedLists()
	{
		ExplicitlyConnectedNamespaces.Clear();
		UserDefinedConstants.Clear();
		UserDefinedConstructors.Clear();
		UserDefinedConstructorIndexes.Clear();
		UserDefinedFunctions.Clear();
		UserDefinedImplementedInterfaces.Clear();
		UserDefinedIndexers.Clear();
		UserDefinedNamespaces.Clear();
		UserDefinedProperties.Clear();
		UserDefinedPropertiesMapping.Clear();
		UserDefinedPropertiesOrder.Clear();
		UserDefinedTypes.Clear();
		Variables.Clear();
	}

	public static (String s, List<String>? errors, String translatedClasses) TranslateProgram(String program)
	{
		var s = new SemanticTree((LexemStream)new CodeSample(program)).Parse(out var errors, out var translatedClasses);
		return (s, errors, translatedClasses);
	}

	public static String ExecuteProgram((String s, List<String>? errors, String translatedClasses) translated,
		out String errors, out Assembly? assembly, params dynamic?[] args)
	{
		var (bytes, errorsInListForm) = CompileProgram(translated);
		assembly = EasyEval.GetAssembly(bytes);
		var result = (Task<object>?)assembly?.GetType("Program")?.GetMethod("F")?.Invoke(null, [args]);
		errors = errorsInListForm == null || errorsInListForm.Length == 0 ? "Ошибок нет" :
			String.Join("\r\n", errorsInListForm.Append([]));
		return result is null ? "null" : JsonConvert.SerializeObject(AsyncContext.Run(async () => await result), JsonConverters.SerializerSettings);
	}

	public static String CompileProgram(String program)
	{
		try
		{
			ClearUserDefinedLists();
			var (s, _, translatedClasses) = TranslateProgram(program);
			return GetSourceCode(s, translatedClasses);
		}
		catch
		{
			return [];
		}
	}

	private static String GetSourceCode(String main, String translatedClasses) => ((String)@"using Avalonia;
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Animation)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.ApplicationLifetimes)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.PanAndZoom)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Controls)).Add('.').AddRange(nameof(Avalonia.Controls.Primitives)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Input)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Interactivity)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Layout)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Markup)).Add('.').AddRange(nameof(Avalonia.Markup.Xaml)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Media)).AddRange(@";
using ").AddRange(nameof(Avalonia)).Add('.').AddRange(nameof(Avalonia.Media)).Add('.').AddRange(nameof(Avalonia.Media.Imaging)).AddRange(@";
using CSharp.NStar;
using ").AddRange(nameof(Mpir)).Add('.').AddRange(nameof(Mpir.NET)).AddRange(@";
using ").AddRange(nameof(Nito)).Add('.').AddRange(nameof(Nito.AsyncEx)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.BufferLib)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Core)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Dictionaries)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Linq)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.MathLib)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.RemoveDoubles)).AddRange(@";
using ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.SumCollections)).AddRange(@";
using ").AddRange(nameof(ReactiveUI)).AddRange(@";
using ").AddRange(nameof(ReactiveUI)).Add('.').AddRange(nameof(ReactiveUI.Avalonia)).AddRange(@";
using System;
using System.Dynamic;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.Core)).Add('.').AddRange(nameof(Extents)).AddRange(@";
using static ").AddRange(nameof(NStar)).Add('.').AddRange(nameof(global::NStar.EasyEvalLib)).Add('.').AddRange(nameof(EasyEval)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(BuiltInMemberCollections)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(NStarUtilityFunctions)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(MemberConverters)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(Quotes)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(SemanticTree)).AddRange(@";
using static CSharp.NStar.").AddRange(nameof(TypeConverters)).AddRange(@";
using static Mpir.NET.").AddRange(nameof(MpzT)).AddRange(@";
using static System.Math;
using static System.Numerics.Complex;
using G = System.Collections.Generic;
using String = NStar.Core.String;

").AddRange(translatedClasses).AddRange(@"
public static class Program
{
	public static string[] args = [];

public static async Task<dynamic?> F(params dynamic?[] args)
{
").AddRange(main).AddRange(""""

				return null;
			}
			
				public static void Main(string[] args)
				{
					Program.args = args;
					BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
					var result = AsyncContext.Run(async () => await F(args));
					if (result != null)
						Console.WriteLine(result);
				}

				// Avalonia configuration, don't remove; also used by visual designer.
				public static AppBuilder BuildAvaloniaApp()
					=> AppBuilder.Configure<App>()
					.UsePlatformDetect()
					.WithInterFont()
					.LogToTrace()
					.UseReactiveUI();
			}

			public partial class App : Application
			{
				public override void Initialize()
				{
					AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(this, """
						<Application xmlns="https://github.com/avaloniaui"
									 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
									 xmlns:fluent="clr-namespace:Avalonia.Themes.Fluent;assembly=Avalonia.Themes.Fluent"
									 x:Class="App"
									 RequestedThemeVariant="Light">
									 <!-- "Default" ThemeVariant follows system theme variant. "Dark" or "Light" are other available options. -->

							<Application.Styles>
								<fluent:FluentTheme />
							</Application.Styles>
						</Application>

						"""));
					var result = Program.F(Program.args);
					if (result != null)
						Console.WriteLine(result);
				}

				public override void OnFrameworkInitializationCompleted()
				{
					if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
						return;
					desktop.MainWindow = null;
					base.OnFrameworkInitializationCompleted();
				}
			}
						
			"""");

	private static (byte[] Bytes, List<String> ErrorsList) CompileProgram((String s, List<String>? errors,
		String translatedClasses) translated)
	{
		var (s, errors, translatedClasses) = translated;
		var bytes = EasyEval.Compile(GetSourceCode(s, translatedClasses), GetExtraAssemblies(), out var compileErrors);
		if (bytes == null || bytes.Length <= 2 || compileErrors != "Compilation done without any error.\r\n")
			throw new EvaluationFailedException();
		return (bytes, errors ?? []);
	}

	public static G.IEnumerable<String> GetExtraAssemblies() =>
		["Avalonia", "Avalonia.Base", "Avalonia.Controls", "Avalonia.Desktop", "Avalonia.FreeDesktop",
		"Avalonia.Fonts.Inter", "Avalonia.Markup.Xaml", "Avalonia.Markup.Xaml.Loader", "Avalonia.Native",
		"Avalonia.Remote.Protocol", "Avalonia.Skia", "Avalonia.Themes.Fluent", "Avalonia.Win32", "Avalonia.X11",
		"BuiltInMemberCollections", "BuiltInTypeCollections", "CodeSample", "DynamicData", "HarfBuzzSharp", "MainParsing",
		"MemberChecks", "MemberConverters", "MicroCom.Runtime", "Nito.AsyncEx.Context",
		"NStar.EasyEval", "NStarEntity", "NStarType", "NStarUtilityFunctions",
		"PanAndZoom", "QuotesAndTreeBranch", "ReactiveUI", "ReactiveUI.Avalonia", "SemanticTree", "SkiaSharp",
		"Splat", "Splat.Builder", "Splat.Core", "Splat.Logging", "System.ObjectModel", "System.Private.Uri", "System.Reactive",
		"System.Runtime.Numerics", "System.Threading.Tasks.Parallel", "Tmds.DBus.Protocol", "TypeChecks", "TypeConverters"];

	private static bool TryReadValue(String s, out NStarEntity value) => NStarEntity.TryParse(s.ToString(), out value)
		|| s.StartsWith("(String)") && NStarEntity.TryParse(s["(String)".Length..].ToString(), out value)
		|| s.StartsWith("((String)") && s.EndsWith(')')
		&& NStarEntity.TryParse(s["((String)".Length..^1].ToString(), out value);

	public override string ToString() => $"({String.Join(", ",
		lexems.ToArray(x => (String)x.ToString())).TakeIntoVerbatimQuotes()}, {input.TakeIntoVerbatimQuotes()},"
		+ $" {((String)topBranch.ToString()).TakeIntoVerbatimQuotes()},"
		+ $" ({(errors != null && errors.Length != 0 ? String.Join(", ", errors.ToArray(x => x.TakeIntoVerbatimQuotes()))
		: "NoErrors")}), {wreckOccurred})";

	[GeneratedRegex(@"\$(@?[A-Za-z_][0-9A-Za-z_]*)\$")]
	private static partial Regex RecursiveTypeRegex();
}

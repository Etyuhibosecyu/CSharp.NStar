namespace CSharp.NStar;
public class LexemStream
{
	private protected readonly List<Lexem> lexems;
	private protected readonly String input;
	private protected List<String>? errorsList;
	private readonly BlockStack? rootContainer;
	private protected bool wreckOccurred;
	private protected int pos;
	private int pos2;
	private readonly BlockStack nestedBlocksChain = new();
	private int globalUnnamedIndex = 1;
	private protected readonly BlocksToJump blocksToJump = [];
	private protected readonly LexemGroup registeredTypes = [];
	private protected readonly LexemGroup parameterLists = [];
	private readonly TypeDictionary<int> actualConstructorIndex = [];
	private int unknownIndex = 1;
	private int figureBk;

	private protected static readonly List<String> StopLexemsList = ["\r\n", ";", "{", "}"];

	private protected LexemStream(List<Lexem> lexems, String input, List<String>? errorsList, bool wreckOccurred, BlockStack? rootContainer = null)
	{
		this.lexems = lexems;
		this.input = input;
		this.errorsList = errorsList;
		this.rootContainer = rootContainer;
		this.wreckOccurred = wreckOccurred;
		pos = 0;
		pos2 = 0;
	}

	private protected LexemStream(LexemStream lexemStream) : this(lexemStream.lexems, lexemStream.input, lexemStream.errorsList, lexemStream.wreckOccurred, lexemStream.rootContainer)
	{
		pos = lexemStream.pos;
		blocksToJump = lexemStream.blocksToJump;
		registeredTypes = lexemStream.registeredTypes;
		parameterLists = lexemStream.parameterLists;
	}

	public (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) Parse()
	{
		PreParse();
		return wreckOccurred ? EmptySyntaxTree() : new MainParsing(this, wreckOccurred).MainParse();
	}

	private void PreParse()
	{
		try
		{
			if (wreckOccurred)
				return;
			while (pos < lexems.Length)
			{
				PreParseIteration();
				if (wreckOccurred)
					return;
			}
			if (figureBk != 0)
			{
				GenerateMessage("Wreck", pos - 1, "unpaired bracket; expected: }");
				return;
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			var pos2 = (pos >= lexems.Length) ? pos - 1 : pos;
			GenerateMessage("Wreck", pos2, "compilation failed because of internal compiler error");
			return;
		}
	}

	private void PreParseIteration()
	{
		if (IsCurrentLexemKeyword("using"))
			PreParseUsing();
		else if (IsCurrentLexemKeyword("Namespace"))
			PreParseNamespace();
		else if (IsCurrentLexemKeyword("Class"))
			PreParseClass();
		else if (IsCurrentLexemKeyword("Function"))
			PreParseFunction();
		else if (IsCurrentLexemKeyword("Constructor"))
			PreParseConstructor();
		else if (IsCurrentLexemOther("{"))
		{
			nestedBlocksChain.Push(new(BlockType.Unnamed, "#" + ((nestedBlocksChain.Length == 0) ? globalUnnamedIndex++ : nestedBlocksChain.Peek().UnnamedIndex++).ToString(), 1));
			figureBk++;
			pos++;
		}
		else if (IsCurrentLexemOther("}"))
		{
			if (figureBk == 0)
			{
				GenerateMessage("Wreck", pos, "unpaired closing bracket");
				return;
			}
			else
			{
				nestedBlocksChain.Pop();
				figureBk--;
			}
			pos++;
		}
		else
			pos++;
	}

	private void PreParseUsing()
	{
		if (pos != 0)
		{
			GenerateMessage("Wreck", pos, "using namespace is declared not at the beginning of the code");
			wreckOccurred = true;
			return;
		}
		pos++;
		String name;
		var nameList = new List<String>();
		while (pos < lexems.Length - 1 && lexems[pos].Type == LexemType.Identifier && lexems[pos + 1].Type == LexemType.Operator && lexems[pos + 1].String == ".")
		{
			nameList.Add(lexems[pos].String);
			pos += 2;
		}
		if (IsEnd()) return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			name = String.Join(".", [.. nameList, lexems[pos].String]);
			pos++;
		}
		else
		{
			GenerateMessage("Wreck", pos, "expected: identifier");
			wreckOccurred = true;
			return;
		}
		if (NotImplementedNamespacesList.Contains(name))
		{
			GenerateMessage("Wreck", pos, "namespace \"" + name + "\" is still not implemented, wait for next versions");
			wreckOccurred = true;
			return;
		}
		else if (OutdatedNamespacesList.TryGetValue(name, out var useInstead))
		{
			GenerateMessage("Wreck", pos, "namespace \"" + name + "\" is outdated, consider using " + useInstead + " instead");
			wreckOccurred = true;
			return;
		}
		else if (ReservedNamespacesList.Contains(name))
		{
			GenerateMessage("Wreck", pos, "namespace \"" + name + "\" is reserved for next versions of C#.NStar and cannot be used");
			wreckOccurred = true;
			return;
		}
		else if (!NamespacesList.Contains(name))
		{
			GenerateMessage("Wreck", pos, "\"" + name + "\" is not a valid namespace");
			wreckOccurred = true;
			return;
		}
		else if (!ExplicitlyConnectedNamespacesList.Add(name))
		{
			GenerateMessage("Wreck", pos, "using \"" + name + "\" is already declared");
			wreckOccurred = true;
			return;
		}
		else if (!IsCurrentLexemOther(";"))
		{
			GenerateMessage("Wreck", pos, "expected: \";\"");
			wreckOccurred = true;
			return;
		}
		pos++;
		lexems.Remove(..pos);
		pos = 0;
	}

	private void PreParseNamespace()
	{
		pos++;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		var blockStart = pos - 1;
		var nameList = new List<String>();
		while (pos < lexems.Length - 1 && lexems[pos].Type == LexemType.Identifier && lexems[pos + 1].Type == LexemType.Operator && lexems[pos + 1].String == ".")
		{
			nameList.Add(lexems[pos].String);
			pos += 2;
		}
		if (IsEnd()) return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			name = String.Join(".", [.. nameList, lexems[pos].String]);
			pos++;
		}
		else
		{
			name = "???" + unknownIndex++.ToString();
			GenerateError(pos, "expected: identifier");
		}
		GetFigureBracketAndSetBlock(BlockType.Namespace, name, 0, () =>
		{
			UserDefinedNamespacesList.Add((container.Length != 0
				? ((String)container.ToShortString()).Add('.') : "").AddRange(name));
			blocksToJump.Add((container, "Namespace", name, blockStart, pos));
		});
	}

	private void PreParseClass()
	{
		pos2 = pos;
		var attributes = TypeAttributes.None;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		GetBlockStart();
		var blockStart = pos2;
		attributes = (TypeAttributes)GetAccessMethod((int)attributes);
		attributes |= (TypeAttributes)AddAttribute(new() { { "static", TypeAttributes.Static },
			{ "sealed", TypeAttributes.Sealed }, { "abstract", TypeAttributes.Abstract } });
		attributes |= (TypeAttributes)AddAttribute("partial", TypeAttributes.Partial);
		while (pos2 < pos)
		{
			GenerateError(pos2, "incorrect word or order of words in construction declaration");
			pos2++;
		}
		if (IsEnd()) return;
		pos++;
		if (IsEnd()) return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			var s = lexems[pos].String;
			if (PrimitiveTypesList.ContainsKey(s) || ExtraTypesList.ContainsKey(("", s)))
				ChangeNameAndGenerateError(out name, "class \"" + s
					+ "\" is standard root C#.NStar type and cannot be redefined");
			else if (UserDefinedTypesList.ContainsKey((container, s)))
				ChangeNameAndGenerateError(out name, "class \"" + s + "\" is already defined in this region");
			else
				CheckAdditionalNameConditions(out name, s, "class \"" + s + "\" is nearest to this outer class");
		}
		else
			ChangeNameAndGenerateError(out name, "expected: identifier");
		pos++;
		pos2 = pos;
		if (IsEnd()) return;
		if (IsCurrentLexemOperator(":"))
		{
			if ((attributes & TypeAttributes.Static) == TypeAttributes.Static)
				GenerateError(pos, "a static class cannot be derived");
			pos++;
			var pos3 = pos;
			while (pos < lexems.Length && (lexems[pos].Type == LexemType.Identifier || IsCurrentLexemOperator(".") || IsLexemOther(lexems[pos], ["(", ")", "[", "]", ","])))
				pos++;
			registeredTypes.Add((container, name, pos3, pos));
		}
		GetFigureBracketAndSetBlock(BlockType.Class, name, attributes, () =>
		{
			UserDefinedTypesList.Add((container, name), new([], attributes, NullType, []));
			blocksToJump.Add((container, "Class", name, blockStart, pos2));
		});
	}

	private void PreParseFunction()
	{
		pos2 = pos;
		var attributes = FunctionAttributes.None;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		GetBlockStart();
		attributes = (FunctionAttributes)GetAccessMethod((int)attributes);
		if (IsPos2LexemKeyword("const"))
		{
			attributes |= FunctionAttributes.Const;
			pos2++;
			goto l0;
		}
		if (IsClass())
		{
			var value = (IsPos2LexemKeyword("static") ? 2 : 0) + (IsStatic() ? 1 : 0);
			if (value == 3)
				GenerateMessage("Warning", pos2, "properties and methods are static in the static class implicitly; word \"static\" is not necessary");
			if (value >= 1)
				attributes |= FunctionAttributes.Static;
			if (value >= 2)
				pos2++;
		}
		else
			CheckKeywordAndGenerateError("static", "static functions are allowed only inside classes");
		if (IsPos2LexemKeyword("new"))
			attributes |= (FunctionAttributes)AddAttribute("new", FunctionAttributes.New);
		else if (IsPos2LexemKeyword("sealed"))
			attributes |= (FunctionAttributes)AddAttribute("sealed", FunctionAttributes.Sealed);
		else if (IsPos2LexemKeyword("abstract"))
			attributes |= (FunctionAttributes)AddAttribute("abstract", FunctionAttributes.Abstract);
		attributes |= (FunctionAttributes)AddAttribute("multiconst", FunctionAttributes.Multiconst);
	l0:
		var blockStart = pos2;
		if (IsPos2LexemKeyword("null"))
		{
			pos2++;
			registeredTypes.Add((container, "", pos2 - 1, pos2));
		}
		else
			registeredTypes.Add((container, "", pos2, pos));
		if (IsEnd()) return;
		pos++;
		if (IsEnd()) return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			var s = lexems[pos].String;
			if (PublicFunctionsList.ContainsKey(s))
				ChangeNameAndGenerateError(out name, "function\"" + s + "\" is standard root C#.NStar function and cannot be redefined");
			else if (UserDefinedFunctionsList.TryGetValue(container, out var methods) && methods.ContainsKey(s))
				ChangeNameAndGenerateError(out name, "function\"" + s + "\" is already defined in this region; overloaded functions are under development");
			else
				CheckAdditionalNameConditions(out name, s, "function cannot have same name as its container class");
		}
		else
			ChangeNameAndGenerateError(out name, "expected: identifier");
		var t = registeredTypes[^1];
		t.Name = name;
		registeredTypes[^1] = t;
		pos++;
		if (IsEnd()) return;
		if (IsCurrentLexemOther("("))
		{
			var start = pos;
			pos++;
			CloseBracket(ref pos, ")", ref errorsList);
			if (pos - start > 2)
				parameterLists.Add((container, name, start + 1, pos - 1));
			if (pos >= lexems.Length)
			{
				GenerateUnexpectedEndError();
				return;
			}
			else if (!IsCurrentLexemOther((attributes & FunctionAttributes.New) == FunctionAttributes.Abstract ? ";" : "{"))
				GenerateError(pos, "\";\" must follow the abstract function declaration, \"{\" - the non-abstract one");
		}
		GetFigureBracketAndSetBlock(BlockType.Function, name, attributes, () =>
		{
			UserDefinedFunctionsList.TryAdd(container, []);
			var list = UserDefinedFunctionsList[container];
			list.Add(name, []);
			list[name].Add(([], (new([new(BlockType.Primitive, "???", 1)]), NoGeneralExtraTypes), attributes, []));
			blocksToJump.Add((container, "Function", name, blockStart, pos));
		});
	}

	private void PreParseConstructor()
	{
		if (!IsClass())
		{
			GenerateError(pos, "constructors are allowed only inside classes");
			pos++;
			return;
		}
		pos2 = pos;
		var attributes = ConstructorAttributes.None;
		BlockStack container = new(nestedBlocksChain.ToList());
		GetBlockStart();
		var blockStart = pos2;
		attributes = (ConstructorAttributes)GetAccessMethod((int)attributes);
		var value = (IsPos2LexemKeyword("static") ? 2 : 0) + (IsStatic() ? 1 : 0);
		if (value == 3)
			GenerateMessage("Warning", pos2, "properties and methods are static in the static class implicitly; word \"static\" is not necessary");
		if (value >= 1)
			attributes |= ConstructorAttributes.Static;
		if (value >= 2)
			pos2++;
		attributes |= (ConstructorAttributes)AddAttribute("multiconst", ConstructorAttributes.Multiconst);
		attributes |= (ConstructorAttributes)AddAttribute("abstract", ConstructorAttributes.Abstract);
		while (pos2 < pos)
		{
			GenerateError(pos2, "incorrect word or order of words in construction declaration");
			pos2++;
		}
		if (IsEnd()) return;
		pos++;
		if (IsEnd()) return;
		if (IsCurrentLexemOther("("))
		{
			var start = pos;
			pos++;
			CloseBracket(ref pos, ")", ref errorsList);
			if (pos - start > 2)
				parameterLists.Add((container, "Constructor", start + 1, pos - 1));
			if (pos >= lexems.Length)
			{
				GenerateUnexpectedEndError();
				return;
			}
		}
		GetFigureBracketAndSetBlock(BlockType.Constructor, "", attributes, () =>
		{
			UserDefinedConstructorsList.TryAdd(container, []);
			UserDefinedConstructorsList[container].Add((attributes, []));
			blocksToJump.Add((container, "Constructor", "", blockStart, pos));
			if (UserDefinedConstructorIndexesList.TryGetValue(container, out var list) && actualConstructorIndex.TryGetValue(container, out var index))
			{
				list.Add(blockStart, index);
				actualConstructorIndex[container]++;
			}
			else
			{
				UserDefinedConstructorIndexesList[container] = new() { { blockStart, 0 } };
				actualConstructorIndex[container] = 1;
			}
		});
	}

	private void CheckAdditionalNameConditions(out String name, String s, String error)
	{
		if (nestedBlocksChain.Length >= 1 && nestedBlocksChain.Peek().Name == s)
			ChangeNameAndGenerateError(out name, error);
		else if (IsReservedNamespaceOrType(s, out var errorPrefix))
			ChangeNameAndGenerateError(out name, errorPrefix + "\" is reserved for next versions of C#.NStar and cannot be used");
		else
			name = s;
	}

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errorsList, int end = -1)
	{
		while (pos < ((end == -1) ? lexems.Length : end))
		{
			if (lexems[pos].Type == LexemType.Other)
			{
				var s = lexems[pos].String;
				if (s == bracket)
				{
					pos++;
					return true;
				}
				else if (new List<String> { "(", "[", "{" }.Contains(s))
				{
					pos++;
					CloseBracket(ref pos, (s == "(") ? ")" : (s == "[") ? "]" : "}", ref errorsList);
				}
				else if (new List<String> { ")", "]", "}" }.Contains(s) || bracket != "}" && (s == ";" || s == "\r\n"))
				{
					GenerateError(pos, "unpaired brackets; expected: " + bracket + "");
					return false;
				}
				else
					pos++;
			}
			else
				pos++;
		}
		return false;
	}

	private bool IsEnd()
	{
		if (pos >= lexems.Length)
		{
			GenerateUnexpectedEndError();
			return true;
		}
		return false;
	}

	private void GenerateError(Index pos, String text) => GenerateMessage("Error", pos, text);

	private void GenerateMessage(String typeName, Index pos, String text)
	{
		(errorsList ??= []).Add(typeName + " in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString() + ": " + text);
		if (typeName == "Wreck")
			wreckOccurred = true;
	}

	private void GenerateUnexpectedEndError() => (errorsList ??= []).Add("Error in line " + lexems[pos - 1].LineN.ToString() + " at position " + (lexems[pos - 1].Pos + lexems[pos - 1].String.Length).ToString() + ": unexpected end of code reached");

	public (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred) EmptySyntaxTree() => (lexems, input, TreeBranch.DoNotAdd(), errorsList, true);

	private bool IsClass() => nestedBlocksChain.Length != 0 && nestedBlocksChain.Peek().BlockType == BlockType.Class;

	private bool IsStatic() => (UserDefinedTypesList[SplitType(nestedBlocksChain)].Attributes & TypeAttributes.Static) == TypeAttributes.Static;

	private void GetBlockStart()
	{
		while (pos2 > 0)
		{
			pos2--;
			if (lexems[pos2].Type == LexemType.Other && StopLexemsList.Contains(lexems[pos2].String))
			{
				pos2++;
				break;
			}
		}
	}

	private int GetAccessMethod(int attributes)
	{
		if (IsLexemKeyword(lexems[pos2], ["closed", "protected"]))
		{
			if (nestedBlocksChain.Length != 0 && nestedBlocksChain.Peek().BlockType == BlockType.Class)
				attributes |= (lexems[pos2].String == "closed") ? 2 : 4;
			else
				GenerateError(pos2, "closed and protected classes are allowed only inside other classes");
			pos2++;
		}
		else if (IsPos2LexemKeyword("internal"))
		{
			attributes |= 6;
			pos2++;
		}
		else
			CheckKeywordAndGenerateError("public", "public access is under development");
		return attributes;
	}

	private void CheckKeywordAndGenerateError(String string_, String error)
	{
		if (IsPos2LexemKeyword(string_))
		{
			GenerateError(pos2, error);
			pos2++;
		}
	}

	private dynamic AddAttribute(Dictionary<String, dynamic> list)
	{
		if (lexems[pos2].Type == LexemType.Keyword && list.TryGetValue(lexems[pos2].String, out var value))
		{
			pos2++;
			return value;
		}
		return 0;
	}

	private dynamic AddAttribute(String string_, dynamic value)
	{
		if (lexems[pos2].String == string_)
		{
			pos2++;
			return value;
		}
		return 0;
	}

	private void GetFigureBracketAndSetBlock(BlockType blockType, String name, dynamic attributes, Action action)
	{
		while (IsLexemOtherNoEnd("\r\n"))
			pos++;
		if (IsEnd()) return;
		if (blockType != BlockType.Function && !IsCurrentLexemOther("{"))
		{
			GenerateError(pos, "expected: {");
			while (pos < lexems.Length && !(lexems[pos].Type == LexemType.Other && (lexems[pos].String == "{" || lexems[pos].String == "\r\n")))
				pos++;
		}
		if (IsEnd()) return;
		if (!(blockType == BlockType.Function && (attributes & FunctionAttributes.New) == FunctionAttributes.Abstract))
		{
			nestedBlocksChain.Push(new(blockType, name, 1));
			figureBk++;
		}
		action();
		pos++;
	}

	private void ChangeNameAndGenerateError(out String name, String error)
	{
		name = "???" + unknownIndex++.ToString();
		GenerateError(pos, error);
	}

	private static bool IsReservedNamespaceOrType(String s, out String errorPrefix)
	{
		if (IsNotImplementedNamespace(s) || IsReservedNamespace(s) || IsNotImplementedType("", s) || IsReservedType("", s) || IsNotImplementedMember(new(), s) || IsReservedMember(new(), s))
		{
			errorPrefix = "identifier \"" + s;
			return true;
		}
		else if (IsNotImplementedEndOfIdentifier(s, out var s2) || IsReservedEndOfIdentifier(s, out s2))
		{
			errorPrefix = "end of identifier \"" + s2;
			return true;
		}
		errorPrefix = [];
		return false;
	}

	public static bool IsLexemKeyword(Lexem lexem, String @string) => lexem.Type == LexemType.Keyword && lexem.String == @string;

	public static bool IsLexemOperator(Lexem lexem, String @string) => lexem.Type == LexemType.Operator && lexem.String == @string;

	public static bool IsLexemOther(Lexem lexem, String @string) => lexem.Type == LexemType.Other && lexem.String == @string;

	public static bool IsLexemKeyword(Lexem lexem, List<String> strings) => lexem.Type == LexemType.Keyword && strings.Contains(lexem.String);

	public static bool IsLexemOperator(Lexem lexem, List<String> strings) => lexem.Type == LexemType.Operator && strings.Contains(lexem.String);

	public static bool IsLexemOther(Lexem lexem, List<String> strings) => lexem.Type == LexemType.Other && strings.Contains(lexem.String);

	public bool IsLexemKeywordNoEnd(String @string) => pos < lexems.Length && IsLexemKeyword(lexems[pos], @string);

	public bool IsLexemOperatorNoEnd(String @string) => pos < lexems.Length && IsLexemOperator(lexems[pos], @string);

	public bool IsLexemOtherNoEnd(String @string) => pos < lexems.Length && IsLexemOther(lexems[pos], @string);

	public bool IsCurrentLexemKeyword(String @string) => IsLexemKeyword(lexems[pos], @string);

	public bool IsCurrentLexemOperator(String @string) => IsLexemOperator(lexems[pos], @string);

	public bool IsCurrentLexemOther(String @string) => IsLexemOther(lexems[pos], @string);

	public bool ValidateKeywordLexem(String @string)
	{
		if (IsCurrentLexemKeyword(@string))
		{
			pos++;
			return true;
		}
		return false;
	}

	public bool ValidateOperatorLexem(String @string)
	{
		if (IsCurrentLexemOperator(@string))
		{
			pos++;
			return true;
		}
		return false;
	}

	public bool ValidateOtherLexem(String @string)
	{
		if (IsCurrentLexemOther(@string))
		{
			pos++;
			return true;
		}
		return false;
	}

	private bool IsPos2LexemKeyword(String string_) => IsLexemKeyword(lexems[pos2], string_);

	private protected static bool IsValidBaseClass(TypeAttributes attributes)
		=> (attributes & (TypeAttributes.Sealed | TypeAttributes.Abstract
		| TypeAttributes.Static | TypeAttributes.Struct | TypeAttributes.Enum
		| TypeAttributes.Delegate)) is TypeAttributes.None or TypeAttributes.Abstract;

	public static implicit operator LexemStream((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred) x) => new(x.Lexems, x.String, x.ErrorsList, x.WreckOccurred);

	public static implicit operator LexemStream(((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred) Main, BlockStack? RootContainer) x) => new(x.Main.Lexems, x.Main.String, x.Main.ErrorsList, x.Main.WreckOccurred, x.RootContainer);

	public static implicit operator LexemStream(CodeSample x) => ((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred))x;

	public static implicit operator (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred)(LexemStream x) => x.Parse();
}

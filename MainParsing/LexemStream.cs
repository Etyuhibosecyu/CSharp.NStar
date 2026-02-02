global using NStar.Core;
global using NStar.Dictionaries;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Collections.Immutable;
global using static CSharp.NStar.BuiltInMemberCollections;
global using static CSharp.NStar.MemberChecks;
global using static CSharp.NStar.NStarType;
global using static CSharp.NStar.TypeChecks;
global using static CSharp.NStar.TypeConverters;
global using static NStar.Core.Extents;
global using static System.Math;
global using G = System.Collections.Generic;
global using String = NStar.Core.String;
using System.IO;

namespace CSharp.NStar;

public class LexemStream
{
	private protected readonly List<Lexem> lexems;
	private protected readonly String input;
	private protected List<String>? errors;
	private readonly BlockStack? rootContainer;
	private protected bool wreckOccurred;
	private protected int pos;
	private int prevPos;
	private readonly Stack<Block> nestedBlocksChain = new();
	private int globalUnnamedIndex = 1;
	private protected readonly BlocksToJump blocksToJump = [];
	private protected readonly LexemGroup registeredTypes = [];
	private protected readonly LexemGroup parameterLists = [];
	private protected readonly TypeDictionary<Dictionary<String, int>> actualFunctionIndexes = [];
	private readonly TypeDictionary<int> actualConstructorIndexes = [];
	private int unknownIndex = 1;
	private int figureBk;

	private protected static readonly ImmutableArray<string> ClassStartLexemsList = ["\r\n", ";", "(", "{", "}"];
	private protected static readonly ImmutableArray<string> StopLexemsList = ["\r\n", ";", "{", "}"];

	private protected LexemStream(List<Lexem> lexems, String input, List<String>? errors,
		bool wreckOccurred, BlockStack? rootContainer = null)
	{
		this.lexems = lexems;
		this.input = input;
		this.errors = errors;
		this.rootContainer = rootContainer;
		this.wreckOccurred = wreckOccurred;
		pos = 0;
		prevPos = 0;
	}

	private protected LexemStream(LexemStream lexemStream) : this(lexemStream.lexems, lexemStream.input,
		lexemStream.errors, lexemStream.wreckOccurred, lexemStream.rootContainer)
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
				GenerateMessage(0x9007, pos - 1);
				return;
			}
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			var targetPos = (pos >= lexems.Length) ? pos - 1 : pos;
			var errorMessage = GetWreckPosPrefix(0xF000, targetPos)
				+ ": compilation failed because of internal compiler error\r\n";
			(errors ??= []).Add(errorMessage + @" (see %TEMP%\CSharp.NStar.log for details)");
			File.WriteAllLines((Environment.GetEnvironmentVariable("TEMP") ?? throw new InvalidOperationException())
				+ @"\CSharp.NStar.log", [errorMessage, "The internal exception was:", ex.GetType().Name,
					"The internal exception message was:", ex.Message,
					"The underlying internal exception was:", ex.InnerException?.GetType().Name ?? "null",
					"The underlying internal exception message was:", ex.InnerException?.Message ?? "null"]);
			wreckOccurred = true;
			return;
		}
	}

	private String GetWreckPosPrefix(ushort code, Index pos) =>
		"Technical wreck " + Convert.ToString(code, 16).ToUpper().PadLeft(4, '0')
		+ "in line " + lexems[pos].LineN.ToString() + " at position " + lexems[pos].Pos.ToString();

	private void PreParseIteration()
	{
		if (IsCurrentLexemKeyword("using"))
			Using();
		else if (IsCurrentLexemKeyword(nameof(Namespace)))
			Namespace();
		else if (IsLexemKeyword(lexems[pos], [nameof(Class), "Megaclass"]))
			Class();
		else if (IsCurrentLexemKeyword(nameof(Function)))
			Function();
		else if (IsCurrentLexemKeyword(nameof(Constructor)))
			Constructor();
		else if (IsCurrentLexemOther("{"))
		{
			BlockStack container = new(nestedBlocksChain);
			var unnamedIndex = (nestedBlocksChain.Length == 0) ? globalUnnamedIndex++ : nestedBlocksChain.Peek().UnnamedIndex++;
			if (UserDefinedTypes.TryGetValue((container, "#RoundBracket#"), out var userDefinedType)
				&& UnnamedTypeStartIndexes.TryGetValue(container, out var containerStartIndexes)
				&& containerStartIndexes.Find(x => int.TryParse(x[1..].ToString(), out var otherUnnamedIndex)
				&& otherUnnamedIndex == unnamedIndex) is var startIndex && startIndex != null)
			{
				UserDefinedTypes.TryAdd((new BlockStack(nestedBlocksChain), startIndex), userDefinedType);
				nestedBlocksChain.Push(new(BlockType.Class, startIndex, 1));
			}
			else
				nestedBlocksChain.Push(new(BlockType.Unnamed, "#" + unnamedIndex, 1));
			figureBk++;
			pos++;
		}
		else if (IsCurrentLexemOther("}"))
		{
			if (figureBk == 0)
			{
				GenerateMessage(0x9008, pos);
				return;
			}
			else
			{
				nestedBlocksChain.Pop();
				UserDefinedTypes.Remove((new BlockStack(nestedBlocksChain), "#RoundBracket#"));
				figureBk--;
			}
			pos++;
		}
		else
			pos++;
	}

	private void Using()
	{
		if (pos != 0)
		{
			GenerateMessage(0x9009, pos);
			return;
		}
		pos++;
		String name;
		var names = new List<String>();
		while (pos < lexems.Length - 1 && lexems[pos].Type == LexemType.Identifier
			&& lexems[pos + 1].Type == LexemType.Operator && lexems[pos + 1].String == ".")
		{
			names.Add(lexems[pos].String);
			pos += 2;
		}
		if (IsEnd()) return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			name = String.Join(".", [.. names, lexems[pos].String]);
			pos++;
		}
		else
		{
			GenerateMessage(0x900A, pos);
			return;
		}
		if (NotImplementedNamespaces.Contains(name))
		{
			GenerateMessage(0x900B, pos, name);
			return;
		}
		else if (OutdatedNamespaces.TryGetValue(name, out var useInstead))
		{
			GenerateMessage(0x900C, pos, name, useInstead);
			return;
		}
		else if (ReservedNamespaces.Contains(name))
		{
			GenerateMessage(0x900D, pos, name);
			return;
		}
		else if (!Namespaces.Contains(name))
		{
			GenerateMessage(0x900E, pos, name);
			return;
		}
		else if (!ExplicitlyConnectedNamespaces.TryAdd(name))
		{
			GenerateMessage(0x900F, pos, name);
			return;
		}
		else if (!IsCurrentLexemOther(";"))
		{
			GenerateMessage(0x9010, pos);
			return;
		}
		pos++;
		lexems.Remove(..pos);
		pos = 0;
	}

	private void Namespace()
	{
		pos++;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		var blockStart = pos - 1;
		var names = new List<String>();
		while (pos < lexems.Length - 1 && lexems[pos].Type == LexemType.Identifier
			&& lexems[pos + 1].Type == LexemType.Operator && lexems[pos + 1].String == ".")
		{
			ValidateOpenName();
			names.Add(lexems[pos].String);
			pos += 2;
		}
		if (IsEnd())
			return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			ValidateOpenName();
			name = String.Join(".", names.Add(lexems[pos].String));
			pos++;
		}
		else
		{
			name = "???" + unknownIndex++.ToString();
			GenerateMessage(0x0004, pos);
		}
		GetFigureBracketAndSetBlock(BlockType.Namespace, name, 0, () =>
		{
			UserDefinedNamespaces.Add((container.Length != 0
				? ((String)container.ToString()).Add('.') : "").AddRange(name));
			blocksToJump.Add((container, "Namespace", name, blockStart, pos));
		});
	}

	private void Class()
	{
		prevPos = pos;
		var attributes = TypeAttributes.None;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		var classKeywordPos = pos;
		GetClassStart(out var roundBracketAtStart);
		var blockStart = prevPos;
		attributes = (TypeAttributes)GetAccessMethod((int)attributes);
		if (IsLexemKeyword(lexems[classKeywordPos], "Megaclass"))
		{
			attributes |= TypeAttributes.Static;
			if (lexems[prevPos].String == "static")
			{
				GenerateMessage(0x8012, prevPos);
				prevPos++;
			}
			else if (lexems[prevPos].String.AsSpan() is "abstract" or "sealed")
			{
				GenerateMessage(0x0012, prevPos);
				prevPos++;
			}
		}
		else
			attributes |= (TypeAttributes)AddOneOfAttributes(new() { { "static", TypeAttributes.Static },
				{ "sealed", TypeAttributes.Sealed }, { "abstract", TypeAttributes.Abstract } });
		attributes |= (TypeAttributes)AddAttribute("partial", TypeAttributes.Partial);
		while (prevPos < pos)
		{
			GenerateMessage(0x0005, prevPos, "incorrect word or order of words in construction declaration");
			prevPos++;
		}
		if (IsEnd())
			return;
		pos++;
		if (IsEnd())
			return;
		if (roundBracketAtStart)
		{
			name = "#RoundBracket#";
			prevPos = pos;
			if (IsCurrentLexemOperator(":"))
			{
				if ((attributes & TypeAttributes.Static) == TypeAttributes.Static)
					GenerateMessage(0x0009, pos);
				pos++;
				var pos3 = pos;
				CloseBracket(ref pos, ")", ref errors);
				registeredTypes.Add((container, name, pos3, --pos));
			}
			var unnamedIndex = (nestedBlocksChain.Length == 0) ? globalUnnamedIndex : nestedBlocksChain.Peek().UnnamedIndex;
			UserDefinedTypes.TryAdd((container, name), new([], attributes, NullType, []));
			var savedContainer = container;
			SubscribeToChanges(name, savedContainer);
			if (!UnnamedTypeStartIndexes.TryGetValue(container, out var containerStartIndexes))
				UnnamedTypeStartIndexes.Add(container, containerStartIndexes = []);
			containerStartIndexes.Add("#" + unnamedIndex);
			blocksToJump.Add((container, nameof(Class), name, blockStart, prevPos));
			return;
		}
		else if (lexems[pos].Type == LexemType.Identifier)
		{
			ValidateOpenName();
			if (container.Length >= 2 && container.Peek().BlockType is BlockType.Class or BlockType.Struct
				or BlockType.Interface or BlockType.Delegate
				&& container.SkipLast(1)[^1].BlockType is BlockType.Class or BlockType.Struct
				or BlockType.Interface or BlockType.Delegate)
				GenerateMessage(0x801C, pos);
			var s = lexems[pos].String;
			if (PrimitiveTypes.ContainsKey(s) || ExtraTypes.ContainsKey(("", s)))
				ChangeNameAndGenerateError(0x0006, out name, s);
			else if (UserDefinedTypes.ContainsKey((container, s)))
				ChangeNameAndGenerateError(0x0007, out name, s);
			else
				CheckAdditionalNameConditions(0x0008, out name, s, s);
		}
		else
			ChangeNameAndGenerateError(0x0004, out name);
		pos++;
		prevPos = pos;
		if (IsEnd())
			return;
		if (IsCurrentLexemOperator(":"))
		{
			if ((attributes & TypeAttributes.Static) == TypeAttributes.Static)
				GenerateMessage(0x0009, pos);
			pos++;
			var pos3 = pos;
			while (pos < lexems.Length && (lexems[pos].Type == LexemType.Identifier
				|| IsLexemOperator(lexems[pos], [".", ","]) || IsLexemOther(lexems[pos], ["(", ")", "[", "]"])))
				pos++;
			registeredTypes.Add((container, name, pos3, pos));
		}
		GetFigureBracketAndSetBlock(BlockType.Class, name, attributes, () =>
		{
			UserDefinedTypes.Add((container, name), new([], attributes, NullType, []));
			blocksToJump.Add((container, nameof(Class), name, blockStart, prevPos));
		});
	}

	protected static void SubscribeToChanges(String name, BlockStack container) => name.ListChanged += s =>
	{
		UserDefinedTypes[(container, s)] = UserDefinedTypes
			.Find(x => x.Key.Container.Equals(container) && x.Key.Type == s).Value;
		var nestedTypes = UserDefinedTypes.FindAll(x => x.Key.Container.Any(y => y.Name == s));
		foreach (var x in nestedTypes)
			UserDefinedTypes[x.Key] = x.Value;
		var properties = UserDefinedProperties.FindAll(x => x.Key.Any(y => y.Name == s));
		foreach (var x in properties)
			UserDefinedProperties[x.Key] = x.Value;
		var functions = UserDefinedFunctions.FindAll(x => x.Key.Any(y => y.Name == s));
		foreach (var x in functions)
			UserDefinedFunctions[x.Key] = x.Value;
		var constructors = UserDefinedConstructors.FindAll(x => x.Key.Any(y => y.Name == s));
		foreach (var x in constructors)
			UserDefinedConstructors[x.Key] = x.Value;
	};
	private void Function()
	{
		prevPos = pos;
		var attributes = FunctionAttributes.None;
		String name;
		BlockStack container = new(nestedBlocksChain.ToList());
		GetBlockStart();
		attributes = (FunctionAttributes)GetAccessMethod((int)attributes);
		if (IsPos2LexemKeyword("const"))
		{
			attributes |= FunctionAttributes.Const;
			prevPos++;
		}
		else
		{
			if (IsClass())
			{
				var mask = (IsPos2LexemKeyword("static") ? 2 : 0) + (IsStatic() ? 1 : 0);
				if (mask == 3)
					GenerateMessage(0x8000, prevPos);
				if (mask >= 1)
					attributes |= FunctionAttributes.Static;
				if (mask >= 2)
					prevPos++;
			}
			else
				CheckKeywordAndGenerateError(0x000A, "static", "static functions are allowed only inside classes");
			if (IsPos2LexemKeyword("new"))
				attributes |= (FunctionAttributes)AddAttribute("new", FunctionAttributes.New);
			else if (IsPos2LexemKeyword("sealed"))
				attributes |= (FunctionAttributes)AddAttribute("sealed", FunctionAttributes.Sealed);
			else if (IsPos2LexemKeyword("abstract"))
				attributes |= (FunctionAttributes)AddAttribute("abstract", FunctionAttributes.Abstract);
			attributes |= (FunctionAttributes)AddAttribute("multiconst", FunctionAttributes.Multiconst);
		}
		var blockStart = prevPos;
		if (IsPos2LexemKeyword("null"))
		{
			prevPos++;
			registeredTypes.Add((container, "", prevPos - 1, prevPos));
		}
		else
			registeredTypes.Add((container, "", prevPos, pos));
		if (IsEnd())
			return;
		pos++;
		if (IsEnd())
			return;
		if (lexems[pos].Type == LexemType.Identifier)
		{
			ValidateOpenName();
			var s = lexems[pos].String;
			if (PublicFunctions.ContainsKey(s))
				ChangeNameAndGenerateError(0x000B, out name, s);
			else
				CheckAdditionalNameConditions(0x000D, out name, s);
		}
		else
			ChangeNameAndGenerateError(0x0004, out name);
		var t = registeredTypes[^1];
		t.Name = name;
		registeredTypes[^1] = t;
		pos++;
		if (IsEnd())
			return;
		if (IsCurrentLexemOther("("))
		{
			var start = pos;
			pos++;
			CloseBracket(ref pos, ")", ref errors);
			if (pos - start > 2)
				parameterLists.Add((container, name, start + 1, pos - 1));
			if (pos >= lexems.Length)
			{
				GenerateUnexpectedEndError();
				return;
			}
			else if (!IsCurrentLexemOther((attributes & FunctionAttributes.New) == FunctionAttributes.Abstract ? ";" : "{"))
				GenerateMessage(0x000E, pos);
		}
		GetFigureBracketAndSetBlock(BlockType.Function, name, attributes, () =>
		{
			UserDefinedFunctions.TryAdd(container, []);
			var containerFunctions = UserDefinedFunctions[container];
			if (!containerFunctions.TryGetValue(name, out var nameFunctions))
				containerFunctions.Add(name, nameFunctions = []);
			if (containerFunctions.Sum(x => x.Value.Length) == CodeStyleRules.MaxFunctionsInClass
				&& CreateVar(blocksToJump.FindLastIndex(x => container.TryPeek(out var block)
				&& RedStarLinq.Equals(x.Container, container.SkipLast(1)) && x.Name == block.Name), out var foundIndex) >= 0
				&& lexems[blocksToJump[foundIndex].End - 2].String == nameof(Class))
				GenerateMessage(0x8013, blocksToJump[foundIndex].End - 1, CodeStyleRules.MaxFunctionsInClass);
			nameFunctions.Add(new(container.TryPeek(out var block) && block.BlockType is BlockType.Class
				or BlockType.Struct or BlockType.Interface or BlockType.Delegate
				? name : RandomVarName().ToNString(), [],
				(new([new(BlockType.Primitive, "???", 1)]), NoBranches), attributes, []));
			blocksToJump.Add((container, nameof(Function), name, blockStart, pos));
			if (!(UserDefinedFunctionIndexes.TryGetValue(container, out var containerIndexes)
				&& actualFunctionIndexes.TryGetValue(container, out var dic)))
			{
				UserDefinedFunctionIndexes[container] = new() { { blockStart, 0 } };
				actualFunctionIndexes[container] = new() { { name, 1 } };
			}
			else if (!dic.TryGetValue(name, out var index))
			{
				containerIndexes.Add(blockStart, 0);
				dic.Add(name, 1);
			}
			else
			{
				containerIndexes.Add(blockStart, index);
				dic[name]++;
			}
		});
	}

	private void Constructor()
	{
		if (!IsClass())
		{
			GenerateMessage(0x000F, pos);
			pos++;
			return;
		}
		prevPos = pos;
		var attributes = ConstructorAttributes.None;
		BlockStack container = new(nestedBlocksChain.ToList());
		GetBlockStart();
		var blockStart = prevPos;
		attributes = (ConstructorAttributes)GetAccessMethod((int)attributes);
		var mask = (IsPos2LexemKeyword("static") ? 2 : 0) + (IsStatic() ? 1 : 0);
		if (mask == 3)
			GenerateMessage(0x8000, prevPos);
		if (mask >= 1)
			attributes |= ConstructorAttributes.Static;
		if (mask >= 2)
			prevPos++;
		attributes |= (ConstructorAttributes)AddAttribute("multiconst", ConstructorAttributes.Multiconst);
		attributes |= (ConstructorAttributes)AddAttribute("abstract", ConstructorAttributes.Abstract);
		while (prevPos < pos)
		{
			GenerateMessage(0x0005, prevPos);
			prevPos++;
		}
		if (IsEnd())
			return;
		pos++;
		if (IsEnd())
			return;
		if (IsCurrentLexemOther("("))
		{
			var start = pos;
			pos++;
			CloseBracket(ref pos, ")", ref errors);
			if (pos - start > 2)
				parameterLists.Add((container, nameof(Constructor), start + 1, pos - 1));
			if (pos >= lexems.Length)
			{
				GenerateUnexpectedEndError();
				return;
			}
		}
		GetFigureBracketAndSetBlock(BlockType.Constructor, "", attributes, () =>
		{
			UserDefinedConstructors.TryAdd(container, []);
			UserDefinedConstructors[container].Add((attributes, [], [-1]));
			blocksToJump.Add((container, nameof(Constructor), "", blockStart, pos));
			if (UserDefinedConstructorIndexes.TryGetValue(container, out var containerConstructorIndexes)
				&& actualConstructorIndexes.TryGetValue(container, out var index))
			{
				containerConstructorIndexes.Add(blockStart, index);
				actualConstructorIndexes[container]++;
			}
			else
			{
				UserDefinedConstructorIndexes[container] = new() { { blockStart, 0 } };
				actualConstructorIndexes[container] = 1;
			}
		});
	}

	private void CheckAdditionalNameConditions(ushort code, out String name, String s, params dynamic[] parameters)
	{
		if (nestedBlocksChain.Length >= 1 && nestedBlocksChain.Peek().BlockType is BlockType.Class or BlockType.Struct
			or BlockType.Interface or BlockType.Delegate && nestedBlocksChain.Peek().Name == s)
			ChangeNameAndGenerateError(code, out name, parameters);
		else if (IsReservedNamespaceOrType(s, out var errorPrefix))
			ChangeNameAndGenerateError(0x0010, out name, errorPrefix);
		else
			name = s;
	}

	private bool CloseBracket(ref int pos, String bracket, ref List<String>? errors, int end = -1)
	{
		while (pos < ((end == -1) ? lexems.Length : end))
		{
			if (lexems[pos].Type != LexemType.Other)
			{
				pos++;
				continue;
			}
			var s = lexems[pos].String;
			if (s == bracket)
			{
				pos++;
				return true;
			}
			else if (s.Length == 1 && s[0] is '(' or '[' or '{')
			{
				pos++;
				CloseBracket(ref pos, (s == "(") ? ")" : (s == "[") ? "]" : "}", ref errors);
			}
			else if (s.Length == 1 && s[0] is ')' or ']' or '}' || bracket != "}" && (s == ";" || s == "\r\n"))
			{
				GenerateMessage(0x9011, pos, bracket);
				return false;
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

	private protected void ValidateOpenName()
	{
		if (CodeStyleRules.TestEnvironment)
			return;
		if (lexems[pos].String.Length == 1)
			GenerateMessage(0x8018, pos, false);
		else if (lexems[pos].String.ToHashSet().ExceptWith("0123456789_").Length == 1)
			GenerateMessage(0x801A, pos, false);
		if (char.IsLower(lexems[pos].String[0]))
			GenerateMessage(0x801B, pos, false);
	}

	private void GenerateMessage(ushort code, Index pos, params dynamic[] parameters)
	{
		Messages.GenerateMessage(ref errors, code, lexems[pos].LineN, lexems[pos].Pos, parameters);
		if (code >> 12 == 0x9)
			wreckOccurred = true;
	}

	private void GenerateUnexpectedEndError() => Messages.GenerateMessage(ref errors, 0x0000,
		lexems[pos - 1].LineN, lexems[pos - 1].Pos + lexems[pos - 1].String.Length);

	public (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList,
		bool WreckOccurred) EmptySyntaxTree() => (lexems, input, TreeBranch.DoNotAdd(), errors, true);

	private bool IsClass() => nestedBlocksChain.Length != 0 && nestedBlocksChain.TryPeek(out var block)
		&& (block.BlockType == BlockType.Class || block.BlockType == BlockType.Unnamed
		&& UserDefinedTypes.ContainsKey((new(nestedBlocksChain.SkipLast(1)), "#RoundBracket#")));

	private bool IsStatic() =>
		(UserDefinedTypes[SplitType(nestedBlocksChain)].Attributes & TypeAttributes.Static) == TypeAttributes.Static;

	private void GetBlockStart()
	{
		while (prevPos > 0)
		{
			prevPos--;
			if (lexems[prevPos].Type == LexemType.Other && StopLexemsList.Contains(lexems[prevPos].String.ToString()))
			{
				prevPos++;
				break;
			}
		}
	}

	private void GetClassStart(out bool roundBracket)
	{
		while (prevPos > 0)
		{
			prevPos--;
			if (lexems[prevPos].Type == LexemType.Other && ClassStartLexemsList.Contains(lexems[prevPos].String.ToString()))
			{
				roundBracket = lexems[prevPos].String == "(";
				prevPos++;
				return;
			}
		}
		roundBracket = false;
	}

	private int GetAccessMethod(int attributes)
	{
		if (IsLexemKeyword(lexems[prevPos], ["private", "protected"]))
		{
			if (nestedBlocksChain.Length != 0 && nestedBlocksChain.Peek().BlockType == BlockType.Class)
				attributes |= (lexems[prevPos].String == "private") ? 2 : 4;
			else
				GenerateMessage(0x0014, prevPos, "private and protected classes are allowed only inside other classes");
			prevPos++;
		}
		else if (IsPos2LexemKeyword("internal"))
		{
			attributes |= 6;
			prevPos++;
		}
		else
			CheckKeywordAndGenerateError(0x0015, "public", "public access is under development");
		return attributes;
	}

	private void CheckKeywordAndGenerateError(ushort code, String string_, String error)
	{
		if (IsPos2LexemKeyword(string_))
		{
			GenerateMessage(code, prevPos, error);
			prevPos++;
		}
	}

	private dynamic AddOneOfAttributes(Dictionary<String, dynamic> attributes)
	{
		if (lexems[prevPos].Type == LexemType.Keyword && attributes.TryGetValue(lexems[prevPos].String, out var value))
		{
			prevPos++;
			return value;
		}
		return 0;
	}

	private dynamic AddAttribute(String string_, dynamic mask)
	{
		if (lexems[prevPos].String == string_)
		{
			prevPos++;
			return mask;
		}
		return 0;
	}

	private void GetFigureBracketAndSetBlock(BlockType blockType, String name, dynamic attributes, Action action)
	{
		while (IsLexemOtherNoEnd("\r\n"))
			pos++;
		if (IsEnd())
			return;
		if (blockType != BlockType.Function && !IsCurrentLexemOther("{"))
		{
			GenerateMessage(0x0011, pos);
			while (pos < lexems.Length && !(lexems[pos].Type == LexemType.Other
				&& (lexems[pos].String == "{" || lexems[pos].String == "\r\n")))
				pos++;
		}
		if (IsEnd())
			return;
		if (!(blockType == BlockType.Function && (attributes & FunctionAttributes.New) == FunctionAttributes.Abstract))
		{
			nestedBlocksChain.Push(new(blockType, name, 1));
			figureBk++;
		}
		action();
		pos++;
	}

	private void ChangeNameAndGenerateError(ushort code, out String name, params dynamic[] parameters)
	{
		name = "???" + unknownIndex++.ToString();
		GenerateMessage(code, pos, parameters);
	}

	private static bool IsReservedNamespaceOrType(String s, out String errorPrefix)
	{
		if (IsNotImplementedNamespace(s) || IsReservedNamespace(s) || IsNotImplementedType("", s) || IsReservedType("", s)
			|| IsNotImplementedMember(new(), s) || IsReservedMember(new(), s))
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

	public static bool IsLexemKeyword(Lexem lexem, String @string) =>
		lexem.Type == LexemType.Keyword && lexem.String == @string;

	public static bool IsLexemOperator(Lexem lexem, String @string) =>
		lexem.Type == LexemType.Operator && lexem.String == @string;

	public static bool IsLexemOther(Lexem lexem, String @string) => lexem.Type == LexemType.Other && lexem.String == @string;

	public static bool IsLexemKeyword(Lexem lexem, G.IEnumerable<string> strings) =>
		lexem.Type == LexemType.Keyword && strings.Contains(lexem.String.ToString());

	public static bool IsLexemOperator(Lexem lexem, G.IEnumerable<string> strings) =>
		lexem.Type == LexemType.Operator && strings.Contains(lexem.String.ToString());

	public static bool IsLexemOther(Lexem lexem, G.IEnumerable<string> strings) =>
		lexem.Type == LexemType.Other && strings.Contains(lexem.String.ToString());

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

	private bool IsPos2LexemKeyword(String string_) => IsLexemKeyword(lexems[prevPos], string_);

	private protected static bool IsValidBaseClass(TypeAttributes attributes)
		=> (attributes & (TypeAttributes.Sealed | TypeAttributes.Abstract
		| TypeAttributes.Static | TypeAttributes.Struct | TypeAttributes.Enum
		| TypeAttributes.Delegate)) is TypeAttributes.None or TypeAttributes.Abstract;

	public static implicit operator LexemStream((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred) x) => new(x.Lexems, x.String, x.ErrorsList, x.WreckOccurred);

	public static implicit operator LexemStream(((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred) Main, BlockStack? RootContainer) x) => new(x.Main.Lexems, x.Main.String, x.Main.ErrorsList, x.Main.WreckOccurred, x.RootContainer);

	public static implicit operator LexemStream(CodeSample x) => ((List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred))x;

	public static implicit operator (List<Lexem> Lexems, String String, TreeBranch TopBranch, List<String>? ErrorsList, bool WreckOccurred)(LexemStream x) => x.Parse();
}

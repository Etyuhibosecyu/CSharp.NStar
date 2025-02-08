global using Corlib.NStar;
global using System;
global using System.Drawing;
global using System.IO;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using G = System.Collections.Generic;
global using static Corlib.NStar.Extents;
global using static CSharp.NStar.Constructions;
global using static CSharp.NStar.Executions;
global using static System.Math;
global using String = Corlib.NStar.String;
using System.Diagnostics;

namespace CSharp.NStar;

public enum LexemType
{
	Int,
	LongInt,
	Real,
	Identifier,
	Keyword,
	Operator,
	String,
	Other,
};

[DebuggerDisplay("{ToString()}")]
public sealed class Lexem(String newString, LexemType newType, int newLineN, int newPos)
{
	public String String { get; set; } = newString;
	public LexemType Type { get; set; } = newType;
	public int LineN { get; set; } = newLineN;
	public int Pos { get; set; } = newPos;

	public Lexem() : this("", LexemType.Other, 1, 0)
	{
	}

	public override string ToString() => $"{Type}: {String}";
}

public class CodeSample(String newString)
{
	private readonly List<Lexem> lexems = [];
	private List<Lexem> tempLexems = [];
	private bool success;
	private readonly String input = newString == "" ? "return null;" : newString;
	private int pos = 0, lineStart = 0;
	private int lineN = 1;
	private bool wreckOccurred = false;
	private readonly List<String> errorsList = [];
	private readonly List<LexemTree> lexemTree = [DoubleEqualLexemTree('^'), DoubleEqualLexemTree('|'), DoubleEqualLexemTree('&'), TripleEqualLexemTree('>'), TripleEqualLexemTree('<'), DoubleEqualLexemTree('!'), new LexemTree('?', [new LexemTree('!', ['='], allowNone: false), EqualLexemTree('>'), EqualLexemTree('<'), '=', '?', '.', '[']), ',', ':', '@', '#', '$', '~', DoubleEqualLexemTree('+'), DoubleEqualLexemTree('-'), EqualLexemTree('*'), EqualLexemTree('/'), EqualLexemTree('%'), new LexemTree('=', ['=', '>']), TripleLexemTree('.')];

	private static LexemTree EqualLexemTree(char c) => new(c, ['=']);

	private static LexemTree DoubleEqualLexemTree(char c) => new(c, [c, '=']);

	private static List<LexemTree> DoubleEqualLexemTreeList(char c) => [DoubleEqualLexemTree(c)];

	private static List<LexemTree> DoubleLexemTreeList(char c) => [new(c, [c])];

	private static LexemTree TripleLexemTree(char c) => new(c, DoubleLexemTreeList(c));

	private static LexemTree TripleEqualLexemTree(char c) => new(c, DoubleEqualLexemTreeList(c).Append('='), true);

	private bool IsNotEnd() => pos < input.Length;

	private bool CheckChar(char tc) => IsNotEnd() && input[pos] == tc;

	private bool ValidateCondition(bool condition)
	{
		if (condition)
		{
			pos++;
			return true;
		}
		else
			return false;
	}

	private bool ValidateChar(char tc) => ValidateCondition(IsNotEnd() && input[pos] == tc);

	private bool ValidateCharList(String tcl) => ValidateCondition(IsNotEnd() && tcl.Contains(input[pos]));

	private String FromStart(int start) => input[start..pos];

	private bool CheckRange(char start, char end) => input[pos] >= start && input[pos] <= end;

	private bool CheckDigit() => IsNotEnd() && CheckRange('0', '9');

	private bool CheckLetter() => IsNotEnd() && (CheckRange('A', 'Z') || CheckRange('a', 'z') || CheckRange('А', 'Я') || CheckRange('а', 'я'));

	private bool CheckLD() => CheckLetter() || CheckDigit();

	private void IncreasePosSmoothly()
	{
		if (ValidateChar('\r') | ValidateChar('\n'))
		{
			lineN++;
			lineStart = pos;
			success = true;
		}
		else
			pos++;
	}

	private String GetNumber(out LexemType type)
	{
		var start = pos;
		List<String> list = [GetNumber2(out type)];
		if (CheckOverflow(list[0], ref type) is String s) return s;
		if (ValidateChar('.'))
		{
			type = LexemType.Real;
			list.Add(input[(pos - 1)..pos]);
			list.Add(GetNumber2(out _));
			if (CheckOverflow(list[^1], ref type) is String s2) return s2;
			if (list[0] == "" && list[2] == "")
			{
				pos = start;
				return "";
			}
		}
		if (list[0] != "" && ValidateCharList("Ee") && ValidateCharList("+-"))
		{
			type = LexemType.Real;
			list.Add(input[(pos - 2)..pos]);
			list.Add(GetNumber2(out _));
			if (CheckOverflow(list[^1], ref type) is String s2) return s2;
		}
		if (list[0] != "" && ValidateChar('r'))
		{
			type = LexemType.Real;
			list.Add("r");
		}
		return String.Join("", [.. list]);
		String? CheckOverflow(String s, ref LexemType type)
		{
			if (s == "null")
			{
				GenerateError(start, "too large number; long long type is under development");
				type = LexemType.Keyword;
				return "null";
			}
			else
				return null;
		}
	}

	private String GetNumber2(out LexemType type)
	{
		var start = pos;
		while (IsNotEnd() && CheckDigit())
			pos++;
		String s = new(32, FromStart(start));
		if (s.Length == 0)
		{
			type = LexemType.Other;
			return s;
		}
		if (int.TryParse(s.ToString(), out _))
			type = LexemType.Int;
		else if (long.TryParse(s.ToString(), out _))
			type = LexemType.LongInt;
		else
		{
			type = LexemType.Keyword;
			return "null";
		}
		return s;
	}

	private String GetWord(bool firstLetter = true)
	{
		var start = pos;
		var b = false;
		void Validate(bool parameter = false)
		{
			if (CheckLetter() || parameter && CheckDigit())
				b = true;
			pos++;
		}
		if ((firstLetter ? CheckLetter() : CheckLD()) || CheckChar('_'))
			Validate();
		while (CheckLD() || CheckChar('_'))
			Validate(true);
		if (b == false)
			pos = start;
		return new(32, FromStart(start));
	}

	private String GetString(out String s2)
	{
		var start = pos;
		String result = [];
		String tempResult = [];
		var hex = 0;
		void AddChar(String target) => target.Add(input[pos++]);
		String EscapeSequence() => ['\\', input[pos - 1]];
		bool ValidateAndAdd(char c)
		{
			if (ValidateChar(c))
			{
				result.Add(c);
				return true;
			}
			return false;
		}
		bool HexSequence(char c, int length)
		{
			if (ValidateChar(c))
			{
				tempResult.AddRange(EscapeSequence());
				hex = length;
				return true;
			}
			return false;
		}
		bool IsCharAfterBackslash(char c, bool addChar = true)
		{
			if (input[pos - 1] != '\\' && CheckChar(c))
			{
				if (addChar)
				{
					pos++;
					result.Add(c);
				}
				return true;
			}
			return false;
		}
		void GenerateEscapeSequenceError(int posDiff) => GenerateMessage("Error", pos++ - posDiff - lineStart, "unrecognized escape-sequence");
		String AddAndReturn(out String s2, char toAdd)
		{
			result.Add(toAdd);
			s2 = input[start..pos];
			return new(result);
		}
		String TriStateCondition(ref String s2, bool condition, bool flag)
		{
			if (condition)
				return "";
			else if (!IsNotEnd())
				return GenerateQuoteWreck(out s2);
			else if (flag)
			{
				if (!CheckChar('\''))
					AddChar(result);
				return "";
			}
			{
				return GenerateQuoteWreck(out s2, "there must be a single character or a single escape-sequence in the single quotes");
			}
		}
		bool ValidateCharOrEscapeSequence()
		{
			if (ValidateChar('\\'))
			{
				if (ValidateCharList("0abfnqrtv'\"!"))
				{
					result.AddRange(EscapeSequence());
					hex = 0;
				}
				else if (!(HexSequence('x', 2) || HexSequence('u', 4)))
					GenerateEscapeSequenceError(1);
				else
					hex = 0;
				AddEscapeSequenceChars(ref tempResult, hex);
				if (tempResult.Length != 0)
					result.AddRange(tempResult);
				return true;
			}
			else
				return false;
		}
		void AddEscapeSequenceChars(ref String tempResult, int hex)
		{
			for (var i = 0; i < hex; i++)
			{
				if (IsNotEnd() && (CheckRange('0', '9') || CheckRange('A', 'F') || CheckRange('a', 'f')))
					AddChar(tempResult);
				else
				{
					GenerateEscapeSequenceError(i + 2);
					tempResult = [];
					break;
				}
			}
		}
		String GenerateQuoteWreck(out String s2, string text = "unexpected end of code reached; expected: single quote", bool double_ = false)
		{
			GenerateMessage("Wreck", pos, text);
			wreckOccurred = true;
			return AddAndReturn(out s2, double_ ? '\"' : '\'');
		}
		String GenerateDoubleQuoteWreck(out String s2) => GenerateQuoteWreck(out s2, "unexpected end of code reached; expected: double quote", true);
		if (ValidateAndAdd('\"'))
		{
			while (IsNotEnd() && input[pos] is not ('\r' or '\n') && !IsCharAfterBackslash('\"', false))
			{
				if (!ValidateCharOrEscapeSequence())
					AddChar(result);
			}
			if (IsNotEnd() && input[pos] is '\r' or '\n')
				return GenerateQuoteWreck(out s2, "classic string (not raw or verbatim) must be single-line; expected: double quote");
			if (!IsCharAfterBackslash('\"'))
				return GenerateDoubleQuoteWreck(out s2);
		}
		else if (ValidateAndAdd('\''))
		{
			s2 = [];
			TriStateCondition(ref s2, ValidateCharOrEscapeSequence(), true);
			TriStateCondition(ref s2, ValidateAndAdd('\''), false);
		}
		else if (ValidateAndAdd('@'))
		{
			if (!ValidateChar('\"'))
			{
				pos = start;
				return s2 = [];
			}
			result.Add('\"');
			while (true)
			{
				if (ValidateAndAdd('\"'))
					goto l0;
				else if (!IsNotEnd())
					return GenerateDoubleQuoteWreck(out s2);
				else
				{
					result.Add(input[pos]);
					IncreasePosSmoothly();
				}
				continue;
			l0:
				if (!ValidateAndAdd('\"'))
					break;
			}
		}
		s2 = input[start..pos];
		return new(32, result);
	}

	private String GetRawString(out String s2)
	{
		var start = pos;
		String result = [];
		if (!ValidateAndAdd('/'))
			return s2 = [];
		if (!ValidateAndAdd('\"'))
		{
			pos = start;
			return s2 = [];
		}
		var depth = 0;
		var state = RawStringState.Normal;
		while (true)
		{
			if (wreckOccurred)
			{
				s2 = input[start..pos];
				return result.AddRange(((String)"\"\\").Repeat(depth + 1));
			}
			else if (pos >= input.Length)
			{
				GenerateMessage("Wreck", pos, "unexpected end of code reached; expected: " + (depth + 1)
					+ " pairs \"double quote - reverse slash\" (starting with quote)");
				wreckOccurred = true;
				s2 = input[start..pos];
				return result.AddRange(((String)"\"\\").Repeat(depth + 1));
			}
			else if (ValidateAndAdd('/'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.ForwardSlash;
					continue;
				}
				var pos2 = pos;
				while (IsNotEnd() && input[pos] is not ('\r' or '\n'))
					pos++;
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('*'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				var pos2 = pos;
				pos++;
				while (IsNotEnd() && !(input[pos - 1] == '*' && input[pos] == '/'))
					IncreasePosSmoothly();
				if (IsNotEnd())
					pos++;
				else
				{
					GenerateMessage("Wreck", pos, "unclosed comment in the end of code");
					wreckOccurred = true;
					s2 = input[start..pos];
					return result.AddRange("*/").AddRange(((String)"\"\\").Repeat(depth + 1));
				}
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('{'))
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				var pos2 = pos;
				SkipNestedComments();
				result.AddRange(input[pos2..pos]);
			}
			else if (ValidateAndAdd('\\'))
			{
				if (state is not (RawStringState.Quote or RawStringState.ForwardSlashAndQuote))
					state = RawStringState.Normal;
				else if (depth == 0 || state == RawStringState.ForwardSlashAndQuote && depth == 1)
					break;
				else if (state == RawStringState.ForwardSlashAndQuote)
				{
					depth -= 2;
					state = RawStringState.Normal;
				}
				else
				{
					depth--;
					state = RawStringState.Normal;
				}
			}
			else if (ValidateAndAdd('\"'))
			{
				if (state == RawStringState.ForwardSlash)
				{
					depth++;
					state = RawStringState.ForwardSlashAndQuote;
				}
				else if (state == RawStringState.EmailSign)
					result.AddRange(GetVerbatimStringInsideRaw());
				else
					state = RawStringState.Quote;
			}
			else if (ValidateAndAdd('@'))
				state = RawStringState.EmailSign;
			else
			{
				result.Add(input[pos]);
				state = RawStringState.Normal;
				IncreasePosSmoothly();
			}
		}
		s2 = input[start..pos];
		return new(32, result);
		bool ValidateAndAdd(char c)
		{
			if (ValidateChar(c))
			{
				result.Add(c);
				return true;
			}
			return false;
		}
	}

	private String GetVerbatimStringInsideRaw()
	{
		String result = [];
		while (true)
		{
			if (ValidateChar('\"'))
			{
				result.Add(input[pos - 1]);
				goto l0;
			}
			else if (!IsNotEnd())
			{
				GenerateMessage("Wreck", pos, "unexpected end of code reached; expected: double quote");
				wreckOccurred = true;
				return result.Add('\"');
			}
			else
			{
				result.Add(input[pos]);
				IncreasePosSmoothly();
			}
			continue;
		l0:
			if (!ValidateChar('\"'))
				break;
			result.Add(input[pos - 1]);
		}
		return result;
	}

	private String GetUnformatted()
	{
		var start = pos;
		while (IsNotEnd() && CheckLD() == false && ("_\"';()[]{} \t\r\n" + (char)160).Contains(input[pos]) == false)
			pos++;
		return input[start..pos];
	}

	private void SkipSpacesAndComments()
	{
		while (true)
		{
			while (IsNotEnd() && input[pos] is ' ' or '\t' or (char)160)
				pos++;
			if (!(pos <= input.Length - 2 && input[pos] == '/'))
				return;
			var c = input[pos + 1];
			if (c == '/')
			{
				pos += 2;
				while (IsNotEnd() && input[pos] is not ('\r' or '\n'))
					pos++;
			}
			else if (c == '*')
			{
				pos += 3;
				while (IsNotEnd() && !(input[pos - 1] == '*' && input[pos] == '/'))
					IncreasePosSmoothly();
				if (IsNotEnd())
					pos++;
				else
				{
					GenerateMessage("Wreck", pos, "unclosed comment in the end of code");
					wreckOccurred = true;
					return;
				}
			}
			else if (c == '{')
			{
				pos += 2;
				SkipNestedComments();
			}
			else
				return;
		}
	}

	private void SkipNestedComments()
	{
		int depth = 0, state = 0;
		while (IsNotEnd())
		{
			var c = input[pos];
			if (c == '/')
			{
				if (state != 2)
					state = 1;
				else if (depth == 0)
				{
					pos++;
					return;
				}
				else
				{
					depth--;
					state = 0;
				}
			}
			else if (c == '{')
			{
				if (state == 1)
					depth++;
				state = 0;
			}
			else if (c == '}')
				state = 2;
			else
				state = 0;
			IncreasePosSmoothly();
		}
		GenerateMessage("Wreck", pos, "unclosed " + (depth + 1) + " nested comments in the end of code");
		wreckOccurred = true;
	}

	private void ValidateEquality(ref String s)
	{
		if (ValidateChar('='))
			s.Add('=');
	}

	public (List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred) Disassemble()
	{
		while (IsNotEnd())
		{
		l0:
			if (wreckOccurred)
			{
				if (success)
					lexems.AddRange(tempLexems);
				tempLexems = [];
				success = false;
				return (lexems, input, errorsList, true);
			}
			SkipSpacesAndComments();
			if (wreckOccurred)
				goto l0;
			if (!IsNotEnd())
			{
				success = true;
				goto l1;
			}
			if (ValidateChar('\r') | ValidateChar('\n'))
			{
				lineN++;
				lineStart = pos;
				success = true;
				goto l1;
			}
			void AddLexem(String string_, LexemType type, int offset) => tempLexems.Add(new Lexem(string_, type, lineN, pos - offset - lineStart));
			void AddOperatorLexem(String string_) => AddLexem(string_, LexemType.Operator, string_.Length);
			void AddOtherLexem(String string_) => AddLexem(string_, LexemType.Other, string_.Length);
			String s;
			if (CheckChar('\'') || CheckChar('\"') || CheckChar('@'))
			{
				s = GetString(out var s2);
				if (s != "")
				{
					AddLexem(s, LexemType.String, s2.Length);
					goto l0;
				}
			}
			else if ((s = GetRawString(out var s3)) != "")
			{
				AddLexem(s, LexemType.String, s3.Length);
				goto l0;
			}
			else if (CheckDigit())
			{
				s = GetNumber(out var numberType);
				if (s != "")
				{
					AddLexem(s, numberType, s.Length);
					goto l0;
				}
			}
			else if (CheckLetter() || CheckChar('_'))
			{
				s = GetWord();
				if (s == "")
					goto l10;
				if (s.ToString() is "_" or "abstract" or "break" or "case" or "Class" or "closed" or "const" or "Constructor" or "continue" or "Delegate" or "delete" or "Destructor" or "else" or "Enum" or "Event" or "Extent" or "extern" or "false" or "for" or "Function" or "if" or "Interface" or "internal" or "lock" or "loop" or "multiconst" or "Namespace" or "new" or "null" or "Operator" or "out" or "override" or "params" or "protected" or "public" or "readonly" or "ref" or "repeat" or "return" or "sealed" or "static" or "Struct" or "switch" or "this" or "throw" or "true" or "using" or "while")
					AddLexem(s, LexemType.Keyword, s.Length);
				else if (s.ToString() is "and" or "or" or "xor" or "is" or "typeof" or "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "ln" or "Infty" or "Uncty" or "Pi" or "E" or "CombineWith" or "CloseOnReturnWith")
					AddOperatorLexem(s);
				else if (s.ToString() is "pow" or "tetra" or "penta" or "hexa")
				{
					ValidateEquality(ref s);
					AddOperatorLexem(s);
				}
				else
					AddLexem(s, LexemType.Identifier, s.Length);
				goto l0;
			}
			else if (";()[]{}".Contains(input[pos]))
			{
				AddOtherLexem("" + input[pos++]);
				goto l0;
			}
		l10:
			var l = 0;
			if (ValidateChar('-'))
			{
				var s3 = GetWord();
				if (s3 == "Infty")
				{
					l += s3.Length + 1;
					s = input.GetRange(pos - l, l);
					AddOperatorLexem(s);
					goto l0;
				}
				else
					pos -= s3.Length + 1;
			}
			pos += l;
			s = new String(32, [.. ValidateLexemTree(new LexemTree('\0', lexemTree), out var success_)]);
			if (s != "" && success_)
			{
				AddOperatorLexem(s);
				goto l0;
			}
			s = GetUnformatted();
			if (s != "")
			{
				GenerateError(pos - s.Length, "unrecognized sequence of symbols");
				goto l0;
			}
		l1:
			if (success)
				lexems.AddRange(tempLexems);
			tempLexems = [];
			success = false;
		}
		return (lexems, input, errorsList, wreckOccurred);
		String ValidateLexemTree(LexemTree lexemTree, out bool success)
		{
			success = false;
			int start = pos, found = 0, lexemIndex = -1;
			var c = input[pos++];
			String result = [];
			String Empty()
			{
				pos = start;
				return [];
			}
			while ((lexemIndex = lexemTree.NextTree.FindIndex(lexemIndex + 1, x => x.Char == c)) != -1)
			{
				found++;
				if (found > 1 && lexemTree.AllowAll == false)
					return Empty();
				result.Add(c);
				var result2 = ValidateLexemTree(lexemTree.NextTree[lexemIndex], out success);
				if (success == false)
					return Empty();
				result.AddRange(result2);
			}
			if (found == 0)
			{
				success = lexemTree.AllowNone;
				return Empty();
			}
			return result;
		}
	}

	private void GenerateError(int pos, String text) => GenerateMessage("Error", pos, text);

	private void GenerateMessage(String type, int pos, String text) => errorsList.Add(type + " in line " + lineN.ToString() + " at position " + (pos - lineStart).ToString() + ": " + text);

	public static implicit operator (List<Lexem> Lexems, String String, List<String> ErrorsList, bool WreckOccurred)(CodeSample x) => x.Disassemble();
}

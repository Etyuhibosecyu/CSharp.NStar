global using NStar.Core;
global using static CSharp.NStar.BuiltInMemberCollections;
global using String = NStar.Core.String;
using System.Diagnostics;

namespace CSharp.NStar;

public enum LexemType
{
	Int,
	UnsignedInt,
	LongInt,
	UnsignedLongInt,
	Real,
	Identifier,
	Keyword,
	Operator,
	String,
	Other,
}

[DebuggerDisplay("{ToString()}")]
public sealed class Lexem(String newString, LexemType newType, int newLineN, int newPos)
{
	public String String { get; set; } = newString;
	public LexemType Type { get; set; } = newType;
	public int LineN { get; set; } = newLineN;
	public int Pos { get; set; } = newPos;

	public Lexem() : this([], LexemType.Other, 1, 0)
	{
	}

	public override string ToString() => $"{Type}: {String}";
}

public class CodeSample(String newString)
{
	private readonly List<Lexem> lexems = [];
	private List<Lexem> lexemsBuffer = [];
	private bool success;
	private readonly String input = newString.Length == 0 ? "return null;" : newString;
	private int pos = 0, lineStart = 0;
	private int lineN = 1;
	private bool wreckOccurred = false;
	private readonly List<String> errors = [];
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

	private String GetNumber(out LexemType lexemType)
	{
		var start = pos;
		List<String> numberParts = [GetNumber2(out lexemType)];
		if (CheckOverflow(numberParts[0], ref lexemType) is String s) return s;
		if (ValidateChar('.'))
		{
			if (CheckChar('.'))
			{
				pos--;
				return input.GetRange(start..pos, true);
			}
			lexemType = LexemType.Real;
			numberParts.Add(input[(pos - 1)..pos]);
			numberParts.Add(GetNumber2(out _));
			if (CheckOverflow(numberParts[^1], ref lexemType) is String s2) return s2;
			if (numberParts[0].Length == 0 && numberParts[2].Length == 0)
			{
				pos = start;
				return [];
			}
		}
		if (numberParts[0].Length != 0 && ValidateCharList("Ee") && ValidateCharList("+-"))
		{
			lexemType = LexemType.Real;
			numberParts.Add(input[(pos - 2)..pos]);
			numberParts.Add(GetNumber2(out _));
			if (CheckOverflow(numberParts[^1], ref lexemType) is String s2) return s2;
		}
		if (numberParts[0].Length != 0 && ValidateChar('r'))
		{
			lexemType = LexemType.Real;
			numberParts.Add("r");
		}
		return String.Join([], [.. numberParts]);
		String? CheckOverflow(String s, ref LexemType lexemType)
		{
			if (s == "null")
			{
				GenerateMessage(0x0001, start);
				lexemType = LexemType.Keyword;
				return "null";
			}
			else
				return null;
		}
	}

	private String GetNumber2(out LexemType lexemType)
	{
		var start = pos;
		while (IsNotEnd() && CheckDigit())
			pos++;
		String s = new(32, FromStart(start));
		if (s.Length == 0)
		{
			lexemType = LexemType.Other;
			return s;
		}
		if (int.TryParse(s.ToString(), out _))
			lexemType = LexemType.Int;
		else if (uint.TryParse(s.ToString(), out _))
			lexemType = LexemType.UnsignedInt;
		else if (long.TryParse(s.ToString(), out _))
			lexemType = LexemType.LongInt;
		else if (ulong.TryParse(s.ToString(), out _))
			lexemType = LexemType.UnsignedLongInt;
		else
		{
			lexemType = LexemType.Keyword;
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
		String buffer = [];
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
				buffer.AddRange(EscapeSequence());
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
		void GenerateEscapeSequenceError(int posDiff) => GenerateMessage(0x0002, pos++ - posDiff - lineStart);
		String AddAndReturn(out String s2, char toAdd)
		{
			result.Add(toAdd);
			s2 = input[start..pos];
			return new(result);
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
				AddEscapeSequenceChars(ref buffer, hex);
				if (buffer.Length != 0)
					result.AddRange(buffer);
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
		String GenerateQuoteWreck(out String s2, ushort code, string text = "unexpected end of code reached; expected: single quote", bool double_ = false)
		{
			GenerateMessage(code, pos, text);
			wreckOccurred = true;
			return AddAndReturn(out s2, double_ ? '\"' : '\'');
		}
		String GenerateDoubleQuoteWreck(out String s2) => GenerateQuoteWreck(out s2, 0x9002, "unexpected end of code reached; expected: double quote", true);
		if (ValidateAndAdd('\"'))
		{
			while (IsNotEnd() && input[pos] is not ('\r' or '\n') && !IsCharAfterBackslash('\"', false))
			{
				if (!ValidateCharOrEscapeSequence())
					AddChar(result);
			}
			if (IsNotEnd() && input[pos] is '\r' or '\n')
				return GenerateQuoteWreck(out s2, 0x9003);
			if (!IsCharAfterBackslash('\"'))
				return GenerateDoubleQuoteWreck(out s2);
		}
		else if (ValidateAndAdd('\''))
		{
			if (ValidateCharOrEscapeSequence()) { }
			else if (!IsNotEnd())
				GenerateQuoteWreck(out _, 0x9000);
			else if (!CheckChar('\''))
					AddChar(result);
			if (ValidateAndAdd('\'')) { }
			else if (!IsNotEnd())
				GenerateQuoteWreck(out _, 0x9000);
			else
				GenerateQuoteWreck(out _, 0x9001);
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
				GenerateMessage(0x9004, pos, depth + 1);
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
					GenerateMessage(0x9005, pos);
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
				GenerateMessage(0x9002, pos);
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
					GenerateMessage(0x9005, pos);
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
		GenerateMessage(0x9006, pos, depth + 1);
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
					lexems.AddRange(lexemsBuffer);
				lexemsBuffer = [];
				success = false;
				return (lexems, input, errors, true);
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
			void AddLexem(String string_, LexemType lexemType, int offset) => lexemsBuffer.Add(new Lexem(string_, lexemType, lineN, pos - offset - lineStart));
			void AddOperatorLexem(String string_) => AddLexem(string_, LexemType.Operator, string_.Length);
			void AddOtherLexem(String string_) => AddLexem(string_, LexemType.Other, string_.Length);
			String s;
			if (CheckChar('\'') || CheckChar('\"') || CheckChar('@'))
			{
				s = GetString(out var s2);
				if (s.Length != 0)
				{
					AddLexem(s, LexemType.String, s2.Length);
					goto l0;
				}
			}
			else if ((s = GetRawString(out var s3)).Length != 0)
			{
				AddLexem(s, LexemType.String, s3.Length);
				goto l0;
			}
			else if (CheckDigit())
			{
				s = GetNumber(out var numberType);
				if (s.Length != 0)
				{
					AddLexem(s, numberType, s.Length);
					goto l0;
				}
			}
			else if (CheckLetter() || CheckChar('_'))
			{
				s = GetWord();
				if (s.Length == 0)
					goto l10;
				if (Keywords.Contains(s))
					AddLexem(s, LexemType.Keyword, s.Length);
				else if (s.ToString() is "and" or "or" or "xor" or "is" or "typeof" or "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "ln" or "Infty" or "Uncty" or "CombineWith" or "CloseOnReturnWith")
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
				AddOtherLexem((String)input[pos++]);
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
			s = new String(32, ValidateLexemTree(new LexemTree('\0', lexemTree), out var success_));
			if (s.Length != 0 && success_)
			{
				AddOperatorLexem(s);
				goto l0;
			}
			s = GetUnformatted();
			if (s.Length != 0)
			{
				GenerateMessage(0x0003, pos - s.Length);
				goto l0;
			}
		l1:
			if (success)
				lexems.AddRange(lexemsBuffer);
			lexemsBuffer = [];
			success = false;
		}
		return (lexems, input, errors, wreckOccurred);
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

	private void GenerateMessage(ushort code, int pos, params dynamic[] parameters) =>
		Messages.GenerateMessage(errors, code, lineN, pos - lineStart, parameters);

	public static implicit operator (List<Lexem> Lexems, String String, List<String> ErrorsList,
		bool WreckOccurred)(CodeSample x) => x.Disassemble();
}

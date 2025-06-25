global using NStar.Core;
global using NStar.Linq;
global using NStar.MathLib;
global using System;
global using System.Diagnostics;
global using System.Text.RegularExpressions;
global using G = System.Collections.Generic;
global using static System.Math;
global using String = NStar.Core.String;

namespace CSharp.NStar;

public static partial class Quotes
{
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
			output = ((String)"/\"").AddRange(input).AddRange("\"\\");
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
		if (!(input.Length >= 4 && input[0] == '/' && input[1] == '\"' && input[^2] == '\"' && input[^1] == '\\'))
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
		var depth = 0;
		var state = RawStringState.Normal;
		for (var i = 0; i < input.Length;)
		{
			if (i >= input.Length)
				return false;
			var c = input[i++];
			if (c == '/')
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.ForwardSlash;
					continue;
				}
				while (i < input.Length && input[i] is not ('\r' or '\n'))
					i++;
			}
			else if (c == '*')
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				i++;
				while (i < input.Length && !(input[i - 1] == '*' && input[i] == '/'))
					i++;
				if (i < input.Length)
					i++;
				else
					return false;
			}
			else if (c == '{')
			{
				if (state != RawStringState.ForwardSlash)
				{
					state = RawStringState.Normal;
					continue;
				}
				if (!SkipNestedComments(input, ref i))
					return false;
			}
			else if (c == '\\')
			{
				if (state is not (RawStringState.Quote or RawStringState.ForwardSlashAndQuote))
					state = RawStringState.Normal;
				else if (depth == 0 || state == RawStringState.ForwardSlashAndQuote && depth == 1)
					return false;
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
			else if (c == '\"')
			{
				if (state == RawStringState.ForwardSlash)
				{
					depth++;
					state = RawStringState.ForwardSlashAndQuote;
				}
				else if (state != RawStringState.EmailSign)
					state = RawStringState.Quote;
				else if (!SkipVerbatimStringInsideRaw(input, ref i))
					return false;
			}
			else if (c == '@')
				state = RawStringState.EmailSign;
			else
				state = RawStringState.Normal;
		}
		return depth == 0;
	}

	private static bool SkipNestedComments(String input, ref int i)
	{
		int depth = 0, state = 0;
		while (i < input.Length)
		{
			var c = input[i];
			if (c == '/')
			{
				if (state != 2)
					state = 1;
				else if (depth == 0)
				{
					i++;
					return true;
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
			i++;
		}
		return false;
	}

	private static bool SkipVerbatimStringInsideRaw(String input, ref int i)
	{
		while (i < input.Length)
		{
			var c = input[i++];
			if (c == '\"')
				goto l0;
			else if (i >= input.Length)
				return false;
			continue;
		l0:
			if (i < input.Length && input[i] != '\"')
				return true;
		}
		return false;
	}

	[GeneratedRegex("[0-9]+")]
	public static partial Regex IntRegex();

	[GeneratedRegex("""\G([0-9]+(?:\.[0-9]+)?(?:[Ee][+-][0-9]+)?)|((?:pow=?|and|or|xor|CombineWith|is|typeof|sin|cos|tan|asin|acos|atan|ln|_|this|null|Infty|-Infty|Uncty|Pi|E|I|G)(?![0-9A-Za-z_]))|((?:abstract|break|case|Class|closed|const|Constructor|continue|default|Delegate|delete|Destructor|else|Enum|Event|Extent|extern|false|for|foreach|Function|if|Interface|internal|lock|loop|multiconst|Namespace|new|null|Operator|out|override|params|protected|public|readonly|ref|repeat|return|sealed|static|Struct|switch|this|throw|true|using|while)(?![0-9A-Za-z_]))|([A-Za-z_][0-9A-Za-z_]*)|(\^[\^=]?|\|[\|=]?|&[&=]|>(?:>>?)?=?|<(?:<<?)?=?|![!=]?|\?(?:!?=|>=?|<=?|\?|\.|\[)?|,|\:|$|~|\+[+=]?|-[\-=]?|\*=?|/=?|%=?|=[=>]?|\.(?:\.\.?)?)|((?:"(?:[^\\"]|\\[0abfnqrtv'"!]|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4})*?")|(?:@"(?:[^"]|"")*?"))|('(?:[^\\'"]|\\[0abfnqrtv'"!]|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4})?')|([;()[\]{}])|([ \t\r\n\xA0]+)|(.)""")]
	public static partial Regex LexemRegex();
}

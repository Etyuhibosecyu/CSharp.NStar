namespace CSharp.NStar;

public static class Messages
{
	public static void GenerateMessage(ref List<String>? errors, ushort code, int line, int column,
		params dynamic[] parameters) => GenerateMessage(errors ??= [], code, line, column, parameters);

	public static void GenerateMessage(List<String> errors, ushort code, int line, int column, params dynamic[] parameters)
	{
		var codeString = Convert.ToString(code, 16).ToUpper().PadLeft(4, '0');
		errors.Add(codeString[0] switch
		{
			>= '0' and <= '7' => "Error ",
			'8' => "Warning ",
			'9' => "Wreck ",
			'F' => "Technical wreck ",
			_ => throw new InvalidOperationException(),
		} + codeString + " in line " + line.ToString() + " at position " + column.ToString() + ": " + code switch
		{
			0x0000 => "unexpected end of code reached",
			0x0001 => "too large number; long long type is under development",
			0x0002 => "unrecognized escape-sequence",
			0x0003 => "unrecognized sequence of symbols",
			0x0004 => "expected: identifier",
			0x0005 => "incorrect word or order of words in construction declaration",
			0x0006 => "the class \"" + parameters[0] + "\" is the standard root C#.NStar type and cannot be redefined",
			0x0007 => "the class \"" + parameters[0] + "\" is already defined in this region",
			0x0008 => "the class \"" + parameters[0] + "\" is nearest to this outer class",
			0x0009 => "a static class cannot be derived",
			0x000A => "the static functions are allowed only inside the classes",
			0x000B => "the function \"" + parameters[0] + "\" is the standard root C#.NStar function and cannot be redefined",
			0x000C => throw new InvalidOperationException(),
			0x000D => "the function cannot have same name as its container class",
			0x000E => "\";\" must follow the abstract function declaration, \"{\" - the non-abstract one",
			0x000F => "the constructors are allowed only inside the classes",
			0x0010 => parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x0011 => "expected: {",
			0x2000 => "unexpected end of code reached",
			0x2001 => "expected: identifier",
			0x2002 => "expected: \";\"",
			0x2003 => "expected: {",
			0x2004 => "expected: }",
			0x2005 => "expected: methods or }",
			0x2006 => "expected: \":\"",
			0x2007 => "unrecognized construction",
			0x2008 => "expected: " + parameters[0],
			0x2009 => throw new InvalidOperationException(),
			0x200A => "expected: (",
			0x200B => "expected: )",
			0x200C => "expected: [",
			0x200D => "expected: ]",
			0x200E => "expected: expression",
			0x200F => "expected: \"if\" or \"else\"",
			0x2010 => "expected: \"loop\" or \"while\" or \"repeat\" or \"for\"",
			0x2011 => "expected: \"continue\" or \"break\"",
			0x2012 => "expected: identifier or basic expression or expression in round brackets",
			0x2013 => "expected: \"in\"",
			0x2014 => "expected: \"null\" or identifier or tuple",
			0x2015 => "expected: non-sealed class or interface",
			0x2016 => "expected: int number; variables and complex expressions at this place are under development",
			0x2017 => "this expression must be implicitly convertible to the \"int\" type",
			0x2018 => "expected: comma; chain of indexes is not ended",
			0x2019 => "incorrect construction in parameters list",
			0x201A => "the parameter with the \"params\" keyword must be last in the list",
			0x201B => "the parameters without the default value and without the \"params\" modifier must appear" +
				" before the parameters with the default value" + (parameters[0] ? "; expected: identifier" : ""),
			0x201C => "at present time the \"is\" operator can be used only with \"null\" and the types",
			0x201D => "only the variables can be assigned",
			0x201E => "the keyword \"" + parameters[0] + "\" is under development",
			0x201F => "the division by the integer zero is forbidden",
			0x2020 => "incorrect word or order of words in construction declaration",
			0x2021 => "too many nested collections of types",
			0x2022 => "the keyword \"var\" is not a type and cannot be used inside the type",
			0x2023 => "cannot create an instance of the abstract type \"" + parameters[0] + "\"",
			0x2024 => "cannot create an instance of the static type \"" + parameters[0] + "\"",
			0x2025 => "incorrect construction after type name",
			0x2026 => "the type \"" + parameters[0] + "\" is not defined in this location",
			0x2027 => "incorrect type with \"short\" or \"long\" word",
			0x2028 => "incorrect type with \"unsigned\" word",
			0x2029 => throw new InvalidOperationException(),
			0x202A => "the identifier \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x202B => "the end of identifier \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x202C => "the namespace \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x202D => "the type \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x202E => "the end of identifier \"" + parameters[0] + "\" is outdated, consider using "
				+ parameters[1] + " instead",
			0x202F => "the properties can only have the proper access, no public",
			0x2030 => "the \"new\" keyword is forbidden here",
			0x2031 => "the \"new\" keyword with implicit type is under development",
			0x2032 => "the function \"" + parameters[0] + "\" with these parameter types is already defined in this region",
			0x2033 => "the switch expression cannot be empty",
			0x2034 => "the switch expression cannot contain cases after \"_\"",
			0x2035 => "the property cannot be static and required at the same time",
			0x203A => "the identifier \"" + parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x203B => "the end of identifier \"" + parameters[0] + "\" is reserved for next versions of C#.NStar" +
				" and cannot be used",
			0x203C => "the constants cannot have getters or setters",
			0x203D => "the constant must have a value",
			0x2048 => "goto is a bad operator, it worsens the organization of the code;" +
				" C#.NStar refused from its using intentionally",
			0x4000 => "internal compiler error",
			0x4001 => "the identifier \"" + parameters[0] + "\" is not defined in this location",
			0x4002 => "cannot apply this operator to this constant",
			0x4003 => "cannot compute factorial of this constant",
			0x4004 => "the division by the integer zero is forbidden",
			0x4005 => "cannot apply the operator \"" + parameters[0] + "\" to the type \"" + parameters[1] + "\"",
			0x4006 => "cannot apply the operator \"" + parameters[0] + "\" to the types \"" + parameters[1]
				+ "\" and \"" + parameters[2] + "\"",
			0x4007 => "the strings cannot be subtracted",
			0x4008 => "the string cannot be multiplied by the string",
			0x4009 => "the strings cannot be divided or give the remainder (%)",
			0x400A => "the abstract members can be located only inside the abstract classes",
			0x400B => "at present time the index in the tuple must be a compilation-time constant",
			0x400C => "incorrect construction is passing with the \"" + parameters[0] + "\" keyword",
			0x400D => "cannot compute this expression",
			0x400E => "the operator \"" + parameters[0] + "\" requires the third operand",
			0x400F => "cannot use the \"?\" operator in this context",
			0x4010 => "the variable \"" + parameters[0] + "\" is not defined in this location;" +
				" multiconst functions cannot use variables that are outside of the function",
			0x4011 => "the variable declared with the keyword \"var\" must be assigned explicitly and in the same expression",
			0x4012 => "one cannot use the local variable \"" + parameters[0]
				+ "\" before it is declared or inside such declaration in line "
				+ parameters[1] + " at position " + parameters[2],
			0x4013 => "the variable \"" + parameters[0] + "\" is already defined in this location" +
				" or in the location that contains this in line " + parameters[1] + " at position " + parameters[2],
			0x4014 => parameters[0] ?? "cannot convert from the type \"" + parameters[1] + "\" to the type \""
				+ parameters[2] + "\"" + (TypeEqualsToPrimitive(parameters[2], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4015 => "there is no implicit conversion between the types \"" + parameters[0] + "\" and \""
				+ parameters[1] + "\"",
			0x4016 => "incorrect index in the list or the tuple; only the positive indexes are supported",
			0x4017 => "the type \"" + parameters[0] + "\" cannot be created via the constructor",
			0x4018 => "the abstract type \"" + parameters[0] + "\" can be created via the constructor"
				+ " but only if you explicitly specify the constructing type (which is not abstract)",
			0x4019 => "the source for the switch expression must have a finite-range numeric or a string type",
			0x4020 => "the function \"" + parameters[0] + "\" cannot be used in the delegate",
			0x4021 => "the function \"" + parameters[0] + "\" is linked with object instance so it cannot be used in delegate",
			0x4022 => "the function \"" + parameters[0] + "\" must have " + (parameters[1] == parameters[2]
				? parameters[1].ToString() : "from " + parameters[2].ToString() + " to " + parameters[1].ToString())
				+ " parameters",
			0x4023 => "the function \"" + parameters[0] + "\" does not have overlaods with parameters",
			0x4024 => "the function \"" + parameters[0] + "\" is not defined in this location;" +
				" the multiconst functions cannot call the external non-multiconst functions",
			0x4025 => "the function \"" + parameters[0] + "\" cannot be called from this location;" +
				" the static functions cannot call non-static functions",
			0x4026 => parameters[0] ?? "incompatibility between the type of the parameter of the call \"" + parameters[1]
					+ "\" and the type of the parameter of the function \"" + parameters[2] + "\""
					+ (TypeEqualsToPrimitive(parameters[3], "string")
					? " - use an addition of zero-length string for this" : ""),
			0x4027 => parameters[0] ?? "the conversion from the type \"" + parameters[1] + "\" to the type \""
				+ parameters[2] + "\" is possible only in the function return,"
				+ " not in the direct assignment and not in the call",
			0x4028 => "incompatibility between the type of the parameter of the call \""
				+ parameters[0] + "\" and all possible types of the parameter of the function (\""
				+ parameters[1] + "\")" + (parameters[2] == 1 && TypeEqualsToPrimitive(parameters[3], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4029 => "the conversion from the type \"" + parameters[0] + "\" to any of the possible target types (\""
				+ parameters[1] + "\" is possible only in the function return,"
				+ " not in the direct assignment and not in the call",
			0x402A => "this function or lambda must return the value on all execution paths",
			0x402B => parameters[0] ?? "incompatibility between the type of the returning value \"" + parameters[1]
				+ "\" and the function return type \"" + parameters[2] + "\""
				+ (TypeEqualsToPrimitive(parameters[2], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4030 => "the property \"" + parameters[0] + "\" is inaccessible from here",
			0x4031 => "the property \"" + parameters[0] + "\" is not defined in this location;" +
				" multiconst functions cannot use external properties",
			0x4032 => "the property \"" + parameters[0] + "\" cannot be used from this location;" +
				" static functions cannot use non-static properties",
			0x4033 => "the type \"" + parameters[0] + "\" does not contain member \"" + parameters[1] + "\"",
			0x4034 => "the type \"" + parameters[0] + "\" does not have constructors with parameters",
			0x4035 => throw new InvalidOperationException(),
			0x4036 => throw new InvalidOperationException(),
			0x4037 => throw new InvalidOperationException(),
			0x4038 => "this call is forbidden",
			0x4039 => "the property \"" + parameters[0] + "\" cannot be set from here",
			0x403A => "the property \"" + parameters[0] + "\" is declared with \"init\" modifier so it can be set"
				+ " only in the initializer or constructor",
			0x403B => "the property \"" + parameters[0] + "\" is at the same time declared with \"init\" modifier"
				+ " and static so it can be set only in the initializer",
			0x403C => "you must set the required properties - it is done with the square brackets",
			0x403D => "the required property \"" + parameters[0] + "\" must be set during the construction",
			0x403E => "there must be a constant type here; variables at this place are temporarily unavailable",
			0x403F => "redundant property initializer - this class does not have so many open settable properties",
			0x4040 => "unexpected lambda expression here",
			0x4041 => "there is no overload of this function with the delegate parameter on this place",
			0x4042 => "incorrect list of the parameters of the lambda expression",
			0x4043 => "incorrect parameter #" + parameters[0] + " of the lambda expression",
			0x4044 => "incorrect type of the lambda expression",
			0x4045 => "this lambda must have " + parameters[0] + " parameters",
			0x4050 => "this expression must be constant but it isn't",
			0x4051 => "this expression must be constant but it isn't; const functions are under development",
			0x4052 => "cannot assign a value to the constant",
			0x4053 => "the local constant must have a value",
			0x4054 => "the local constant declaration must not contain the other operators than the single assignment",
			0x4055 => "too deep constant definition tree",
			0x4056 => "this expression must be the type but it isn't",
			0x4057 => "this expression must be constant and implicitly convertible to the \"int\" type",
			0x4060 => "the constructor of the type \"" + parameters[0] + "\" must have " + (parameters[1] == parameters[2]
				? parameters[1].ToString() : "from " + parameters[2].ToString() + " to " + parameters[1].ToString())
				+ " parameters",
			0x4061 => parameters[0] ?? "incompatibility between the type of the parameter of the call \"" + parameters[1]
					+ "\" and the type of the parameter of the constructor \"" + parameters[2] + "\""
					+ (TypeEqualsToPrimitive(parameters[3], "string")
					? " - use an addition of zero-length string for this" : ""),
			0x4062 => "incompatibility between the type of the parameter of the call \""
				+ parameters[0] + "\" and all possible types of the parameter of the constructor (\""
				+ parameters[1] + "\")" + (parameters[2] == 1 && TypeEqualsToPrimitive(parameters[3], "string")
				? " - use an addition of zero-length string for this" : ""),
			0x4063 => "a loop detected while analyzing if all the required properties are initialized"
				+ " in the constructor the type \"" + parameters[0] + "\"; consider reordering the class members,"
				+ " especially placing the properties before the constructors",
			0x8000 => "the properties and the methods are static in the static class implicitly;" +
				" the word \"static\" is not necessary",
			0x8001 => "the semicolon in the end of the line with condition or cycle may easily be unnoticed" +
				" and lead to hard-catchable errors",
			0x8002 => "the syntax \"return;\" is deprecated; consider using \"return null;\" instead",
			0x8003 => "two \"list\" modifiers in a row; consider using the multi-dimensional list instead",
			0x8004 => "the count of the identical types in the tuple cannot be zero; it has been set to 1",
			0x8005 => "the unreachable code has been detected",
			0x8006 => "at present time the word \"internal\" does nothing because C#.NStar does not have multiple assemblies",
			0x8007 => "the variable is assigned to itself - are you sure this is not a mistake?",
			0x8008 => "the method \"" + parameters[0]
				+ "\" has the same parameter types as its base method with the same name but it also" +
				" has the other significant differences such as the access modifier or the return type," +
				" so it cannot override that base method and creates a new one;" +
				" if this is intentional, add the \"new\" keyword, otherwise fix the differences",
			0x8009 => "this expression, used with conditional constructions, is constant;" +
				" maybe you wanted to check equality of these values? - it is done with the operator \"==\"",
			0x800A => parameters[0] ?? "the type of the returning value \"" + parameters[1]
				+ "\" and the function return type \"" + parameters[2] + "\" are badly compatible, you may lost data",
			0x800B => "the constants are static implicitly; the word \"static\" is not necessary",
			0x9000 => "unexpected end of code reached; expected: single quote",
			0x9001 => "there must be a single character or a single escape-sequence in the single quotes",
			0x9002 => "unexpected end of code reached; expected: double quote",
			0x9003 => "classic string (not raw or verbatim) must be single-line; expected: double quote",
			0x9004 => "unexpected end of code reached; expected: " + parameters[0]
				+ " pairs \"double quote - reverse slash\" (starting with quote)",
			0x9005 => "unclosed comment in the end of code",
			0x9006 => "unclosed " + parameters[0] + " nested comments in the end of code",
			0x9007 => "unpaired bracket; expected: }",
			0x9008 => "unpaired closing bracket",
			0x9009 => "using namespace is declared not at the beginning of the code",
			0x900A => "expected: identifier",
			0x900B => "namespace \"" + parameters[0] + "\" is still not implemented, wait for next versions",
			0x900C => "namespace \"" + parameters[0] + "\" is outdated, consider using " + parameters[1] + " instead",
			0x900D => "namespace \"" + parameters[0] + "\" is reserved for next versions of C#.NStar and cannot be used",
			0x900E => "\"" + parameters[0] + "\" is not a valid namespace",
			0x900F => "using \"" + parameters[0] + "\" is already declared",
			0x9010 => "expected: \";\"",
			0x9011 => "unpaired brackets; expected: " + parameters[0],
			0x9012 => "at present time the abstract constructors is forbidden",
			0x9013 => "this parameter must pass with the \"" + parameters[0] + "\" keyword",
			0xF000 => "compilation failed because of internal compiler error",
			_ => throw new InvalidOperationException(),
		});
	}
}

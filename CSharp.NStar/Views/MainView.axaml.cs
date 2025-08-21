#define VERIFY
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Utils;
using MsBox.Avalonia;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using static CSharp.NStar.SemanticTree;

namespace CSharp.NStar.Views;

public partial class MainView : UserControl
{
	private Assembly? compiledAssembly;
	private int inputHighlightTarget = -1;
	private readonly String enteredText = [];
	private readonly Random random = new();

	private static readonly ImmutableArray<string> minorVersions = ["2o"];
	private static readonly ImmutableArray<string> langs = ["C#"];
	private static readonly string AlphanumericCharactersWithoutDot = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
	private static readonly G.SortedSet<String> AutoCompletionList = [.. new List<String>("abstract", "break", "case", "Class", "closed", "const", "Constructor", "continue", "default", "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst", "Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref", "repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while", "and", "or", "xor", "is", "typeof", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "Infty", "Uncty", "Pi", "E", "CombineWith", "CloseOnReturnWith", "pow", "tetra", "penta", "hexa").AddRange(PrimitiveTypesList.Keys).AddRange(ExtraTypesList.Convert(x => x.Key.Namespace.Concat(".").AddRange(x.Key.Type))).AddRange(PublicFunctionsList.Keys)];
	private static readonly G.SortedSet<string> AutoCompletionAfterDotList = [.. PrimitiveTypesList.Values.ToList().AddRange(ExtraTypesList.Values).ConvertAndJoin(x => x.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList(x => PropertyMappingBack(x.Name)).AddRange(x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList(x => FunctionMappingBack(x.Name)))).Filter(x => !x.Contains('_'))];
#if VERIFY
	private static readonly Dictionary<String, (String TargetResult, String? TargetErrors)> testPrograms = new() { { """
		return ("7" * "2", "7" * 2, 7 * "2", "7" / "2", "7" / 2, 7 / "2", "7" % "2", "7" % 2, 7 % "2", "7" - "2", "7" - 2, 7 - "2");

		""", ("""(null, "77", "2222222", null, null, null, null, null, null, null, null, null)""", @"Error 4008 in line 1 at position 12: the string cannot be multiplied by the string
Error 4009 in line 1 at position 41: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 59: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 81: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 88: the strings cannot be divided or give the remainder (%)
Error 4007 in line 1 at position 99: the strings cannot be subtracted
Error 4007 in line 1 at position 110: the strings cannot be subtracted
Error 4007 in line 1 at position 117: the strings cannot be subtracted
") }, { """
		var a = 7;
		var b = 2;
		var aq = "7";
		var bq = "2";
		return (aq * bq, aq * b, a * bq, aq / bq, aq / b, a / bq, aq % bq, aq % b, a % bq, aq - bq, aq - b, a - bq);

		""", ("""(null, "77", "2222222", null, null, 0, null, null, 0, null, null, null)""", @"Error 4008 in line 5 at position 11: the string cannot be multiplied by the string
Error 4009 in line 5 at position 36: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 45: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 61: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 77: the strings cannot be divided or give the remainder (%)
Error 4007 in line 5 at position 86: the strings cannot be subtracted
Error 4007 in line 5 at position 95: the strings cannot be subtracted
Error 4007 in line 5 at position 102: the strings cannot be subtracted
") }, { """
		return (("A", 77777, 3.14159) + 5, ("A", 77777, 3.14159) - 5, ("A", 77777, 3.14159) * 5, ("A", 77777, 3.14159) / 5, ("A", 77777, 3.14159) % 5);
		
		""", ("""(null, null, null, null, null)""", @"Error 4006 in line 1 at position 30: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 57: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 84: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 111: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 138: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
") }, { """
		return (5 + ("A", 77777, 3.14159), 5 - ("A", 77777, 3.14159), 5 * ("A", 77777, 3.14159), 5 / ("A", 77777, 3.14159), 5 % ("A", 77777, 3.14159));
				
		""", ("""(null, null, null, 0, 0)""", """
			Error 4006 in line 1 at position 10: cannot apply the operator "+" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 37: cannot apply the operator "-" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 64: cannot apply the operator "*" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 91: cannot apply the operator "/" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 118: cannot apply the operator "%" to the types "byte" and "(string, int, real)"

			""") }, { """
		return (5 + null, 5 - null, 5 * null, 5 / null, 5 % null, null + 5, null - 5, null * 5, null / 5, null % 5);
						
		""", ("""(5, 5, 0, 0, 0, 5, -5, 0, 0, 0)""", @"Error 4006 in line 1 at position 40: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 1 at position 50: cannot apply the operator ""%"" to the types ""byte"" and ""null""
") }, { """
		var a = ("A", 77777, 3.14159);
		var b = 5;
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a);
								
		""", ("""(null, null, null, null, null, null, null, null, 0, 0)""", @"Error 4006 in line 3 at position 10: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 17: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 24: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 45: cannot apply the operator ""+"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 52: cannot apply the operator ""-"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 59: cannot apply the operator ""*"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 66: cannot apply the operator ""/"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 73: cannot apply the operator ""%"" to the types ""byte"" and ""(string, int, real)""
") }, { """return (sin "Infty", tan "Uncty", asin "2.71828", acos "-42", ln "-5", 1000000000000!, Infty!, 2.5!);""",
			("null", @"Error 4002 in line 1 at position 8: cannot apply this operator to this constant
Error 4002 in line 1 at position 21: cannot apply this operator to this constant
Error 4002 in line 1 at position 34: cannot apply this operator to this constant
Error 4002 in line 1 at position 50: cannot apply this operator to this constant
Error 4002 in line 1 at position 62: cannot apply this operator to this constant
Error 4003 in line 1 at position 84: cannot compute factorial of this constant
Error 4003 in line 1 at position 92: cannot compute factorial of this constant
Error 4003 in line 1 at position 98: cannot compute factorial of this constant
") }, { """
		var a = 5;
		var b = null;
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a);
								
		""", ("""(5, 5, 0, 0, 0, 5, -5, 0, 0, 0)""", @"Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""byte"" and ""null""
") }, { @"return (IntToReal(5), IntToReal(77777), IntToReal(777777777777));
", (@"(5, 77777, 777777777777)", "Ошибок нет") }, { @"var a = 5;
var b = 3;
return (a / b, IntToReal(a) / b);
", (@"(1, 1.6666666666666667)", "Ошибок нет") }, { @"var x = 5;
var y = 3;
var a = x > y;
var b = x < y + 2;
var c = x > y && x < y + 2;
var d = x > y || x < y + 2;
return (a, b, c, d);
", (@"(true, false, false, true)", "Ошибок нет") }, { @"return DateTime.IsLeapYear(2025) ? -1234567890 : 2345678901;
", ("2345678901", "Ошибок нет") }, { @"var a = DateTime.IsLeapYear(2025) ? -1234567890 : 2345678901;
return a;
", ("null", @"Error 4041 in line 1 at position 48: there is no implicit conversion between the types ""int"" and ""unsigned int""
") }, { @"list() int list = (5, 8);
var a = DateTime.IsLeapYear(2025) ? list : DateTime.IsLeapYear(2024) ? 12 : 20;
return a;
", (@"(12)", "Ошибок нет") }, { @"list() int list = (5, 8);
var a = DateTime.IsLeapYear(2025) ? (DateTime.IsLeapYear(2024) ? 12 : 20) : list;
return a;
", (@"(5, 8)", "Ошибок нет") }, { @"var a = 1 ?> 2 : 3 ?> 2 : 1;
return a;
", ("3", "Ошибок нет") }, { @"return ""A"" ?= ""B"" : ""C"";
", (@"""C""", "Ошибок нет") }, { @"var a = ""A"" ?= ""B"" : ""C"";
return a;
", (@"""C""", "Ошибок нет") }, { @"return ""A"" ?!= ""B"" : ""C"";
", (@"""A""", "Ошибок нет") }, { @"var a = ""A"" ?!= ""B"" : ""C"";
return a;
", (@"""A""", "Ошибок нет") }, { @"return ""A"" ?> ""B"" : ""C"";
", ("null", @"Error 4006 in line 1 at position 11: cannot apply the operator ""?>"" to the types ""string"" and ""string""
") }, { @"var a = ""A"" ?> ""B"" : ""C"";
return a;
", ("null", @"Error 4006 in line 1 at position 12: cannot apply the operator ""?>"" to the types ""string"" and ""string""
") }, { @"return 3 ?> 2 : ""A"";
", ("3", "Ошибок нет") }, { @"var a = 3 ?> 2 : ""A"";
return a;
", ("null", @"Error 4041 in line 1 at position 15: there is no implicit conversion between the types ""byte"" and ""string""
") }, { @"real Function F(real x, real y)
{
	return x * x + x * y + y * y;
}
System.Func[real, real, real] f;
f = F;
real a = f(3.14159, 2.71828);
f = Max;
real b = f(3.14159, 2.71828);
return (a, b);
", (@"(25.798355151699997, 3.14159)", "Ошибок нет") }, { @"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass a1 = new MyClass();
MyClass a2 = new MyClass(8, 2.71828, ""$"");
MyClass a3 = new MyClass(8, 2.71828);
MyClass a4 = new MyClass(true);
return (a1, a2, a3, a4);
", ("""(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	Namespace MyNamespace
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";

			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace.MyClass a1 = new MyNamespace.MyNamespace.MyClass();
MyNamespace.MyNamespace.MyClass a2 = new MyNamespace.MyNamespace.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace.MyClass a3 = new MyNamespace.MyNamespace.MyClass(8, 2.71828);
MyNamespace.MyNamespace.MyClass a4 = new MyNamespace.MyNamespace.MyClass(true);
return (a1, a2, a3, a4);
", ("""(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	abstract Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
			
			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace2.MyClass a1 = new MyNamespace.MyNamespace2.MyClass();
MyNamespace.MyNamespace2.MyClass a2 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace2.MyClass a3 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828);
MyNamespace.MyNamespace2.MyClass a4 = new MyNamespace.MyNamespace2.MyClass(true);
return (a1, a2, a3, a4);
", ("""(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	static Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
			
			Constructor(bool bool)
			{
				if (bool)
					a = 12;
			}
		}
	}
}
MyNamespace.MyNamespace2.MyClass a1 = new MyNamespace.MyNamespace2.MyClass();
MyNamespace.MyNamespace2.MyClass a2 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828, ""$"");
MyNamespace.MyNamespace2.MyClass a3 = new MyNamespace.MyNamespace2.MyClass(8, 2.71828);
MyNamespace.MyNamespace2.MyClass a4 = new MyNamespace.MyNamespace2.MyClass(true);
return (a1, a2, a3, a4);
", ("""(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет") }, { @"(int, int)[2] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", ("((5, 8), (5, 8))", "Ошибок нет") }, { @"Class MyClass
{
	int Function F1()
	{
		return 0;
	}
	
	int Function F2(int n)
	{
		return n * n;
	}
	
	static int Function G1()
	{
		return 0;
	}
	
	static int Function G2(int n)
	{
		return n * n;
	}
}
int Function F1()
{
	return 0;
}

int Function F2(int n)
{
	return n * n;
}
var a = new MyClass();
return (F1(10), F2(10, 10), F2(10.01), a.F1(10), a.F2(10, 10), a.F2(10.01), MyClass.G1(10), MyClass.G2(10, 10), MyClass.G2(10.01));
", ("null", @"Error 4022 in line 33 at position 11: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 19: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 31: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 44: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 54: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 68: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 87: the function ""G1"" must have 0 parameters
Error 4022 in line 33 at position 103: the function ""G2"" must have 1 parameters
Error 4027 in line 33 at position 123: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
") }, { @"real Function Factorial (int x)
{
	if (x <= 0)
		return 1;
	else
		return x * Factorial(x - 1);
}
return Factorial(100);", ("9.33262154439441E+157", "Ошибок нет") }, { @"int n = 0;
while (n < 1000)
{
	n++;
}
return n;", ("1000", "Ошибок нет") }, { @"int n = 0;
for (int i in Chain(1, 1000))
{
	n++;
}
return n;", ("1000", "Ошибок нет") }, { @"real a = 0;
loop
{
	if !(a >= 10)
		a++;
	else if !(a >= 12)
	{
		a += 0.25;
		continue;
	}
	else if !(a > null)
		continue;
	else
		break;
}
return a;
", ("12", "Ошибок нет") }, { @"list(3) int a = (((1, 2, 3), (4, 5, 6), (7, 8, 9)), ((10, 11, 12), (13, 14, 15), (16, 17, 18)), ((19, 20, 21), (22, 23, 24), (25, 26, 27)));
return a[1, 2, 3];
", ("6", "Ошибок нет") }, { @"list() (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list() (list() (string, int, real), list() (string, int, real),
	list() (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list() (list() (list() (string, int, real), list() (string, int, real), list() (string, int, real)),
	list() (list() (string, int, real), list() (string, int, real), list() (string, int, real)),
	list() (list() (string, int, real), list() (string, int, real), list() (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", ("""((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет") },
		{ @"using System.Collections;
ListHashSet[string] hs = new ListHashSet[string](3, ""A"", ""B"", ""C"");
hs.Add(""B"");
return hs[2];
", (@"""B""", "Ошибок нет") }, { @"using System.Collections;
ListHashSet[int] hs = new ListHashSet[int](3, 5, 10, 15);
hs.Add(10);
return hs[2];
", ("10", "Ошибок нет") }, { @"int a = 3.14159;
byte b = 77777;
real c = ""2.71828"";
return (a, b, c);
", ("null", @"Error 4027 in line 1 at position 6: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 2 at position 7: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 3 at position 7: cannot convert from the type ""string"" to the type ""real""
Error 4001 in line 4 at position 8: the identifier ""a"" is not defined in this location
Error 4001 in line 4 at position 11: the identifier ""b"" is not defined in this location
Error 4001 in line 4 at position 14: the identifier ""c"" is not defined in this location
") }, { @"int a = 0;
byte b = 0;
real c = 0;
a = 3.14159;
b = 77777;
c = ""2.71828"";
return (a, b, c);
", (@"(0, 0, 0)", @"Error 4027 in line 4 at position 2: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 5 at position 2: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 6 at position 2: cannot convert from the type ""string"" to the type ""real""
") }, { @"list() int list = (0);
return (list.Dispose(10), Fibonacci(10, 10), Fibonacci(""10""), Fibonacci(10.01));
", ("null", @"Error 4022 in line 2 at position 21: the function ""Dispose"" must have 0 parameters
Error 4022 in line 2 at position 36: the function ""Fibonacci"" must have 1 parameters
Error 4026 in line 2 at position 55: incompatibility between the type of the parameter of the call ""string"" and the type of the parameter of the function ""int""
Error 4027 in line 2 at position 72: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
") }, { @"bool bool=bool;
", ("null", @"Error 4012 in line 1 at position 10: one cannot use the local variable ""bool"" before it is declared or inside such declaration in line 1 at position 0
") }, { @"bool Function One()
{
	int Function Two()
	{
		return -1;
	}
	return Two();
}
return One();
", ("false", @"Warning 800A in line 7 at position 8: the type of the returning value ""int"" and the function return type ""bool"" are badly compatible, you may lost data
") }, { @"System.Func[int] Function F()
{
	int Function F2()
	{
		return 100;
	}
	return F2;
}
return F()();
", ("100", "Ошибок нет") }, { @"Class MyClass /{Class - с большой буквы
{
   unsigned int Function F1(unsigned int n) /{Слово Function обязательно
   {
	  return n * 2;
   }
 
   null Function F2()
   {
	  return null; //Просто ""return;"" не катит
   }
}
", ("null", @"Wreck 9006 in line 13 at position 0: unclosed 2 nested comments in the end of code
") }, { @"return /""Hello, world!""ssssssssssssssss\;
", ("null", @"Wreck 9004 in line 2 at position 0: unexpected end of code reached; expected: 1 pairs ""double quote - reverse slash"" (starting with quote)
") }, { @"return /""Hello, world!/""\;
", (@"""Hello, world!/""", "Ошибок нет") }, { @"return /""Hell@""/""o, world!""\;
", (@"/""Hell@""/""o, world!""\", "Ошибок нет") }, { @"return /""Hell@""/{""o, world!""\;
", (@"/""Hell@""/{""o, world!""\", "Ошибок нет") }, { @"return /""Hell@""\""""\""o, world!""\;
", (@"/""Hell@""\""""\""o, world!""\", "Ошибок нет") }, { @"using System;
real Function F(int n)
{
	return 1r / n;
}
list() real Function Calculate(Func[real, int] function)
{
	return (function(5), function(8), function(13));
}
return Calculate(F);
", (@"(0.2, 0.125, 0.07692307692307693)", "Ошибок нет") }, { @"list(3) int list = 8;
list = 123;
return list;
", (@"(((123)))", "Ошибок нет") }, { @"var x = DateTime.UTCNow.IsSummertime();
return x ^ x;
", (@"false", "Ошибок нет") }, { @"var x = 5;
return 5 pow x += 3;
", ("null", @"Error 201D in line 2 at position 15: only the variables can be assigned
") }, { @"return ;
", ("null", @"Warning 8002 in line 1 at position 7: the syntax ""return;"" is deprecated; consider using ""return null;"" instead
") }, { @"null Function F(list() int n)
{
	n++;
}
int a = 5;
F(a);
F(a);
F(a);
return a;
", ("5", @"Error 4005 in line 3 at position 2: cannot apply the operator ""postfix ++"" to the type ""list() int""
") }, { @"var a = false;
a++;
return a;
", (@"true", "Ошибок нет") }, { @"list() int list = (123, 456, 789, 111, 222, 333, 444, 555, 777);
return list;
", (@"(123, 456, 789, 111, 222, 333, 444, 555, 777)", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list;
", (@"(1, 2, 3, 4, 5, 6, 7)", "Ошибок нет") }, { @"var a = false;
var b = 5;
return a + b;
", ("5", "Ошибок нет") }, { @"var a = false;
var b = 5;
return a * b;
", ("null", @"Error 4006 in line 3 at position 9: cannot apply the operator ""*"" to the types ""bool"" and ""byte""
") }, { @"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs;
", ("""("1", "2", "3")""", "Ошибок нет") }, { @"using System.Collections;
var hs = new ListHashSet[int]();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;
", ("""(1, 2, 3)""", "Ошибок нет") }, { @"using System.Collections;
var dic = new Dictionary[string, int]();
dic.TryAdd(""1"", 1);
dic.TryAdd(""2"", 2);
dic.TryAdd(""3"", 3);
return dic;
", ("""(("1", 1), ("2", 2), ("3", 3))""", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.IndexOf(2, 2);
", ("2", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.LastIndexOf(2, 1);
", ("0", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Remove(2, 3);
", (@"(1, 5, 6, 7)", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveAt(5);
", (@"(1, 2, 3, 4, 6, 7)", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveEnd(5);
", (@"(1, 2, 3, 4)", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Reverse(2, 3);
", (@"(1, 4, 3, 2, 5, 6, 7)", "Ошибок нет") }, { @"list() string list = ""1"";
list.Add(""2"");
list.Add("""");
list.Add(""2"");
return list;
", ("""("1", "2", "", "2")""", "Ошибок нет") }, { @"list() string list = """";
list.Add(""1"");
list.Add("""");
list.Add(""2"");
return list;
", ("""("", "1", "", "2")""", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.IndexOf(2, 2);
", ("2", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.LastIndexOf(2, 1);
", ("0", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Remove(2, 3);
", (@"(1, 5, 6, 7)", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveAt(5);
", (@"(1, 2, 3, 4, 6, 7)", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveEnd(5);
", (@"(1, 2, 3, 4)", "Ошибок нет") }, { @"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Reverse(2, 3);
", (@"(1, 4, 3, 2, 5, 6, 7)", "Ошибок нет") }, { @"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add("""");
hs.Add(""2"");
return hs;
", ("""("1", "2", "")""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass
{
	Constructor(string s) {}
}
string Function F()
{
	return 5;
}
int Function G(string s)
{
	return 0;
}
string a = 8;
return (G(12), new ListHashSet[string](1, ""A"", 10), new MyClass(77777));
", ("null", @"Error 4039 in line 8 at position 8: incompatibility between the type of the returning value ""byte"" and the function return type ""string"" - use an addition of zero-length string for this
Error 4014 in line 14 at position 9: cannot convert from the type ""byte"" to the type ""string"" - use an addition of zero-length string for this
Error 4026 in line 15 at position 10: incompatibility between the type of the parameter of the call ""byte"" and the type of the parameter of the function ""string"" - use an addition of zero-length string for this
Error 4036 in line 15 at position 47: incompatibility between the type of the parameter of the call ""byte"" and the type of the parameter of the constructor ""System.Collections.IEqualityComparer[string]""
Error 4036 in line 15 at position 64: incompatibility between the type of the parameter of the call ""int"" and the type of the parameter of the constructor ""string"" - use an addition of zero-length string for this
") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
var hs = new MyClass();
hs.Add("1");
hs.Add("2");
hs.Add("3");
hs.Add("2");
return hs;

""", ("""("1", "2", "3")""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", ("""("1", "2", "3")""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().Length;

""", ("3", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().RemoveAt(2);

""", ("""("1", "3")""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
var hs = new MyClass2();
hs.Add("1");
hs.Add("2");
hs.Add("3");
hs.Add("2");
return hs;

""", ("""("1", "2", "3")""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", ("""("1", "2", "3")""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().Length;
", ("3", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().RemoveAt(2);
", ("""("1", "3")""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
var hs = new MyClass2();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs;
", ("""("1", "2", "3")""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F();
", ("""("1", "2", "3")""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(""1"");
	hs.Add(""2"");
	hs.Add(""3"");
	hs.Add(""2"");
	return hs;
}
return F().Length;
", ("3", "Ошибок нет") }, { """
using System.Collections;
Class MyClass : ListHashSet[string]
{
}
Class MyClass2 : ListHashSet[string]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F().RemoveAt(2);

""", ("""("1", "3")""", "Ошибок нет") }, { """
using System.Collections;
abstract Class MyClass : ListHashSet[string]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add("1");
	hs.Add("2");
	hs.Add("3");
	hs.Add("2");
	return hs;
}
return F();

""", ("null", """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
var hs = new MyClass();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;

""", ("""(1, 2, 3)""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", ("""(1, 2, 3)""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;

""", ("3", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);

""", ("""(1, 3)""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
var hs = new MyClass2();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;

""", ("""(1, 2, 3)""", "Ошибок нет") }, { """
using System.Collections;
			
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", ("""(1, 2, 3)""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;
", ("3", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : MyClass
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);
", ("""(1, 3)""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
var hs = new MyClass2();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;
", ("""(1, 2, 3)""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();
", ("""(1, 2, 3)""", "Ошибок нет") }, { @"using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().Length;
", ("3", "Ошибок нет") }, { """
using System.Collections;
Class MyClass : ListHashSet[int]
{
}
Class MyClass2 : ListHashSet[int]
{
}
MyClass2 Function F()
{
	var hs = new MyClass2();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F().RemoveAt(2);

""", ("""(1, 3)""", "Ошибок нет") }, { """
using System.Collections;
abstract Class MyClass : ListHashSet[int]
{
}
MyClass Function F()
{
	var hs = new MyClass();
	hs.Add(1);
	hs.Add(2);
	hs.Add(3);
	hs.Add(2);
	return hs;
}
return F();

""", ("null", """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""") }, { @"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass a1 = new MyClass2();
MyClass a2 = new MyClass2(8, 2.71828, ""$"");
MyClass a3 = new MyClass2(8, 2.71828);
MyClass a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	Namespace MyNamespace
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	static Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace2.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Namespace MyNamespace
{
	sealed Class MyNamespace2
	{
		Class MyClass
		{
			int a = 5;
			real b = 3.14159;
			string c = ""A"";
		}
	}
}

Class MyClass2 : MyNamespace.MyNamespace2.MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"Class MyClass
{
	int a = 5;
	real b = 3.14159;
}

Class MyClass2 : MyClass
{
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет") }, { @"static Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("""(new MyClass2(), null, null, new MyClass2())""", @"Error 2015 in line 8 at position 17: expected: non-sealed class or interface
Error 4035 in line 17 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
Error 4035 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
") }, { @"Class MyClass
{
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}

static Class MyClass2 : MyClass
{
	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", ("null", @"Error 0009 in line 8 at position 22: a static class cannot be derived
Error 2024 in line 16 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 17 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 18 at position 18: cannot create an instance of the static type ""MyClass2""
Error 2024 in line 19 at position 18: cannot create an instance of the static type ""MyClass2""
Error 4000 in line 16 at position 26: internal compiler error
Error 4000 in line 17 at position 26: internal compiler error
Error 4000 in line 18 at position 26: internal compiler error
Error 4000 in line 19 at position 26: internal compiler error
Error 4001 in line 20 at position 8: the identifier ""a1"" is not defined in this location
Error 4001 in line 20 at position 12: the identifier ""a2"" is not defined in this location
Error 4001 in line 20 at position 16: the identifier ""a3"" is not defined in this location
Error 4001 in line 20 at position 20: the identifier ""a4"" is not defined in this location
") }, { @"Class MyClass
{
	static Class N
	{
		MyClass S = new MyClass();
	}
	int a = 5;
	real b = 3.14159;
	string c = ""A"";
}
return (MyClass.N.S);
", (@"new MyClass(5, 3.14159, ""A"")", "Ошибок нет") }, { @"Class MyClass2 : MyClass
{
	string c = ""A"";

	Constructor(bool bool)
	{
		if (bool)
			a = 12;
	}
}

Class MyClass
{
	int a = 5;
	real b = 3.14159;
}
MyClass2 a1 = new MyClass2();
MyClass2 a2 = new MyClass2(8, 2.71828, ""$"");
MyClass2 a3 = new MyClass2(8, 2.71828);
MyClass2 a4 = new MyClass2(true);
return (a1, a2, a3, a4);
", (@"(new MyClass2(5, 3.14159, ""A""), null, new MyClass2(8, 2.71828, ""A""), new MyClass2(12, 3.14159, ""A""))",
	@"Error 4035 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 2 parameters
") }, { @"Class Person
{
	closed string name;
	closed int age;

	string Function GetName()
	{
		return name;
	}

	int Function GetAge()
	{
		return age;
	}
}

 Person person = new Person(""Alice"", 30);
 return (person.GetName(), person.GetAge());
", (@"(""Alice"", 30)", "Ошибок нет") }, { @"Class Animal
{
	protected string species;

	string Function GetSpecies()
	{
		return species;
	}

	string Function Speak()
	{
		return ""Animal sound"";
	}
}

Class Dog : Animal
{
	Constructor()
	{
		species = ""Dog"";
	}

	string Function Bark()
	{
		return ""Woof!"";
	}
}

Dog dog = new Dog();
return (dog.GetSpecies(), dog.Speak(), dog.Bark());
", (@"(""Dog"", ""Animal sound"", ""Woof!"")", "Ошибок нет") }, { @"Class BankAccount
{
	closed real balance;

	Constructor(real initialBalance)
	{
		balance = initialBalance;
	}

	null Function Deposit(real amount)
	{
		if (amount > 0)
			balance += amount;
	}

	null Function Withdraw(real amount)
	{
		if (amount > 0 && amount <= balance)
			balance -= amount;
	}

	real Function GetBalance()
	{
		return balance;
	}
}

BankAccount account = new BankAccount(1000);
account.Deposit(500);
account.Withdraw(200);
return account.GetBalance();
", ("1300", "Ошибок нет") }, { @"Class Vehicle
{
	string Function Start()
	{
		return ""Vehicle starting"";
	}
}

Class Car : Vehicle
{
	string Function Start()
	{
		return ""Car starting"";
	}
}

Vehicle vehicle = new Car();
return vehicle.Start();
", (@"""Car starting""", "Ошибок нет") }, { @"Class Engine
{
	string Function Start()
	{
		return ""Engine starting"";
	}
}

Class Car
{
	closed Engine engine = new Engine();

	string Function Start()
	{
		return engine.Start() + ""\r\nCar is now running"";
	}
}

Car car = new Car();
return car.Start();
", (@"""Engine starting\r\nCar is now running""", "Ошибок нет") }, { @"Class BaseClass
{
	string Function Display()
	{
		return ""Display from BaseClass"";
	}

	string Function Info()
	{
		return ""Info from BaseClass"";
	}
}

Class DerivedClass : BaseClass
{
	string Function Display()
	{
		return ""Display from DerivedClass"";
	}

	new string Function Info()
	{
		return ""Info from DerivedClass"";
	}
}

BaseClass obj1 = new DerivedClass();
DerivedClass obj2 = new DerivedClass();
return (obj1.Display(), obj1.Info(), obj2.Display(), obj2.Info());
", ("""("Display from DerivedClass", "Info from BaseClass", "Display from DerivedClass", "Info from DerivedClass")""",
			"Ошибок нет") }, { @"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

Class Dog : Animal
{
	string Function Speak()
	{
		return ""Woof"";
	}

	string Function Eat()
	{
		return ""Dog is eating"";
	}
}

Class Cat : Animal
{
	string Function Speak()
	{
		return ""Meow"";
	}

	// Не переопределяем метод Eat, используем базовую реализацию
}

Animal myDog = new Dog();
Animal myCat = new Cat();
return (myDog.Speak(), myDog.Eat(), myCat.Speak(), myCat.Eat());
", ("""("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Ошибок нет") }, { @"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

Class Dog : Animal
{
	string Function Speak()
	{
		return ""Woof"";
	}

	string Function Eat()
	{
		return ""Dog is eating"";
	}
}

Class Cat : Animal
{
	string Function Speak()
	{
		return ""Meow"";
	}

	list() string Function Eat()
	{
		return ""Cat is eating"";
	}
}

Animal myDog = new Dog();
Animal myCat = new Cat();
return (myDog.Speak(), myDog.Eat(), myCat.Speak(), myCat.Eat());
", ("""("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Warning 8008 in line 30 at position 1: the method \"Eat\"" +
			" has the same parameter types as its base method with the same name but it also" +
			" has the other significant differences such as the access modifier or the return type," +
			" so it cannot override that base method and creates a new one;" +
			" if this is intentional, and the \"new\" keyword, otherwise fix the differences\r\n") }, { @"Class MyClass
{
	abstract string Function Go();
}
", ("null", @"Error 400A in line 3 at position 10: the abstract members can be located only inside the abstract classes
") }, { @"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(ref a);
F(ref a);
F(ref a);
return a;
", ("8", "Ошибок нет") }, { @"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(a);
F(a);
F(a);
return a;
", ("null", @"Wreck 9013 in line 6 at position 2: this parameter must pass with the ""ref"" keyword
") }, { @"int Function F(real n)
{
	return Truncate(n * n);
}
return Fill(F(3.14159) >= 10, 1000);
", ('(' + string.Join(", ", RedStarLinq.FillArray("false", 1000)) + ')', "Ошибок нет") }, { @"string s = """";
repeat (1000000)
	s += 'A';
return s;
", ('\"' + new string('A', 1000000) + '\"', "Ошибок нет") }, { @"using System;
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.FrequencyTable(list);
", (@"((2, 3), (3, 1), (1, 3))", "Ошибок нет") }, { @"using System;
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.GroupIndexes(list);
", (@"((0, 1, 5), (2), (3, 4, 6))", "Ошибок нет") }, { @"using System;
real Function Reciproc(int x)
{
	return 1r / x;
}
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.GroupIndexes(list, Reciproc);
", (@"((0, 1, 5), (2), (3, 4, 6))", "Ошибок нет") }, { @"using System;
list() int list = (5, 10, 15, 20, 25);
return RedStarLinq.ToList(list, x => x * x);
", (@"(25, 100, 225, 400, 625)", "Ошибок нет") }, { @"using System;
Func[real, real] f = x => x * x;
return f(100);
", ("10000", "Ошибок нет") }, { @"using System;
list() Func[real, real] list = (x => x * x, x => 1 / x, x => E pow x);
return (list[1](3.14), list[2](3.14), list[3](3.14), list[1](-5), list[2](-5), list[3](-5));
", (@"(9.8596, 0.3184713375796178, 23.10386685872218, 25, -0.2, 0.006737946999085469)", "Ошибок нет") }, { @"using System;
Func[real, real] f = x =>
{
	return x * x;
};
return f(100);
", ("10000", "Ошибок нет") }, { @"using System;
Func[real, real] f = x =>
{
	if (x >= 0)
		return x * x;
	else
		return -x * x;
};
return (f(100), f(-5));
", (@"(10000, -25)", "Ошибок нет") }, { @"return 100000000000000000*100000000000000000000;
", ("0", @"Error 0001 in line 1 at position 26: too large number; long long type is under development
") }, { @"return ExecuteString(""return args[1];"", Q());
", ("""
/"return ExecuteString("return args[1];", Q());
"\
""", "Ошибок нет") }, { """var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();""",
			("""/"var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();"\""",
			"Ошибок нет") }, { @"int x=null;
return x*1;
", ("0", "Ошибок нет") }, { @"return куегкт;
", ("null", @"Error 4001 in line 1 at position 7: the identifier ""куегкт"" is not defined in this location
") }, { """
real Function D(real[3] abc)
{
	return abc[2] * abc[2] - 4 * abc[1] * abc[3];
}
string Function DecomposeSquareTrinomial(real[3] abc)
{
	if (abc[1] == 0)
		return "Это не квадратный трехчлен";
	var d = D(abc);
	string first;
	if (abc[1] == 1)
		first = "";
	else if (abc[1] == -1)
		first = "-";
	else
		first = abc[1] + "";
	if (d < 0)
		return "Неразложимо";
	else if (d == 0)
		return first + Format(abc[2] / (2 * abc[1])) + '²';
	else
	{
		var sqrtOfD = Sqrt(d);
		return first + Format((abc[2] + sqrtOfD) / (2 * abc[1])) + Format((abc[2] - sqrtOfD) / (2 * abc[1]));
	}
}
string Function Format(real n)
{
	if (n == 0)
		return "x";
	else if (n < 0)
		return "(x - " + (-n) + ")";
	else
		return "(x + " + n + ")";
}
return (DecomposeSquareTrinomial((3, 9, -30)), DecomposeSquareTrinomial((1, 16, 64)),
	DecomposeSquareTrinomial((-1, -1, -10)), DecomposeSquareTrinomial((0, 11, 5)),
	DecomposeSquareTrinomial((-1, 0, 0)), DecomposeSquareTrinomial((2, -11, 0)));

""", ("""("3(x + 5)(x - 2)", "(x + 8)²", "Неразложимо", "Это не квадратный трехчлен", "-x²", "2x(x - 5.5)")""", "Ошибок нет") } };
#endif

	public MainView()
	{
		CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		InitializeComponent();
		TextBoxInput.Options.EnableTextDragDrop = true;
		TextBoxInput.AddHandler(PointerReleasedEvent, TextBoxInput_PointerReleased, handledEventsToo: true);
		TextBoxInput.TextArea.TextEntering += TextBoxInput_TextArea_TextEntering;
		TextBoxInput.TextArea.TextEntered += TextBoxInput_TextArea_TextEntered;
		using (var stream = new MemoryStream(NStar.Resources.SyntaxHighlighting))
		{
			using var reader = new System.Xml.XmlTextReader(stream);
			TextBoxInput.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
			var spans = TextBoxInput.SyntaxHighlighting.MainRuleSet.Spans;
			var nestedCommentSpans = spans.FindAll(x => x.SpanColor.Foreground.ToString()?.Contains("#ffbfbf00") ?? false);
			nestedCommentSpans[^1].RuleSet.Spans.AddRange(nestedCommentSpans);
			var rules = TextBoxInput.SyntaxHighlighting.MainRuleSet.Rules;
			var stringRule = rules.Find(x => x.Color.Foreground.ToString()?.Contains("#ffbf4000") ?? false);
			var stringSpans = spans.FindAll(x => x.SpanColor.Foreground.ToString()?.Contains("#ffbf4000") ?? false);
			stringSpans[^1].RuleSet.Rules.Add(stringRule ?? throw new InvalidOperationException());
			stringSpans[^1].RuleSet.Spans.AddRange(stringSpans);
			stringSpans[^1].RuleSet.Spans.AddRange(nestedCommentSpans);
		}
		_ = TextBoxInput.TextArea.TextView.GetDocumentLineByVisualTop(TextBoxInput.TextArea.TextView.ScrollOffset.Y).LineNumber;
#if RELEASE
		ButtonSaveExe.IsEnabled = false;
#endif
		TextBoxInput.Text = $"Loading ({0:F2}%)\r\n";

	}

	private void UserControl_Loaded(object? sender, RoutedEventArgs e) => Task.Factory.StartNew(async () =>
	{
		String result;
		var i = 0;
		foreach (var (Key, Value) in testPrograms)
		{
			await Dispatcher.UIThread.InvokeAsync(() =>
				TextBoxInput.Text = $"Loading ({Clamp((double)(i++ * 100 + random.Next(-50, 50))
				/ testPrograms.Length, 0, 100):F2}%)\r\n");
			if ((result = ExecuteProgram(Key, out var errors)) == Value.TargetResult
				&& (Value.TargetErrors == null || errors == Value.TargetErrors))
				continue;
			throw new Exception("Error: @\"" + Key.Replace("\"", "\"\"") + "\"" + (result == Value.TargetResult ? ""
				: " returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\""
				+ Value.TargetResult.Replace("\"", "\"\"") + "\"") + (Value.TargetErrors == null
				|| errors == Value.TargetErrors ? "" : " and produced errors @\"" + errors.Replace("\"", "\"\"")
				+ "\" instead of @\"" + Value.TargetErrors.Replace("\"", "\"\"") + "\"") + "!");
		}
		await Dispatcher.UIThread.InvokeAsync(() =>
		{
			TextBoxInput.Text = "return \"Hello, world!\";\r\n";
			ButtonExecute.IsEnabled = true;
			ButtonOpenCode.IsEnabled = true;
			ButtonSaveCode.IsEnabled = true;
		});
	}).Wait();

	private void UserControl_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
		ScrollViewerMain.Width = TopLevel.GetTopLevel(this)?.Width ?? 1024;
		ScrollViewerMain.Height = TopLevel.GetTopLevel(this)?.Height ?? 768;
		CopyrightsView.Width = TopLevel.GetTopLevel(this)?.Width ?? 1024;
		CopyrightsView.Height = TopLevel.GetTopLevel(this)?.Height ?? 768;
		TextBoxInput.Height = TextBoxInput.MaxHeight = TextBoxInput.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 5 / 13;
		ButtonExecute.Height = ButtonExecute.MaxHeight = ButtonExecute.MinHeight
			= ButtonSaveExe.Height = ButtonSaveExe.MaxHeight = ButtonSaveExe.MinHeight
			= ButtonOpenCode.Height = ButtonOpenCode.MaxHeight = ButtonOpenCode.MinHeight
			= ButtonSaveCode.Height = ButtonSaveCode.MaxHeight = ButtonSaveCode.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 1 / 13;
		TextBoxOutput.Height = TextBoxOutput.MaxHeight = TextBoxOutput.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 2 / 13;
		TextBoxErrors.Height = TextBoxErrors.MaxHeight = TextBoxErrors.MinHeight
			= (TopLevel.GetTopLevel(this)?.Height ?? 768) * 3 / 13;
	}

	private static string FunctionMappingBack(String function) => function.ToString() switch
	{
		nameof(function.AddRange) => "Add",
		nameof(DateTime.IsDaylightSavingTime) => "IsSummertime",
		_ => function.ToString(),
	};

	private static string PropertyMappingBack(String property) => property.ToString() switch
	{
		nameof(DateTime.UtcNow) => "UTCNow",
		_ => property.ToString(),
	};

	private void UpdateInputPos()
	{
		var line = TextBoxInput.Document.GetLineByOffset(TextBoxInput.SelectionStart).LineNumber;
		TextBlockLine.Text = "Line " + line;
		TextBlockPos.Text = "Pos " + (TextBoxInput.SelectionStart - TextBoxInput.Document.Lines[line - 1].Offset);
	}

	private void SetupEnteredText()
	{
		var textBeforeCursor = TextBoxInput.Text.AsSpan(..TextBoxInput.SelectionStart);
		var i = textBeforeCursor.Length - 1;
		for (; i >= 0; i--)
			if (!AlphanumericCharactersWithoutDot.Contains(textBeforeCursor[i]))
				break;
		if (i >= 0 && textBeforeCursor[i] == '.')
			enteredText.Replace(textBeforeCursor[i..]);
		else
			enteredText.Replace(textBeforeCursor[(i + 1)..]);
	}

	private void TextBoxInput_KeyUp(object? sender, KeyEventArgs e)
	{
		if (e.KeyModifiers == KeyModifiers.Control)
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				ButtonExecute_Click(ButtonExecute, e);
			}
			else if (e.Key is Key.Y or Key.Z)
			{
				UpdateInputPos();
				SetupEnteredText();
			}
		}
	}

	private void TextBoxInput_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		UpdateInputPos();
		SetupEnteredText();
		if (inputHighlightTarget >= 0)
		{
			TextBoxInput.SelectionStart = inputHighlightTarget;
			TextBoxInput.SelectionLength = 0;
			inputHighlightTarget = -1;
		}
	}

	private void TextBoxInput_TextChanged(object? sender, EventArgs e)
	{
		UpdateInputPos();
		compiledAssembly = null;
	}

	CompletionWindow? completionWindow;

	void TextBoxInput_TextArea_TextEntered(object? sender, TextInputEventArgs e)
	{
		// Open code completion after the user has pressed dot:
		completionWindow = new CompletionWindow(TextBoxInput.TextArea);
		var data = completionWindow.CompletionList.CompletionData;
		if (e.Text == ".")
			enteredText.Replace(e.Text);
		else
			enteredText.AddRange(e.Text ?? "");
		if (enteredText.StartsWith('.'))
			data.AddRange(AutoCompletionAfterDotList.Filter(x => x.StartsWith(enteredText[1..])).Convert(x =>
				new MyCompletionData(x.ToString(), enteredText.Length - 1)));
		else
			data.AddRange(AutoCompletionList.Filter(x => x.StartsWith(enteredText)).Convert(x =>
				new MyCompletionData(x.ToString(), enteredText.Length)));
		completionWindow.Show();
		completionWindow.Closed += (_, _) => completionWindow = null;
	}

	void TextBoxInput_TextArea_TextEntering(object? sender, TextInputEventArgs e)
	{
		if (e.Text?.Length > 0 && completionWindow != null)
		{
			if (!char.IsLetterOrDigit(e.Text[0]))
			{
				// Whenever a non-letter is typed while the completion window is open,
				// insert the currently selected element.
				completionWindow.CompletionList.RequestInsertion(e);
			}
		}
		// Do not set e.Handled=true.
		// We still want to insert the character that was typed.
	}

	private void TextBoxErrors_DoubleTapped(object? sender, TappedEventArgs e)
	{
		using var before = TextBoxErrors.Text?.ToNString().RemoveEnd(Min(TextBoxErrors.SelectionStart,
			TextBoxErrors.Text.Length));
		using var after = TextBoxErrors.Text?.ToNString().Skip(TextBoxErrors.SelectionStart).GetBefore("\r\n");
		if (before == null || after == null)
			return;
		before.AddRange(after);
		var line = before.GetAfterLast("\r\n");
		if (line.Length == 0)
			line.Replace(before);
		line.GetBeforeSetAfter(" in line ");
		if (line.Length == 0)
			return;
		using var lineN = line.GetBeforeSetAfter(" at position ");
		var position = line.GetBefore(": ");
		if (!(int.TryParse(lineN.ToString(), out var y) && int.TryParse(position.ToString(), out var x)))
			return;
		TextBoxInput.ScrollTo(y, x);
		inputHighlightTarget = TextBoxInput.Document.Lines[y - 1].Offset + x;
	}

	private void ButtonExecute_Click(object? sender, RoutedEventArgs e) => Execute();

	private void ButtonOpenCode_Click(object? sender, RoutedEventArgs e) => OpenCode();

	private void ButtonSaveCode_Click(object? sender, RoutedEventArgs e) => SaveCode();

	private void ButtonSaveExe_Click(object? sender, RoutedEventArgs e) => SaveExe();

	private void Copyrights_Click(object? sender, RoutedEventArgs e) => CopyrightsView.IsVisible = true;

	private void Execute()
	{
		TextBoxOutput.Text = TranslateAndExecuteProgram(TextBoxInput.Text, out var errors, out compiledAssembly).ToString();
		TextBoxErrors.Text = errors.ToString();
	}

	private async void OpenCode()
	{
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.OpenFilePickerAsync(new()
			{
				Title = "Select the C#.NStar code file",
				FileTypeFilter = [new("C#.NStar code files") { Patterns = ["*.n-star-pre-pre-i"], AppleUniformTypeIdentifiers = ["UTType.Item"], MimeTypes = ["multipart/mixed"] }],
			})!);
		if (fileResult?.Count == 0)
			return;
		var filename = fileResult?[0]?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(filename))
			return;
		String content;
		try
		{
			content = File.ReadAllText(filename);
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Произошла ошибка при попытке открыть файл. Вероятно, он был удален или используется другим приложением.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		var prefix = ".NStar Pre-Pre-I-";
		if (!content.StartsWith(prefix))
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Pre-Pre-I или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		content.Remove(0, prefix.Length);
		var versionIndex = 0;
		for (; versionIndex < minorVersions.Length; versionIndex++)
		{
			prefix = minorVersions[versionIndex] + '\n';
			if (content.StartsWith(prefix))
			{
				content.Remove(0, prefix.Length);
				break;
			}
		}
		if (versionIndex == minorVersions.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Pre-Pre-I совместимой ревизии или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		var langIndex = 0;
		for (; langIndex < langs.Length; langIndex++)
		{
			prefix = langs[langIndex] + '\n';
			if (content.StartsWith(prefix))
			{
				content.Remove(0, prefix.Length);
				break;
			}
		}
		if (langIndex == langs.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Pre-Pre-I совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		prefix = "<Project>\n";
		if (!content.StartsWith(prefix))
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Pre-Pre-I совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		content.Remove(0, prefix.Length);
		prefix = "\n</Project>\n";
		var settings = content.GetBefore(prefix);
		if (settings.Length + prefix.Length > content.Length)
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Ошибка! Файл не является кодом .NStar Pre-Pre-I совместимой ревизии на совместимом языке или поврежден.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
		TextBoxInput.Text = content.Remove(0, settings.Length + prefix.Length).ToString();
		Execute();
	}

	private async void SaveCode()
	{
		if (compiledAssembly == null)
		{
			TextBoxOutput.Text = "Чтобы сохранить код, сначала выполните программу!";
			return;
		}
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.SaveFilePickerAsync(new()
			{
				Title = "Select the path to save a C#.NStar code file",
				DefaultExtension = "n-star-pre-pre-i",
				SuggestedFileName = "Program",
			})!);
		var filename = fileResult?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(filename))
			return;
		try
		{
			File.WriteAllText(filename, ".NStar Pre-Pre-I-" + minorVersions[^1] + "\nC#\n<Project>\n\n</Project>\n" + TextBoxInput.Text);
		}
		catch
		{
			await Dispatcher.UIThread.InvokeAsync(async () =>
				await MessageBoxManager.GetMessageBoxStandard("",
				"Произошла ошибка при попытке сохранить файл. Вероятно, файл с таким именем"
				+ " используется другим приложением или у приложения нет прав на запись по этому пути.",
				MsBox.Avalonia.Enums.ButtonEnum.Ok).ShowAsPopupAsync(this));
			return;
		}
	}

	private async void SaveExe()
	{
		if (compiledAssembly == null)
		{
			TextBoxOutput.Text = "Чтобы сохранить EXE, сначала выполните программу!";
			return;
		}
		var fileResult = await Dispatcher.UIThread.InvokeAsync(async () =>
			await TopLevel.GetTopLevel(this)?.StorageProvider.SaveFilePickerAsync(new()
			{
				Title = "Select the path to save an EXE file",
				DefaultExtension = ".exe",
				SuggestedFileName = "Program.exe",
			})!);
		var filename = fileResult?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(filename))
			return;
		var dir = Path.GetDirectoryName(filename);
		foreach (var dependency in GetNecessaryDependencies(compiledAssembly.GetReferencedAssemblies().ToList(x => x.Name ?? throw new NotSupportedException())))
			File.WriteAllBytes(dir + "/" + dependency.Name + ".dll", dependency.Bytes);
		File.WriteAllText(dir + "/" + Path.GetFileNameWithoutExtension(filename) + ".runtimeconfig.json", @$"{{
	""runtimeOptions"": {{
		""tfm"": ""net{Environment.Version.Major}.{Environment.Version.Minor}"",
		""framework"": {{
			""name"": ""Microsoft.NETCore.App"",
			""version"": ""{Environment.Version.Major}.{Environment.Version.Minor}.{Environment.Version.Build}""
		}}
	}}
}}");
		File.WriteAllBytes(filename, CompileProgram(TextBoxInput.Text));
	}

	private class MyCompletionData(string text, int offset) : ICompletionData
	{
		public IImage Image => null!;

		public string Text { get; private set; } = text;

		// Use this property if you want to show a fancy UIElement in the list.
		public object Content => Text;

		public object Description => "Description for " + Text;

		public double Priority { get; }

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs) =>
			textArea.Document.Replace(completionSegment, Text[Min(offset, Text.Length)..]);
	}
}

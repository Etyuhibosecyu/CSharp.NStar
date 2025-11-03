global using System;
global using String = NStar.Core.String;
using static CSharp.NStar.SemanticTree;
using System.Globalization;

namespace CSharp.NStar.Tests;

[TestClass]
public class CSharpNStarTests
{
	private const string A10 = "AAAAAAAAAA";
	private const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
	private const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
	private const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
	private const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
	private const string A1000000 = "\"" + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + "\"";

	[TestMethod]
	[DataRow("""
		return ("7" * "2", "7" * 2, 7 * "2", "7" / "2", "7" / 2, 7 / "2", "7" % "2", "7" % 2, 7 % "2", "7" - "2", "7" - 2, 7 - "2");

		""", """(null, "77", "2222222", null, null, null, null, null, null, null, null, null)""", @"Error 4008 in line 1 at position 12: the string cannot be multiplied by the string
Error 4009 in line 1 at position 41: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 59: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 81: the strings cannot be divided or give the remainder (%)
Error 4009 in line 1 at position 88: the strings cannot be divided or give the remainder (%)
Error 4007 in line 1 at position 99: the strings cannot be subtracted
Error 4007 in line 1 at position 110: the strings cannot be subtracted
Error 4007 in line 1 at position 117: the strings cannot be subtracted
")]
	[DataRow("""
		var a = 7;
		var b = 2;
		var aq = "7";
		var bq = "2";
		return (aq * bq, aq * b, a * bq, aq / bq, aq / b, a / bq, aq % bq, aq % b, a % bq, aq - bq, aq - b, a - bq);

		""", """(null, "77", "2222222", null, null, 0, null, null, 0, null, null, null)""", @"Error 4008 in line 5 at position 11: the string cannot be multiplied by the string
Error 4009 in line 5 at position 36: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 45: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 52: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 61: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 70: the strings cannot be divided or give the remainder (%)
Error 4009 in line 5 at position 77: the strings cannot be divided or give the remainder (%)
Error 4007 in line 5 at position 86: the strings cannot be subtracted
Error 4007 in line 5 at position 95: the strings cannot be subtracted
Error 4007 in line 5 at position 102: the strings cannot be subtracted
")]
	[DataRow("""
		return (("A", 77777, 3.14159) + 5, ("A", 77777, 3.14159) - 5, ("A", 77777, 3.14159) * 5, ("A", 77777, 3.14159) / 5, ("A", 77777, 3.14159) % 5);
		
		""", """(null, null, null, null, null)""", @"Error 4006 in line 1 at position 30: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 57: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 84: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 111: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 1 at position 138: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
")]
	[DataRow("""
		return (5 + ("A", 77777, 3.14159), 5 - ("A", 77777, 3.14159), 5 * ("A", 77777, 3.14159), 5 / ("A", 77777, 3.14159), 5 % ("A", 77777, 3.14159));
				
		""", """(null, null, null, 0, 0)""", """
			Error 4006 in line 1 at position 10: cannot apply the operator "+" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 37: cannot apply the operator "-" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 64: cannot apply the operator "*" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 91: cannot apply the operator "/" to the types "byte" and "(string, int, real)"
			Error 4006 in line 1 at position 118: cannot apply the operator "%" to the types "byte" and "(string, int, real)"

			""")]
	[DataRow("""
		return (5 + null, 5 - null, 5 * null, 5 / null, 5 % null, null + 5, null - 5, null * 5, null / 5, null % 5);
						
		""", """(5, 5, 0, 0, 0, 5, -5, 0, 0, 0)""", @"Error 4006 in line 1 at position 40: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 1 at position 50: cannot apply the operator ""%"" to the types ""byte"" and ""null""
")]
	[DataRow("""
		var a = ("A", 77777, 3.14159);
		var b = 5;
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a);
								
		""", """(null, null, null, null, null, null, null, null, 0, 0)""", @"Error 4006 in line 3 at position 10: cannot apply the operator ""+"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 17: cannot apply the operator ""-"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 24: cannot apply the operator ""*"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""(string, int, real)"" and ""byte""
Error 4006 in line 3 at position 45: cannot apply the operator ""+"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 52: cannot apply the operator ""-"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 59: cannot apply the operator ""*"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 66: cannot apply the operator ""/"" to the types ""byte"" and ""(string, int, real)""
Error 4006 in line 3 at position 73: cannot apply the operator ""%"" to the types ""byte"" and ""(string, int, real)""
")]
	[DataRow("""return (sin "Infty", tan "Uncty", asin "2.71828", acos "-42", ln "-5", 1000000000000!, Infty!, 2.5!);""",
"null", @"Error 4002 in line 1 at position 8: cannot apply this operator to this constant
Error 4002 in line 1 at position 21: cannot apply this operator to this constant
Error 4002 in line 1 at position 34: cannot apply this operator to this constant
Error 4002 in line 1 at position 50: cannot apply this operator to this constant
Error 4002 in line 1 at position 62: cannot apply this operator to this constant
Error 4003 in line 1 at position 84: cannot compute factorial of this constant
Error 4003 in line 1 at position 92: cannot compute factorial of this constant
Error 4003 in line 1 at position 98: cannot compute factorial of this constant
")]
	[DataRow("""
		var a = 5;
		var b = null;
		return (a + b, a - b, a * b, a / b, a % b, b + a, b - a, b * a, b / a, b % a);
								
		""", """(5, 5, 0, 0, 0, 5, -5, 0, 0, 0)""", @"Error 4006 in line 3 at position 31: cannot apply the operator ""/"" to the types ""byte"" and ""null""
Error 4006 in line 3 at position 38: cannot apply the operator ""%"" to the types ""byte"" and ""null""
")]
	[DataRow(@"return (IntToReal(5), IntToReal(77777), IntToReal(777777777777));
", @"(5, 77777, 777777777777)", "Ошибок нет")]
	[DataRow(@"var a = 5;
var b = 3;
return (a / b, IntToReal(a) / b);
", @"(1, 1.6666666666666667)", "Ошибок нет")]
	[DataRow(@"var x = 5;
var y = 3;
var a = x > y;
var b = x < y + 2;
var c = x > y && x < y + 2;
var d = x > y || x < y + 2;
return (a, b, c, d);
", @"(true, false, false, true)", "Ошибок нет")]
	[DataRow(@"return Max(3);
", "3", "Ошибок нет")]
	[DataRow(@"return Max(3, 1);
", "3", "Ошибок нет")]
	[DataRow(@"return Max(3, 1, 4);
", "4", "Ошибок нет")]
	[DataRow(@"return Max(3, 1, 4, 2);
", "4", "Ошибок нет")]
	[DataRow(@"return Mean(3);
", "3", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1);
", "2", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1, 3.5);
", "2.5", "Ошибок нет")]
	[DataRow(@"return Mean(3, 1, 4, 2);
", "2.5", "Ошибок нет")]
	[DataRow(@"return Min(2);
", "2", "Ошибок нет")]
	[DataRow(@"return Min(2, 4);
", "2", "Ошибок нет")]
	[DataRow(@"return Min(2, 4, 1);
", "1", "Ошибок нет")]
	[DataRow(@"return Min(2, 4, 1, 3);
", "1", "Ошибок нет")]
	[DataRow(@"return DateTime.IsLeapYear(2025) ? -1234567890 : 2345678901;
", "2345678901", "Ошибок нет")]
	[DataRow(@"var a = DateTime.IsLeapYear(2025) ? -1234567890 : 2345678901;
return a;
", "null", @"Error 4015 in line 1 at position 48: there is no implicit conversion between the types ""int"" and ""unsigned int""
")]
	[DataRow(@"list() int list = (5, 8);
var a = DateTime.IsLeapYear(2025) ? list : DateTime.IsLeapYear(2024) ? 12 : 20;
return a;
", @"(12)", "Ошибок нет")]
	[DataRow(@"list() int list = (5, 8);
var a = DateTime.IsLeapYear(2025) ? (DateTime.IsLeapYear(2024) ? 12 : 20) : list;
return a;
", @"(5, 8)", "Ошибок нет")]
	[DataRow(@"var a = 1 ?> 2 : 3 ?> 2 : 1;
return a;
", "3", "Ошибок нет")]
	[DataRow(@"return ""A"" ?= ""B"" : ""C"";
", "\"C\"", "Ошибок нет")]
	[DataRow(@"var a = ""A"" ?= ""B"" : ""C"";
return a;
", "\"C\"", "Ошибок нет")]
	[DataRow(@"return ""A"" ?!= ""B"" : ""C"";
", "\"A\"", "Ошибок нет")]
	[DataRow(@"var a = ""A"" ?!= ""B"" : ""C"";
return a;
", "\"A\"", "Ошибок нет")]
	[DataRow(@"return ""A"" ?> ""B"" : ""C"";
", "null", @"Error 4006 in line 1 at position 11: cannot apply the operator ""?>"" to the types ""string"" and ""string""
")]
	[DataRow(@"var a = ""A"" ?> ""B"" : ""C"";
return a;
", "null", @"Error 4006 in line 1 at position 12: cannot apply the operator ""?>"" to the types ""string"" and ""string""
")]
	[DataRow(@"return 3 ?> 2 : ""A"";
", "3", "Ошибок нет")]
	[DataRow(@"var a = 3 ?> 2 : ""A"";
return a;
", "null", @"Error 4015 in line 1 at position 15: there is no implicit conversion between the types ""byte"" and ""string""
")]
	[DataRow(@"real Function F(real x, real y)
{
	return x * x + x * y + y * y;
}
real Function Max2(real x, real y)
{
	return Max(x, y);
}
System.Func[real, real, real] f;
f = F;
real a = f(3.14159, 2.71828);
f = Max2;
real b = f(3.14159, 2.71828);
return (a, b);
", @"(25.798355151699997, 3.14159)", "Ошибок нет")]
	[DataRow(@"Class MyClass
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
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass(5, 3.14159, "A"), new MyClass(8, 2.71828, "$"), new MyClass(8, 2.71828, "A"), new MyClass(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"(int, int)[2] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"Class MyClass
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
", "null", @"Error 4022 in line 33 at position 11: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 19: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 31: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 44: the function ""F1"" must have 0 parameters
Error 4022 in line 33 at position 54: the function ""F2"" must have 1 parameters
Error 4027 in line 33 at position 68: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4022 in line 33 at position 87: the function ""G1"" must have 0 parameters
Error 4022 in line 33 at position 103: the function ""G2"" must have 1 parameters
Error 4027 in line 33 at position 123: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
")]
	[DataRow(@"real Function Factorial (int x)
{
	if (x <= 0)
		return 1;
	else
		return x * Factorial(x - 1);
}
return Factorial(100);
", "9.33262154439441E+157", "Ошибок нет")]
	[DataRow(@"int Function F(int x)
{
	if (x < 0)
		return 0;
	else
		return 1;
	x++;
}
return F(-5);
", "0", @"Warning 8005 in line 7 at position 1: the unreachable code has been detected
")]
	[DataRow(@"int Function F(int x)
{
	if (x < 0)
	if (x % 2 == 0)
		return 0;
	else
		return 1;
	x++;
}
return F(-5);
", "0", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function F(int x)
{
	while (x < 0)
	if (x % 2 == 0)
		return 0;
	else
		return 1;
	x++;
}
return F(-5);
", "0", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function F(int x)
{
	loop
	if (x % 2 == 0)
		return 0;
	else
		return 1;
	x++;
}
return F(-5);
", "1", @"Warning 8005 in line 8 at position 1: the unreachable code has been detected
")]
	[DataRow(@"int Function F(int x)
{
	if (x < 0)
	if (x % 2 == 0)
		return 0;
	else
		return 1;
	else
		return 2;
	x++;
}
return F(-5);
", "1", @"Warning 8005 in line 10 at position 1: the unreachable code has been detected
")]
	[DataRow(@"int Function F(int x)
{
	if (x < 0)
	if (x > -100)
	if (x % 2 == 0)
		return 0;
	else
		return 1;
	else
		return 2;
	x++;
}
return F(-5);
", "0", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function ComplexCondition(int x, int y)
{
	if (x > 0)
		if (y > 0)
			return x + y;
		else
			return x - y;
	else
		if (y > 0)
			return x * y;
}
", "null", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function ForLoopFunction(list() int list)
{
	for (int i in Chain(1, list.Length))
		if (list[i] > 0)
				return list[i];
}
", "null", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"int Function ComplexFunction(int x, list() int list)
{
	if (x > 0)
		for (int i in Chain(1, list.Length))
			if (list[i] > x)
				return list[i];
	else
		while (x < 10)
			x++;
		if (x % 2 == 0)
			return x;
}
", "null", @"Error 402A in line 3 at position 1: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"null Function F()
{
}
int Function F()
{
	return 5;
}
return F();
", "null", @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F(int x)
{
}
int Function F(int x)
{
	return x * x;
}
return F(5);
", "null", @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"null Function F()
{
}
int Function F(int x)
{
	return x * x;
}
F();
return F(5);
", "25", "Ошибок нет")]
	[DataRow(@"{
	F();
	null Function F()
	{
	}
}
int Function F(int x)
{
	return x * x;
}
return F(5);
", "25", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
MyClass.F();
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	int Function F(int x)
	{
		F();
		null Function F()
		{
		}
		return x * x;
	}
}
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	int Function F(int x)
	{
		null Function F()
		{
		}
		F();
		return x * x;
	}
}
return MyClass.F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	null Function F()
	{
	}
	int Function F(int x)
	{
		return x * x;
	}
}
new MyClass().F();
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int Function F(int x)
	{
		F();
		null Function F()
		{
		}
		return x * x;
	}
}
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	int Function F(int x)
	{
		null Function F()
		{
		}
		F();
		return x * x;
	}
}
return new MyClass().F(5);
", "25", "Ошибок нет")]
	[DataRow(@"null Function F(int x)
{
}
int Function F(int x)
{
	return x * x;
}
F(5);
return F(5);
", "null", @"Error 2032 in line 4 at position 0: the function ""F"" with these parameter types is already defined in this region
")]
	[DataRow(@"{
	F(5);
	null Function F(int x)
	{
	}
}
int Function F(int x)
{
	return x * x;
}
return F(5);
", "25", "Ошибок нет")]
	[DataRow(@"int n = 0;
while (n < 1000)
{
	n++;
}
return n;", "1000", "Ошибок нет")]
	[DataRow(@"int n = 0;
for (int i in Chain(1, 1000))
{
	n++;
}
return n;", "1000", "Ошибок нет")]
	[DataRow(@"real a = 0;
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
", "12", "Ошибок нет")]
	[DataRow(@"int int int int int = 5;
", "null", @"Error 2008 in line 1 at position 8: expected: "";""
Error 2007 in line 1 at position 8: unrecognized construction
")]
	[DataRow(@"var var var var var = 5;
", "null", @"Error 2008 in line 1 at position 8: expected: "";""
Error 2007 in line 1 at position 8: unrecognized construction
")]
	[DataRow(@"list(3) int a = (((1, 2, 3), (4, 5, 6), (7, 8, 9)), ((10, 11, 12), (13, 14, 15), (16, 17, 18)), ((19, 20, 21), (22, 23, 24), (25, 26, 27)));
return a[1, 2, 3];
", "6", "Ошибок нет")]
	[DataRow(@"list() (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list() (list() (string, int, real), list() (string, int, real),
	list() (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list() (list() (list() (string, int, real), list() (string, int, real), list() (string, int, real)),
	list() (list() (string, int, real), list() (string, int, real), list() (string, int, real)),
	list() (list() (string, int, real), list() (string, int, real), list() (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет")]
	[DataRow(@"list(2) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list() (list(2) (string, int, real), list(2) (string, int, real),
	list(2) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list() (list() (list(2) (string, int, real), list(2) (string, int, real), list(2) (string, int, real)),
	list() (list(2) (string, int, real), list(2) (string, int, real), list(2) (string, int, real)),
	list() (list(2) (string, int, real), list(2) (string, int, real), list(2) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """(((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159))), ((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159))), ((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159))))""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
ListHashSet[string] hs = new ListHashSet[string](3, ""A"", ""B"", ""C"");
hs.Add(""B"");
return hs[2];
", "\"B\"", "Ошибок нет")]
	[DataRow(@"using System.Collections;
ListHashSet[int] hs = new ListHashSet[int](3, 5, 10, 15);
hs.Add(10);
return hs[2];
", "10", "Ошибок нет")]
	[DataRow(@"int a = 3.14159;
byte b = 77777;
real c = ""2.71828"";
return (a, b, c);
", "null", @"Error 4027 in line 1 at position 6: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 2 at position 7: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 3 at position 7: cannot convert from the type ""string"" to the type ""real""
Error 4001 in line 4 at position 8: the identifier ""a"" is not defined in this location
Error 4001 in line 4 at position 11: the identifier ""b"" is not defined in this location
Error 4001 in line 4 at position 14: the identifier ""c"" is not defined in this location
")]
	[DataRow(@"int a = 0;
byte b = 0;
real c = 0;
a = 3.14159;
b = 77777;
c = ""2.71828"";
return (a, b, c);
", @"(0, 0, 0)", @"Error 4027 in line 4 at position 2: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
Error 4027 in line 5 at position 2: the conversion from the type ""int"" to the type ""byte"" is possible only in the function return, not in the direct assignment and not in the call
Error 4014 in line 6 at position 2: cannot convert from the type ""string"" to the type ""real""
")]
	[DataRow(@"list() int list = (0);
return (list.Dispose(10), Fibonacci(10, 10), Fibonacci(""10""), Fibonacci(10.01));
", "null", @"Error 4022 in line 2 at position 21: the function ""Dispose"" must have 0 parameters
Error 4022 in line 2 at position 36: the function ""Fibonacci"" must have 1 parameters
Error 4026 in line 2 at position 55: incompatibility between the type of the parameter of the call ""string"" and the type of the parameter of the function ""int""
Error 4027 in line 2 at position 72: the conversion from the type ""real"" to the type ""int"" is possible only in the function return, not in the direct assignment and not in the call
")]
	[DataRow(@"bool bool=bool;
", "null", @"Error 4012 in line 1 at position 10: one cannot use the local variable ""bool"" before it is declared or inside such declaration in line 1 at position 0
")]
	[DataRow(@"bool Function One()
{
	int Function Two()
	{
		return -1;
	}
	return Two();
}
return One();
", "false", @"Warning 800A in line 7 at position 8: the type of the returning value ""int"" and the function return type ""bool"" are badly compatible, you may lost data
")]
	[DataRow(@"System.Func[int] Function F()
{
	int Function F2()
	{
		return 100;
	}
	return F2;
}
return F()();
", "100", "Ошибок нет")]
	[DataRow(@"Class MyClass /{Class - с большой буквы
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
", "null", @"Wreck 9006 in line 13 at position 0: unclosed 2 nested comments in the end of code
")]
	[DataRow(@"return /""Hello, world!""ssssssssssssssss\;
", "null", @"Wreck 9004 in line 2 at position 0: unexpected end of code reached; expected: 1 pairs ""double quote - reverse slash"" (starting with quote)
")]
	[DataRow(@"return /""Hello, world!/""\;
", "\"Hello, world!/\"", "Ошибок нет")]
	[DataRow(@"return /""Hell@""/""o, world!""\;
", @"/""Hell@""/""o, world!""\", "Ошибок нет")]
	[DataRow(@"return /""Hell@""/{""o, world!""\;
", @"/""Hell@""/{""o, world!""\", "Ошибок нет")]
	[DataRow(@"return /""Hell@""\""""\""o, world!""\;
", @"/""Hell@""\""""\""o, world!""\", "Ошибок нет")]
	[DataRow(@"return 'Hello, world!';
", "null", @"Wreck 9001 in line 1 at position 9: there must be a single character or a single escape-sequence in the single quotes
")]
	[DataRow(@"return 'H
;
", "null", @"Wreck 9001 in line 1 at position 9: there must be a single character or a single escape-sequence in the single quotes
")]
	[DataRow(@"return '", "null", @"Wreck 9000 in line 1 at position 8: unexpected end of code reached; expected: single quote
Wreck 9000 in line 1 at position 8: unexpected end of code reached; expected: single quote
")]
	[DataRow(@"using System;
real Function F(int n)
{
	return 1r / n;
}
list() real Function Calculate(Func[real, int] function)
{
	return (function(5), function(8), function(13));
}
return Calculate(F);
", @"(0.2, 0.125, 0.07692307692307693)", "Ошибок нет")]
	[DataRow(@"list(3) int list = 8;
list = 123;
return list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"var x = DateTime.UTCNow.IsSummertime();
return x ^ x;
", @"false", "Ошибок нет")]
	[DataRow(@"var x = 5;
return 5 pow x += 3;
", "null", @"Error 201D in line 2 at position 15: only the variables can be assigned
")]
	[DataRow(@"return ;
", "null", @"Warning 8002 in line 1 at position 7: the syntax ""return;"" is deprecated; consider using ""return null;"" instead
")]
	[DataRow(@"null Function F(list() int n)
{
	n++;
}
int a = 5;
F(a);
F(a);
F(a);
return a;
", "5", @"Error 4005 in line 3 at position 2: cannot apply the operator ""postfix ++"" to the type ""list() int""
")]
	[DataRow(@"var a = false;
a++;
return a;
", @"true", "Ошибок нет")]
	[DataRow(@"list() int list = (123, 456, 789, 111, 222, 333, 444, 555, 777);
return list;
", @"(123, 456, 789, 111, 222, 333, 444, 555, 777)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list;
", @"(1, 2, 3, 4, 5, 6, 7)", "Ошибок нет")]
	[DataRow(@"var a = false;
var b = 5;
return a + b;
", "5", "Ошибок нет")]
	[DataRow(@"var a = false;
var b = 5;
return a * b;
", "null", @"Error 4006 in line 3 at position 9: cannot apply the operator ""*"" to the types ""bool"" and ""byte""
")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs;
", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[int]();
hs.Add(1);
hs.Add(2);
hs.Add(3);
hs.Add(2);
return hs;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.GetRange(2..);
", """("2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.GetRange(2, 2);
", """("2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.Remove(2);
", "null", @"Error 4026 in line 7 at position 17: incompatibility between the type of the parameter of the call ""byte"" and the type of the parameter of the function ""range""
")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.Remove(""2"");
", "null", @"Error 4026 in line 7 at position 17: incompatibility between the type of the parameter of the call ""string"" and the type of the parameter of the function ""range""
")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.Remove(2..);
", @"(""1"")", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.Remove(2, 1);
", @"(""1"", ""3"")", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs.remove(2);
", "null", @"Error 4033 in line 7 at position 10: the type ""ListHashSet"" does not contain member ""remove""
")]
	[DataRow(@"using System.Collections;
var dic = new Dictionary[string, int]();
dic.TryAdd(""1"", 1);
dic.TryAdd(""2"", 2);
dic.TryAdd(""3"", 3);
return dic;
", """(("1", 1), ("2", 2), ("3", 3))""", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.IndexOf(2, 2);
", "2", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.LastIndexOf(2, 1);
", "0", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Remove(2, 3);
", @"(1, 5, 6, 7)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveAt(5);
", @"(1, 2, 3, 4, 6, 7)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveEnd(5);
", @"(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Reverse(2, 3);
", @"(1, 4, 3, 2, 5, 6, 7)", "Ошибок нет")]
	[DataRow(@"list() string list = ""1"";
list.Add(""2"");
list.Add("""");
list.Add(""2"");
return list;
", """("1", "2", "", "2")""", "Ошибок нет")]
	[DataRow(@"list() string list = """";
list.Add(""1"");
list.Add("""");
list.Add(""2"");
return list;
", """("", "1", "", "2")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.IndexOf(2, 2);
", "2", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.LastIndexOf(2, 1);
", "0", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Remove(2, 3);
", @"(1, 5, 6, 7)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveAt(5);
", @"(1, 2, 3, 4, 6, 7)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveEnd(5);
", @"(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Buffer[int] list = new Buffer[int](16, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Reverse(2, 3);
", @"(1, 4, 3, 2, 5, 6, 7)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list[0];
", "null", @"Error 4016 in line 4 at position 11: incorrect index in the list or the tuple; only the positive indexes are supported
")]
	[DataRow(@"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list[-2];
", "null", @"Error 4016 in line 4 at position 11: incorrect index in the list or the tuple; only the positive indexes are supported
")]
	[DataRow(@"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add("""");
hs.Add(""2"");
return hs;
", """("1", "2", "")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", "null", @"Error 4039 in line 8 at position 8: incompatibility between the type of the returning value ""byte"" and the function return type ""string"" - use an addition of zero-length string for this
Error 4014 in line 14 at position 9: cannot convert from the type ""byte"" to the type ""string"" - use an addition of zero-length string for this
Error 4026 in line 15 at position 10: incompatibility between the type of the parameter of the call ""byte"" and the type of the parameter of the function ""string"" - use an addition of zero-length string for this
Error 4036 in line 15 at position 47: incompatibility between the type of the parameter of the call ""byte"" and the type of the parameter of the constructor ""System.Collections.IEqualityComparer[string]""
Error 4036 in line 15 at position 64: incompatibility between the type of the parameter of the call ""int"" and the type of the parameter of the constructor ""string"" - use an addition of zero-length string for this
")]
	[DataRow("""
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

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
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

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
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

""", "3", "Ошибок нет")]
	[DataRow("""
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

""", """("1", "3")""", "Ошибок нет")]
	[DataRow("""
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

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow("""
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

""", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", "3", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """("1", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """("1", "2", "3")""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", "3", "Ошибок нет")]
	[DataRow("""
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

""", """("1", "3")""", "Ошибок нет")]
	[DataRow("""
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

""", "null", """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""")]
	[DataRow("""
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

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
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

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
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

""", "3", "Ошибок нет")]
	[DataRow("""
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

""", """(1, 3)""", "Ошибок нет")]
	[DataRow("""
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

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow("""
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

""", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", "3", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """(1, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", """(1, 2, 3)""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
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
", "3", "Ошибок нет")]
	[DataRow("""
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

""", """(1, 3)""", "Ошибок нет")]
	[DataRow("""
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

""", "null", """
Error 2023 in line 7 at position 14: cannot create an instance of the abstract type "MyClass"
Error 4000 in line 7 at position 21: internal compiler error
Error 4011 in line 7 at position 1: the variable declared with the keyword "var" must be assigned explicitly and in the same expression
Error 4001 in line 8 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 9 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 10 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 11 at position 1: the identifier "hs" is not defined in this location
Error 4001 in line 12 at position 8: the identifier "hs" is not defined in this location

""")]
	[DataRow(@"Class MyClass
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Class MyClass
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Namespace MyNamespace
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"Class MyClass
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
", """(new MyClass2(5, 3.14159, "A"), new MyClass2(8, 2.71828, "$"), new MyClass2(8, 2.71828, "A"), new MyClass2(12, 3.14159, "A"))""", "Ошибок нет")]
	[DataRow(@"static Class MyClass
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
", """(new MyClass2(), null, null, new MyClass2())""", @"Error 2015 in line 8 at position 17: expected: non-sealed class or interface
Error 4035 in line 17 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
Error 4035 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 1 parameters
")]
	[DataRow(@"Class MyClass
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
", "null", @"Error 0009 in line 8 at position 22: a static class cannot be derived
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
")]
	[DataRow(@"Class MyClass
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
", @"new MyClass(5, 3.14159, ""A"")", "Ошибок нет")]
	[DataRow(@"Class MyClass2 : MyClass
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
", @"(new MyClass2(5, 3.14159, ""A""), null, new MyClass2(8, 2.71828, ""A""), new MyClass2(12, 3.14159, ""A""))",
	@"Error 4035 in line 18 at position 27: the constructor of the type ""MyClass2"" must have from 0 to 2 parameters
")]
	[DataRow(@"Class Person
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
", @"(""Alice"", 30)", "Ошибок нет")]
	[DataRow(@"Class Person
{
	closed string name;
	closed int age;

	string Function GetName()
	{
		return name;
	}

	string Function GetAge()
	{
		return age;
	}
}

 Person person = new Person(""Alice"", 30);
 return (person.GetName(), person.GetAge());
", @"(""Alice"", null)", @"Error 4039 in line 13 at position 9: incompatibility between the type of the returning value ""int"" and the function return type ""string"" - use an addition of zero-length string for this
")]
	[DataRow(@"Class Animal
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
", @"(""Dog"", ""Animal sound"", ""Woof!"")", "Ошибок нет")]
	[DataRow(@"Class BankAccount
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
", "1300", "Ошибок нет")]
	[DataRow(@"Class Vehicle
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
", "\"Car starting\"", "Ошибок нет")]
	[DataRow(@"Class Engine
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
", @"""Engine starting\r\nCar is now running""", "Ошибок нет")]
	[DataRow(@"Class BaseClass
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
", """("Display from DerivedClass", "Info from BaseClass", "Display from DerivedClass", "Info from DerivedClass")""",
			"Ошибок нет")]
	[DataRow(@"abstract Class Animal
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
", """("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Ошибок нет")]
	[DataRow(@"abstract Class Animal
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
", """("Woof", "Dog is eating", "Meow", "Animal is eating")""", "Warning 8008 in line 30 at position 1: the method \"Eat\"" +
			" has the same parameter types as its base method with the same name but it also" +
			" has the other significant differences such as the access modifier or the return type," +
			" so it cannot override that base method and creates a new one;" +
			" if this is intentional, add the \"new\" keyword, otherwise fix the differences\r\n")]
	[DataRow(@"abstract Class Animal
{
	abstract string Function Speak();
	string Function Eat()
	{
		return ""Animal is eating"";
	}
}

sealed Class Cat : Animal
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

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", """("Meow", "Animal is eating")""", "Warning 8008 in line 17 at position 1: the method \"Eat\"" +
			" has the same parameter types as its base method with the same name but it also" +
			" has the other significant differences such as the access modifier or the return type," +
			" so it cannot override that base method and creates a new one;" +
			" if this is intentional, add the \"new\" keyword, otherwise fix the differences\r\n")]
	[DataRow(@"Class Cat : int
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

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", "null", @"Error 2015 in line 1 at position 12: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: "";""
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : long int
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

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", "null", @"Error 2015 in line 1 at position 12: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: "";""
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : System.RedStarLinqExtras
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

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", "null", @"Error 2015 in line 1 at position 19: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: "";""
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class Cat : System.Func[int]
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

Animal myCat = new Cat();
return (myCat.Speak(), myCat.Eat());
", "null", @"Error 2015 in line 1 at position 19: expected: non-sealed class or interface
Error 2008 in line 14 at position 7: expected: "";""
Error 2007 in line 14 at position 7: unrecognized construction
Error 4001 in line 15 at position 8: the identifier ""myCat"" is not defined in this location
Error 4001 in line 15 at position 23: the identifier ""myCat"" is not defined in this location
")]
	[DataRow(@"Class MyClass
{
	abstract string Function Go();
}
", "null", @"Error 400A in line 3 at position 10: the abstract members can be located only inside the abstract classes
")]
	[DataRow(@"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(ref a);
F(ref a);
F(ref a);
return a;
", "8", "Ошибок нет")]
	[DataRow(@"null Function F(ref int n)
{
	n++;
}
int a = 5;
F(a);
F(a);
F(a);
return a;
", "null", @"Wreck 9013 in line 6 at position 2: this parameter must pass with the ""ref"" keyword
")]
	[DataRow(@"int Function F(real n)
{
	return Truncate(n * n);
}
return Fill(F(3.14159) >= 10, 100);
", "(false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false)", "Ошибок нет")]
	[DataRow(@"string s = """";
repeat (1000000)
	s += 'A';
return s;
", A1000000, "Ошибок нет")]
	[DataRow(@"using System;
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.FrequencyTable(list);
", @"((2, 3), (3, 1), (1, 3))", "Ошибок нет")]
	[DataRow(@"using System;
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.GroupIndexes(list);
", @"((0, 1, 5), (2), (3, 4, 6))", "Ошибок нет")]
	[DataRow(@"using System;
real Function Reciproc(int x)
{
	return 1r / x;
}
list() int list = (2, 2, 3, 1, 1, 2, 1);
return RedStarLinqExtras.GroupIndexes(list, Reciproc);
", @"((0, 1, 5), (2), (3, 4, 6))", "Ошибок нет")]
	[DataRow(@"using System;
list() int list = (5, 10, 15, 20, 25);
return RedStarLinq.ToList(list, x => x * x);
", @"(25, 100, 225, 400, 625)", "Ошибок нет")]
	[DataRow(@"using System;
Func[real, real] f = x => x * x;
return f(100);
", "10000", "Ошибок нет")]
	[DataRow(@"x => x * x;
", "null", @"Error 4040 in line 1 at position 0: unexpected lambda expression here
")]
	[DataRow(@"using System;
list() Func[real, real] list = (x => x * x, x => 1 / x, x => E pow x);
return (list[1](3.14), list[2](3.14), list[3](3.14), list[1](-5), list[2](-5), list[3](-5));
", @"(9.8596, 0.3184713375796178, 23.10386685872218, 25, -0.2, 0.006737946999085469)", "Ошибок нет")]
	[DataRow(@"using System;
Func[real, real] f = x =>
{
	return x * x;
};
return f(100);
", "10000", "Ошибок нет")]
	[DataRow(@"using System;
Func[real, real] f = x =>
{
	if (x >= 0)
		return x * x;
	else
		return -x * x;
};
return (f(100), f(-5));
", @"(10000, -25)", "Ошибок нет")]
	[DataRow(@"using System;
Func[int, int, int] sum = (x, y) => x + y;
int result = sum(5, 3);
return result;
", "8", "Ошибок нет")]
	[DataRow(@"using System;
list() int numbers = (1, 2, 3, 4, 5, 6);
var evenNumbers = RedStarLinq.Filter(numbers, x => x % 2 == 0);
return (numbers, evenNumbers);
", "((1, 2, 3, 4, 5, 6), (2, 4, 6))", "Ошибок нет")]
	[DataRow(@"using System;
string s = null;
Func[null, string, int] logMessage = (message, level) => {
	s = ""Уровень: "" + level + "", Сообщение: "" + message;
};
logMessage(""Ошибка"", 1);
return s;
", "\"Уровень: 1, Сообщение: Ошибка\"", "Ошибок нет")]
	[DataRow(@"using System;
int multiplier = 2;
Func[int, int] multiply = x => x * multiplier;
int result = multiply(5);
return result;
", "10", "Ошибок нет")]
	[DataRow(@"using System;
Func[bool, int] isPrime = (number) => {
	if (number < 2) return false;
	for (int i in Chain(2, number - 2)) {
		if(number % i == 0) return false;
	}
	return true;
};
return (isPrime(1), isPrime(2), isPrime(3), isPrime(4), isPrime(5), isPrime(6), isPrime(7), isPrime(8));
", "(false, true, true, false, true, false, true, false)", "Ошибок нет")]
	[DataRow(@"using System;
Func[real, int] f = x => x * x;
var a = f(5);
f = x => 1r / x;
var b = f(5);
return (a, b);
", "(25, 0.2)", "Ошибок нет")]
	[DataRow(@"using System;
Func[int, int] invalidFunc = x => { x + 1; };
", "null", @"Error 402A in line 2 at position 36: this function or lambda must return the value on all execution paths
")]
	[DataRow(@"using System;
string s = null;
Func[null, int] wrongParams = (x, y) => s = x;
return s;
", "null", @"Error 4042 in line 3 at position 31: incorrect list of the parameters of the lambda expression
")]
	[DataRow(@"using System;
Func[string, int] typeMismatch = x => x + 1;
return typeMismatch(5);
", "null", @"Error 4014 in line 2 at position 38: cannot convert from the type ""int"" to the type ""string"" - use an addition of zero-length string for this
")]
	[DataRow(@"using System;
Func[string, string] typeMismatch = x => x + 1;
return typeMismatch();
", "null", @"Error 4045 in line 3 at position 19: this lambda must have 1 parameters
")]
	[DataRow(@"using System;
Func[string, string] typeMismatch = x => x + 1;
return typeMismatch(5, 8, 12);
", "null", @"Error 4045 in line 3 at position 19: this lambda must have 1 parameters
")]
	[DataRow(@"using System;
Func[string, string] typeMismatch = x => x + 1;
return typeMismatch(5);
", "null", @"Error 4014 in line 3 at position 20: cannot convert from the type ""byte"" to the type ""string"" - use an addition of zero-length string for this
")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
return list[^1];
", "5", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
return list[3..5];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
return list[3..^2];
", "(3, 4, 5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
return list[^3..^2];
", "(5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
return list[^5..5];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 1;
return list[^n];
", "5", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = ^1;
return list[n];
", "5", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 5;
return list[start..end];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
index start = 3;
index end = 5;
return list[start..end];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 2;
return list[start..^end];
", "(3, 4, 5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = ^2;
return list[start..end];
", "(3, 4, 5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 2;
return list[^start..^end];
", "(5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = ^3;
var end = ^2;
return list[start..end];
", "(5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 5;
var end = 5;
return list[^start..end];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = ^5;
var end = 5;
return list[start..end];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 5;
var range = start..end;
return list[range];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
index start = 3;
index end = 5;
var range = start..end;
return list[range];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 2;
var range = start..^end;
return list[range];
", "(3, 4, 5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = ^2;
var range = start..end;
return list[range];
", "(3, 4, 5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 3;
var end = 2;
var range = ^start..^end;
return list[range];
", "(5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = ^3;
var end = ^2;
var range = start..end;
return list[range];
", "(5, 6)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = 5;
var end = 5;
var range = ^start..end;
return list[range];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5, 6, 7);
var start = ^5;
var end = 5;
var range = start..end;
return list[range];
", "(3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
return list[n..];
", "(2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
index n = 2;
return list[n..];
", "(2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
return list[^n..];
", "(4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = ^2;
return list[n..];
", "(4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
return list[..n];
", "(1, 2)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
index n = 2;
return list[..n];
", "(1, 2)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
return list[..^n];
", "(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = ^2;
return list[..n];
", "(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
return list[..];
", "(1, 2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
var range = n..;
return list[range];
", "(2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
index n = 2;
var range = n..;
return list[range];
", "(2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
var range = ^n..;
return list[range];
", "(4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = ^2;
var range = n..;
return list[range];
", "(4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
var range = ..n;
return list[range];
", "(1, 2)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
index n = 2;
var range = ..n;
return list[range];
", "(1, 2)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = 2;
var range = ..^n;
return list[range];
", "(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var n = ^2;
var range = ..n;
return list[range];
", "(1, 2, 3, 4)", "Ошибок нет")]
	[DataRow(@"list() int list = (1, 2, 3, 4, 5);
var range = ..;
return list[range];
", "(1, 2, 3, 4, 5)", "Ошибок нет")]
	[DataRow(@"list() int list = new(3, 1, 2, 3);
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"var list = new(3, 1, 2, 3);
return list;
", "(1, 2, 3)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
var list = new(new ListHashSet[string](""A""), new(""B""), new(""C""));
return list;
", """(("A"), ("B"), ("C"))""", "Ошибок нет")]
	[DataRow(@"using System.Collections;
ListHashSet[string] list = new(""A"", ""B"", ""C"");
list = new(""C"", ""D"", ""E"");
list.Add(""D"");
return list;
", """("C", "D", "E")""", "Ошибок нет")]
	[DataRow(@"int a = 5;
a = new(8);
return a;
", "0", @"Error 4017 in line 2 at position 4: the type ""int"" cannot be created via the constructor
Error 4000 in line 2 at position 7: internal compiler error
")]
	[DataRow(@"abstract Class MyClass { }
MyClass a = new(5);
a = new(8);
return a;
", "null", @"Error 4018 in line 2 at position 12: the abstract type ""MyClass"" can be created via the constructor but only if you explicitly specify the constructing type (which is not abstract)
Error 4000 in line 2 at position 15: internal compiler error
Error 4018 in line 3 at position 4: the abstract type ""MyClass"" can be created via the constructor but only if you explicitly specify the constructing type (which is not abstract)
Error 4000 in line 3 at position 7: internal compiler error
")]
	[DataRow(@"static Class MyClass
{
	const int a = 5;
	const real b = 3.14159;
	const string c { get, set } = ""A"";
}
return (MyClass.a, MyClass.b, MyClass.c);
", "(5, 3.14159, null)", @"Error 203C in line 5 at position 16: the constants cannot have getters or setters
Error 4033 in line 7 at position 38: the type ""MyClass"" does not contain member ""c""
")]
	[DataRow(@"static Class MyClass
{
	const int a;
	const real b = 3.14159;
	const string c = ""A"";
}
return (MyClass.a, MyClass.b, MyClass.c);
", "(null, 3.14159, \"A\")", @"Error 203D in line 3 at position 12: the constant must have a value
Error 4033 in line 7 at position 16: the type ""MyClass"" does not contain member ""a""
")]
	[DataRow(@"static Class MyClass
{
	const string A1000000 = A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000;
	closed const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
	closed const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
	closed const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
	closed const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
	closed const string A10 = ""AAAAAAAAAA"";
}
return MyClass.A1000000;
", A1000000, "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const string A1000000 = MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000 + MyClass.A100000;
	closed const string A100000 = MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000 + MyClass.A10000;
	closed const string A10000 = MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000 + MyClass.A1000;
	closed const string A1000 = MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100 + MyClass.A100;
	closed const string A100 = MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10 + MyClass.A10;
	closed const string A10 = ""AAAAAAAAAA"";
}
return MyClass.A1000000;
", A1000000, "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	closed const string A1000000 = A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000;
	closed const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
	closed const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
	closed const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
	closed const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
	closed const string A10 = ""AAAAAAAAAA"";
}
return MyClass.A1000000;
", "null", @"Error 4030 in line 10 at position 15: the property ""MyClass.A1000000"" is inaccessible from here
")]
	[DataRow(@"const int a = 5;
const real b = 3.14159;
const string c = ""A"";
const bool d = true;
return (a, b, c, d);
", "(5, 3.14159, \"A\", true)", "Ошибок нет")]
	[DataRow(@"const int a;
const real b = 3.14159;
const string c = ""A"";
const bool d = true;
return (a, b, c, d);
", "(null, 3.14159, \"A\", true)", @"Error 203D in line 1 at position 11: the constant must have a value
Error 4001 in line 5 at position 8: the identifier ""a"" is not defined in this location
")]
	[DataRow(@"static Class MyClass
{
	const int a = 5;
	const real b = 3.14159;
	const string c = ""A"";
}
return (MyClass.a = 8, MyClass.b *= 2, MyClass.c);
", "(null, null, \"A\")", @"Error 4052 in line 7 at position 18: cannot assign a value to the constant
Error 4052 in line 7 at position 33: cannot assign a value to the constant
")]
	[DataRow(@"const int a = 5;
const real b = 3.14159;
const string c = ""A"";
return (a = 8, b *= 2, c);
", "(null, null, \"A\")", @"Error 4052 in line 4 at position 10: cannot assign a value to the constant
Error 4052 in line 4 at position 17: cannot assign a value to the constant
")]
	[DataRow(@"const string A1000000 = A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000 + A100000;
const string A100000 = A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000 + A10000;
const string A10000 = A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000 + A1000;
const string A1000 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
const string A10 = ""AAAAAAAAAA"";
return A1000000;
", A1000000, "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	closed const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
	closed string A10 = ""AAAAAAAAAA"";
}
return MyClass.A100;
", "null", @"Error 4050 in line 3 at position 28: this expression must be constant but it isn't
Error 4050 in line 3 at position 34: this expression must be constant but it isn't
Error 4050 in line 3 at position 40: this expression must be constant but it isn't
Error 4050 in line 3 at position 46: this expression must be constant but it isn't
Error 4050 in line 3 at position 52: this expression must be constant but it isn't
Error 4050 in line 3 at position 58: this expression must be constant but it isn't
Error 4050 in line 3 at position 64: this expression must be constant but it isn't
Error 4050 in line 3 at position 70: this expression must be constant but it isn't
Error 4050 in line 3 at position 76: this expression must be constant but it isn't
Error 4050 in line 3 at position 82: this expression must be constant but it isn't
")]
	[DataRow(@"const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
string A10 = ""AAAAAAAAAA"";
return A100;
", "null", @"Error 4012 in line 1 at position 20: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 26: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 32: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
Error 4012 in line 1 at position 38: one cannot use the local variable ""A10"" before it is declared or inside such declaration in line 2 at position 0
")]
	[DataRow(@"string A10 = ""AAAAAAAAAA"";
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
return A100;
", "null", @"Error 4050 in line 2 at position 20: this expression must be constant but it isn't
Error 4050 in line 2 at position 26: this expression must be constant but it isn't
Error 4050 in line 2 at position 32: this expression must be constant but it isn't
Error 4050 in line 2 at position 38: this expression must be constant but it isn't
Error 4050 in line 2 at position 44: this expression must be constant but it isn't
Error 4050 in line 2 at position 50: this expression must be constant but it isn't
Error 4050 in line 2 at position 56: this expression must be constant but it isn't
Error 4050 in line 2 at position 62: this expression must be constant but it isn't
Error 4050 in line 2 at position 68: this expression must be constant but it isn't
Error 4050 in line 2 at position 74: this expression must be constant but it isn't
")]
	[DataRow(@"const string A10 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
return A10;
", "null", @"Error 4055 in line 1 at position 19: too deep constant definition tree
Error 4055 in line 1 at position 26: too deep constant definition tree
Error 4055 in line 1 at position 33: too deep constant definition tree
Error 4055 in line 1 at position 40: too deep constant definition tree
")]
	[DataRow(@"static Class MyClass
{
	const string A10 = A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100 + A100;
	const string A100 = A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10 + A10;
}
return MyClass.A10;
", "null", @"Error 4055 in line 3 at position 20: too deep constant definition tree
Error 4055 in line 3 at position 27: too deep constant definition tree
Error 4055 in line 3 at position 34: too deep constant definition tree
Error 4055 in line 3 at position 41: too deep constant definition tree
")]
	[DataRow(@"list(2.5) int list = 8;
list = 123;
return list;
", @"(123)", @"Error 2017 in line 1 at position 5: this expression must be implicitly convertible to the ""int"" type
")]
	[DataRow(@"list(""AAA"") int list = 8;
list = 123;
return list;
", @"(123)", @"Error 2017 in line 1 at position 5: this expression must be implicitly convertible to the ""int"" type
")]
	[DataRow(@"static Class MyClass
{
	const int n = 3;
	list(n) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int bool = 3;
	list(bool) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 3;
list(n) int list = 8;
list = 123;
return list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int bool = 3;
list(bool) int list = 8;
list = 123;
return list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int n = 2;
	list(n + 1) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int bool = 2;
	list(bool + 1) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 2;
list(n + 1) int list = 8;
list = 123;
return list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int bool = 2;
list(bool + 1) int list = 8;
list = 123;
return list;
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 1;
list(n) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list(n) (list(n) (string, int, real), list(n) (string, int, real),
	list(n) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list(n) (list(n) (list(n) (string, int, real), list(n) (string, int, real), list(n) (string, int, real)),
	list(n) (list(n) (string, int, real), list(n) (string, int, real), list(n) (string, int, real)),
	list(n) (list(n) (string, int, real), list(n) (string, int, real), list(n) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет")]
	[DataRow(@"const int n = 0;
list(n + 1) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list(n + 1) (list(n + 1) (string, int, real), list(n + 1) (string, int, real),
	list(n + 1) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list(n + 1) (list(n + 1) (list(n + 1) (string, int, real), list(n + 1) (string, int, real), list(n + 1) (string, int, real)),
	list(n + 1) (list(n + 1) (string, int, real), list(n + 1) (string, int, real), list(n + 1) (string, int, real)),
	list(n + 1) (list(n + 1) (string, int, real), list(n + 1) (string, int, real), list(n + 1) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет")]
	[DataRow(@"const int bool = 1;
list(bool) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list(bool) (list(bool) (string, int, real), list(bool) (string, int, real),
	list(bool) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list(bool) (list(bool) (list(bool) (string, int, real), list(bool) (string, int, real), list(bool) (string, int, real)),
	list(bool) (list(bool) (string, int, real), list(bool) (string, int, real), list(bool) (string, int, real)),
	list(bool) (list(bool) (string, int, real), list(bool) (string, int, real), list(bool) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет")]
	[DataRow(@"const int bool = 0;
list(bool + 1) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real),
	list(bool + 1) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list(bool + 1) (list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real)),
	list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real)),
	list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", """((("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)), (("A", 77777, 3.14159), ("A", 77777, 3.14159), ("A", 77777, 3.14159)))""", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	int n = 3;
	list(n) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(123)", @"Error 4057 in line 4 at position 6: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"static Class MyClass
{
	int bool = 3;
	list(bool) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(123)", @"Error 4057 in line 4 at position 6: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"int n = 3;
list(n) int list = 8;
list = 123;
return list;
", @"(123)", @"Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"int bool = 3;
list(bool) int list = 8;
list = 123;
return list;
", @"(123)", @"Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"static Class MyClass
{
	int n = 2;
	list(n + 1) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(123)", @"Error 4057 in line 4 at position 6: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"static Class MyClass
{
	int bool = 2;
	list(bool + 1) int list = 8;
}
MyClass.list = 123;
return MyClass.list;
", @"(123)", @"Error 4057 in line 4 at position 6: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"int n = 2;
list(n + 1) int list = 8;
list = 123;
return list;
", @"(123)", @"Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"int bool = 2;
list(bool + 1) int list = 8;
list = 123;
return list;
", @"(123)", @"Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"int bool = 0;
list(bool + 1) (string, int, real) a = Fill((""A"", 77777, 3.14159), 3);
list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real),
	list(bool + 1) (string, int, real)) b = ((a, a, a), (a, a, a), (a, a, a));
list(bool + 1) (list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real)),
	list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real)),
	list(bool + 1) (list(bool + 1) (string, int, real), list(bool + 1) (string, int, real), list(bool + 1) (string, int, real))) c
	= ((b, b, b), (b, b, b), (b, b, b));
return c[1, 2, 3];
", "null", @"Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 2 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 3 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 3 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 5 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
Error 4057 in line 5 at position 5: this expression must be constant and implicitly convertible to the ""int"" type
")]
	[DataRow(@"using System;
static Class MyClass
{
	const int n = 3;
	Func[list(n) int] list = () => 123;
}
return MyClass.list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
static Class MyClass
{
	const int bool = 3;
	Func[list(bool) int] list = () => 123;
}
return MyClass.list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
const int n = 3;
Func[list(n) int] list = () => 123;
return list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
const int bool = 3;
Func[list(bool) int] list = () => 123;
return list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
static Class MyClass
{
	const int n = 2;
	Func[list(n + 1) int] list = () => 123;
}
return MyClass.list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
static Class MyClass
{
	const int bool = 2;
	Func[list(bool + 1) int] list = () => 123;
}
return MyClass.list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
const int n = 2;
Func[list(n + 1) int] list = () => 123;
return list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System;
const int bool = 2;
Func[list(bool + 1) int] list = () => 123;
return list();
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int n = 3;

	list(n) int Function list(list(n) int x)
	{
		return x[1, 1, 1] + 115;
	}
}
return MyClass.list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int bool = 3;

	list(bool) int Function list(list(bool) int x)
	{
		return x[1, 1, 1] + 115;
	}
}
return MyClass.list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 3;

list(n) int Function list(list(n) int x)
{
	return x[1, 1, 1] + 115;
}
return list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int bool = 3;

list(bool) int Function list(list(bool) int x)
{
	return x[1, 1, 1] + 115;
}
return list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int n = 2;

	list(n + 1) int Function list(list(n + 1) int x)
	{
		return x[1, 1, 1] + 115;
	}
}
return MyClass.list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"static Class MyClass
{
	const int bool = 2;

	list(bool + 1) int Function list(list(bool + 1) int x)
	{
		return x[1, 1, 1] + 115;
	}
}
return MyClass.list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 2;

list(n + 1) int Function list(list(n + 1) int x)
{
	return x[1, 1, 1] + 115;
}
return list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int bool = 2;

list(bool + 1) int Function list(list(bool + 1) int x)
{
	return x[1, 1, 1] + 115;
}
return list(8);
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
static Class MyClass
{
	const int n = 2;

	Buffer[list(n) int] Function list(Buffer[list(n) int] x)
	{
		return new(1, x[1, 1, 1] + 115);
	}
}
return MyClass.list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
static Class MyClass
{
	const int bool = 2;

	Buffer[list(bool) int] Function list(Buffer[list(bool) int] x)
	{
		return new(1, x[1, 1, 1] + 115);
	}
}
return MyClass.list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const int n = 2;

Buffer[list(n) int] Function list(Buffer[list(n) int] x)
{
	return new(1, x[1, 1, 1] + 115);
}
return list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const int bool = 2;

Buffer[list(bool) int] Function list(Buffer[list(bool) int] x)
{
	return new(1, x[1, 1, 1] + 115);
}
return list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
static Class MyClass
{
	const int n = 3;

	Buffer[list(n - 1) int] Function list(Buffer[list(n - 1) int] x)
	{
		return new(1, x[1, 1, 1] + 115);
	}
}
return MyClass.list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
static Class MyClass
{
	const int bool = 3;

	Buffer[list(bool - 1) int] Function list(Buffer[list(bool - 1) int] x)
	{
		return new(1, x[1, 1, 1] + 115);
	}
}
return MyClass.list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const int n = 3;

Buffer[list(n - 1) int] Function list(Buffer[list(n - 1) int] x)
{
	return new(1, x[1, 1, 1] + 115);
}
return list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const int bool = 3;

Buffer[list(bool - 1) int] Function list(Buffer[list(bool - 1) int] x)
{
	return new(1, x[1, 1, 1] + 115);
}
return list(new(1, 8));
", @"(((123)))", "Ошибок нет")]
	[DataRow(@"const int n = 2;
(int, int)[n] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int bool = 2;
(int, int)[bool] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int n = 1;
(int, int)[n + 1] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int bool = 1;
(int, int)[bool + 1] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 2;
}
(int, int)[Klass.n] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 2;
}
(int, int)[Klass.bool] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 1;
}
(int, int)[Klass.n + 1] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 1;
}
(int, int)[Klass.bool + 1] Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int n = 2;
(int[n], int[n]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int bool = 2;
(int[bool], int[bool]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int n = 1;
(int[n + 1], int[n + 1]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int bool = 1;
(int[bool + 1], int[bool + 1]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 2;
}
(int[Klass.n], int[Klass.n]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 2;
}
(int[Klass.bool], int[Klass.bool]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 1;
}
(int[Klass.n + 1], int[Klass.n + 1]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 1;
}
(int[Klass.bool + 1], int[Klass.bool + 1]) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a));
}
return F();
", "((5, 8), (5, 8))", "Ошибок нет")]
	[DataRow(@"const int n = 2;
(int[n], int[n], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"const int bool = 2;
(int[bool], int[bool], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"const int n = 1;
(int[n + 1], int[n + 1], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"const int bool = 1;
(int[bool + 1], int[bool + 1], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 2;
}
(int[Klass.n], int[Klass.n], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 2;
}
(int[Klass.bool], int[Klass.bool], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int n = 1;
}
(int[Klass.n + 1], int[Klass.n + 1], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"static Class Klass
{
	const int bool = 1;
}
(int[Klass.bool + 1], int[Klass.bool + 1], real) Function F()
{
	Class MyClass
	{
		int a = 0;
	}
	MyClass b = new MyClass(5);
	return ((b.a, new MyClass(8).a), (b.a, new MyClass(8).a), 3.14159);
}
return F();
", "((5, 8), (5, 8), 3.14159)", "Ошибок нет")]
	[DataRow(@"typename real = int;
return real;
", "int", "Ошибок нет")]
	[DataRow(@"typename real = list() int;
return real;
", "list() int", "Ошибок нет")]
	[DataRow(@"typename real = list() string;
return real;
", "list() string", "Ошибок нет")]
	[DataRow(@"typename real = list() bool;
return real;
", "list() bool", "Ошибок нет")]
	[DataRow(@"typename real = list(2) int;
return real;
", "list(2) int", "Ошибок нет")]
	[DataRow(@"typename real = list(2) string;
return real;
", "list(2) string", "Ошибок нет")]
	[DataRow(@"typename real = list(2) bool;
return real;
", "list(2) bool", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[int];
return real;
", "System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[string];
return real;
", "System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[int];
return real;
", "list() System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[string];
return real;
", "list() System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"typename Function F()
{
	return real;
}
return F();
", "real", "Ошибок нет")]
	[DataRow(@"const typename real = int;
typename T = list() real;
return T;
", "list() int", "Ошибок нет")]
	[DataRow(@"const typename real = list() int;
typename T = list() real;
return T;
", "list(2) int", "Ошибок нет")]
	[DataRow(@"const typename real = list() string;
typename T = list() real;
return T;
", "list(2) string", "Ошибок нет")]
	[DataRow(@"const typename real = list() bool;
typename T = list() real;
return T;
", "list(2) bool", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) int;
typename T = list() real;
return T;
", "list(3) int", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) string;
typename T = list() real;
return T;
", "list(3) string", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) bool;
typename T = list() real;
return T;
", "list(3) bool", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[int];
typename T = list() real;
return T;
", "list() System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[string];
typename T = list() real;
return T;
", "list() System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[int];
typename T = list() real;
return T;
", "list(2) System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[string];
typename T = list() real;
return T;
", "list(2) System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() string;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() bool;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() bool]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list(2) int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list(2) string;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list(2) bool;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) bool]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[int];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[string];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[int];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[string];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = int;
typename T = Func[real];
return T;
", "System.Func[int]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list() int;
typename T = Func[real];
return T;
", "System.Func[list() int]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list() string;
typename T = Func[real];
return T;
", "System.Func[list() string]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list() bool;
typename T = Func[real];
return T;
", "System.Func[list() bool]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list(2) int;
typename T = Func[real];
return T;
", "System.Func[list(2) int]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list(2) string;
typename T = Func[real];
return T;
", "System.Func[list(2) string]", "Ошибок нет")]
	[DataRow(@"using System;
const typename real = list(2) bool;
typename T = Func[real];
return T;
", "System.Func[list(2) bool]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
const typename real = ListHashSet[int];
typename T = Func[real];
return T;
", "System.Func[System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
const typename real = ListHashSet[string];
typename T = Func[real];
return T;
", "System.Func[System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
const typename real = list() ListHashSet[int];
typename T = Func[real];
return T;
", "System.Func[list() System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
const typename real = list() ListHashSet[string];
typename T = Func[real];
return T;
", "System.Func[list() System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"const typename real = int;
typename T = (int, real);
return T;
", "int[2]", "Ошибок нет")]
	[DataRow(@"const typename real = list() int;
typename T = (int, real);
return T;
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"const typename real = list() string;
typename T = (int, real);
return T;
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"const typename real = list() bool;
typename T = (int, real);
return T;
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) int;
typename T = (int, real);
return T;
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) string;
typename T = (int, real);
return T;
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) bool;
typename T = (int, real);
return T;
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[int];
typename T = (int, real);
return T;
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[string];
typename T = (int, real);
return T;
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[int];
typename T = (int, real);
return T;
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[string];
typename T = (int, real);
return T;
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = int;
}
typename T = (int, MyClass.real);
return T;
", "int[2]", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list() int;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list() string;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list() bool;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list(2) int;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list(2) string;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	const typename real = list(2) bool;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	const typename real = ListHashSet[int];
}
typename T = (int, MyClass.real);
return T;
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	const typename real = ListHashSet[string];
}
typename T = (int, MyClass.real);
return T;
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	const typename real = list() ListHashSet[int];
}
typename T = (int, MyClass.real);
return T;
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	const typename real = list() ListHashSet[string];
}
typename T = (int, MyClass.real);
return T;
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"const typename real = int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "int[2]", "Ошибок нет")]
	[DataRow(@"const typename real = list() int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"const typename real = list() string;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"const typename real = list() bool;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) string;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"const typename real = list(2) bool;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[int];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = ListHashSet[string];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[int];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
const typename real = list() ListHashSet[string];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"typename real = int;
typename T = list() real;
return T;
", "list() int", "Ошибок нет")]
	[DataRow(@"typename real = list() int;
typename T = list() real;
return T;
", "list(2) int", "Ошибок нет")]
	[DataRow(@"typename real = list() string;
typename T = list() real;
return T;
", "list(2) string", "Ошибок нет")]
	[DataRow(@"typename real = list() bool;
typename T = list() real;
return T;
", "list(2) bool", "Ошибок нет")]
	[DataRow(@"typename real = list(2) int;
typename T = list() real;
return T;
", "list(3) int", "Ошибок нет")]
	[DataRow(@"typename real = list(2) string;
typename T = list() real;
return T;
", "list(3) string", "Ошибок нет")]
	[DataRow(@"typename real = list(2) bool;
typename T = list() real;
return T;
", "list(3) bool", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[int];
typename T = list() real;
return T;
", "list() System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[string];
typename T = list() real;
return T;
", "list() System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[int];
typename T = list() real;
return T;
", "list(2) System.Collections.ListHashSet[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[string];
typename T = list() real;
return T;
", "list(2) System.Collections.ListHashSet[string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() string;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() bool;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() bool]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list(2) int;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) int]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list(2) string;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) string]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list(2) bool;
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list(2) bool]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[int];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[string];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[int];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[string];
typename T = Buffer[real];
return T;
", "System.Collections.Buffer[list() System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = int;
typename T = Func[real];
return T;
", "System.Func[int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() int;
typename T = Func[real];
return T;
", "System.Func[list() int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() string;
typename T = Func[real];
return T;
", "System.Func[list() string]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() bool;
typename T = Func[real];
return T;
", "System.Func[list() bool]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) int;
typename T = Func[real];
return T;
", "System.Func[list(2) int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) string;
typename T = Func[real];
return T;
", "System.Func[list(2) string]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) bool;
typename T = Func[real];
return T;
", "System.Func[list(2) bool]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = ListHashSet[int];
typename T = Func[real];
return T;
", "System.Func[System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = ListHashSet[string];
typename T = Func[real];
return T;
", "System.Func[System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = list() ListHashSet[int];
typename T = Func[real];
return T;
", "System.Func[list() System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = list() ListHashSet[string];
typename T = Func[real];
return T;
", "System.Func[list() System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = int;
typename T = Func[null, real];
return T;
", "System.Func[null, int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() int;
typename T = Func[null, real];
return T;
", "System.Func[null, list() int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() string;
typename T = Func[null, real];
return T;
", "System.Func[null, list() string]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list() bool;
typename T = Func[null, real];
return T;
", "System.Func[null, list() bool]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) int;
typename T = Func[null, real];
return T;
", "System.Func[null, list(2) int]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) string;
typename T = Func[null, real];
return T;
", "System.Func[null, list(2) string]", "Ошибок нет")]
	[DataRow(@"using System;
typename real = list(2) bool;
typename T = Func[null, real];
return T;
", "System.Func[null, list(2) bool]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = ListHashSet[int];
typename T = Func[null, real];
return T;
", "System.Func[null, System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = ListHashSet[string];
typename T = Func[null, real];
return T;
", "System.Func[null, System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = list() ListHashSet[int];
typename T = Func[null, real];
return T;
", "System.Func[null, list() System.Collections.ListHashSet[int]]", "Ошибок нет")]
	[DataRow(@"using System;
using System.Collections;
typename real = list() ListHashSet[string];
typename T = Func[null, real];
return T;
", "System.Func[null, list() System.Collections.ListHashSet[string]]", "Ошибок нет")]
	[DataRow(@"typename real = int;
typename T = (int, real);
return T;
", "int[2]", "Ошибок нет")]
	[DataRow(@"typename real = list() int;
typename T = (int, real);
return T;
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"typename real = list() string;
typename T = (int, real);
return T;
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"typename real = list() bool;
typename T = (int, real);
return T;
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) int;
typename T = (int, real);
return T;
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) string;
typename T = (int, real);
return T;
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) bool;
typename T = (int, real);
return T;
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[int];
typename T = (int, real);
return T;
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[string];
typename T = (int, real);
return T;
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[int];
typename T = (int, real);
return T;
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[string];
typename T = (int, real);
return T;
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = int;
}
typename T = (int, MyClass.real);
return T;
", "int[2]", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list() int;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list() string;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list() bool;
}
typename T = (int, MyClass.real);
return T;
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list(2) int;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list(2) string;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"Class MyClass
{
	static typename real = list(2) bool;
}
typename T = (int, MyClass.real);
return T;
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	static typename real = ListHashSet[int];
}
typename T = (int, MyClass.real);
return T;
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	static typename real = ListHashSet[string];
}
typename T = (int, MyClass.real);
return T;
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	static typename real = list() ListHashSet[int];
}
typename T = (int, MyClass.real);
return T;
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
Class MyClass
{
	static typename real = list() ListHashSet[string];
}
typename T = (int, MyClass.real);
return T;
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"typename real = int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "int[2]", "Ошибок нет")]
	[DataRow(@"typename real = list() int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() int)", "Ошибок нет")]
	[DataRow(@"typename real = list() string;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() string)", "Ошибок нет")]
	[DataRow(@"typename real = list() bool;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() bool)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) int;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) int)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) string;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) string)", "Ошибок нет")]
	[DataRow(@"typename real = list(2) bool;
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list(2) bool)", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[int];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = ListHashSet[string];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[int];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() System.Collections.ListHashSet[int])", "Ошибок нет")]
	[DataRow(@"using System.Collections;
typename real = list() ListHashSet[string];
if (true)
{
	typename T = (int, real);
	return T;
}
", "(int, list() System.Collections.ListHashSet[string])", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = list() real;
}
return real;
", "list(10) int", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = (int, real);
}
return real;
", "(int, (int, (int, (int, (int, (int, (int, (int, (int, int[2])))))))))", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = System.Collections.Buffer[real];
}
return real;
", "System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[" +
		"System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[System.Collections.Buffer[int]]]]]]]]]]", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = System.Func[real];
}
return real;
", "System.Func[System.Func[System.Func[System.Func[System.Func[" +
		"System.Func[System.Func[System.Func[System.Func[System.Func[int]]]]]]]]]]", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = System.Func[null, real];
}
return real;
", "System.Func[null, System.Func[null, System.Func[null, System.Func[null, System.Func[null, " +
		"System.Func[null, System.Func[null, System.Func[null, System.Func[null, System.Func[null, int]]]]]]]]]]", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (10)
{
	real = System.Func[null, int, real];
}
return real;
", "System.Func[null, int, System.Func[null, int, System.Func[null, int, System.Func[null, int, System.Func[null, int, " +
		"System.Func[null, int, System.Func[null, int, System.Func[null, int, System.Func[null, int, System.Func[null, int, int]]]]]]]]]]", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (1)
{
	real = list() real;
}
real x = new(1, 123);
return x;
", "(123)", "Ошибок нет")]
	[DataRow(@"typename real = int;
repeat (1)
{
	real = System.Collections.Buffer[real];
}
real x = new(1, 123);
return x;
", "(123)", "Ошибок нет")]
	[DataRow(@"const typename real = int;
const typename string = list() real;
const typename T = list() string;
const typename var = System.Collections.Buffer[T];
var x = new(1, 123);
return x;
", "(((123)))", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 => 10,
	6 => 12,
	7 => 14,
	_ => 16,
};
", "10", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16,
};
", "16", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 => 10,
	6 => 12,
	7 => 14,
	_ => 16
};
", "null", @"Error 2008 in line 12 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
};
", "null", @"Error 2008 in line 8 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch
{
	0 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
};
", "null", @"Error 2008 in line 3 at position 3: expected: ""if"" or =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0
	1 => 2,
	2 => 4,
	3 => 6,
	_ => 16
};
", "null", @"Error 2008 in line 4 at position 1: expected: comma
")]
	[DataRow(@"return 12345678905 switch
{
	12345678900 => 12345678900,
	12345678901 => 12345678902,
	12345678902 => 12345678904,
	12345678903 => 12345678906,
	12345678904 => 12345678908,
	12345678905 => 123456789010,
	12345678906 => 123456789012,
	12345678907 => 123456789014,
	_ => 123456789016,
};
", "123456789010", "Ошибок нет")]
	[DataRow(@"return 12345678905 switch
{
	12345678900 => 12345678900,
	12345678901 => 12345678902,
	12345678902 => 12345678904,
	12345678903 => 12345678906,
	_ => 123456789016,
};
", "123456789016", "Ошибок нет")]
	[DataRow(@"return 5.1 switch
{
	0.1 => 0.1,
	1.1 => 2.1,
	2.1 => 4.1,
	3.1 => 6.1,
	4.1 => 8.1,
	5.1 => 10.1,
	6.1 => 12.1,
	7.1 => 14.1,
	_ => 16.1,
};
", "10.1", "Ошибок нет")]
	[DataRow(@"return 5.1 switch
{
	0.1 => 0.1,
	1.1 => 2.1,
	2.1 => 4.1,
	3.1 => 6.1,
	_ => 16.1,
};
", "16.1", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	""4"" => ""8"",
	""5"" => ""10"",
	""6"" => ""12"",
	""7"" => ""14"",
	_ => ""16"",
};
", @"""10""", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	_ => ""16"",
};
", @"""16""", "Ошибок нет")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	""4"" => ""8"",
	""5"" => ""10"",
	""6"" => ""12"",
	""7"" => ""14"",
	_ => ""16""
};
", "null", @"Error 2008 in line 12 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return ""5"" switch
{
	""0"" => ""0"",
	""1"" => ""2"",
	""2"" => ""4"",
	""3"" => ""6"",
	_ => ""16""
};
", "null", @"Error 2008 in line 8 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"const string s = """";
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	s + ""4"" => ""8"",
	s + ""5"" => ""10"",
	s + ""6"" => ""12"",
	s + ""7"" => ""14"",
	_ => ""16"",
};
", @"""10""", "Ошибок нет")]
	[DataRow(@"const string s = """";
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	_ => ""16"",
};
", @"""16""", "Ошибок нет")]
	[DataRow(@"const string s = ""A"";
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	s + ""4"" => ""8"",
	s + ""5"" => ""10"",
	s + ""6"" => ""12"",
	s + ""7"" => ""14"",
	_ => ""16"",
};
", @"""10""", "Ошибок нет")]
	[DataRow(@"const string s = ""A"";
return s + ""5"" switch
{
	s + ""0"" => ""0"",
	s + ""1"" => ""2"",
	s + ""2"" => ""4"",
	s + ""3"" => ""6"",
	_ => ""16"",
};
", @"""16""", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 if false => 10,
	6 => 12,
	7 => 14,
	_ => 16,
};
", "16", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false => 16,
	_ => -42,
};
", "-42", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => -5,
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => -42,
};
", "-42", "Ошибок нет")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => -5,
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => ""error"",
};
", "null", @"Error 4014 in line 8 at position 6: cannot convert from the type ""string"" to the type ""real""
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => ""error"",
	2 => 49152,
	3 => -49152,
	_ if false => 3.14159,
	_ => -42,
};
", "null", @"Error 4015 in line 4 at position 6: there is no implicit conversion between the types ""byte"" and ""string""
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	4 => 8,
	5 if false 10,
	6 => 12,
	7 => 14,
	_ => 16,
};
", "null", @"Error 2008 in line 8 at position 12: expected: =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false 16,
	_ => -42,
};
", "null", @"Error 2008 in line 7 at position 12: expected: =>
")]
	[DataRow(@"return 5 switch
{
	0 => 0,
	1 => 2,
	2 => 4,
	3 => 6,
	_ if false => 16,
	_ => -42,
;
", "null", @"Wreck 9007 in line 9 at position 0: unpaired bracket; expected: }
")]
	[DataRow(@"return 5 switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 };
", "16", "Ошибок нет")]
	[DataRow(@"return 5
switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 };
", "null", @"Error 2008 in line 2 at position 49: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch
{ 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16 };
", "null", @"Error 2008 in line 2 at position 42: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch { 0 => 0, 1 => 2, 2 => 4, 3 => 6, _ => 16
};
", "null", @"Error 2008 in line 2 at position 0: expected: comma; no final comma is allowed only if the switch expression is single-line
")]
	[DataRow(@"return 5 switch { };
", "null", @"Error 2033 in line 1 at position 18: the switch expression cannot be empty
")]
	[DataRow(@"return 100000000000000000*100000000000000000000;
", "0", @"Error 0001 in line 1 at position 26: too large number; long long type is under development
")]
	[DataRow(@"return ExecuteString(""return args[1];"", Q());
", """
/"return ExecuteString("return args[1];", Q());
"\
""", "Ошибок нет")]
	[DataRow("""var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();""",
"""/"var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();"\""",
			"Ошибок нет")]
	[DataRow(@"int x=null;
return x*1;
", "0", "Ошибок нет")]
	[DataRow(@"return куегкт;
", "null", @"Error 4001 in line 1 at position 7: the identifier ""куегкт"" is not defined in this location
")]
	[DataRow("""
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

""", """("3(x + 5)(x - 2)", "(x + 8)²", "Неразложимо", "Это не квадратный трехчлен", "-x²", "2x(x - 5.5)")""", "Ошибок нет")]
	public void Test(string Key, string TargetResult, string TargetErrors)
	{
		CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		String result;
		if ((result = ExecuteProgram(Key, out var errors)) == TargetResult
			&& (TargetErrors == null || errors == TargetErrors))
			return;
		throw new Exception("Error: @\"" + Key.Replace("\"", "\"\"") + "\"" + (result == TargetResult ? ""
			: " returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\""
			+ TargetResult.Replace("\"", "\"\"") + "\"") + (TargetErrors == null
			|| errors == TargetErrors ? "" : " and produced errors @\"" + errors.Replace("\"", "\"\"")
			+ "\" instead of @\"" + TargetErrors.Replace("\"", "\"\"") + "\"") + "!");
	}
}

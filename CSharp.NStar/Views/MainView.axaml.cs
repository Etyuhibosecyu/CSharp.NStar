using Avalonia.Controls;
using EasyEvalLib;
using Newtonsoft.Json;
using System.Globalization;

namespace CSharp.NStar.Views;

public partial class MainView : UserControl
{
	public MainView()
    {
        InitializeComponent();
		CultureInfo.CurrentCulture = CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		var s = new SemanticTree((LexemStream)new CodeSample(@"return (""7"" * ""2"", ""7"" * 2, 7 * ""2"", ""7"" / ""2"", ""7"" / 2, 7 / ""2"", ""7"" % ""2"", ""7"" % 2, 7 % ""2"", ""7"" - ""2"", ""7"" - 2, 7 - ""2"");
")).Parse(out var errorsList);
		var result = EasyEval.Eval(s, ["HighLevelAnalysis.Debug", "LowLevelAnalysis", "MidLayer", "Core", "EasyEval"], ["CSharp.NStar", "static EasyEvalLib.EasyEval"]);
		_ = (result, errorsList);
		string program, targetResult, targetErrors = "";
		if ((result = ExecuteProgram(program = @"return (""7"" * ""2"", ""7"" * 2, 7 * ""2"", ""7"" / ""2"", ""7"" / 2, 7 / ""2"", ""7"" % ""2"", ""7"" % 2, 7 % ""2"", ""7"" - ""2"", ""7"" - 2, 7 - ""2"");
", out var errors)) != (targetResult = @"(""77"", ""77"", ""2222222"", 3, 3, 3, 1, 1, 1, 5, 5, 5)") || errors != (targetErrors = @"Warning in line 1 at position 12: the string cannot be multiplied by string; one of them can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 41: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 52: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 59: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 70: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 81: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 88: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 99: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 110: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 117: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss") || (result = ExecuteProgram(program = @"real Function F(real x, real y)
{
	return x * x + x * y + y * y;
}
System.Func[real, real, real] f;
f = F;
real a = f(3.14159, 2.71828);
f = Max;
real b = f(3.14159, 2.71828);
return (a, b);
", out errors)) != (targetResult = "(25.798355151699997, 3.14159)") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"Class MyClass
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
", out errors)) != (targetResult = "(new MyClass(5, 3.14159, \"A\"), new MyClass(8, 2.71828, \"$\"), new MyClass(8, 2.71828, \"A\"), new MyClass(12, 3.14159, \"A\"))") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"real Function Factorial (int x)
{
	if (x <= 0)
		return 1;
	else
		return x * Factorial(x - 1);
}
return Factorial(100);", out errors)) != (targetResult = "9.33262154439441E+157") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"int n = 0;
while (n < 1000)
{
	n++;
}
return n;", out errors)) != (targetResult = "1000") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"real a = 0;
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
", out errors)) != (targetResult = "12") || errors != (targetErrors = @"Warning in line 1 at position 7: conversion from type ""byte"" to type ""real"" is possible but not recommended, you may lost data
") || (result = ExecuteProgram(program = @"list(3) int a = (((1, 2, 3), (4, 5, 6), (7, 8, 9)), ((10, 11, 12), (13, 14, 15), (16, 17, 18)), ((19, 20, 21), (22, 23, 24), (25, 26, 27)));
return a[1, 2, 3];
", out errors)) != (targetResult = "6") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"bool bool=bool;
", out errors)) != (targetResult = "null") || errors != (targetErrors = @"Error in line 1 at position 10: one cannot use the local variable ""bool"" before it is declared or inside such declaration in line 1 at position 0
") || (result = ExecuteProgram(program = @"return 100000000000000000*100000000000000000000;
", out errors)) != (targetResult = "null") || errors != (targetErrors = @"Error in line 1 at position 26: too large number; long long type is under development
") || (result = ExecuteProgram(program = @"int x=null;
return x*1;
", out errors)) != (targetResult = "null") || errors != (targetErrors = "Ошибок нет") || (result = ExecuteProgram(program = @"return куегкт;
", out errors)) != (targetResult = "null") || errors != (targetErrors = @"Error in line 1 at position 7: identifier ""куегкт"" is not defined in this location
"))
		{
			throw new Exception("Error: @\"" + program.Replace("\"", "\"\"") + "\"" + (result == targetResult ? "" : " returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\"" + targetResult.Replace("\"", "\"\"") + "\"" + (errors == targetErrors ? "" : " and")) + (errors == targetErrors ? "" : " produced errors @\"" + errors.Replace("\"", "\"\"") + "\" instead of @\"" + targetErrors.Replace("\"", "\"\"") + "\"") + "!");
			//ScintillaInput.Text = "Error: @\"" + program.Replace("\"", "\"\"") + "\"" + (result == targetResult ? "" : " returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\"" + targetResult.Replace("\"", "\"\"") + "\"" + (errors == targetErrors ? " and " : "")) + (errors == targetErrors ? "" : " produced errors @\"" + errors.Replace("\"", "\"\"") + "\" instead of @\"" + targetErrors.Replace("\"", "\"\"") + "\"") + "!";
			//return;
		}
		//var branchString = "(Main : (Class : MyClass#3, (ClassMain@MyClass : (Properties : (Property : (type :: int)#5, a#6, (Expr : 5i#8)), (Property : (type :: real)#11, b#12, (Expr : 3.14159r#14)), (Property : (type :: string)#17, c#18, (Expr : \"A\"#20))), (Methods : (Constructor : (Parameters : (Parameter : (type :: bool)#26, bool#27, no optional#28)), (Main@Constructor : (if : (Expr : (Hypername : bool#34))), (Main@Unnamed(#1) : (Expr : 12i#41, (Hypername : a#39), =#40))))))), (Expr : (Hypername : (new type :: MyClass)#54, ConstructorCall#55), (Declaration : (type :: MyClass)#50, a1#51), =#52), (Expr : (Hypername : (new type :: MyClass)#63, (ConstructorCall : 8i#65, 2.71828r#67, \"$\"#69)), (Declaration : (type :: MyClass)#59, a2#60), =#61), (Expr : (Hypername : (new type :: MyClass)#77, (ConstructorCall : 8i#79, 2.71828r#81)), (Declaration : (type :: MyClass)#73, a3#74), =#75), (Expr : (Hypername : (new type :: MyClass)#89, (ConstructorCall : true#91)), (Declaration : (type :: MyClass)#85, a4#86), =#87), (return : (Expr : (List : (Hypername : a1#97), (Hypername : a2#99), (Hypername : a3#101), (Hypername : a4#103)))))";
		//if (!(new MainParsing((LexemStream)new CodeSample(branchString), false).ParseTreeBranch(new(), out TreeBranch? branch) && branch != null && branch.ToString() == branchString))
		//{
		//	MessageBox.Show("Parsing failed!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//	return;
		//}
	}

	private static String ExecuteProgram(String program, out String errors)
	{
		try
		{
			Executions.ClearVariables();
			Executions.VariableAddRn();
			Executions.ClearProperties();
			UserDefinedConstantsList.Clear();
			UserDefinedConstructorsList.Clear();
			UserDefinedFunctionsList.Clear();
			UserDefinedImplementedInterfacesList.Clear();
			UserDefinedIndexersList.Clear();
			UserDefinedNamespacesList.Clear();
			UserDefinedPropertiesList.Clear();
			UserDefinedPropertiesMapping.Clear();
			UserDefinedPropertiesOrder.Clear();
			UserDefinedTypesList.Clear();
			VariablesList.Clear();
			var s = new SemanticTree((LexemStream)new CodeSample(program)).Parse(out var errorsList);
			var result = EasyEval.Eval(s, ["HighLevelAnalysis.Debug", "LowLevelAnalysis", "MidLayer", "Core", "EasyEval"], ["CSharp.NStar", "static EasyEvalLib.EasyEval"]);
			errors = errorsList == null || errorsList.Length == 0 ? "Ошибок нет" : String.Join("\r\n", [.. errorsList, .. ""]);
			return result is null ? "null" : JsonConvert.SerializeObject(result, SerializerSettings);
		}
		catch (OutOfMemoryException)
		{
			errors = "Memory limit exceeded during compilation or execution; program has not been executed\r\n";
			return "null";
		}
		catch
		{
			errors = "A serious error occurred during compilation or execution; program has not been executed\r\n";
			return "null";
		}
	}
}

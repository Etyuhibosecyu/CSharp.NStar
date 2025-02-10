#define RELEASE
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
	private readonly String enteredText = [];

	private static readonly ImmutableArray<string> minorVersions = ["2o"];
	private static readonly ImmutableArray<string> langs = ["C#"];
	private static readonly string AlphanumericCharactersWithoutDot = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
	private static readonly G.SortedSet<String> AutoCompletionList = new(new List<String>("abstract", "break", "case", "Class", "closed", "const", "Constructor", "continue", "default", "Delegate", "delete", "Destructor", "else", "Enum", "Event", "Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst", "Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref", "repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while", "and", "or", "xor", "is", "typeof", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "Infty", "Uncty", "Pi", "E", "CombineWith", "CloseOnReturnWith", "pow", "tetra", "penta", "hexa").AddRange(PrimitiveTypesList.Keys).AddRange(ExtraTypesList.Convert(x => x.Key.Namespace.Concat(".").AddRange(x.Key.Type))).AddRange(PublicFunctionsList.Keys));
	private static readonly G.SortedSet<string> AutoCompletionAfterDotList = new(PrimitiveTypesList.Values.ToList().AddRange(ExtraTypesList.Values).ConvertAndJoin(x => x.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList(x => PropertyMappingBack(x.Name)).AddRange(x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).ToList(x => FunctionMappingBack(x.Name)))).Filter(x => !x.Contains('_')));
	private static readonly Dictionary<String, (String TargetResult, String? TargetErrors)> testPrograms = new() { { """
		return ("7" * "2", "7" * 2, 7 * "2", "7" / "2", "7" / 2, 7 / "2", "7" % "2", "7" % 2, 7 % "2", "7" - "2", "7" - 2, 7 - "2");

		""", ("""("77", "77", "2222222", 3, 3, 3, 1, 1, 1, 5, 5, 5)""", @"Warning in line 1 at position 12: the string cannot be multiplied by string; one of them can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 41: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 52: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 59: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 70: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 81: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 88: the strings cannot be divided or give remainder (%); they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 99: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 110: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss
Warning in line 1 at position 117: the strings cannot be subtracted; they can be converted to number but this is not recommended and can cause data loss
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
", ("((5, 8), (5, 8))", "Ошибок нет") }, { @"real Function Factorial (int x)
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
", ("6", "Ошибок нет") }, { @"bool bool=bool;
", ("null", @"Error in line 1 at position 10: one cannot use the local variable ""bool"" before it is declared or inside such declaration in line 1 at position 0
") }, { @"bool Function One()
{
	int Function Two()
	{
		return -1;
	}
	return Two();
}
return One();
", ("false", @"Warning in line 7 at position 8: type of the returning value ""int"" and the function return type ""bool"" are badly compatible, you may lost data
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
", ("null", @"Wreck in line 13 at position 0: unclosed 2 nested comments in the end of code
") }, { @"return /""Hello, world!""ssssssssssssssss\;
", ("null", @"Wreck in line 2 at position 0: unexpected end of code reached; expected: 1 pairs ""double quote - reverse slash"" (starting with quote)
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
", (@"null", @"Error in line 2 at position 15: only variables can be assigned
") }, { @"return ;
", (@"null", @"Warning in line 1 at position 7: syntax ""return;"" is deprecated; consider using ""return null;"" instead
") }, { @"using System.Collections;
NList[int] bitList = new NList[int](10, 123, 456, 789, 111, 222, 333, 444, 555, 777);
return bitList;
", (@"(123, 456, 789, 111, 222, 333, 444, 555, 777)", "Ошибок нет") }, { @"list() int list = (1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list;
", (@"(1, 2, 3, 4, 5, 6, 7)", "Ошибок нет") }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list;
", (@"(1, 2, 3, 4, 5, 6, 7)", "Ошибок нет") }, { @"using System.Collections;
var hs = new ListHashSet[string]();
hs.Add(""1"");
hs.Add(""2"");
hs.Add(""3"");
hs.Add(""2"");
return hs;
", ("""("1", "2", "3")""", "Ошибок нет") }, { @"using System.Collections;
var dic = new Dictionary[string, int]();
dic.TryAdd(""1"", 1);
dic.TryAdd(""2"", 2);
dic.TryAdd(""3"", 3);
return dic;
", ("""(("1", 1), ("2", 2), ("3", 3))""", "Ошибок нет") }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.IndexOf(2, 2);
", (@"2", null) }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.LastIndexOf(2, 1);
", (@"0", null) }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Remove(2, 3);
", (@"(1, 5, 6, 7)", null) }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveAt(5);
", (@"(1, 2, 3, 4, 6, 7)", null) }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.RemoveEnd(5);
", (@"(1, 2, 3, 4)", null) }, { @"using System.Collections;
NList[int] list = new NList[int](3, 1, 2, 3);
list.Add(4);
list.Add((5, 6, 7));
return list.Reverse(2, 3);
", (@"(1, 4, 3, 2, 5, 6, 7)", null) }, { @"return 100000000000000000*100000000000000000000;
", (@"null", @"Error in line 1 at position 26: too large number; long long type is under development
") }, { """
return ExecuteString("return args[1];", Q());

""", ("""
/"return ExecuteString("return args[1];", Q());
"\
""", "Ошибок нет") }, { """var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();""",
			("""/"var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();var s = /"var s = /""\;return s.Insert(10, s) + Q();"\;return s.Insert(10, s) + Q();"\""",
			"Ошибок нет") }, { @"int x=null;
return x*1;
", (@"0", "Ошибок нет") }, { @"return куегкт;
", (@"null", @"Error in line 1 at position 7: identifier ""куегкт"" is not defined in this location
") }, { @"real Function D(real[3] abc)
{
	return abc[2] * abc[2] - 4 * abc[1] * abc[3];
}
string Function DecomposeSquareTrinomial(real[3] abc)
{
	if (abc[1] == 0)
		return ""Это не квадратный трехчлен"";
	var d = D(abc);
	string first;
	if (abc[1] == 1)
		first = """";
	else if (abc[1] == -1)
		first = ""-"";
	else
		first = abc[1];
	if (d < 0)
		return ""Неразложимо"";
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
		return ""x"";
	else if (n < 0)
		return ""(x - "" + (-n) + "")"";
	else
		return ""(x + "" + n + "")"";
}
return (DecomposeSquareTrinomial((3, 9, -30)), DecomposeSquareTrinomial((1, 16, 64)),
	DecomposeSquareTrinomial((-1, -1, -10)), DecomposeSquareTrinomial((0, 11, 5)),
	DecomposeSquareTrinomial((-1, 0, 0)), DecomposeSquareTrinomial((2, -11, 0)));
", ("""("3(x + 5)(x - 2)", "(x + 8)²", "Неразложимо", "Это не квадратный трехчлен", "-x²", "2x(x - 5.5)")""", "Ошибок нет") } };

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
			stringSpans[^1].RuleSet.Rules.Add(stringRule);
			stringSpans[^1].RuleSet.Spans.AddRange(stringSpans);
			stringSpans[^1].RuleSet.Spans.AddRange(nestedCommentSpans);
		}
		_ = TextBoxInput.TextArea.TextView.GetDocumentLineByVisualTop(TextBoxInput.TextArea.TextView.ScrollOffset.Y).LineNumber;
#if RELEASE
		ButtonSaveExe.IsEnabled = false;
		String result;
		foreach (var (Key, Value) in testPrograms)
		{
			if ((result = ExecuteProgram(Key, out var errors)) == Value.TargetResult
				&& (Value.TargetErrors == null || errors == Value.TargetErrors))
				continue;
			throw new Exception("Error: @\"" + Key.Replace("\"", "\"\"") + "\"" + (result == Value.TargetResult ? ""
				: " returned @\"" + result.Replace("\"", "\"\"") + "\" instead of @\""
				+ Value.TargetResult.Replace("\"", "\"\"") + "\"" + (Value.TargetErrors == null
				|| errors == Value.TargetErrors ? "" : " and produced errors @\"" + errors.Replace("\"", "\"\"")
				+ "\" instead of @\"" + Value.TargetErrors.Replace("\"", "\"\"") + "\"") + "!"));
		}
#endif
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

	private void UserControl_SizeChanged(object? sender, SizeChangedEventArgs e)
	{
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

	private void ButtonExecute_Click(object? sender, RoutedEventArgs e) => Execute();

	private void ButtonOpenCode_Click(object? sender, RoutedEventArgs e) => OpenCode();

	private void ButtonSaveCode_Click(object? sender, RoutedEventArgs e) => SaveCode();

	private void ButtonSaveExe_Click(object? sender, RoutedEventArgs e) => SaveExe();

	private void Copyrights_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => CopyrightsView.IsVisible = true;

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
		public IImage? Image => null;

		public string Text { get; private set; } = text;

		// Use this property if you want to show a fancy UIElement in the list.
		public object Content => Text;

		public object Description => "Description for " + Text;

		public double Priority { get; }

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs) =>
			textArea.Document.Replace(completionSegment, Text[Min(offset, Text.Length)..]);
	}
}

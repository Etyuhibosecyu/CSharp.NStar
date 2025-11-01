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
	private static readonly G.SortedSet<String> AutoCompletionList = [.. new List<String>("abstract", "break", "case", "Class",
		"closed", "const", "Constructor", "continue", "default", "Delegate", "delete", "Destructor", "else", "Enum", "Event",
		"Extent", "extern", "false", "for", "foreach", "Function", "if", "Interface", "internal", "lock", "loop", "multiconst",
		"Namespace", "new", "null", "Operator", "out", "override", "params", "protected", "public", "readonly", "ref",
		"repeat", "return", "sealed", "static", "Struct", "switch", "this", "throw", "true", "using", "while", "and", "or",
		"xor", "is", "typeof", "sin", "cos", "tan", "asin", "acos", "atan", "ln", "Infty", "Uncty", "Pi", "E", "CombineWith",
		"CloseOnReturnWith", "pow", "tetra", "penta", "hexa").AddRange(PrimitiveTypes.Keys)
		.AddRange(ExtraTypes.Convert(x => x.Key.Namespace.Concat(".").AddRange(x.Key.Type)))
		.AddRange(PublicFunctions.Keys)];
	private static readonly G.SortedSet<string> AutoCompletionAfterDotList = [.. PrimitiveTypes.Values.ToList()
		.AddRange(ExtraTypes.Values).ConvertAndJoin(x =>
		x.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
		.ToList(x => PropertyMappingBack(x.Name))
		.AddRange(x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
		.ToList(x => FunctionMappingBack(x.Name)))).Filter(x => !x.Contains('_'))];

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
		var completionData = completionWindow.CompletionList.CompletionData;
		if (e.Text == ".")
			enteredText.Replace(e.Text);
		else
			enteredText.AddRange(e.Text ?? "");
		if (enteredText.StartsWith('.'))
			completionData.AddRange(AutoCompletionAfterDotList.Filter(x => x.StartsWith(enteredText[1..])).Convert(x =>
				new MyCompletionData(x.ToString(), enteredText.Length - 1)));
		else
			completionData.AddRange(AutoCompletionList.Filter(x => x.StartsWith(enteredText)).Convert(x =>
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
		var outputPath = fileResult?.TryGetLocalPath() ?? "";
		if (string.IsNullOrEmpty(outputPath))
			return;
		var outputDir = Path.GetDirectoryName(outputPath);
		var assembly = Assembly.GetExecutingAssembly();
		// Получаем все зависимости
		var dependencies = GetAllDependencies(assembly);
		// Копируем все зависимости
		foreach (var dep in dependencies)
		{
			var targetPath = Path.Combine(outputDir!, dep.GetName().Name + ".dll");
			File.Copy(dep.Location, targetPath, true);
		}
		// Копируем основную сборку
		File.Copy(assembly.Location, outputPath, true);

		// Создаем runtimeconfig.json
		CreateRuntimeConfig(outputPath);
		File.WriteAllBytes(outputPath, CompileProgram(TextBoxInput.Text));
	}

	private List<Assembly> GetAllDependencies(Assembly assembly)
	{
		var dependencies = new List<Assembly>();
		var seen = new ListHashSet<string>();

		void AddDependencies(Assembly asm)
		{
			if (asm.FullName == null)
				return;
			if (seen.Contains(asm.FullName))
				return;

			seen.Add(asm.FullName);
			dependencies.Add(asm);

			foreach (var reference in asm.GetReferencedAssemblies())
			{
				try
				{
					var refAssembly = Assembly.Load(reference);
					AddDependencies(refAssembly);
				}
				catch { }
			}
		}

		AddDependencies(assembly);
		return dependencies;
	}

	private void CreateRuntimeConfig(string outputPath)
	{
		var config = $@"{{
    ""runtimeOptions"": {{
		""tfm"": ""net{Environment.Version.Major}.{Environment.Version.Minor}"",
        ""frameworks"": [
            {{
                ""name"": ""Microsoft.NETCore.App"",
                ""version"": ""{Environment.Version.Major}.{Environment.Version.Minor}.{Environment.Version.Build}""
            }}
        ],
        ""configProperties"": {{
            ""System.Runtime.Loader.AssemblyLoadContext.DebuggingEnabled"": true
        }}
    }}
}}";

		var configPath = Path.GetFileNameWithoutExtension(outputPath) + ".runtimeconfig.json";
		File.WriteAllText(configPath, config);
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

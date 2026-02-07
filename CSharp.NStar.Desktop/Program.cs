using Avalonia;
using ReactiveUI.Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static CSharp.NStar.SemanticTree;
using String = NStar.Core.String;

namespace CSharp.NStar.Desktop;

internal static class Program
{
#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable SYSLIB1054 // Используйте \"LibraryImportAttribute\" вместо \"DllImportAttribute\" для генерирования кода маршализации P/Invoke во время компиляции
	[DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
	public static extern IntPtr GetStdHandle(int nStdHandle);

	[DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
	public static extern int AllocConsole();

	private const int STD_OUTPUT_HANDLE = -11;
#pragma warning restore SYSLIB1054 // Используйте \"LibraryImportAttribute\" вместо \"DllImportAttribute\" для генерирования кода маршализации P/Invoke во время компиляции
#pragma warning restore IDE0079 // Удалить ненужное подавление

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		if (args.Length >= 1 && args[0].Equals("repl", StringComparison.OrdinalIgnoreCase))
		{
			if (AllocConsole() == 0)
				Environment.Exit(0);
			var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
			var safeFileHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true);
			var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
			var encoding = Console.OutputEncoding;
			var standardOutput = new StreamWriter(fileStream, encoding) { AutoFlush = true };
			Console.SetOut(standardOutput);
			REPL();
			Environment.Exit(0);
		}
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp() =>
		AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().UseReactiveUI();

	private static void REPL()
	{
		Console.WriteLine(@"Welcome to interactive C#.NStar coding console!
You can enter C#.NStar code here. Write empty line to finish and execute.");
		String program = [];
		while (true)
		{
			var readLine = Console.ReadLine();
			if (!string.IsNullOrEmpty(readLine))
			{
				program.AddRange(readLine).Add('\n');
				continue;
			}
			Console.WriteLine(TranslateAndExecuteProgram(program, [], out var errors, out _).ToString());
			if (errors != "Ошибок нет")
				Console.WriteLine("=====ERRORS=====");
			Console.WriteLine(errors.ToString());
			program = [];
		}
	}
}

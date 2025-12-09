namespace CSharp.NStar;

public static class CodeStyleRules
{
	private static readonly CodeStyleRule _charactersInLine = new(80, 100, 128, 200);
	private static readonly CodeStyleRule _linesInFunction = new(32, 64, 128, 256);
	private static readonly CodeStyleRule _functionsInClass = new(10, 16, 25, 50);
	public static RuleStrictness CharactersInLineStrictness { get; set; } = RuleStrictness.Lite;
	public static RuleStrictness LinesInFunctionStrictness { get; set; } = RuleStrictness.Lite;
	public static RuleStrictness FunctionsInClassStrictness { get; set; } = RuleStrictness.Normal;
	public static bool TestEnvironment { get; set; } = false;
	public static int MaxCharactersInLine => _charactersInLine[CharactersInLineStrictness];
	public static int MaxLinesInFunction => _linesInFunction[LinesInFunctionStrictness];
	public static int MaxFunctionsInClass => _functionsInClass[FunctionsInClassStrictness];
}

public record struct CodeStyleRule(int Strict, int Normal, int Lite, int UltraLite)
{
	public readonly int this[RuleStrictness strictness] => strictness switch
	{
		RuleStrictness.Strict => Strict,
		RuleStrictness.Normal => Normal,
		RuleStrictness.Lite => Lite,
		RuleStrictness.UltraLite => UltraLite,
		_ => int.MaxValue,
	};
}

public enum RuleStrictness : byte
{
	Strict,
	Normal,
	Lite,
	UltraLite,
	Disable,
}

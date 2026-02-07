using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CSharp.NStar.Views;

public partial class SettingsView : UserControl
{
	public SettingsView()
	{
		InitializeComponent();
	}

	private void ComboCharactersInLine_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboCharactersInLine == null)
			return;
		CodeStyleRules.CharactersInLineStrictness = (RuleStrictness)ComboCharactersInLine.SelectedIndex;
	}

	private void ComboLinesInFunction_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboLinesInFunction == null)
			return;
		CodeStyleRules.LinesInFunctionStrictness = (RuleStrictness)ComboLinesInFunction.SelectedIndex;
	}

	private void ComboFunctionsInClass_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (ComboFunctionsInClass == null)
			return;
		CodeStyleRules.FunctionsInClassStrictness = (RuleStrictness)ComboFunctionsInClass.SelectedIndex;
	}

	private void CheckBoxTestEnvironment_CheckedChanged(object? sender, RoutedEventArgs e)
	{
		if (CheckBoxTestEnvironment == null)
			return;
		CodeStyleRules.TestEnvironment = CheckBoxTestEnvironment.IsChecked ?? false;
	}

	private void Close_Click(object? sender, RoutedEventArgs e) => IsVisible = false;
}

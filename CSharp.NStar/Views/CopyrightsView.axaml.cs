using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace CSharp.NStar.Views;
public partial class CopyrightsView : UserControl
{
	public CopyrightsView() => InitializeComponent();

	private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (sender is not Button button || button.GetVisualChildren().FirstOrDefault() is not ContentPresenter presenter || presenter.GetVisualChildren().FirstOrDefault() is not TextBlock textBlock || textBlock.Text == null)
			return;
		if (OperatingSystem.IsWindows())
			Process.Start(new ProcessStartInfo(textBlock.Text) { UseShellExecute = true });
		else if (OperatingSystem.IsLinux())
			Process.Start("xdg-open", textBlock.Text);
	}

	private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => IsVisible = false;
}

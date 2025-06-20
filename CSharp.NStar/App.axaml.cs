global using Avalonia;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Markup.Xaml;
global using Corlib.NStar;
global using CSharp.NStar.ViewModels;
global using CSharp.NStar.Views;
global using Dictionaries.NStar;
global using LINQ.NStar;
global using System;
global using System.Diagnostics;
global using System.IO;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using G = System.Collections.Generic;
global using static CSharp.NStar.Quotes;
global using static CSharp.NStar.DeclaredConstructions;
global using static System.Math;
global using String = Corlib.NStar.String;

namespace CSharp.NStar;

public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

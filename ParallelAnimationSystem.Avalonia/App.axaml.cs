using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Avalonia.Generated;
using ParallelAnimationSystem.Avalonia.ViewModels;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;
using ShadUI;

namespace ParallelAnimationSystem.Avalonia;

public class App : Application, IDisposable
{
    private MainWindowViewModel? mainWindowViewModel;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection()
            .AddMvvm();
        
        var serviceProvider = services.BuildServiceProvider();
        var viewLocator = serviceProvider.GetRequiredService<IViewLocator>();
        DataTemplates.Add(viewLocator);
        
        mainWindowViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void Dispose()
    {
        mainWindowViewModel?.Dispose();
    }
}
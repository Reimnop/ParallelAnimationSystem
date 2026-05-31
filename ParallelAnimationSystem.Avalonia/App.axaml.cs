using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ParallelAnimationSystem.Avalonia.Generated;
using ParallelAnimationSystem.Avalonia.ViewModels;
using ParallelAnimationSystem.Avalonia.Views;
using Reimnop.MvvmHelper;

namespace ParallelAnimationSystem.Avalonia;

public partial class App : Application
{
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
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
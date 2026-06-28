using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Reimnop.MvvmHelper;

/// <summary>
/// Avalonia <see cref="IDataTemplate"/> that resolves Views from <see cref="IViewRegistry"/>.
/// </summary>
public sealed class ViewLocator(IViewRegistry registry) : IViewLocator
{
    /// <inheritdoc/>
    public bool Match(object? data)
        => data is not null && registry.IsRegistered(data.GetType());
 
    /// <inheritdoc/>
    public Control? Build(object? data)
    {
        if (data is null)
            return null;
 
        var view = registry.CreateView(data.GetType());
        view.DataContext = data;
        return view;
    }
}

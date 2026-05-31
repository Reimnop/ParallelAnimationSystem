using Avalonia.Controls;

namespace Reimnop.MvvmHelper;

/// <summary>
/// Generated base class for <see cref="IViewRegistry"/> implementations.
/// The generator creates a concrete subclass with a
/// <see cref="ViewModelViewAttribute"/>-decorated ViewModel classes,
/// and fills in the <see cref="ViewFactories"/> dictionary.
/// </summary>
public abstract class AbstractGeneratedViewRegistry : IViewRegistry
{
    protected abstract Dictionary<Type, Func<Control>> ViewFactories { get; }
    
    public bool IsRegistered(Type viewModelType)
        => ViewFactories.ContainsKey(viewModelType);

    public Control CreateView(Type viewModelType)
    {
        if (!ViewFactories.TryGetValue(viewModelType, out var factory))
            throw new InvalidOperationException($"No view registered for '{viewModelType.FullName}'");
        
        return factory();
    }
}
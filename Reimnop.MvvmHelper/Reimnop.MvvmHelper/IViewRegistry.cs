using Avalonia.Controls;

namespace Reimnop.MvvmHelper;

/// <summary>
/// Maps ViewModel <see cref="Type"/> → a factory that creates the matching View.
/// </summary>
public interface IViewRegistry
{
    /// <summary>Returns <c>true</c> when a View is registered for <paramref name="viewModelType"/>.</summary>
    bool IsRegistered(Type viewModelType);
 
    /// <summary>Creates and returns the View for the given ViewModel type.</summary>
    /// <exception cref="InvalidOperationException">No view is registered for the type.</exception>
    Control CreateView(Type viewModelType);
}

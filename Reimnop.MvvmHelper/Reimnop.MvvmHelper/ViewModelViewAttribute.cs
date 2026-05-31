using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace Reimnop.MvvmHelper;

/// <summary>
/// Place on a <see cref="ViewModelBase"/> subclass to declare which Avalonia
/// <see cref="Control"/> renders it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViewModelViewAttribute : Attribute
{
    /// <summary>The Avalonia <see cref="Control"/> type that renders this ViewModel.</summary>
    public Type ViewType { get; }
 
    /// <param name="viewType">
    /// Must be a concrete <see cref="Control"/> with a public parameterless constructor.
    /// </param>
    public ViewModelViewAttribute(Type viewType)
    {
        ViewType = viewType;
    }
}

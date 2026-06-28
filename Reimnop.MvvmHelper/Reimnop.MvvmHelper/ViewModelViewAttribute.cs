using Avalonia.Controls;

namespace Reimnop.MvvmHelper;

/// <summary>
/// Place on a view model class to declare which Avalonia
/// <see cref="Control"/> renders it.
/// <typeparam name="T">Must be a concrete <see cref="Control"/> with a public parameterless constructor.</typeparam>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViewModelViewAttribute<T> : Attribute where T : Control, new();

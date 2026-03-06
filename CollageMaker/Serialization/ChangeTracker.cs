using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CollageMaker.Serialization;

/// <summary>
/// Tracks which properties have been explicitly set on <see cref="INotifyPropertyChanged"/> objects.
/// Uses a <see cref="ConditionalWeakTable{TKey,TValue}"/> so tracking data is
/// automatically collected when the tracked object goes out of scope.
/// </summary>
internal static class ChangeTracker
{
    private static readonly ConditionalWeakTable<object, HashSet<string>> TrackedObjects = [];

    /// <summary>
    /// Begins tracking property changes on the specified object.
    /// </summary>
    public static T Track<T>(T obj) where T : INotifyPropertyChanged
    {
        HashSet<string> set = [];
        TrackedObjects.AddOrUpdate(obj, set);
        obj.PropertyChanged += (_, e) => OnPropertyChanged(set, e);
        return obj;
    }

    /// <summary>
    /// Explicitly marks a property as set, for cases where the assigned value
    /// equals the field default and <see cref="INotifyPropertyChanged"/> would not fire.
    /// </summary>
    public static void MarkPropertySet(object obj, string propertyName)
    {
        if (TrackedObjects.TryGetValue(obj, out HashSet<string>? set))
            set.Add(propertyName);
    }

    /// <summary>
    /// Returns whether the specified property was set on a tracked object.
    /// </summary>
    internal static bool WasPropertySet(object obj, string propertyName)
    {
        return TrackedObjects.TryGetValue(obj, out HashSet<string>? set)
            && set.Contains(propertyName);
    }

    /// <summary>
    /// Returns whether the specified object is being tracked.
    /// </summary>
    internal static bool IsTracked(object obj)
    {
        return TrackedObjects.TryGetValue(obj, out _);
    }

    private static void OnPropertyChanged(HashSet<string> set, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null)
            set.Add(e.PropertyName);
    }
}

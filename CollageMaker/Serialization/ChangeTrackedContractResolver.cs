using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace CollageMaker.Serialization;

/// <summary>
/// Contract resolver that only serializes properties explicitly set on change-tracked objects.
/// For untracked objects, default serialization behavior is preserved.
/// </summary>
internal sealed class ChangeTrackedContractResolver : DefaultContractResolver
{
    /// <summary>
    /// Creates a property with a <see cref="JsonProperty.ShouldSerialize"/> predicate
    /// that checks change tracking for tracked instances.
    /// </summary>
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        string memberName = member.Name;
        Predicate<object>? original = property.ShouldSerialize;
        property.ShouldSerialize = instance => ShouldSerialize(instance, memberName, original);
        return property;
    }

    private static bool ShouldSerialize(object instance, string memberName, Predicate<object>? fallback)
    {
        return ChangeTracker.IsTracked(instance) ? 
            ChangeTracker.WasPropertySet(instance, memberName) : 
            fallback?.Invoke(instance) ?? true;
    }
}

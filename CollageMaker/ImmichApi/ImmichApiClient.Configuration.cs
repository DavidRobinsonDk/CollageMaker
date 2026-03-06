using CollageMaker.Serialization;
using Newtonsoft.Json;

namespace CollageMaker.ImmichApi;

/// <summary>
/// Configures serialization settings for the auto-generated Immich API client.
/// Uses property change tracking so only explicitly set properties are serialized.
/// </summary>
public partial class ImmichApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.ContractResolver = new ChangeTrackedContractResolver();
    }
}

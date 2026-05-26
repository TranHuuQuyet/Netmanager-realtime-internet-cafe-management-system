using System.Text.Json;
using System.Text.Json.Serialization;

namespace NETManager.Shared.Utilities.JsonHelper;


public static class JsonSerializerOptions
{

    public static readonly System.Text.Json.JsonSerializerOptions Shared = new()
    {
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
        Converters =
        {
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
        }

    };
}

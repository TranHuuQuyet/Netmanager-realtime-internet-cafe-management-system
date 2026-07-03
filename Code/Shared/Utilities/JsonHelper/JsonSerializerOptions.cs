using System.Text.Json;
using System.Text.Json.Serialization;

// Namespace chua helper JSON dung chung.
namespace Shared.Utilities.JsonHelper;


// Cau hinh serialize/deserialize JSON dung chung cho client va server.
public static class JsonSerializerOptions
{

    // Options thong nhat de cac ben doc/ghi JSON cung mot cach.
    public static readonly System.Text.Json.JsonSerializerOptions Shared = new()
    {
        // Khong tu dong bo qua khac biet hoa/thuong cua ten property.
        PropertyNameCaseInsensitive = false,

        // Khi ghi JSON thi bo cac property null.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Khong doi ten property theo camelCase/PascalCase ngoai JsonPropertyName da khai bao.
        PropertyNamingPolicy = null,

        // Enum se ghi/doc dang chuoi, khong cho phep so de tranh sai contract.
        Converters =
        {
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
        }

    };
}

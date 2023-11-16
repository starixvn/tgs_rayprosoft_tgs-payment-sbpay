using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.SBPay.Client;

public static class SBPayUtility
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Converters = {
        }
    };
}

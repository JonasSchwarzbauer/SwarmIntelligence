using System.Text.Json.Serialization;

namespace Webserver.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SwarmMode
    {
        Formation = 0,
        Manual = 1,
    }
}

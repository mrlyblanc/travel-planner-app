using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelPlannerApp.Application.Common.Utilities;

public static class AuditJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

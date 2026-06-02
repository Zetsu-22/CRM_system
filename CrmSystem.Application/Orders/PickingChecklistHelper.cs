using System.Text.Json;
using System.Text.Json.Serialization;
using CrmSystem.Application.Orders.Dtos;

namespace CrmSystem.Application.Orders;

/// <summary>Сериализация/десериализация JSON чек-листа сборки из/в строку.</summary>
public static class PickingChecklistHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static List<PickingChecklistItem> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<PickingChecklistItem>();
        return JsonSerializer.Deserialize<List<PickingChecklistItem>>(json, Options)
               ?? new List<PickingChecklistItem>();
    }

    public static string Serialize(List<PickingChecklistItem> items) =>
        JsonSerializer.Serialize(items, Options);

    public static PickingChecklistResponse ToResponse(Guid orderId, List<PickingChecklistItem> items)
    {
        var allDone = items.Count > 0 && items.All(x => x.Done);
        return new PickingChecklistResponse(orderId, allDone, items);
    }
}

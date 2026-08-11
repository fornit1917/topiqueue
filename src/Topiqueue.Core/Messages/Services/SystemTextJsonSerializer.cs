using System.Text.Json;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.Core.Messages.Services;

public class SystemTextJsonSerializer : ITpqMessageDataSerializer
{
    private readonly JsonSerializerOptions _opts;

    public SystemTextJsonSerializer(JsonSerializerOptions opts)
    {
        _opts = opts;
    }

    public string SerializeToText<T>(T data)
    {
        return JsonSerializer.Serialize(data, _opts);
    }

    public T? DeserializeFromText<T>(string? text)
    {
        if (text == null)
            return default;
        
        return JsonSerializer.Deserialize<T>(text, _opts);
    }
}
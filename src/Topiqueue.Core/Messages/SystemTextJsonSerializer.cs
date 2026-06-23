using System.Text.Json;

namespace Topiqueue.Core.Messages;

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
}
namespace Topiqueue.Core.Messages.Interfaces;

public interface ITpqMessageDataSerializer
{
    string SerializeToText<T>(T data);
    T? DeserializeFromText<T>(string? text);
}
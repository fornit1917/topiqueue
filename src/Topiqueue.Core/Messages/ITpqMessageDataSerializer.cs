namespace Topiqueue.Core.Messages;

public interface ITpqMessageDataSerializer
{
    string SerializeToText<T>(T data);
}
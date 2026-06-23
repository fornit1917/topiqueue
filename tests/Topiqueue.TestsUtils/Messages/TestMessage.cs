using Topiqueue.Core.Messages;

namespace Topiqueue.TestsUtils.Messages;

public class TestMessageData : ITpqMessageData
{
    public static string GetMessageType() => "TestMessage";
}
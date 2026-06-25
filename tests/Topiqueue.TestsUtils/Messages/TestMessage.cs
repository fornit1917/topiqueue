using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.TestsUtils.Messages;

public class TestMessageData : ITpqMessageData
{
    public static string GetMessageType() => "TestMessage";
}
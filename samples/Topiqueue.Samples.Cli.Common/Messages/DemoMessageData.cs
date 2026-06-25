using Topiqueue.Core.Messages;
using Topiqueue.Core.Messages.Interfaces;

namespace Topiqueue.Samples.Cli.Common.Messages;

public class DemoMessageData : ITpqMessageData
{
    public int Id { get; set; }
    public string Value { get; set; }
    
    public static string GetMessageType() => "DemoMessage";
}
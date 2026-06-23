using System;

namespace Topiqueue.Core.Exceptions;

public class UnknownTopicException : Exception
{
    public UnknownTopicException(string topicName)
        : base(
            $"The topic '{topicName}' is unknown. All topics must be passed to the method when configuring the Topiqueue library.")
    {
        
    }
}
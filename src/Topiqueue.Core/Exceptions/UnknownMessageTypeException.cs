using System;

namespace Topiqueue.Core.Exceptions;

public class UnknownMessageTypeException : Exception
{
    public  UnknownMessageTypeException()
    {
    }

    public UnknownMessageTypeException(string message) : base(message)
    {
    }
}
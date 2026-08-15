using System;

namespace Topiqueue.Core.Exceptions;

public class InvalidMessageException : Exception
{
    public InvalidMessageException(string message) : base(message) { }
}
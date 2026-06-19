using System;

namespace Topiqueue.Core.Exceptions;

public class InvalidTopiqueueConfigException : Exception
{
    public InvalidTopiqueueConfigException(string message) : base(message) { }
}
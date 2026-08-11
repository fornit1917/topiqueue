using System;

namespace Topiqueue.Core.Exceptions;

public class CreateHandlerException : Exception
{
    public CreateHandlerException(string message) : base(message) { }
}
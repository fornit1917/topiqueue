using System;

namespace Topiqueue.Core.Exceptions;

public class UnexpectedDbResultException : Exception
{
    public UnexpectedDbResultException(string message) : base(message) { }
}
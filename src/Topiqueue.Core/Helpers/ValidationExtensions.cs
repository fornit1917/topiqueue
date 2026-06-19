using System;

namespace Topiqueue.Core.Helpers;

internal static class ValidationExtensions
{
    public static int EnsureGreaterThan(this int value, int min, string argumentName)
    {
        return value <= min 
            ? throw new ArgumentException($"Value of {argumentName} must be greater than {min}") 
            : value;
    }

    public static TimeSpan EnsureGreaterThan(this TimeSpan value, TimeSpan min, string argumentName)
    {
        return value <= min 
            ? throw new ArgumentException($"Value of {argumentName} must be greater than {min}") 
            : value;
    }

    public static string EnsureNotEmpty(this string? value, string argumentName)
    {
        return string.IsNullOrEmpty(value) 
            ? throw new ArgumentException($"Value of {argumentName} cannot be empty") 
            : value;
    }
}
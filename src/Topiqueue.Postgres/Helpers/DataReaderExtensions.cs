using System;
using System.Data.Common;

namespace Topiqueue.Postgres.Helpers;

internal static class DataReaderExtensions
{
    public static string? GetNullableString(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetString(index);
    }

    public static DateTime? GetNullableDatetime(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetDateTime(index);
    }

    public static Guid? GetNullableGuid(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(index))
        {
            return null;
        }
        return reader.GetGuid(index);
    }
}
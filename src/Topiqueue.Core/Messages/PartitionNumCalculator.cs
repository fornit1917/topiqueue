using System;
using System.IO.Hashing;
using System.Text;

namespace Topiqueue.Core.Messages;

internal class PartitionNumCalculator : IPartitionNumCalculator
{
    public static readonly PartitionNumCalculator Instance = new();
    
    public int GetPartitionNum(string? partitionKey, int partitionsCount)
    {
        object partitionCount;
        if (string.IsNullOrEmpty(partitionKey))
        {
            return Random.Shared.Next(0, partitionsCount);
        }
        
        int utf8ByteCount = Encoding.UTF8.GetByteCount(partitionKey);
        uint hash;
        if (utf8ByteCount <= 256)
        {
            Span<byte> buffer = stackalloc byte[utf8ByteCount];
            Encoding.UTF8.GetBytes(partitionKey, buffer);
            hash = Crc32.HashToUInt32(buffer);
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes(partitionKey);
            hash = Crc32.HashToUInt32(bytes);
        }
        
        return Math.Abs((int)hash) % partitionsCount;
    }
}
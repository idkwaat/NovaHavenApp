using System.Buffers.Binary;

namespace NovaHaven.Application.Common.Concurrency;

public static class ConcurrencyVersion
{
    public static byte[] ToBytes(uint version)
    {
        var bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, version);
        return bytes;
    }

    public static bool Matches(uint currentVersion, byte[]? expectedVersion) =>
        expectedVersion is { Length: sizeof(uint) }
            && expectedVersion.AsSpan().SequenceEqual(ToBytes(currentVersion));
}

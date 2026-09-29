using System.Buffers.Binary;

namespace NovaHaven.Application.Features.Media;

public static class WikiMediaInspector
{
    public const long MaxBytes = 5_000_000;
    private const int MaxDimension = 10_000;

    public static bool TryInspect(ReadOnlySpan<byte> bytes, string declaredContentType, out WikiMediaInspection inspection)
    {
        inspection = default!;
        if (bytes.Length == 0 || bytes.Length > MaxBytes) return false;

        if (IsPng(bytes, out var pngWidth, out var pngHeight))
        {
            if (!ContentTypeIs(declaredContentType, "image/png") || !ValidDimensions(pngWidth, pngHeight)) return false;
            inspection = new("image/png", ".png", pngWidth, pngHeight);
            return true;
        }

        if (IsJpeg(bytes, out var jpegWidth, out var jpegHeight))
        {
            if (!ContentTypeIs(declaredContentType, "image/jpeg") || !ValidDimensions(jpegWidth, jpegHeight)) return false;
            inspection = new("image/jpeg", ".jpg", jpegWidth, jpegHeight);
            return true;
        }

        if (IsWebp(bytes, out var webpWidth, out var webpHeight))
        {
            if (!ContentTypeIs(declaredContentType, "image/webp") || !ValidDimensions(webpWidth, webpHeight)) return false;
            inspection = new("image/webp", ".webp", webpWidth, webpHeight);
            return true;
        }

        return false;
    }

    private static bool ContentTypeIs(string declared, string expected) =>
        string.Equals(declared?.Split(';')[0].Trim(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool ValidDimensions(int width, int height) => width > 0 && height > 0 &&
        width <= MaxDimension && height <= MaxDimension;

    private static bool IsPng(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = height = 0;
        if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != (byte)'P' || bytes[2] != (byte)'N' ||
            bytes[3] != (byte)'G' || bytes[4] != 0x0d || bytes[5] != 0x0a || bytes[6] != 0x1a || bytes[7] != 0x0a)
            return false;
        if (bytes[12] != (byte)'I' || bytes[13] != (byte)'H' || bytes[14] != (byte)'D' || bytes[15] != (byte)'R')
            return false;
        width = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(16, 4));
        height = BinaryPrimitives.ReadInt32BigEndian(bytes.Slice(20, 4));
        return true;
    }

    private static bool IsJpeg(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = height = 0;
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8) return false;
        var offset = 2;
        while (offset + 3 < bytes.Length)
        {
            if (bytes[offset++] != 0xff) continue;
            while (offset < bytes.Length && bytes[offset] == 0xff) offset++;
            if (offset >= bytes.Length) return false;
            var marker = bytes[offset++];
            if (marker is 0xd8 or 0xd9) continue;
            if (marker == 0xda) return false;
            if (offset + 1 >= bytes.Length) return false;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length) return false;
            if (IsJpegFrameMarker(marker))
            {
                if (segmentLength < 7) return false;
                height = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3, 2));
                width = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5, 2));
                return true;
            }
            offset += segmentLength;
        }
        return false;
    }

    private static bool IsJpegFrameMarker(byte marker) =>
        marker is >= 0xc0 and <= 0xc3 or >= 0xc5 and <= 0xc7 or >= 0xc9 and <= 0xcb or >= 0xcd and <= 0xcf;

    private static bool IsWebp(ReadOnlySpan<byte> bytes, out int width, out int height)
    {
        width = height = 0;
        if (bytes.Length < 30 || !bytes[..4].SequenceEqual("RIFF"u8) || !bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
            return false;

        if (bytes.Slice(12, 4).SequenceEqual("VP8X"u8) && bytes.Length >= 30)
        {
            width = 1 + ReadUInt24LittleEndian(bytes.Slice(24, 3));
            height = 1 + ReadUInt24LittleEndian(bytes.Slice(27, 3));
            return true;
        }

        if (bytes.Slice(12, 4).SequenceEqual("VP8L"u8) && bytes.Length >= 25 && bytes[20] == 0x2f)
        {
            width = 1 + (bytes[21] | ((bytes[22] & 0x3f) << 8));
            height = 1 + (((bytes[22] >> 6) & 0x03) | (bytes[23] << 2) | ((bytes[24] & 0x0f) << 10));
            return true;
        }

        if (bytes.Slice(12, 4).SequenceEqual("VP8 "u8) && bytes.Length >= 30 &&
            bytes[23] == 0x9d && bytes[24] == 0x01 && bytes[25] == 0x2a)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(26, 2)) & 0x3fff;
            height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(28, 2)) & 0x3fff;
            return true;
        }

        return false;
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
        bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
}

namespace NovaHaven.Api.Http;

public static class RowVersionEtag
{
    public static string Format(byte[] rowVersion)
    {
        ArgumentNullException.ThrowIfNull(rowVersion);
        return $"\"{Convert.ToBase64String(rowVersion)}\"";
    }

    /// <summary>Returns null for a missing header and an empty token for any non-canonical/stale value.</summary>
    public static byte[]? ParseExpectedVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"') return [];

        try
        {
            var rowVersion = Convert.FromBase64String(value[1..^1]);
            return string.Equals(Format(rowVersion), value, StringComparison.Ordinal) ? rowVersion : [];
        }
        catch (FormatException)
        {
            return [];
        }
    }
}

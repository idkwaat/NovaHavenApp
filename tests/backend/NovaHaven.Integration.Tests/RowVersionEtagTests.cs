using NovaHaven.Api.Http;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class RowVersionEtagTests
{
    [Fact]
    public void Format_and_parse_round_trip_the_exact_row_version()
    {
        byte[] rowVersion = [1, 2, 3, 254];

        var etag = RowVersionEtag.Format(rowVersion);

        Assert.Equal(rowVersion, RowVersionEtag.ParseExpectedVersion(etag));
    }

    [Fact]
    public void Missing_and_non_canonical_tokens_remain_distinguishable()
    {
        Assert.Null(RowVersionEtag.ParseExpectedVersion(null));
        Assert.Null(RowVersionEtag.ParseExpectedVersion(" "));
        Assert.Empty(RowVersionEtag.ParseExpectedVersion("*")!);
        Assert.Empty(RowVersionEtag.ParseExpectedVersion("\" AQID \"")!);
        Assert.Empty(RowVersionEtag.ParseExpectedVersion("W/\"AQID\"")!);
    }
}

using SharePrint.Application.Listings;

namespace SharePrint.Api.IntegrationTests;

public class DescriptionPreviewTests
{
    [Fact]
    public void Empty_input_returns_empty()
    {
        Assert.Equal("", DescriptionPreview.From(""));
        Assert.Equal("", DescriptionPreview.From("   "));
        Assert.Equal("", DescriptionPreview.From(null!));
    }

    [Fact]
    public void Short_description_returns_unchanged_no_ellipsis()
    {
        Assert.Equal("five word description right here", DescriptionPreview.From("five word description right here"));
    }

    [Fact]
    public void Truncates_at_50_words_when_chars_under_200()
    {
        var input = string.Join(' ', Enumerable.Repeat("ab", 80)); // 80 two-char words = 239 chars w/ spaces
        var preview = DescriptionPreview.From(input);
        Assert.EndsWith("…", preview);
        var wordCount = preview.TrimEnd('…').Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.Equal(50, wordCount);
    }

    [Fact]
    public void Truncates_at_200_chars_when_words_under_50()
    {
        // 10 words of 30 chars each = ~309 chars
        var input = string.Join(' ', Enumerable.Repeat(new string('x', 30), 10));
        var preview = DescriptionPreview.From(input);
        Assert.EndsWith("…", preview);
        Assert.True(preview.Length <= 201, $"preview length was {preview.Length}");
    }

    [Fact]
    public void Trims_surrounding_whitespace()
    {
        Assert.Equal("hello world", DescriptionPreview.From("   hello world   "));
    }
}
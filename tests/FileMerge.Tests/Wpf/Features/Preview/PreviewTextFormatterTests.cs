using FileMerger.Wpf.Features.Preview;

namespace FileMerger.Tests.Wpf.Features.Preview;

public sealed class PreviewTextFormatterTests
{
    [Fact]
    public void Format_Should_Throw_When_Content_Is_Null()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => PreviewTextFormatter.Format(null!));

        Assert.Equal("fullContent", ex.ParamName);
    }

    [Fact]
    public void Format_Should_Return_Full_Content_When_Content_Is_Shorter_Than_Limit()
    {
        string content = "short preview";

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content);

        Assert.Equal(content, result.Text);
        Assert.Equal(content.Length, result.TotalCharacters);
        Assert.Equal(content.Length, result.DisplayedCharacters);
        Assert.False(result.WasTruncated);
        Assert.Equal(0, result.OmittedCharacters);
    }

    [Fact]
    public void Format_Should_Return_Full_Content_When_Content_Length_Equals_Limit()
    {
        string content = new('x', PreviewTextFormatter.MaxPreviewCharacters);

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content);

        Assert.Equal(content, result.Text);
        Assert.Equal(content.Length, result.TotalCharacters);
        Assert.Equal(content.Length, result.DisplayedCharacters);
        Assert.False(result.WasTruncated);
        Assert.Equal(0, result.OmittedCharacters);
    }

    [Fact]
    public void Format_Should_Truncate_Content_And_Report_OmittedCharacters_When_Content_Exceeds_Limit()
    {
        string visiblePart = new('x', PreviewTextFormatter.MaxPreviewCharacters);
        string omittedPart = "abc";
        string content = visiblePart + omittedPart;

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content);

        Assert.True(result.WasTruncated);
        Assert.Equal(content.Length, result.TotalCharacters);
        Assert.Equal(PreviewTextFormatter.MaxPreviewCharacters, result.DisplayedCharacters);
        Assert.Equal(omittedPart.Length, result.OmittedCharacters);
        Assert.StartsWith(visiblePart, result.Text);
        Assert.DoesNotContain(omittedPart, result.Text);
    }

    [Fact]
    public void Format_Should_Append_Truncation_Footer_When_Content_Exceeds_Limit()
    {
        string content = new('x', PreviewTextFormatter.MaxPreviewCharacters + 1);

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content);

        Assert.True(result.WasTruncated);
        Assert.Contains("// Preview truncated. 1 character(s) omitted.", result.Text);
        Assert.Contains("// Save still writes the full output.", result.Text);
    }

    [Fact]
    public void Format_Should_Report_TotalDisplayed_And_OmittedCharacters_When_Content_Is_Truncated()
    {
        string content = new('x', PreviewTextFormatter.MaxPreviewCharacters + 3);

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content);

        Assert.Equal(PreviewTextFormatter.MaxPreviewCharacters + 3, result.TotalCharacters);
        Assert.Equal(PreviewTextFormatter.MaxPreviewCharacters, result.DisplayedCharacters);
        Assert.Equal(3, result.OmittedCharacters);
        Assert.True(result.WasTruncated);
    }

    [Fact]
    public void Format_Should_Use_Custom_Display_Limit()
    {
        const int customLimit = 10;
        string content = "0123456789abc";

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content, customLimit);

        Assert.Equal(customLimit, result.DisplayedCharacters);
        Assert.Equal(content.Length - customLimit, result.OmittedCharacters);
        Assert.StartsWith(content[..customLimit], result.Text);
    }

    [Fact]
    public void Format_Should_Return_Full_Content_When_Content_Length_Equals_Custom_Limit()
    {
        const int customLimit = 10;
        string content = new('x', customLimit);

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content, customLimit);

        Assert.Equal(content, result.Text);
        Assert.Equal(customLimit, result.DisplayedCharacters);
        Assert.False(result.WasTruncated);
    }

    [Fact]
    public void Format_Should_Truncate_Content_When_Content_Exceeds_Custom_Limit()
    {
        const int customLimit = 10;
        string content = new('x', customLimit + 5);

        PreviewTextFormatResult result = PreviewTextFormatter.Format(content, customLimit);

        Assert.True(result.WasTruncated);
        Assert.Equal(customLimit, result.DisplayedCharacters);
        Assert.Equal(5, result.OmittedCharacters);
        Assert.StartsWith(content[..customLimit], result.Text);
        Assert.Contains("Save still writes the full output.", result.Text);
    }

    [Fact]
    public void Format_Should_Throw_When_Custom_Limit_Is_Zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreviewTextFormatter.Format("content", 0));
    }

    [Fact]
    public void Format_Should_Throw_When_Custom_Limit_Is_Negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PreviewTextFormatter.Format("content", -1));
    }
}
using SharePrint.Application.Abstractions;

namespace SharePrint.Api.IntegrationTests;

public class PictureValidatorTests
{
        [Fact]
    public void Accepts_jpeg_magic_bytes()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 };
        Assert.True(PictureValidation.IsAlloedImage(bytes, "image/jpeg", maxBytes: 1024, out _));
    }

    [Fact]
    public void Accepts_png_magic_bytes()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3 };
        Assert.True(PictureValidation.IsAlloedImage(bytes, "image/png", maxBytes: 1024, out _));
    }

    [Fact]
    public void Accepts_webp_magic_bytes()
    {
        // RIFF....WEBP
        var bytes = new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50, 1, 2, 3 };
        Assert.True(PictureValidation.IsAlloedImage(bytes, "image/webp", maxBytes: 1024, out _));
    }

    [Fact]
    public void Rejects_pdf_claiming_jpeg_mime()
    {
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 }; // %PDF
        Assert.False(PictureValidation.IsAlloedImage(bytes, "image/jpeg", maxBytes: 1024, out var error));
        Assert.Equal("magic_byte_mismatch", error);
    }

    [Fact]
    public void Rejects_unknown_mime()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 };
        Assert.False(PictureValidation.IsAlloedImage(bytes, "image/gif", maxBytes: 1024, out var error));
        Assert.Equal("mime_not_allowed", error);
    }

    [Fact]
    public void Rejects_oversize()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 };
        Assert.False(PictureValidation.IsAlloedImage(bytes, "image/jpeg", maxBytes: 4, out var error));
        Assert.Equal("size_exceeded", error);
    }

    [Fact]
    public void Rejects_short_buffer()
    {
        var bytes = new byte[] { 0xFF, 0xD8 };
        Assert.False(PictureValidation.IsAlloedImage(bytes, "image/jpeg", maxBytes: 1024, out var error));
        Assert.Equal("magic_byte_mismatch", error);
    }
}
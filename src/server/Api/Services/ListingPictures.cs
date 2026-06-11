using SharePrint.Application.Listings;

namespace SharePrint.Api.Services;

internal static class ListingPictures
{
    public static string PictureUrl(string storageKey) => $"/api/pictures/{storageKey}";

    public static async Task<bool> ValidateImageAsync(IFormFile file)
    {
        if (file.Length > PictureValidator.DefaultMaxBytes) return false;
        var buffer = new byte[Math.Min(file.Length, 16)];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(buffer);
        return PictureValidator.IsAllowedImage(
            buffer.AsSpan(0, read),
            file.ContentType,
            out _);
    }
}

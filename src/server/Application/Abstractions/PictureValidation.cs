namespace SharePrint.Application.Abstractions;

public class PictureValidation
{
    public const long DefaultMaxBytes = 5 * 1024 * 1024; // 5 MB

    public static bool IsAlloedImage(ReadOnlySpan<byte> header, string contentType, long maxBytes, out string error)
    {
        if (header.Length > maxBytes)
        {
            error = "size_exceeded";
            return false;
        }

        switch (contentType?.ToLowerInvariant())
        {
            case "image/jpeg":
                if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                { error = ""; return true; }
                break;
            case "image/png":
                if (header.Length >= 8
                    && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                    && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                { error = ""; return true; }
                break;
            case "image/webp":
                if (header.Length >= 12
                    && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                    && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                { error = ""; return true; }
                break;
            default:
                error = "mime_not_allowed";
                return false;
        }

        error = "magic_byte_mismatch";
        return false;
    }
}
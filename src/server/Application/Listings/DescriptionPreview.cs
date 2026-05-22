namespace SharePrint.Application.Listings;

public static class DescriptionPreview
{
    public static string From(string description, int maxWords = 50, int maxChars = 200)
    {
        if (string.IsNullOrWhiteSpace(description)) return "";

        var trimmed = description.Trim();

        var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var byWords = words.Length <= maxWords
            ? trimmed : string.Join(' ', words.Take(maxWords));

        var byChars = byWords.Length <= maxChars
            ? byWords
            : byWords[..maxWords].TrimEnd();
        
        return byChars == trimmed ? trimmed : byChars + "…";
    }
}
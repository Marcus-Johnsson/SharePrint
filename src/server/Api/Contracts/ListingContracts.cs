namespace SharePrint.Api.Contracts;

public class ListingContracts
{
    public record ListingSummary(
        Guid Id,
        string Title,
        string DescriptionPreview,
        decimal Price,
        string MarketPictureLocation,
        string SellerUsername);

    public record DescriptionPicture(Guid Id, string Url);

    public record ListingDetail(
        Guid Id,
        string Title,
        string Description,
        decimal Price,
        string MarketPictureLocation,
        IReadOnlyList<DescriptionPicture> DescriptionPictures,
        string SellerUsername,
        string Status);

    public record UpdateListingRequest(string Title, string Description, decimal Price);
}
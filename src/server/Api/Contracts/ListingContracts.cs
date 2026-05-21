namespace SharePrint.Api.Contracts;

public class ListingContracts
{
    public record ListingSummary(Guid Id, string Title, decimal Price, string MarketPictureLocation, string DescriptionPictureLocation, string SellerUsername);
    public record ListingDetail(Guid Id, string Title, string Description, decimal Price, string MarketPictureLocation, string DescriptionPictureLocation, string SellerUsername, string Status);
    public record UpdateListingRequest(string Title, string Description, decimal Price);
}
namespace SharePrint.Api.Contracts;

public class OrderContracts
{
    public record ConfirmResult(Guid OrderId, string Status);

    public record DownloadSummary(
        Guid OrderId,
        Guid OrderItemId,
        string ListingTitle,
        DateTimeOffset PurchasedAt,
        int DownloadsRemaining);

    public record OrderItem(
        Guid Id,
        Guid ListingId,
        string ListingTitle,
        decimal UnitPrice,
        bool DownloadPath,
        bool PrintPath,
        int? DownloadsRemaining);

    public record OrderDetail(
        Guid Id,
        string Status,
        decimal TotalPrice,
        string Currency,
        DateTimeOffset CreatedAt,
        IReadOnlyList<OrderItem> Items);
}

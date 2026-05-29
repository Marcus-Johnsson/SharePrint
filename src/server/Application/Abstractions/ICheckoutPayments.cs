namespace SharePrint.Application.Abstractions;

public record PaymentIntentResult(string PaymentIntentId, string ClientSecret);

public interface ICheckoutPayments
{
    /// <summary>
    /// Create a Stripe PaymentIntent for the given amount and return its id + client secret.
    /// </summary>
    /// <param name="amountMinor">Amount in minor units (öre for SEK).</param>
    /// <param name="currency">ISO 4217, lowercase (e.g. "sek").</param>
    /// <param name="buyerId">User id, stored in Stripe metadata for reconciliation.</param>
    Task<PaymentIntentResult> CreateIntentAsync(
        long amountMinor,
        string currency,
        string buyerId,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    /// <summary>Retrieve a PaymentIntent from Stripe (status + metadata).</summary>
    Task<PaymentIntentResult> GetIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>True if intent.status == "succeeded".</summary>
    Task<bool> IsSucceededAsync(string paymentIntentId, CancellationToken ct = default);
}

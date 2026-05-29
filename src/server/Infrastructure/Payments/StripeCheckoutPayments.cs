using Microsoft.Extensions.Logging;
using SharePrint.Application.Abstractions;
using Stripe;

namespace SharePrint.Infrastructure.Payments;

public class StripeCheckoutPayments : ICheckoutPayments
{
    private readonly ILogger<StripeCheckoutPayments> _logger;
    private readonly PaymentIntentService _intents;

    public StripeCheckoutPayments(ILogger<StripeCheckoutPayments> logger)
    {
        _logger = logger;
        _intents = new PaymentIntentService();
    }

    public async Task<PaymentIntentResult> CreateIntentAsync(
        long amountMinor,
        string currency,
        string buyerId,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var meta = new Dictionary<string, string> { ["buyerId"] = buyerId };
        if (metadata is not null)
            foreach (var kv in metadata) meta[kv.Key] = kv.Value;

        var opts = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = meta
        };

        var pi = await _intents.CreateAsync(opts, cancellationToken: ct);
        _logger.LogInformation("Created PaymentIntent {Id} amount {Amount} {Currency} for {Buyer}",
            pi.Id, amountMinor, currency, buyerId);
        return new PaymentIntentResult(pi.Id, pi.ClientSecret);
    }

    public async Task<PaymentIntentResult> GetIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var pi = await _intents.GetAsync(paymentIntentId, cancellationToken: ct);
        return new PaymentIntentResult(pi.Id, pi.ClientSecret);
    }

    public async Task<bool> IsSucceededAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var pi = await _intents.GetAsync(paymentIntentId, cancellationToken: ct);
        return pi.Status == "succeeded";
    }
}

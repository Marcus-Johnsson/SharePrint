using Microsoft.Extensions.Logging;
using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Payments;

public class FakePaymentProcessor : IPaymentProcessor
{
    private readonly ILogger<FakePaymentProcessor> _logger;
    public FakePaymentProcessor(ILogger<FakePaymentProcessor> log) => _logger = log;

    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string buyerId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Fake charge {Amount} {Currency} buyer {Buyer}", amount, currency, buyerId);
        return Task.FromResult(new PaymentResult(true, "fake_" + Guid.NewGuid().ToString("N")));
    }
}
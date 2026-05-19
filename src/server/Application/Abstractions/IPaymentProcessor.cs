namespace SharePrint.Application.Abstractions;

public interface IPaymentProcessor
{
    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string buyerId, CancellationToken cancellationToken = default);
}
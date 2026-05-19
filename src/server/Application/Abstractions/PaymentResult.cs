namespace SharePrint.Application.Abstractions;

public class PaymentResult
{
    public sealed record IPaymentResult(bool Success, string TransactionId);
}
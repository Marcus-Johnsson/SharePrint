namespace SharePrint.Application.Abstractions;

public sealed record PaymentResult(bool Succeeded, string TransactionId);

namespace SharePrint.Api.Contracts;

public class CheckoutContracts
{
    public record CreateIntentResult(string ClientSecret, Guid OrderId);
}

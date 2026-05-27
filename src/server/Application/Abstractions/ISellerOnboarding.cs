namespace SharePrint.Application.Abstractions;

public interface ISellerOnboarding
{
    Task<string> CreateCardSetupAsync(string userId, CancellationToken ct = default);
    Task<bool> ConfirmCardAndPromoteAsync(string userId, string setupIntentId, CancellationToken ct = default);
}
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using Stripe;

namespace SharePrint.Infrastructure.Payments;

public class StripeSellerOnboarding : ISellerOnboarding
{
    private readonly UserManager<User> _users;
    private readonly ILogger<StripeSellerOnboarding> _logger;
    private readonly SetupIntentService _setupIntents;

    public StripeSellerOnboarding(UserManager<User> users, ILogger<StripeSellerOnboarding> logger)
    {
        _users = users;
        _logger = logger;
        _setupIntents = new SetupIntentService();
    }

    public async Task<string> CreateCardSetupAsync(string userId, CancellationToken ct = default)
    {
        var opts = new SetupIntentCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Usage = "off_session",
            Metadata = new Dictionary<string, string> { ["userId"] = userId }
        };
        var si = await _setupIntents.CreateAsync(opts, cancellationToken: ct);
        _logger.LogInformation("Created SetupIntent {Id} for user {User}", si.Id, userId);
        return si.ClientSecret;
    }

    public async Task<bool> ConfirmCardAndPromoteAsync(string userId, string setupIntentId, CancellationToken ct = default)
    {
        SetupIntent si;
        try
        {
            si = await _setupIntents.GetAsync(setupIntentId, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe lookup failed for {SetupIntent}", setupIntentId);
            return false;
        }

        if (si.Status != "succeeded")
        {
            _logger.LogInformation("SetupIntent {Id} status {Status} - not promoting", si.Id, si.Status);
            return false;
        }

        // Anti-tamper: SetupIntent must belong to caller.
        if (!si.Metadata.TryGetValue("userId", out var ownerId) || ownerId != userId)
        {
            _logger.LogWarning("SetupIntent {Id} userId mismatch (owner={Owner}, caller={Caller})",
                si.Id, ownerId, userId);
            return false;
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return false;

        if (await _users.IsInRoleAsync(user, Roles.Seller)) return true;

        var add = await _users.AddToRoleAsync(user, Roles.Seller);
        return add.Succeeded;
    }
}

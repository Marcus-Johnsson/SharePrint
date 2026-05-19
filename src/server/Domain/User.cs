using Microsoft.AspNetCore.Identity;

namespace SharePrint.Domain;

public class User : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
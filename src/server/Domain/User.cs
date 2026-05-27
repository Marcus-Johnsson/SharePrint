using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SharePrint.Domain;

public class User : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; 
}

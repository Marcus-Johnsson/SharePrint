using System.ComponentModel.DataAnnotations;

namespace SharePrint.Api.Contracts;

public static class AuthContracts
{
    public record RegisterRequest(
        [property: Required, EmailAddress] string Email,
        [property: Required, MinLength(8)] string Password,
        [property: Required,
                   MinLength(2),
                   MaxLength(50),
                   RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "DisplayName: letters and digits only.")]
        string Username);

    public record LoginRequest(
        [property: Required, EmailAddress] string Email,
        [property: Required] string Password);
    public record MeResponse(
        [Required]
        string Id,
        [Required]
        string Email,
        [Required]
        string DisplayName,
        [Required,  MinLength(1), MaxLength(3)]
        string[] Roles);
}
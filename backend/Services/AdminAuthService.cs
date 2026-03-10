using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IIS_Site_Manager.API.Services;

public class AdminAuthService(IConfiguration config, PasswordHashingService passwordHashing)
{
    public bool ValidateCredentials(string username, string password)
    {
        var configuredUser = config["Admin:Username"];
        var configuredHash = config["Admin:PasswordHash"];

        return string.Equals(username?.Trim(), configuredUser, StringComparison.Ordinal) &&
               passwordHashing.VerifyPassword(password, configuredHash ?? string.Empty);
    }

    public string GenerateToken(string username)
    {
        var secret = config["Admin:JwtKey"]!;
        var issuer = config["Admin:JwtIssuer"] ?? "IIS-Site-Manager";
        var audience = config["Admin:JwtAudience"] ?? "IIS-Site-Manager-Admin";
        var expiresMinutes = int.TryParse(config["Admin:JwtExpiresMinutes"], out var parsed) ? parsed : 720;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

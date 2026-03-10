namespace IIS_Site_Manager.API.Services;

public static class AdminSecurityConfiguration
{
    public static void Validate(IConfiguration config)
    {
        Require("Admin:Username", config["Admin:Username"]);
        Require("Admin:PasswordHash", config["Admin:PasswordHash"]);
        Require("Admin:JwtKey", config["Admin:JwtKey"], minLength: 32);
        Require("ConnectionStrings:Default", config.GetConnectionString("Default"));
    }

    static void Require(string key, string? value, int minLength = 1)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required configuration value '{key}'.");

        if (value.Length < minLength)
            throw new InvalidOperationException($"Configuration value '{key}' must be at least {minLength} characters.");

        if (value.Contains("<", StringComparison.Ordinal) || value.Contains("change-me", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Configuration value '{key}' still contains a placeholder.");
    }
}

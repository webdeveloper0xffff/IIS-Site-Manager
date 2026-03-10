namespace IIS_Site_Manager.API.Data.Entities;

public class CustomerAccountEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordHashAlgorithm { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedUtc { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public Guid? AssignedServerNodeId { get; set; }
}

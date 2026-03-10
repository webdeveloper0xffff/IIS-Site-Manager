namespace IIS_Site_Manager.API.Data.Entities;

public class WaitlistEntryEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}

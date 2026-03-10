namespace IIS_Site_Manager.API.Data.Entities;

public class AuditLogEntryEntity
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

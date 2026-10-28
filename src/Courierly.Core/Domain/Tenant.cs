namespace Courierly.Core.Domain;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Asia/Manila";
}

namespace Courierly.Core.Domain;

public class Merchant
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

namespace Courierly.Api.Controllers;

public class QuoteRequest
{
    public int TenantId { get; set; }
    public string Zone { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string? PromoCode { get; set; }
}

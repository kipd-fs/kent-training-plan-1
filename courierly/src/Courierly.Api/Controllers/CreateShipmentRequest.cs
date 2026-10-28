namespace Courierly.Api.Controllers;

public class CreateShipmentRequest
{
    public string Reference { get; set; } = string.Empty;
    public int MerchantId { get; set; }
    public string Zone { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string? PromoCode { get; set; }
}

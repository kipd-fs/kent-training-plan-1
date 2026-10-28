namespace Courierly.Core.Domain;

public class Shipment
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int MerchantId { get; set; }
    public Merchant? Merchant { get; set; }
    public string Reference { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
    public string Zone { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal Fee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? MerchantNotifiedAt { get; set; }
}

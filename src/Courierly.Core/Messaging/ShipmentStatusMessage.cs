namespace Courierly.Core.Messaging;

public class ShipmentStatusMessage
{
    public int ShipmentId { get; set; }
    public int TenantId { get; set; }
    public string Status { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

namespace Courierly.Api.Controllers;

public class CarrierWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public string ShipmentReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

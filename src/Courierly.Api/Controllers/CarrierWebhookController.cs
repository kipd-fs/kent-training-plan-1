using Courierly.Core.Data;
using Courierly.Core.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courierly.Api.Controllers;

[ApiController]
[Route("api/webhooks/carrier")]
public class CarrierWebhookController : ControllerBase
{
    private readonly CourierlyDbContext _db;
    private readonly ILogger<CarrierWebhookController> _logger;

    public CarrierWebhookController(CourierlyDbContext db, ILogger<CarrierWebhookController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] CarrierWebhookEvent evt)
    {
        var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Reference == evt.ShipmentReference);
        if (shipment == null)
        {
            return NotFound();
        }

        shipment.Status = Enum.Parse<ShipmentStatus>(evt.Status, true);
        shipment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Carrier event {EventId} applied to {Reference}", evt.EventId, evt.ShipmentReference);
        return Ok();
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using Courierly.Core.Data;
using Courierly.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Courierly.Core.Messaging;

public class ShipmentStatusHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CourierlyDbContext _db;
    private readonly ILogger<ShipmentStatusHandler> _logger;

    public ShipmentStatusHandler(CourierlyDbContext db, ILogger<ShipmentStatusHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(string body)
    {
        var msg = JsonSerializer.Deserialize<ShipmentStatusMessage>(body, JsonOptions);

        var shipment = _db.Shipments
            .Include(s => s.Merchant)
            .FirstOrDefault(s => s.Id == msg!.ShipmentId);

        var status = Enum.Parse<ShipmentStatus>(msg!.Status);

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", shipment!.Merchant!.ApiKey);
        var response = await client.PostAsJsonAsync(shipment.Merchant.CallbackUrl, new
        {
            shipmentReference = shipment.Reference,
            status = msg.Status,
            occurredAt = msg.OccurredAt
        });

        shipment.Status = status;
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.MerchantNotifiedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        _logger.LogInformation($"Shipment {shipment.Reference} set to {status}, merchant {shipment.Merchant.Name} notified with key {shipment.Merchant.ApiKey}");
    }
}

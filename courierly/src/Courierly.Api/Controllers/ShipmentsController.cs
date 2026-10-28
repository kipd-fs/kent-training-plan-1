using Courierly.Core.Data;
using Courierly.Core.Domain;
using Courierly.Core.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courierly.Api.Controllers;

[ApiController]
[Route("api/shipments")]
public class ShipmentsController : ControllerBase
{
    private readonly CourierlyDbContext _db;
    private readonly DeliveryFeeCalculator _calculator;

    public ShipmentsController(CourierlyDbContext db, DeliveryFeeCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            return BadRequest("Missing tenant");
        }

        var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment == null)
        {
            return NotFound(new { error = "Shipment not found" });
        }

        return Ok(ToResponse(shipment));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) || !int.TryParse(tenantHeader, out var tenantId))
        {
            return BadRequest("Missing tenant");
        }

        var query = _db.Shipments.Where(s => s.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ShipmentStatus>(status, true, out var parsed))
            {
                return BadRequest("Unknown status");
            }

            query = query.Where(s => s.Status == parsed);
        }

        var shipments = await query.OrderBy(s => s.Id).ToListAsync();
        return Ok(shipments.Select(ToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request)
    {
        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) || !int.TryParse(tenantHeader, out var tenantId))
        {
            return BadRequest("Missing tenant");
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            return BadRequest("Reference is required");
        }

        if (request.WeightKg <= 0)
        {
            return BadRequest("Weight must be greater than zero");
        }

        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == request.MerchantId && m.TenantId == tenantId);
        if (merchant == null)
        {
            return BadRequest("Unknown merchant");
        }

        if (await _db.Shipments.AnyAsync(s => s.TenantId == tenantId && s.Reference == request.Reference))
        {
            return Conflict(new { message = $"Reference {request.Reference} already exists", code = "DUPLICATE_REFERENCE" });
        }

        var rule = await _db.FeeRules.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Zone == request.Zone);
        if (rule == null)
        {
            return BadRequest("Unknown zone");
        }

        try
        {
            var quote = _calculator.Calculate(rule, request.WeightKg, request.PromoCode);
            var now = DateTime.UtcNow;
            var shipment = new Shipment
            {
                TenantId = tenantId,
                MerchantId = merchant.Id,
                Reference = request.Reference,
                Zone = request.Zone,
                WeightKg = request.WeightKg,
                Fee = quote.Total,
                Status = ShipmentStatus.Created,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Shipments.Add(shipment);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = shipment.Id }, ToResponse(shipment));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) || !int.TryParse(tenantHeader, out var tenantId))
        {
            return BadRequest("Missing tenant");
        }

        try
        {
            var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
            if (shipment == null)
            {
                return NotFound(new { error = "Shipment not found" });
            }

            if (shipment.Status != ShipmentStatus.Created && shipment.Status != ShipmentStatus.PickedUp)
            {
                return Conflict(new { message = $"Cannot cancel a shipment that is {shipment.Status}", code = "INVALID_STATE" });
            }

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(ToResponse(shipment));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    private static ShipmentResponse ToResponse(Shipment s) =>
        new(s.Id, s.Reference, s.Status.ToString(), s.Zone, s.WeightKg, s.Fee, s.CreatedAt, s.UpdatedAt);
}

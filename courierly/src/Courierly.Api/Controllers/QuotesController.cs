using Courierly.Core.Data;
using Courierly.Core.Pricing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Courierly.Api.Controllers;

[ApiController]
[Route("api/quotes")]
public class QuotesController : ControllerBase
{
    private readonly CourierlyDbContext _db;
    private readonly DeliveryFeeCalculator _calculator;

    public QuotesController(CourierlyDbContext db, DeliveryFeeCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    [HttpPost]
    public async Task<IActionResult> Quote([FromBody] QuoteRequest request)
    {
        var rule = await _db.FeeRules.FirstOrDefaultAsync(r => r.TenantId == request.TenantId && r.Zone == request.Zone);
        if (rule == null)
        {
            return BadRequest("Unknown zone");
        }

        try
        {
            var quote = _calculator.Calculate(rule, request.WeightKg, request.PromoCode);
            return Ok(new
            {
                fee = quote.Total,
                breakdown = quote
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

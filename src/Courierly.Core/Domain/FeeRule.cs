namespace Courierly.Core.Domain;

public class FeeRule
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Zone { get; set; } = string.Empty;
    public decimal BaseFee { get; set; }
    public decimal PerKgFee { get; set; }
    public decimal WeekendSurchargePct { get; set; }
    public decimal AfterHoursSurchargePct { get; set; }
    public int AfterHoursStartHour { get; set; }
    public int AfterHoursEndHour { get; set; }
    public decimal MinimumFee { get; set; }
    public string? PromoCode { get; set; }
    public decimal PromoDiscountPct { get; set; }
}

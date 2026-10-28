using Courierly.Core.Domain;

namespace Courierly.Core.Pricing;

public class DeliveryFeeCalculator
{
    private const decimal MaxWeightKg = 50m;

    public FeeQuote Calculate(FeeRule rule, decimal weightKg, string? promoCode)
    {
        if (weightKg <= 0 || weightKg > MaxWeightKg)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKg), weightKg, "Weight must be greater than 0 and at most 50 kg.");
        }

        var now = DateTime.Now;

        var weightCharge = rule.PerKgFee * (int)weightKg;
        var subtotal = rule.BaseFee + weightCharge;

        decimal surchargePct = 0m;
        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
        {
            surchargePct += rule.WeekendSurchargePct;
        }

        if (now.Hour >= rule.AfterHoursStartHour && now.Hour < rule.AfterHoursEndHour)
        {
            surchargePct += rule.AfterHoursSurchargePct;
        }

        var surcharge = subtotal * surchargePct / 100m;
        var total = subtotal + surcharge;

        if (total < rule.MinimumFee)
        {
            total = rule.MinimumFee;
        }

        decimal discount = 0m;
        if (!string.IsNullOrWhiteSpace(promoCode)
            && rule.PromoCode is not null
            && string.Equals(promoCode, rule.PromoCode, StringComparison.OrdinalIgnoreCase))
        {
            discount = total * rule.PromoDiscountPct / 100m;
            total -= discount;
        }

        return new FeeQuote(
            rule.BaseFee,
            Math.Round(weightCharge, 2, MidpointRounding.AwayFromZero),
            Math.Round(surcharge, 2, MidpointRounding.AwayFromZero),
            Math.Round(discount, 2, MidpointRounding.AwayFromZero),
            Math.Round(total, 2, MidpointRounding.AwayFromZero));
    }
}

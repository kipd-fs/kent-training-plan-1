namespace Courierly.Core.Pricing;

public record FeeQuote(
    decimal BaseFee,
    decimal WeightCharge,
    decimal Surcharge,
    decimal Discount,
    decimal Total);

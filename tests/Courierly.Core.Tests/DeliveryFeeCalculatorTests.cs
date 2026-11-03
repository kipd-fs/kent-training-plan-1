using Courierly.Core.Domain;
using Courierly.Core.Pricing;

namespace Courierly.Core.Tests;

public class DeliveryFeeCalculatorTests
{
    private DeliveryFeeCalculator _deliveryFeeCalculator;
    private FeeRule _feeRule;

    public DeliveryFeeCalculatorTests()
    {
        _deliveryFeeCalculator = new DeliveryFeeCalculator();
        _feeRule = new FeeRule();
    }

    //  1. **Validation.** Weight must be greater than 0 kg and at most 50 kg.
    //  Anything else is rejected with `ArgumentOutOfRangeException`.
    [Theory]
    [InlineData(-1.0)]
    [InlineData(51.0)]
    public void Weight_MustBeGreaterThanZeroToFiftyKg(decimal weight)
    {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _deliveryFeeCalculator.Calculate(_feeRule, weight, promoCode: null);
        });
    }

    // 2. **Base charge.** `BaseFee + PerKgFee × chargeable kg`.
    // Every started kilogram is charged: 1.0 kg is 1 kg, 1.2 kg is 2 kg, 0.3 kg is 1 kg.
    // Remarks: the Weight Charge formula: var weightCharge = rule.PerKgFee * (int)weightKg;
    //          is incorrect. Should be rounded up rather than casting to int which only ignores decimal places
    //          Tests are expected to fail because of this
    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.2, 2.0)]
    [InlineData(0.3, 1.0)]
    public void BaseCharge_EveryStartedKgIsCharged(decimal weight, decimal expectedChargeableWeight)
    {
        // Arrange
        _feeRule.BaseFee = 100;
        _feeRule.PerKgFee = 20;
        var expectedBaseCharge = _feeRule.BaseFee + _feeRule.PerKgFee * expectedChargeableWeight;
        var expectedWeightCharge = _feeRule.PerKgFee * expectedChargeableWeight;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weight, promoCode: null);

        // Assert
        Assert.Equal(expectedWeightCharge, result.WeightCharge);
        Assert.Equal(expectedBaseCharge, result.Total);
    }

    // 3. **Weekend surcharge.** If the quote time falls on a Saturday or Sunday (tenant local),
    // the weekend surcharge percentage applies to the base charge.
    // Remarks: Skipping test for this rule because the current date cannot be controlled.
    //          To fix this the current date must be passed as a parameter as well.
    //          And it should be based on UTC + or - the tenant's Timezone datetime offset


    // 4. **After-hours surcharge.** If the quote hour (tenant local) is inside the after-hours window,
    // the after-hours percentage applies to the base charge.The window starts at `AfterHoursStartHour`
    // (inclusive) and ends at `AfterHoursEndHour` (exclusive), using whole hours 0–23.
    // A window can cross midnight: start 20, end 6 means 20:00–05:59.
    // Remarks: This is also current date related, so will skip adding test as well


    // 5. ** Surcharges do not stack.** When both the weekend and the after-hours surcharge apply,
    // only the larger percentage is used.
    // Remarks: This is also current date related, so will skip adding test as well


    // 6. **Promo discount.** If the merchant's promo code matches the rule's `PromoCode` (case-insensitive),
    // `PromoDiscountPct` is taken off the amount after surcharges.A rule with no `PromoCode` never discounts.
    // Remarks: Set surcharges percentage values to 0 to not deviate the expected total when tested during after hours and weekend
    //          Will update once current date handling is fixed in the implementation
    [Theory]
    [InlineData("KENTPROMO20", true)]
    [InlineData("KentPromo20", true)]
    [InlineData("kentpromo20", true)]
    [InlineData("kentpr0m020", false)]
    public void PromoDiscount_WhenPromoCodematches_AppliesDiscountPercentage(string promoCode, bool expectedDiscounted)
    {
        // Arrange
        _feeRule.BaseFee = 100;
        _feeRule.PerKgFee = 20;
        _feeRule.PromoDiscountPct = 20;
        _feeRule.WeekendSurchargePct = 0;
        _feeRule.AfterHoursSurchargePct = 0;
        var weight = 10;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weight, promoCode);

        // Assert
        if (expectedDiscounted)
            Assert.True(result.Discount > 0);
        else
            Assert.True(result.Discount == 0);
    }

    // 7. **Minimum fee.** The final fee is never below `MinimumFee`. This check runs last, after the discount.
    // Remarks: Set surcharges percentage values to 0 to not deviate the expected total when tested during after hours and weekend
    //          Will update once current date handling is fixed in the implementation
    [Theory]
    [InlineData(1.0, true)]
    [InlineData(5.0, false)]
    public void FinalFee_NeverBelowMinimumFee(decimal weight, bool belowMinimum)
    {
        // Arrange
        _feeRule.BaseFee = 100;
        _feeRule.PerKgFee = 20;
        _feeRule.MinimumFee = 150;
        _feeRule.WeekendSurchargePct = 0;
        _feeRule.AfterHoursSurchargePct = 0;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weight, promoCode: null);

        // Assert
        if (belowMinimum)
            Assert.Equal(_feeRule.MinimumFee, result.Total);
        else
            Assert.True(result.Total > _feeRule.MinimumFee);
    }

    // 8. **Rounding.** Every money value in the quote is rounded to 2 decimal places, midpoint away from zero.
    // Round once at the end, not per step.
    // Remarks: Set surcharges percentage values to 0 to not deviate the expected total when tested during after hours and weekend
    //          Will update once current date handling is fixed in the implementation
    [Theory]
    [InlineData(1.0, 120.53, 20.53)]
    [InlineData(2.0, 141.05, 41.05)]
    [InlineData(5.0, 202.63, 102.63)]
    public void Rounding_2DecimalPlaces_MidpointAwatFromZero(decimal weight, decimal expecttTotal, decimal expectedWeightCharge)
    {
        // Arrange
        _feeRule.PerKgFee = 20.523456m;
        _feeRule.BaseFee = 100;
        _feeRule.PerKgFee = 20.525m;
        _feeRule.MinimumFee = 0;
        _feeRule.WeekendSurchargePct = 0;
        _feeRule.AfterHoursSurchargePct = 0;
        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weight, promoCode: null);

        // Assert
        Assert.Equal(expecttTotal, result.Total);
        Assert.Equal(expectedWeightCharge, result.WeightCharge);
    }
}

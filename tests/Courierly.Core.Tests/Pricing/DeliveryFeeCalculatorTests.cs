using Courierly.Core.Domain;
using Courierly.Core.Pricing;
using Microsoft.Extensions.Time.Testing;

namespace Courierly.Core.Tests.Pricing;

public class DeliveryFeeCalculatorTests
{
    private readonly DeliveryFeeCalculator _deliveryFeeCalculator;
    private readonly FeeRule _feeRule;

    public DeliveryFeeCalculatorTests()
    {
        _deliveryFeeCalculator = new DeliveryFeeCalculator(
            TimeProvider.System,
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")
        );

        _feeRule = new FeeRule();
    }

    #region Validation
    [Theory]
    [InlineData(-0.5)]
    [InlineData(50.5)]
    public void Calculate_OutOfAcceptedRangeWeight_ThrowsArgumentOutOfRangeException(decimal weightKg)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _deliveryFeeCalculator.Calculate(_feeRule, weightKg, promoCode: null)
        );
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(50.0)]
    public void Calculate_InAcceptedRangeWeight_DoesNotThrowArgumentOutOfRangeException(decimal weightKg)
    {
        _deliveryFeeCalculator.Calculate(_feeRule, weightKg, promoCode: null);
    }
    #endregion

    #region Base Charge
    [Theory]
    [InlineData(0.3, 20)]
    [InlineData(1.0, 20)]
    [InlineData(1.2, 40)]
    [InlineData(1.9, 40)]
    [InlineData(2.0, 40)]
    [InlineData(49.9, 1000)]
    [InlineData(50.0, 1000)]
    public void Calculate_InAcceptedRangeWeight_ChargedEveryStartedKilogram(decimal weightKg, decimal expectedWeightCharge)
    {
        // Arrange
        _feeRule.BaseFee = 50;
        _feeRule.PerKgFee = 20;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weightKg, promoCode: null);

        // Assert
        Assert.Equal(expectedWeightCharge, result.WeightCharge);
    }

    [Theory]
    [InlineData(50, 20, 1.0, 70)]
    [InlineData(50, 20, 1.2, 90)]
    [InlineData(100, 10, 2.3, 130)]
    [InlineData(0, 20, 1.5, 40)]
    public void Calculate_InAcceptedRangeWeight_AddsBaseFeeAndWeightCharge(decimal baseFee, decimal perKgFee, decimal weightKg, decimal expectedBaseCharge)
    {
        // Arrange
        _feeRule.BaseFee = baseFee;
        _feeRule.PerKgFee = perKgFee;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, weightKg, promoCode: null);

        // Assert
        var actualBaseCharge = result.BaseFee + result.WeightCharge; // Isolating Base charge only as Total already got processed against other charges like surcharges

        Assert.Equal(expectedBaseCharge, actualBaseCharge);
    }
    #endregion

    #region Weekend Surcharge
    [Theory]
    [InlineData(2026, 11, 6, 23, 0, 0)]
    [InlineData(2026, 11, 7, 0, 0, 12)]
    [InlineData(2026, 11, 7, 12, 0, 12)]
    [InlineData(2026, 11, 8, 23, 59, 12)]
    [InlineData(2026, 11, 9, 0, 0, 0)]
    public void Calculate_WeekendSurcharge_AppliesOnSaturdayAndSunday(int year, int month, int day, int hour, int minute, decimal expectedSurcharge)
    {
        // Arrange
        _feeRule.BaseFee = 100m;
        _feeRule.PerKgFee = 20m;
        _feeRule.MinimumFee = 0m;

        _feeRule.WeekendSurchargePct = 10m;
        _feeRule.AfterHoursSurchargePct = 0m;

        var expectedBaseCharge = _feeRule.BaseFee + _feeRule.PerKgFee;

        var calculator = CreateCalculator(year, month, day, hour, minute);

        // Act
        var result = calculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedSurcharge, result.Surcharge);
        Assert.Equal(expectedBaseCharge + expectedSurcharge, result.Total);
    }
    #endregion

    #region After-Hours Surcharge
    [Theory]
    [InlineData(2026, 11, 9, 19, 59, 0)]
    [InlineData(2026, 11, 9, 20, 0, 18)]
    [InlineData(2026, 11, 9, 23, 59, 18)]
    [InlineData(2026, 11, 10, 0, 0, 18)]
    [InlineData(2026, 11, 10, 5, 59, 18)]
    [InlineData(2026, 11, 10, 6, 0, 0)]
    [InlineData(2026, 11, 10, 12, 0, 0)]
    public void Calculate_AfterHoursSurcharge_HandlesOvernightWindow(int year, int month, int day, int hour, int minute, decimal expectedSurcharge)
    {
        // Arrange
        _feeRule.BaseFee = 100m;
        _feeRule.PerKgFee = 20m;
        _feeRule.MinimumFee = 0m;

        _feeRule.WeekendSurchargePct = 0m;
        _feeRule.AfterHoursSurchargePct = 15m;

        _feeRule.AfterHoursStartHour = 20;
        _feeRule.AfterHoursEndHour = 6;

        var expectedBaseCharge = _feeRule.BaseFee + _feeRule.PerKgFee;

        var calculator = CreateCalculator(year, month, day, hour, minute);

        // Act
        var result = calculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedSurcharge, result.Surcharge);
        Assert.Equal(expectedBaseCharge + expectedSurcharge, result.Total);
    }
    #endregion

    #region Surcharges do not stack
    [Theory]
    [InlineData(10, 15, 18)]
    [InlineData(20, 5, 24)]
    [InlineData(10, 10, 12)]
    [InlineData(0, 15, 18)]
    [InlineData(15, 0, 18)]
    public void Calculate_WeekendAndAfterHoursSurcharges_AppliesOnlyLargerPercentage(decimal weekendSurchargePct, decimal afterHoursSurchargePct, decimal expectedSurcharge)
    {
        // Arrange
        _feeRule.BaseFee = 100m;
        _feeRule.PerKgFee = 20m;
        _feeRule.MinimumFee = 0m;

        _feeRule.WeekendSurchargePct = weekendSurchargePct;
        _feeRule.AfterHoursSurchargePct = afterHoursSurchargePct;

        _feeRule.AfterHoursStartHour = 18;
        _feeRule.AfterHoursEndHour = 23;

        var expectedBaseCharge = _feeRule.BaseFee + _feeRule.PerKgFee;

        // Saturday, November 7, 2026, 21:00 Philippine time
        var calculator = CreateCalculator(2026, 11, 7, 21);

        // Act
        var result = calculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedSurcharge, result.Surcharge);
        Assert.Equal(expectedBaseCharge + expectedSurcharge, result.Total);
    }
    #endregion

    #region Promo Discount
    [Theory]
    [InlineData(10, 12, 108)]
    [InlineData(20, 24, 96)]
    [InlineData(50, 60, 60)]
    [InlineData(100, 120, 0)]
    public void Calculate_MatchingPromoCode_AppliesDiscount(decimal promoDiscountPct, decimal expectedDiscount, decimal expectedTotal)
    {
        // Arrange
        _feeRule.BaseFee = 100m;
        _feeRule.PerKgFee = 20m;
        _feeRule.MinimumFee = 0m;
        _feeRule.WeekendSurchargePct = 0m;
        _feeRule.AfterHoursSurchargePct = 0m;

        _feeRule.PromoCode = "KENTPROMO10";
        _feeRule.PromoDiscountPct = promoDiscountPct;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, 1.0m, promoCode: "KENTPROMO10");

        // Assert
        Assert.Equal(expectedDiscount, result.Discount);
        Assert.Equal(expectedTotal, result.Total);
    }
    #endregion

    #region Minimum Fee
    [Theory]
    [InlineData(50, 120)]
    [InlineData(120, 120)]
    [InlineData(200, 200)]
    public void Calculate_FinalFee_NeverBelowMinimumFee(decimal minimumFee, decimal expectedTotal)
    {
        // Arrange
        _feeRule.BaseFee = 100m;
        _feeRule.PerKgFee = 20m;
        _feeRule.MinimumFee = minimumFee;
        _feeRule.WeekendSurchargePct = 0m;
        _feeRule.AfterHoursSurchargePct = 0m;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedTotal, result.Total);
    }
    #endregion

    #region Rounding
    [Theory]
    [InlineData(50.1234, 50.12)]
    [InlineData(50.125, 50.13)]
    [InlineData(50.135, 50.14)]
    [InlineData(50.999, 51.00)]
    public void Calculate_BaseFee_RoundsToTwoDecimalPlacesAwayFromZero(decimal baseFee, decimal expectedBaseFee)
    {
        // Arrange
        _feeRule.BaseFee = baseFee;

        // Act
        var result = _deliveryFeeCalculator.Calculate(
            _feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedBaseFee, result.BaseFee);
    }

    [Theory]
    [InlineData(20.1234, 20.12)]
    [InlineData(20.1264, 20.13)]
    [InlineData(20.125, 20.13)]
    [InlineData(20.135, 20.14)]
    [InlineData(20.145, 20.15)]
    public void Calculate_WeightCharge_RoundsToTwoDecimalPlacesAwayFromZero(decimal perKgFee, decimal expectedWeightCharge)
    {
        // Arrange
        _feeRule.PerKgFee = perKgFee;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedWeightCharge, result.WeightCharge);
    }

    [Theory]
    [InlineData(10.004, 0.004, 10.01)]
    [InlineData(10.005, 0.005, 10.01)]
    [InlineData(10.006, 0.006, 10.01)]
    public void Calculate_Total_RoundsOnlyAfterAllCalculations(decimal baseFee, decimal perKgFee, decimal expectedTotal)
    {
        // Arrange
        _feeRule.BaseFee = baseFee;
        _feeRule.PerKgFee = perKgFee;
        _feeRule.MinimumFee = 0m;
        _feeRule.WeekendSurchargePct = 0m;
        _feeRule.AfterHoursSurchargePct = 0m;

        // Act
        var result = _deliveryFeeCalculator.Calculate(_feeRule, 1.0m, promoCode: null);

        // Assert
        Assert.Equal(expectedTotal, result.Total);
    }
    #endregion

    #region Private Methods
    private static DeliveryFeeCalculator CreateCalculator(int year, int month, int day, int hour, int minute = 0)
    {
        var localTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.FromHours(8));

        var fakeTimeProvider = new FakeTimeProvider(localTime.ToUniversalTime());

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        return new DeliveryFeeCalculator(fakeTimeProvider, timeZone);
    }
    #endregion
}

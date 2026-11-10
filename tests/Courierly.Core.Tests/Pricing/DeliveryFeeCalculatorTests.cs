using System;
using System.Collections.Generic;
using System.Text;
using Courierly.Core.Domain;
using Courierly.Core.Pricing;

namespace Courierly.Core.Tests.Pricing;

public class DeliveryFeeCalculatorTests
{
    private readonly DeliveryFeeCalculator _deliveryFeeCalculator;
    private readonly FeeRule _feeRule;

    public DeliveryFeeCalculatorTests()
    {
        _deliveryFeeCalculator = new DeliveryFeeCalculator();
        _feeRule = new FeeRule();
    }

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
}

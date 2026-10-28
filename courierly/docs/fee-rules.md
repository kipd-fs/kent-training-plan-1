# Delivery fee rules

Product owner: Courierly pricing team. These are the rules the business signed off. `DeliveryFeeCalculator` must implement them exactly.

Each tenant has one `FeeRule` per zone (`A`, `B`, `C`). A quote is calculated at the moment it is requested ("quote time").

## Inputs

- `FeeRule` for the tenant and zone
- Parcel weight in kilograms
- Optional promo code typed by the merchant
- Quote time, read in the **tenant's local time zone** (`Tenant.TimeZoneId`, an IANA id such as `Asia/Manila`). Server time and UTC are not used for any rule below.

## Rules, in the order they apply

1. **Validation.** Weight must be greater than 0 kg and at most 50 kg. Anything else is rejected with `ArgumentOutOfRangeException`.
2. **Base charge.** `BaseFee + PerKgFee × chargeable kg`. Every started kilogram is charged: 1.0 kg is 1 kg, 1.2 kg is 2 kg, 0.3 kg is 1 kg.
3. **Weekend surcharge.** If the quote time falls on a Saturday or Sunday (tenant local), the weekend surcharge percentage applies to the base charge.
4. **After-hours surcharge.** If the quote hour (tenant local) is inside the after-hours window, the after-hours percentage applies to the base charge. The window starts at `AfterHoursStartHour` (inclusive) and ends at `AfterHoursEndHour` (exclusive), using whole hours 0–23. A window can cross midnight: start 20, end 6 means 20:00–05:59.
5. **Surcharges do not stack.** When both the weekend and the after-hours surcharge apply, only the larger percentage is used.
6. **Promo discount.** If the merchant's promo code matches the rule's `PromoCode` (case-insensitive), `PromoDiscountPct` is taken off the amount after surcharges. A rule with no `PromoCode` never discounts.
7. **Minimum fee.** The final fee is never below `MinimumFee`. This check runs last, after the discount.
8. **Rounding.** Every money value in the quote is rounded to 2 decimal places, midpoint away from zero. Round once at the end, not per step.

## Worked example

Swift Cebu, zone A: base 60.00, per kg 15.00, weekend 10%, after-hours 20% (20→6), minimum 80.00, promo `SUKI10` at 10%.

Parcel 1.2 kg, Saturday 21:30 Manila time, promo `suki10`:

- Chargeable weight 2 kg → base charge 60 + 30 = 90.00
- Weekend 10% and after-hours 20% both apply → use 20% → surcharge 18.00 → 108.00
- Promo 10% → discount 10.80 → 97.20
- Minimum 80.00 does not bite → **fee 97.20**

## Quote breakdown returned

`FeeQuote(BaseFee, WeightCharge, Surcharge, Discount, Total)` — all rounded per rule 8.

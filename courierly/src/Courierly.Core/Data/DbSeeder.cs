using Courierly.Core.Domain;

namespace Courierly.Core.Data;

public static class DbSeeder
{
    public static void Seed(CourierlyDbContext db)
    {
        if (db.Tenants.Any())
        {
            return;
        }

        var swift = new Tenant { Name = "Swift Cebu", TimeZoneId = "Asia/Manila" };
        var metro = new Tenant { Name = "Metro Haulers", TimeZoneId = "Asia/Singapore" };
        db.Tenants.AddRange(swift, metro);
        db.SaveChanges();

        var bakery = new Merchant { TenantId = swift.Id, Name = "Lahug Bakery", CallbackUrl = "http://localhost:5099/callbacks/lahug", ApiKey = "lahug-live-7f3a91c2" };
        var gadgets = new Merchant { TenantId = swift.Id, Name = "IT Park Gadgets", CallbackUrl = "http://localhost:5099/callbacks/itpark", ApiKey = "itpark-live-0b88e4d1" };
        var florist = new Merchant { TenantId = metro.Id, Name = "Makati Florist", CallbackUrl = "http://localhost:5099/callbacks/florist", ApiKey = "florist-live-c41d2e77" };
        db.Merchants.AddRange(bakery, gadgets, florist);

        db.FeeRules.AddRange(
            new FeeRule { TenantId = swift.Id, Zone = "A", BaseFee = 60m, PerKgFee = 15m, WeekendSurchargePct = 10m, AfterHoursSurchargePct = 20m, AfterHoursStartHour = 20, AfterHoursEndHour = 6, MinimumFee = 80m, PromoCode = "SUKI10", PromoDiscountPct = 10m },
            new FeeRule { TenantId = swift.Id, Zone = "B", BaseFee = 90m, PerKgFee = 18m, WeekendSurchargePct = 10m, AfterHoursSurchargePct = 20m, AfterHoursStartHour = 20, AfterHoursEndHour = 6, MinimumFee = 110m, PromoCode = "SUKI10", PromoDiscountPct = 10m },
            new FeeRule { TenantId = swift.Id, Zone = "C", BaseFee = 140m, PerKgFee = 25m, WeekendSurchargePct = 15m, AfterHoursSurchargePct = 25m, AfterHoursStartHour = 21, AfterHoursEndHour = 5, MinimumFee = 160m, PromoCode = null, PromoDiscountPct = 0m },
            new FeeRule { TenantId = metro.Id, Zone = "A", BaseFee = 70m, PerKgFee = 12m, WeekendSurchargePct = 5m, AfterHoursSurchargePct = 15m, AfterHoursStartHour = 18, AfterHoursEndHour = 22, MinimumFee = 85m, PromoCode = "METRO5", PromoDiscountPct = 5m },
            new FeeRule { TenantId = metro.Id, Zone = "B", BaseFee = 95m, PerKgFee = 16m, WeekendSurchargePct = 5m, AfterHoursSurchargePct = 15m, AfterHoursStartHour = 18, AfterHoursEndHour = 22, MinimumFee = 110m, PromoCode = "METRO5", PromoDiscountPct = 5m });
        db.SaveChanges();

        var now = DateTime.UtcNow;
        db.Shipments.AddRange(
            new Shipment { TenantId = swift.Id, MerchantId = bakery.Id, Reference = "SWC-1001", Status = ShipmentStatus.Created, Zone = "A", WeightKg = 1.2m, Fee = 90m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = swift.Id, MerchantId = bakery.Id, Reference = "SWC-1002", Status = ShipmentStatus.PickedUp, Zone = "A", WeightKg = 0.5m, Fee = 80m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = swift.Id, MerchantId = gadgets.Id, Reference = "SWC-1003", Status = ShipmentStatus.InTransit, Zone = "B", WeightKg = 3.4m, Fee = 162m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = swift.Id, MerchantId = gadgets.Id, Reference = "SWC-1004", Status = ShipmentStatus.Delivered, Zone = "C", WeightKg = 7m, Fee = 315m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = metro.Id, MerchantId = florist.Id, Reference = "MTH-2001", Status = ShipmentStatus.Created, Zone = "A", WeightKg = 2m, Fee = 94m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = metro.Id, MerchantId = florist.Id, Reference = "MTH-2002", Status = ShipmentStatus.InTransit, Zone = "B", WeightKg = 4.5m, Fee = 159m, CreatedAt = now, UpdatedAt = now },
            new Shipment { TenantId = metro.Id, MerchantId = florist.Id, Reference = "MTH-2003", Status = ShipmentStatus.Delivered, Zone = "A", WeightKg = 1m, Fee = 85m, CreatedAt = now, UpdatedAt = now });
        db.SaveChanges();
    }
}

namespace Courierly.Api.Controllers;

public record ShipmentResponse(
    int Id,
    string Reference,
    string Status,
    string Zone,
    decimal WeightKg,
    decimal Fee,
    DateTime CreatedAt,
    DateTime UpdatedAt);

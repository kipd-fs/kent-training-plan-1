using Courierly.Core.Messaging;
using Microsoft.Azure.Functions.Worker;

namespace Courierly.Functions;

public class ShipmentStatusChangedFunction
{
    private readonly ShipmentStatusHandler _handler;

    public ShipmentStatusChangedFunction(ShipmentStatusHandler handler)
    {
        _handler = handler;
    }

    [Function("ShipmentStatusChanged")]
    public async Task Run([ServiceBusTrigger("shipment-status", Connection = "ServiceBusConnection")] string body)
    {
        await _handler.HandleAsync(body);
    }
}

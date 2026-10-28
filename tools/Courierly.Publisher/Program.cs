using System.Text.Json;
using Azure.Messaging.ServiceBus;

var options = PublisherOptions.Parse(args);
if (options is null)
{
    Console.WriteLine("""
        Usage:
          dotnet run --project tools/Courierly.Publisher -- --connection "<service bus connection string>" [--queue shipment-status] [--mode single|duplicate|out-of-order|poison] [--shipment 2] [--tenant 1] [--sessions]

        Modes:
          single        one InTransit message
          duplicate     the same message twice with the same MessageId
          out-of-order  Delivered (sequence 4) sent before InTransit (sequence 3)
          poison        one message for a shipment that does not exist and one with an unknown status
        --sessions sets SessionId = shipment id (use with queue shipment-status-sessions)
        """);
    return 1;
}

await using var client = new ServiceBusClient(options.Connection);
await using var sender = client.CreateSender(options.Queue);

var messages = options.Mode switch
{
    "single" => [Build(options, "InTransit", 3, "single")],
    "duplicate" => [Build(options, "InTransit", 3, "dup"), Build(options, "InTransit", 3, "dup")],
    "out-of-order" => [Build(options, "Delivered", 4, "ooo-4"), Build(options, "InTransit", 3, "ooo-3")],
    "poison" => new List<ServiceBusMessage>
    {
        Build(options with { ShipmentId = 99999 }, "Delivered", 1, "poison-missing"),
        Build(options, "Teleported", 5, "poison-status")
    },
    _ => throw new ArgumentException($"Unknown mode {options.Mode}")
};

foreach (var message in messages)
{
    await sender.SendMessageAsync(message);
    Console.WriteLine($"Sent MessageId={message.MessageId} SessionId={message.SessionId ?? "-"} Body={message.Body}");
}

return 0;

static ServiceBusMessage Build(PublisherOptions o, string status, long sequence, string idSuffix)
{
    var body = JsonSerializer.Serialize(new
    {
        shipmentId = o.ShipmentId,
        tenantId = o.TenantId,
        status,
        sequence,
        occurredAt = o.RunAt.AddMinutes(sequence)
    });

    var message = new ServiceBusMessage(body)
    {
        ContentType = "application/json",
        MessageId = $"shp-{o.ShipmentId}-seq-{sequence}-{idSuffix}-{o.RunId}"
    };

    if (o.Sessions)
    {
        message.SessionId = o.ShipmentId.ToString();
    }

    return message;
}

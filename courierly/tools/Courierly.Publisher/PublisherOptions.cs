public record PublisherOptions(string Connection, string Queue, string Mode, int ShipmentId, int TenantId, bool Sessions, string RunId, DateTimeOffset RunAt)
{
    public static PublisherOptions? Parse(string[] args)
    {
        string? connection = null;
        var queue = "shipment-status";
        var mode = "single";
        var shipmentId = 2;
        var tenantId = 1;
        var sessions = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--connection": connection = args[++i]; break;
                case "--queue": queue = args[++i]; break;
                case "--mode": mode = args[++i]; break;
                case "--shipment": shipmentId = int.Parse(args[++i]); break;
                case "--tenant": tenantId = int.Parse(args[++i]); break;
                case "--sessions": sessions = true; break;
                default: return null;
            }
        }

        return connection is null
            ? null
            : new PublisherOptions(connection, queue, mode, shipmentId, tenantId, sessions, DateTime.UtcNow.ToString("HHmmss"), DateTimeOffset.UtcNow.AddHours(-1));
    }
}

using System.Collections.Concurrent;
using System.Text.Json;

// Stand-in for the merchants' callback endpoints (Merchant.CallbackUrl in the seed points here).
// Logs every callback it receives and can be told to fail, so you can see what a merchant experiences.
//
//   dotnet run --project tools/Courierly.FakeMerchant          -> listens on http://localhost:5099
//   POST /callbacks/{merchant}        what Courierly calls
//   GET  /received                    every callback received so far (JSON)
//   POST /admin/fail?count=3          return 500 for the next 3 callbacks
//   POST /admin/slow?seconds=30       delay every callback by 30 s (0 turns it off)
//   POST /admin/reset                 clear the log and the failure settings

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("FAKE_MERCHANT_URL") ?? "http://localhost:5099");
var app = builder.Build();

var received = new ConcurrentQueue<ReceivedCallback>();
var gate = new object();
var failRemaining = 0;
var delaySeconds = 0;

app.MapPost("/callbacks/{merchant}", async (string merchant, HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    var key = request.Headers["X-Api-Key"].ToString();
    var keyShown = key.Length > 4 ? $"...{key[^4..]}" : (key.Length == 0 ? "(none)" : "(short)");

    int delay;
    bool fail;
    lock (gate)
    {
        delay = delaySeconds;
        fail = failRemaining > 0;
        if (fail) failRemaining--;
    }
    if (delay > 0)
        await Task.Delay(TimeSpan.FromSeconds(delay));
    var status = fail ? 500 : 200;

    var entry = new ReceivedCallback(DateTimeOffset.UtcNow, merchant, keyShown, body, status);
    received.Enqueue(entry);
    Console.WriteLine($"{entry.At:HH:mm:ss.fff} {merchant} key={keyShown} -> {status} {body}");
    return Results.StatusCode(status);
});

app.MapGet("/received", () => Results.Json(received.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

app.MapPost("/admin/fail", (int count) => { lock (gate) failRemaining = count; return Results.Ok(new { failNext = count }); });
app.MapPost("/admin/slow", (int seconds) => { lock (gate) delaySeconds = seconds; return Results.Ok(new { delaySeconds = seconds }); });
app.MapPost("/admin/reset", () =>
{
    received.Clear();
    lock (gate) { failRemaining = 0; delaySeconds = 0; }
    return Results.Ok();
});

app.Run();

record ReceivedCallback(DateTimeOffset At, string Merchant, string ApiKey, string Body, int StatusReturned);

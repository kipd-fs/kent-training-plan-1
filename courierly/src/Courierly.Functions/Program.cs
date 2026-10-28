using Courierly.Core.Data;
using Courierly.Core.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<CourierlyDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("Courierly")
                ?? context.Configuration["ConnectionStrings:Courierly"]));
        services.AddScoped<ShipmentStatusHandler>();
    })
    .Build();

host.Run();

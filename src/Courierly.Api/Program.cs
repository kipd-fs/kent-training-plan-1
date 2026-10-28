using Courierly.Core.Data;
using Courierly.Core.Pricing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<CourierlyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Courierly")));
builder.Services.AddScoped<DeliveryFeeCalculator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CourierlyDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.MapControllers();

app.Run();

public partial class Program;

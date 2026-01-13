using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory.AspNetCore;
using OrderEntry.Domain;
using OrderEntry.Ef;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// Neatoo - Register factories from Domain.Server assembly
builder.Services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
builder.Services.AddScoped<IOrderEntryContext, OrderEntryContext>();
builder.Services.AddDbContext<OrderEntryContext>();

var app = builder.Build();

// Create database on startup (for demo purposes)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderEntryContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine($"Database created at: {db.DbPath}");
}

app.UseNeatoo();

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

await app.RunAsync();

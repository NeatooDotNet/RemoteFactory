using HorseFarm.DomainModel;
using HorseFarm.Ef;
using Neatoo.RemoteFactory;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Local, typeof(IHorseFarmFactory).Assembly);
builder.Services.AddScoped<IHorseFarmContext, HorseFarmContext>();
var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseAuthorization();

app.MapControllers();

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Run();

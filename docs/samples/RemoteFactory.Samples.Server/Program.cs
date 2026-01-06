/// <summary>
/// Code samples for docs/concepts/three-tier-execution.md
///
/// Snippets in this file:
/// - docs:concepts/three-tier-execution:server-setup
/// </summary>

using Neatoo.RemoteFactory.AspNetCore;
using RemoteFactory.Samples.DomainModel.FactoryOperations;

var builder = WebApplication.CreateBuilder(args);

#region docs:concepts/three-tier-execution:server-setup
// Add CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add RemoteFactory services - pass the domain model assembly
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Register your application services
// builder.Services.AddScoped<IPersonContext, PersonContext>();

var app = builder.Build();

app.UseCors();

// Add the RemoteFactory endpoint at /api/neatoo
app.UseNeatoo();
#endregion

app.Run();

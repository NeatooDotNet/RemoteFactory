using Neatoo.RemoteFactory;
using Person.DomainModel;
using Person.Ef;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Neatoo
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);
builder.Services.AddScoped<IPersonContext, PersonContext>();
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

var app = builder.Build();

// Ensure database exists
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<IPersonContext>() as PersonContext;
	await db!.Database.EnsureCreatedAsync();
}

// Hosted Blazor WASM middleware
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Custom middleware: read UserRoles from request headers
app.Use((context, next) =>
{
	var role = context.Request.Headers["UserRoles"];
	var user = context.RequestServices.GetRequiredService<IUser>();
	user.Role = Role.None;
	if (!string.IsNullOrEmpty(role))
	{
		user.Role = Enum.Parse<Role>(role.ToString());
	}
	return next(context);
});

app.UseNeatoo();

// Fallback: serve index.html for unmatched routes (SPA routing)
app.MapFallbackToFile("index.html");

await app.RunAsync();


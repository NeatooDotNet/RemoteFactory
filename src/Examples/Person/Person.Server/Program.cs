using Neatoo.RemoteFactory;
using Person.DomainModel;
using Person.Ef;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// Neatoo
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Local, typeof(IPersonModel).Assembly);
builder.Services.AddScoped<IPersonContext, PersonContext>();
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

var app = builder.Build();

app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request) =>
{
	var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
	return handleRemoteDelegateRequest(request);
});

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

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

await app.RunAsync();


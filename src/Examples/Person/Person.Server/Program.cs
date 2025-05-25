using Neatoo.RemoteFactory;
using Person.DomainModel;
using Person.Ef;
using Neatoo.RemoteFactory.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// Neatoo
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(IPersonModel).Assembly);
builder.Services.AddScoped<IPersonContext, PersonContext>();
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

var app = builder.Build();

app.UseNeatooRemoteFactory();

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


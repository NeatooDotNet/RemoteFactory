using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;
using RemoteFactory.AspNetCore;
using RemoteFactory.AspNetCore.TestServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("TestPolicy", policy =>
	{
		policy.RequireUserName("Test");
	});
});

builder.Services.AddAuthentication("Test")
	.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

builder.Services.AddTransient<AspAuthorize>();

var app = builder.Build();

app.UseNeatooRemoteFactory();

app.MapGet("/", async (HttpContext httpContext) => {

	var authorize = httpContext.RequestServices.GetRequiredService<AspAuthorize>();

	await authorize.Authorize();
});

app.Run();


public partial class Program { }
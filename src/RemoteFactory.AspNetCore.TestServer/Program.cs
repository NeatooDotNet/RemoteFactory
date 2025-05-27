using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;
using RemoteFactory.AspNetCore;
using RemoteFactory.AspNetCore.TestServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(AspAuthorizeTestObj).Assembly);

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("TestPolicy", policy =>
	{
		policy.RequireUserName("Test user");
		policy.RequireRole("Test role");
	});
});

builder.Services.AddAuthentication("Test")
	.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

builder.Services.AddSingleton<IAspAuthorize, AspAuthorize>();

var app = builder.Build();

app.UseNeatooRemoteFactory();

app.MapGet("/", async (HttpContext httpContext) => {
	var authorize = httpContext.RequestServices.GetRequiredService<AspAuthorize>();

	await authorize.Authorize([new AuthorizeAttribute("TestPolicy")]);
});

app.Run();


public partial class Program { }
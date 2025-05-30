using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;
using RemoteFactory.AspNetCore;
using RemoteFactory.AspNetCore.TestServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNeatooAspNetCore(typeof(AspAuthorizeTestObj).Assembly);
builder.Services.AddSingleton<AspAuthorizeTestObjAuth>();
builder.Services.AddSingleton<InterfaceAuthorizeTestObjAuth>();
builder.Services.AddTransient<IInterfaceAuthorizeTestObj, InterfaceAuthorizeTestObj>();

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


var app = builder.Build();

app.UseNeatoo();

app.Run();


public partial class Program { }
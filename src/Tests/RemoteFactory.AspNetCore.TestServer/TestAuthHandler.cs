﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace RemoteFactory.AspNetCore.TestServer;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
   public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
   {
   }

   protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		var claims = new[]
		{
				new Claim(ClaimTypes.Name, "Test user"),
				new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
				new Claim(ClaimTypes.Role, "Test role"),
				new Claim(ClaimTypes.Role, "Test role 2"),
		  };
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, "Test");

		var result = AuthenticateResult.Success(ticket);

		return Task.FromResult(result);
	}
}
using Microsoft.AspNetCore.Authorization;

namespace Neatoo.RemoteFactory.AspNetCore;

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class AspAuthorizeAttribute : Attribute, IAuthorizeData
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
	/// </summary>
	public AspAuthorizeAttribute() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with the specified policy.
	/// </summary>
	/// <param name="policy">The name of the policy to require for authorization.</param>
	public AspAuthorizeAttribute(string policy)
	{
		this.Policy = policy;
	}

	/// <summary>
	/// Gets or sets the policy name that determines access to the resource.
	/// </summary>
	public string? Policy { get; set; }

	/// <summary>
	/// Gets or sets a comma delimited list of roles that are allowed to access the resource.
	/// </summary>
	public string? Roles { get; set; }

	/// <summary>
	/// Gets or sets a comma delimited list of schemes from which user information is constructed.
	/// </summary>
	public string? AuthenticationSchemes { get; set; }
}
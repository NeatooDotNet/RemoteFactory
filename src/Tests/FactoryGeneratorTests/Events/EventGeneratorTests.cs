using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Events;

/// <summary>
/// Tests for the [Event] attribute generator functionality.
/// </summary>
public class EventGeneratorTests
{
	/// <summary>
	/// Verifies that the generator creates the expected delegate type for an event method.
	/// </summary>
	[Fact]
	public void Generator_CreatesEventDelegate_ForInstanceMethod()
	{
		// Act - Check if the delegate type was generated
		var delegateType = typeof(OrderEventTarget).GetNestedType("SendOrderConfirmationEvent");

		// Assert
		Assert.NotNull(delegateType);
		Assert.True(typeof(Delegate).IsAssignableFrom(delegateType));

		// Verify delegate signature - should take (Guid orderId) and return Task
		var invokeMethod = delegateType!.GetMethod("Invoke");
		Assert.NotNull(invokeMethod);
		Assert.Equal(typeof(Task), invokeMethod!.ReturnType);
		Assert.Single(invokeMethod.GetParameters());
		Assert.Equal(typeof(Guid), invokeMethod.GetParameters()[0].ParameterType);
	}

	/// <summary>
	/// Verifies that void event methods also generate Task-returning delegates.
	/// </summary>
	[Fact]
	public void Generator_CreatesTaskDelegate_ForVoidMethod()
	{
		// Act
		var delegateType = typeof(OrderEventTarget).GetNestedType("NotifyOrderShippedEvent");

		// Assert
		Assert.NotNull(delegateType);

		var invokeMethod = delegateType!.GetMethod("Invoke");
		Assert.NotNull(invokeMethod);
		Assert.Equal(typeof(Task), invokeMethod!.ReturnType); // Even void methods get Task delegate
	}

	/// <summary>
	/// Verifies that CancellationToken is excluded from the delegate signature.
	/// </summary>
	[Fact]
	public void Generator_ExcludesCancellationToken_FromDelegateSignature()
	{
		// Act
		var delegateType = typeof(OrderEventTarget).GetNestedType("SendOrderConfirmationEvent");
		var invokeMethod = delegateType!.GetMethod("Invoke");
		var parameters = invokeMethod!.GetParameters();

		// Assert - CancellationToken should not be in delegate parameters
		Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(CancellationToken));
	}

	/// <summary>
	/// Verifies that [Service] parameters are excluded from the delegate signature.
	/// </summary>
	[Fact]
	public void Generator_ExcludesServiceParameters_FromDelegateSignature()
	{
		// Act
		var delegateType = typeof(OrderEventTarget).GetNestedType("SendOrderConfirmationEvent");
		var invokeMethod = delegateType!.GetMethod("Invoke");
		var parameters = invokeMethod!.GetParameters();

		// Assert - Should only have orderId, not IEventTestService
		Assert.Single(parameters);
		Assert.Equal("orderId", parameters[0].Name);
	}

	/// <summary>
	/// Verifies that static class events are also generated correctly.
	/// </summary>
	[Fact]
	public void Generator_CreatesEventDelegate_ForStaticClass()
	{
		// Act
		var delegateType = typeof(OrderEventHandler).GetNestedType("NotifyWarehouseEvent");

		// Assert
		Assert.NotNull(delegateType);

		var invokeMethod = delegateType!.GetMethod("Invoke");
		Assert.NotNull(invokeMethod);
		Assert.Equal(typeof(Task), invokeMethod!.ReturnType);
	}

	/// <summary>
	/// Verifies that void method with multiple parameters generates correct delegate.
	/// </summary>
	[Fact]
	public void Generator_CreatesDelegate_WithMultipleParameters()
	{
		// Act
		var delegateType = typeof(OrderEventTarget).GetNestedType("NotifyOrderShippedEvent");
		var invokeMethod = delegateType!.GetMethod("Invoke");
		var parameters = invokeMethod!.GetParameters();

		// Assert - Should have orderId and message (not IEventTestService or CancellationToken)
		Assert.Equal(2, parameters.Length);
		Assert.Equal("orderId", parameters[0].Name);
		Assert.Equal(typeof(Guid), parameters[0].ParameterType);
		Assert.Equal("message", parameters[1].Name);
		Assert.Equal(typeof(string), parameters[1].ParameterType);
	}
}

// =============================================================================
// DESIGN SOURCE OF TRUTH: Commands That Change an Aggregate Through Its Root
// =============================================================================
//
// A static command is a command (VisionAlignment D12): the client calls it,
// it runs on the server, and it returns a simple result or nothing.
//
// DESIGN DECISION: A command fetches the aggregate, calls its verb, and saves
//
// The order's rules live on Order (Submit, Cancel) and its persistence in its
// own Insert/Update. A command that wrote the order's row itself would skip
// both. So it asks the order's own factory: Fetch, the verb, Save. Each object
// writes only its own row (VisionAlignment section 2, statement 6). On the
// server the factory runs locally: no second round trip.
//
// DESIGN DECISION: How a missing order is reported depends on the return type
//
// A command with a result reports it in the result (SubmitOrder's NotFound).
// A command that returns only Task has nowhere to put it, so a missing order is
// an exception (CancelOrder). Over HTTP that exception reaches the caller as
// an HttpRequestException (the server answers 500). (VisionAlignment R-I.)
// An order that is already cancelled is not missing: CancelOrder reads its
// status and leaves it, so Order.Cancel throws only for a caller that ignored
// it (R-E).
//
// =============================================================================

using Design.Domain.Aggregates;
using Neatoo.RemoteFactory;

namespace Design.Domain.Services;

/// <summary>
/// What submitting an order did.
/// </summary>
public enum SubmitOutcome
{
    Submitted,
    NotFound,
    /// <summary>The order is not a draft, or has no lines.</summary>
    NotSubmittable,
}

/// <summary>
/// Order commands a client sends to the server.
/// </summary>
#region ordercommands-declaration
[Factory]
public static partial class OrderCommands
#endregion
{
    /// <summary>
    /// Submits a stored draft order.
    /// </summary>
    /// <remarks>
    /// Reads the order's CanSubmit verdict before calling Submit, which throws
    /// only for an order that cannot be submitted (R-E). Parameters follow the
    /// standard order: values, then [Service] parameters, then the token.
    /// </remarks>
    #region operations-execute
    [Remote, Execute]
    private static async Task<SubmitOutcome> _SubmitOrder(
        int orderId,
        [Service] IOrderFactory orderFactory,
        CancellationToken cancellationToken)
    {
        var order = await orderFactory.Fetch(orderId, cancellationToken);
        if (order == null)
            return SubmitOutcome.NotFound;
        if (!order.CanSubmit)
            return SubmitOutcome.NotSubmittable;

        order.Submit();
        await orderFactory.Save(order, cancellationToken);
        return SubmitOutcome.Submitted;
    }
    #endregion

    /// <summary>
    /// Cancels a stored order.
    /// </summary>
    #region operations-execute-command
    [Remote, Execute]
    private static async Task _CancelOrder(int orderId, [Service] IOrderFactory orderFactory)
    {
        var order = await orderFactory.Fetch(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found");
        if (order.Status == OrderStatus.Cancelled)
            return;   // already cancelled: a retried call is not an error

        order.Cancel();
        await orderFactory.Save(order);
    }
    #endregion
}

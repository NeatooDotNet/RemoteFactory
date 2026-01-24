# Snippet Placeholders Summary

This document lists all MarkdownSnippets placeholders created in the RemoteFactory documentation, organized by file. The docs-code-samples agent will create compilable C# code for each placeholder.

## Target Audience

Expert .NET developers familiar with:
- C# 12+, .NET 8/9/10
- DDD patterns (aggregates, entities, value objects, repositories)
- ASP.NET Core (minimal APIs, DI, middleware)
- Blazor WebAssembly
- Entity Framework Core

**Do not explain basic C# or DDD concepts.** Focus on RemoteFactory-specific usage.

## Project Context

**RemoteFactory** is a Roslyn source generator that:
- Eliminates DTOs by generating factories that serialize domain model state
- Generates ASP.NET Core endpoints automatically
- Provides client-side HTTP stubs for Blazor WASM
- Supports DI via `[Service]` parameters
- Uses ordinal (compact) or named (verbose) JSON serialization

**Key assemblies:**
- `Neatoo.RemoteFactory` - Core library + source generator
- `Neatoo.RemoteFactory.AspNetCore` - Server-side integration

## Snippet Naming Convention

All snippets follow this pattern:
- `{document}-{topic}` for feature guides
- `readme-{section}` for README
- `getting-started-{topic}` for getting started

## README.md (7 snippets)

### readme-domain-model
**Context:** Domain model with factory operations for create, fetch, update, delete
**Requirements:**
- Class with `[Factory]` attribute
- Constructor with `[Create]` attribute
- `[Fetch]` method that loads state from database
- `[Update]` and `[Delete]` methods with `[Service]` parameters
- Use a simple entity like `Person` with FirstName, LastName, Email properties

### readme-client-usage
**Context:** Client code using the generated factory
**Requirements:**
- Inject `IPersonFactory` (generated interface)
- Call `Create()`, `Fetch()`, `Update()`, `Delete()` methods
- Show Blazor component or simple client code

### readme-client-assembly-mode
**Context:** Configure client assembly for RemoteOnly mode
**Requirements:**
- AssemblyAttributes.cs file
- `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`

### readme-server-setup
**Context:** Server Program.cs setup
**Requirements:**
- `AddNeatooAspNetCore(typeof(Person).Assembly)`
- `UseNeatoo()`
- Register domain services

### readme-client-setup
**Context:** Client Program.cs setup (Blazor WASM)
**Requirements:**
- `AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Person).Assembly)`
- Register keyed `HttpClient` with `RemoteFactoryServices.HttpClientKey`

### readme-full-example
**Context:** Complete domain model example with all operations
**Requirements:**
- Same as `readme-domain-model` but more complete
- Include authorization with `[AuthorizeFactory<T>]`

## docs/getting-started.md (6 snippets)

### getting-started-client-mode
**Context:** Set client assembly to RemoteOnly mode
**Requirements:** Same as `readme-client-assembly-mode`

### getting-started-server-program
**Context:** Complete server Program.cs
**Requirements:**
- Full ASP.NET Core setup
- AddNeatooAspNetCore
- UseNeatoo
- CORS for Blazor client
- Register DbContext and domain services

### getting-started-client-program
**Context:** Complete client Program.cs (Blazor WASM)
**Requirements:**
- Full Blazor WASM setup
- AddNeatooRemoteFactory
- Register keyed HttpClient
- MudBlazor or similar UI framework

### getting-started-person-model
**Context:** Complete Person domain model
**Requirements:**
- Interface `IPerson` with properties
- Class implementing interface with `[Factory]`
- `[Create]` constructor
- `[Fetch]`, `[Update]`, `[Delete]` methods with EF Core integration
- Use IPersonContext (DbContext) as `[Service]` parameter

### getting-started-usage
**Context:** Blazor component using the factory
**Requirements:**
- Inject `IPersonFactory`
- Form with validation
- Create/Fetch/Update/Delete button handlers
- Error handling

### getting-started-serialization-config
**Context:** Custom serialization configuration
**Requirements:**
- NeatooSerializationOptions with Named format
- Pass to AddNeatooAspNetCore

## docs/factory-operations.md (23 snippets)

### operations-create-constructor
**Context:** Constructor with `[Create]` attribute
**Requirements:**
- Simple constructor with initialization
- Generate default values

### operations-create-static
**Context:** Static factory method with `[Create]`
**Requirements:**
- Static method returning new instance
- Complex initialization logic

### operations-create-return-types
**Context:** Create methods with different return types
**Requirements:**
- Void (sets properties on existing instance)
- Instance return (new instance)
- Static method returning instance

### operations-fetch-instance
**Context:** Instance method with `[Fetch]` that loads state
**Requirements:**
- Method loads data from database via `[Service]` DbContext
- Sets properties on `this`
- Returns `Task`

### operations-fetch-bool-return
**Context:** Fetch with bool return indicating success
**Requirements:**
- Return `true` if entity found
- Return `false` if not found

### operations-insert
**Context:** Insert operation
**Requirements:**
- Add entity to DbContext
- SaveChangesAsync

### operations-update
**Context:** Update operation
**Requirements:**
- Modify existing entity
- SaveChangesAsync

### operations-delete
**Context:** Delete operation
**Requirements:**
- Remove entity from DbContext
- SaveChangesAsync

### operations-insert-update
**Context:** Combined Insert and Update method
**Requirements:**
- Method with both `[Insert]` and `[Update]` attributes
- Check if entity exists
- Add or update accordingly

### operations-execute
**Context:** Execute operation for commands
**Requirements:**
- Method with `[Execute]` attribute
- Command pattern (e.g., "ApproveOrder")

### operations-execute-command
**Context:** Command with parameters
**Requirements:**
- Execute method with business logic
- Parameters for command input

### operations-event
**Context:** Event handler with `[Event]` attribute
**Requirements:**
- Method with `[Event]` attribute
- CancellationToken as final parameter
- Fire-and-forget semantics

### operations-event-tracker
**Context:** Accessing IEventTracker
**Requirements:**
- Inject IEventTracker
- Call WaitForPendingEventsAsync in tests

### operations-remote
**Context:** `[Remote]` attribute for client/server split
**Requirements:**
- Method with `[Remote]` attribute
- Show server-side execution
- Show client-side serialization

### operations-lifecycle-onstart
**Context:** IFactoryOnStart implementation
**Requirements:**
- Implement IFactoryOnStart
- FactoryStart method with validation

### operations-lifecycle-oncomplete
**Context:** IFactoryOnComplete implementation
**Requirements:**
- Implement IFactoryOnComplete
- FactoryComplete method with logging

### operations-lifecycle-oncancelled
**Context:** IFactoryOnCancelled implementation
**Requirements:**
- Implement IFactoryOnCancelled
- FactoryCancelled method with cleanup

### operations-cancellation
**Context:** CancellationToken support
**Requirements:**
- Method with CancellationToken parameter
- Pass to async operations
- Check IsCancellationRequested

### operations-params-value
**Context:** Value parameters
**Requirements:** Method with int, string, DateTime parameters

### operations-params-service
**Context:** Service parameters with `[Service]`
**Requirements:** Parameters marked with `[Service]` attribute

### operations-params-array
**Context:** Array parameters
**Requirements:** Method with array or collection parameter

### operations-params-cancellation
**Context:** CancellationToken parameter
**Requirements:** Optional CancellationToken parameter

## docs/service-injection.md (11 snippets)

### service-injection-basic
**Context:** Basic service injection
**Requirements:**
- Method with `[Service]` parameter
- Inject IRepository or DbContext

### service-injection-multiple
**Context:** Multiple service parameters
**Requirements:**
- Method with multiple `[Service]` parameters
- Mix services and value parameters

### service-injection-scoped
**Context:** Scoped service injection
**Requirements:**
- Register scoped service in DI
- Inject into factory method

### service-injection-constructor
**Context:** Constructor injection
**Requirements:**
- Constructor with `[Service]` parameters
- Initialize readonly fields

### service-injection-server-only
**Context:** Services only available on server
**Requirements:**
- DbContext or repository only on server
- Not available in client assembly

### service-injection-client
**Context:** Client-side service injection
**Requirements:**
- Client-side service (e.g., ILogger)
- Available in both client and server

### service-injection-matching-name
**Context:** RegisterMatchingName convention
**Requirements:**
- Interface IPersonRepository
- Implementation PersonRepository
- Call RegisterMatchingName

### service-injection-mixed
**Context:** Mix of value and service parameters
**Requirements:**
- Method with value parameters, service parameters, and CancellationToken

### service-injection-httpcontext
**Context:** HttpContext injection on server
**Requirements:**
- Inject IHttpContextAccessor
- Access HttpContext.User

### service-injection-serviceprovider
**Context:** IServiceProvider injection
**Requirements:**
- Inject IServiceProvider
- Resolve services dynamically

### service-injection-lifetimes
**Context:** Service lifetime scope
**Requirements:**
- Show scoped vs transient vs singleton
- Explain per-request scope

### service-injection-testing
**Context:** Testing with mocked services
**Requirements:**
- Register mock services in test container
- Inject into factory methods

## docs/authorization.md (12 snippets)

### authorization-interface
**Context:** Authorization interface definition
**Requirements:**
- Interface with CanCreate, CanUpdate, etc. methods
- Methods with `[AuthorizeFactory]` attribute

### authorization-implementation
**Context:** Authorization implementation
**Requirements:**
- Implement authorization interface
- Inject user context
- Return bool based on user roles/claims

### authorization-apply
**Context:** Apply authorization to domain model
**Requirements:**
- `[AuthorizeFactory<IPersonAuth>]` on class

### authorization-generated
**Context:** Generated Can* methods on factory
**Requirements:**
- Show generated CanCreate(), CanFetch(), etc.
- Client can check before calling

### authorization-combined-flags
**Context:** Combined authorization flags
**Requirements:**
- `[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]`

### authorization-method-level
**Context:** Method-level authorization override
**Requirements:**
- `[AuthorizeFactory]` on specific method
- Override class-level authorization

### authorization-policy-config
**Context:** ASP.NET Core policy configuration
**Requirements:**
- Define authorization policies in Program.cs

### authorization-policy-apply
**Context:** Apply policy to factory method
**Requirements:**
- `[AspAuthorize("PolicyName")]` on method

### authorization-policy-multiple
**Context:** Multiple policies
**Requirements:**
- Multiple `[AspAuthorize]` attributes

### authorization-policy-roles
**Context:** Role-based authorization
**Requirements:**
- `[AspAuthorize(Roles = "Admin,Manager")]`

### authorization-combined
**Context:** Combine custom and ASP.NET Core auth
**Requirements:**
- Both `[AuthorizeFactory<T>]` and `[AspAuthorize]`
- Custom auth runs first, then ASP.NET Core

### authorization-exception
**Context:** NotAuthorizedException handling
**Requirements:**
- Try/catch NotAuthorizedException
- Handle unauthorized requests

### authorization-events
**Context:** Event authorization
**Requirements:**
- Events bypass authorization
- Use for internal operations

### authorization-testing
**Context:** Testing authorization
**Requirements:**
- Mock authorization service
- Test authorized and unauthorized scenarios

### authorization-context
**Context:** Access authorization context
**Requirements:**
- Inject HttpContext or ClaimsPrincipal
- Read user claims

## docs/serialization.md (14 snippets)

### serialization-config
**Context:** Serialization configuration
**Requirements:**
- NeatooSerializationOptions
- SerializationFormat.Ordinal vs Named

### serialization-ordinal-generated
**Context:** Generated ordinal serialization code
**Requirements:**
- Show array-based format
- Properties in alphabetical order

### serialization-ordinal-versioning
**Context:** Ordinal versioning strategy
**Requirements:**
- Add properties at the end
- Maintain alphabetical order

### serialization-custom-ordinal
**Context:** Custom ordinal converter
**Requirements:**
- Implement IOrdinalSerializable
- Implement IOrdinalConverterProvider
- Custom JsonConverter

### serialization-references
**Context:** Object references and cycles
**Requirements:**
- NeatooReferenceHandler
- Detect and preserve references

### serialization-interface
**Context:** Interface serialization
**Requirements:**
- Serialize as interface type
- Deserialize to concrete type

### serialization-collections
**Context:** Collection serialization
**Requirements:**
- List, array, IEnumerable

### serialization-polymorphism
**Context:** Polymorphic serialization
**Requirements:**
- Base class with derived types
- Type discriminator

### serialization-validation
**Context:** Client-side validation
**Requirements:**
- DataAnnotations attributes
- Blazor EditForm validation

### serialization-validation-server
**Context:** Server-side validation
**Requirements:**
- Validate in factory method
- Throw exception if invalid

### serialization-custom-converter
**Context:** Custom JsonConverter
**Requirements:**
- Custom converter for value object
- Register with NeatooSerializationOptions

### serialization-logging
**Context:** Serialization logging
**Requirements:**
- Enable serialization logging
- Log payload size, format

### serialization-debug-named
**Context:** Debug with Named format
**Requirements:**
- Use Named format in development
- Compare payload sizes

### serialization-json-options
**Context:** Custom JsonSerializerOptions
**Requirements:**
- Configure JsonSerializerOptions
- Pass to NeatooSerializationOptions

## docs/save-operation.md (17 snippets)

### save-ifactorysavemeta
**Context:** IFactorySaveMeta implementation
**Requirements:**
- Implement IsNew and IsDeleted properties
- Track entity state

### save-write-operations
**Context:** Insert, Update, Delete methods
**Requirements:**
- Methods with corresponding attributes
- Server-side persistence logic

### save-usage
**Context:** Client using Save method
**Requirements:**
- Call factory.Save(entity)
- Save routes based on IsNew/IsDeleted

### save-generated
**Context:** Generated Save method
**Requirements:**
- Show generated IFactorySave<T> implementation
- Routing logic

### save-state-new
**Context:** New entity state
**Requirements:**
- IsNew = true
- Save calls Insert

### save-state-fetch
**Context:** Fetched entity state
**Requirements:**
- IsNew = false (set in Fetch)
- Save calls Update

### save-state-delete
**Context:** Deleted entity state
**Requirements:**
- IsDeleted = true
- Save calls Delete

### save-complete-example
**Context:** Complete domain model with Save
**Requirements:**
- IFactorySaveMeta implementation
- Insert, Update, Delete methods
- State tracking

### save-complete-usage
**Context:** Complete client usage
**Requirements:**
- Create/Fetch/Update/Delete flows
- All use factory.Save()

### save-partial-methods
**Context:** Partial operation support
**Requirements:**
- Only Insert and Update (no Delete)
- Save still works

### save-authorization
**Context:** Authorization with Save
**Requirements:**
- Authorize Insert/Update/Delete separately
- Save respects per-operation auth

### save-authorization-combined
**Context:** Combined Read/Write authorization
**Requirements:**
- AuthorizeFactoryOperation.Write covers all write operations

### save-validation
**Context:** Validation in Save
**Requirements:**
- Validate before routing
- Return null if invalid

### save-validation-throw
**Context:** Throw exception on validation failure
**Requirements:**
- Throw ValidationException
- Client handles error

### save-optimistic-concurrency
**Context:** Optimistic concurrency
**Requirements:**
- RowVersion property
- DbUpdateConcurrencyException handling

### save-no-delete
**Context:** Save without Delete support
**Requirements:**
- Only Insert/Update
- IsDeleted ignored

### save-explicit
**Context:** Explicit operation calls
**Requirements:**
- Call Insert/Update/Delete directly
- Bypass Save routing

### save-extensions
**Context:** Save extension methods
**Requirements:**
- SaveAsync extension
- Batch save extension

### save-testing
**Context:** Testing Save routing
**Requirements:**
- Test Insert, Update, Delete routes
- Verify state transitions

## docs/factory-modes.md (13 snippets)

### modes-full-config
**Context:** Full mode configuration (default)
**Requirements:**
- No FactoryModeAttribute needed (default)
- Or `[assembly: FactoryMode(FactoryMode.Full)]`

### modes-full-generated
**Context:** Generated factory in Full mode
**Requirements:**
- Show local and remote execution paths
- Local calls entity methods directly
- Remote serializes and sends HTTP

### modes-remoteonly-config
**Context:** RemoteOnly mode configuration
**Requirements:**
- `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`
- Client assembly only

### modes-remoteonly-generated
**Context:** Generated factory in RemoteOnly mode
**Requirements:**
- Only HTTP stubs
- No local implementation

### modes-logical-config
**Context:** Logical mode configuration
**Requirements:**
- NeatooFactory.Logical in AddNeatooRemoteFactory
- Single-tier app

### modes-logical-execution
**Context:** Logical mode execution
**Requirements:**
- Direct method calls
- No serialization

### modes-logical-testing
**Context:** Logical mode in tests
**Requirements:**
- Use Logical mode for unit tests
- Test domain logic without HTTP

### modes-full-example
**Context:** Full mode example (server)
**Requirements:**
- Complete server setup
- Local and remote execution

### modes-remoteonly-example
**Context:** RemoteOnly example (client)
**Requirements:**
- Complete client setup
- HTTP-only execution

### modes-logical-example
**Context:** Logical mode example (single-tier)
**Requirements:**
- Console app or desktop app
- Direct execution

### modes-local-remote-methods
**Context:** Mix of local and remote methods
**Requirements:**
- `[Remote]` on some methods
- Others are local-only

### modes-logging
**Context:** Mode-specific logging
**Requirements:**
- Log when using remote vs local
- Debug mode switching

## docs/events.md (19 snippets)

### events-basic
**Context:** Basic event handler
**Requirements:**
- Method with `[Event]` attribute
- CancellationToken parameter
- Fire-and-forget semantics

### events-caller
**Context:** Calling an event
**Requirements:**
- Factory.SendNotification() call
- Returns Task but doesn't await
- Continues immediately

### events-tracker-generated
**Context:** Generated event tracker delegate
**Requirements:**
- Show generated code that uses IEventTracker
- Isolated scope

### events-requirements
**Context:** Event method requirements
**Requirements:**
- Must have CancellationToken as final parameter
- Must return void or Task

### events-scope-isolation
**Context:** Event scope isolation
**Requirements:**
- Event runs in new IServiceScope
- Independent transaction

### events-scope-example
**Context:** Example showing scope isolation
**Requirements:**
- Main operation saves to database
- Event handler saves to different table
- Both scopes independent

### events-cancellation
**Context:** Event cancellation
**Requirements:**
- Respect CancellationToken
- Graceful shutdown

### events-graceful-shutdown
**Context:** Graceful shutdown with events
**Requirements:**
- IHostApplicationLifetime.ApplicationStopping
- Wait for pending events

### events-eventtracker-access
**Context:** Access IEventTracker
**Requirements:**
- Inject IEventTracker
- Query pending events

### events-eventtracker-wait
**Context:** Wait for events in tests
**Requirements:**
- Call WaitForPendingEventsAsync
- Assert event completed

### events-eventtracker-count
**Context:** Count pending events
**Requirements:**
- IEventTracker.PendingEventCount

### events-error-handling
**Context:** Event error handling
**Requirements:**
- Try/catch in event handler
- Log errors
- Don't throw (fire-and-forget)

### events-aspnetcore
**Context:** ASP.NET Core event setup
**Requirements:**
- EventTrackerHostedService registered automatically
- Shutdown handling

### events-domain-events
**Context:** Domain events pattern
**Requirements:**
- Publish domain event
- Event handler updates read model

### events-notifications
**Context:** Notification event
**Requirements:**
- Send email or push notification
- Background processing

### events-audit
**Context:** Audit logging event
**Requirements:**
- Audit trail in separate table
- Fire-and-forget

### events-integration
**Context:** Integration event
**Requirements:**
- Call external API
- Fire-and-forget

### events-authorization
**Context:** Events bypass authorization
**Requirements:**
- No authorization on events
- Use for internal operations

### events-testing
**Context:** Testing events
**Requirements:**
- Use IEventTracker.WaitForPendingEventsAsync
- Assert side effects

### events-testing-latch
**Context:** Testing with CountdownEvent
**Requirements:**
- CountdownEvent to wait for event
- Assert event completed

### events-correlation
**Context:** Correlation ID in events
**Requirements:**
- CorrelationContext.CorrelationId available in event handler

## docs/attributes-reference.md (20 snippets)

### attributes-factory
**Context:** `[Factory]` attribute usage
**Requirements:**
- On class or interface
- Triggers factory generation

### attributes-suppressfactory
**Context:** `[SuppressFactory]` attribute
**Requirements:**
- On derived class
- Prevents factory generation

### attributes-create
**Context:** `[Create]` attribute
**Requirements:**
- On constructor or method
- Generates Create method on factory

### attributes-fetch
**Context:** `[Fetch]` attribute
**Requirements:**
- On method
- Generates Fetch method on factory

### attributes-insert
**Context:** `[Insert]` attribute
**Requirements:**
- On method
- Generates Insert method

### attributes-update
**Context:** `[Update]` attribute
**Requirements:**
- On method
- Generates Update method

### attributes-delete
**Context:** `[Delete]` attribute
**Requirements:**
- On method
- Generates Delete method

### attributes-execute
**Context:** `[Execute]` attribute
**Requirements:**
- On method
- Generates Execute method

### attributes-event
**Context:** `[Event]` attribute
**Requirements:**
- On method
- Generates event delegate
- CancellationToken required

### attributes-remote
**Context:** `[Remote]` attribute
**Requirements:**
- On method
- Marks for remote execution

### attributes-service
**Context:** `[Service]` attribute
**Requirements:**
- On parameter
- Injects from DI container

### attributes-authorizefactory-generic
**Context:** `[AuthorizeFactory<T>]` on class
**Requirements:**
- Generic attribute
- References authorization interface

### attributes-authorizefactory-interface
**Context:** Authorization interface definition
**Requirements:**
- Interface with Can* methods
- `[AuthorizeFactory]` on methods

### attributes-authorizefactory-method
**Context:** `[AuthorizeFactory]` on method
**Requirements:**
- Method-level authorization
- Override class-level

### attributes-aspauthorize
**Context:** `[AspAuthorize]` attribute
**Requirements:**
- Policy, Roles, AuthenticationSchemes

### attributes-factorymode
**Context:** `[FactoryMode]` attribute
**Requirements:**
- Assembly-level attribute
- FactoryMode.Full/RemoteOnly

### attributes-factoryhintnamelength
**Context:** `[FactoryHintNameLength]` attribute
**Requirements:**
- Assembly-level
- Limit generated file name length

### attributes-multiple-operations
**Context:** Multiple operation attributes
**Requirements:**
- `[Insert, Update]` on same method

### attributes-remote-operation
**Context:** `[Remote]` with operation attribute
**Requirements:**
- `[Remote, Fetch]` on method

### attributes-authorization-operation
**Context:** Authorization with operations
**Requirements:**
- `[AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]`

### attributes-inheritance
**Context:** Attribute inheritance
**Requirements:**
- Base class with `[Factory]`
- Derived class inherits or suppresses

### attributes-pattern-crud
**Context:** CRUD pattern
**Requirements:**
- Complete CRUD entity

### attributes-pattern-readonly
**Context:** Read-only pattern
**Requirements:**
- Only Create and Fetch

### attributes-pattern-command
**Context:** Command pattern
**Requirements:**
- Execute operations only

### attributes-pattern-event
**Context:** Event pattern
**Requirements:**
- Event handlers only

## docs/interfaces-reference.md (13 snippets)

### interfaces-factoryonstart
**Context:** IFactoryOnStart implementation
**Requirements:**
- Implement FactoryStart method
- Pre-operation logic

### interfaces-factoryonstart-async
**Context:** IFactoryOnStartAsync implementation
**Requirements:**
- Implement FactoryStartAsync method
- Async pre-operation logic

### interfaces-factoryoncomplete
**Context:** IFactoryOnComplete implementation
**Requirements:**
- Implement FactoryComplete method
- Post-operation logic

### interfaces-factoryoncomplete-async
**Context:** IFactoryOnCompleteAsync implementation
**Requirements:**
- Implement FactoryCompleteAsync method
- Async post-operation logic

### interfaces-factoryoncancelled
**Context:** IFactoryOnCancelled implementation
**Requirements:**
- Implement FactoryCancelled method
- Cancellation cleanup

### interfaces-factoryoncancelled-async
**Context:** IFactoryOnCancelledAsync implementation
**Requirements:**
- Implement FactoryCancelledAsync method
- Async cancellation cleanup

### interfaces-lifecycle-order
**Context:** Lifecycle hook execution order
**Requirements:**
- Implement multiple lifecycle hooks
- Show execution order

### interfaces-factorysavemeta
**Context:** IFactorySaveMeta implementation
**Requirements:**
- IsNew and IsDeleted properties

### interfaces-factorysave
**Context:** Using IFactorySave<T>
**Requirements:**
- Call factory.Save(entity)

### interfaces-aspauthorize
**Context:** Custom IAspAuthorize implementation
**Requirements:**
- Implement GetAspAuthorizeData
- Custom authorization logic

### interfaces-ordinalserializable
**Context:** IOrdinalSerializable marker
**Requirements:**
- Implement interface on type

### interfaces-ordinalconverterprovider
**Context:** IOrdinalConverterProvider implementation
**Requirements:**
- Implement GetOrdinalConverter
- Return custom JsonConverter

## docs/aspnetcore-integration.md (18 snippets)

### aspnetcore-basic-setup
**Context:** Basic ASP.NET Core setup
**Requirements:**
- AddNeatooAspNetCore
- UseNeatoo

### aspnetcore-addneatoo
**Context:** AddNeatooAspNetCore call
**Requirements:**
- Register with domain assembly

### aspnetcore-custom-serialization
**Context:** Custom serialization options
**Requirements:**
- NeatooSerializationOptions with Named format

### aspnetcore-middleware-order
**Context:** Middleware order
**Requirements:**
- CORS, UseNeatoo, other middleware

### aspnetcore-cancellation
**Context:** Cancellation support
**Requirements:**
- Client disconnect
- Server shutdown
- CancellationToken

### aspnetcore-correlation-id
**Context:** Correlation ID access
**Requirements:**
- CorrelationContext.CorrelationId in factory method

### aspnetcore-logging
**Context:** Logging configuration
**Requirements:**
- ILogger injection
- Log categories

### aspnetcore-custom-authorization
**Context:** Custom IAspAuthorize
**Requirements:**
- Implement IAspAuthorize
- Register in DI

### aspnetcore-service-registration
**Context:** Service registration
**Requirements:**
- Register DbContext, repositories, etc.

### aspnetcore-multi-assembly
**Context:** Multiple assemblies
**Requirements:**
- Pass multiple assemblies to AddNeatooAspNetCore

### aspnetcore-development
**Context:** Development configuration
**Requirements:**
- Named format for debugging

### aspnetcore-production
**Context:** Production configuration
**Requirements:**
- Ordinal format (default)

### aspnetcore-error-handling
**Context:** Error handling
**Requirements:**
- Try/catch in client
- Handle NotAuthorizedException

### aspnetcore-cors
**Context:** CORS configuration
**Requirements:**
- AddCors
- UseCors

### aspnetcore-minimal-api
**Context:** Minimal API integration
**Requirements:**
- UseNeatoo with other endpoints

### aspnetcore-testing
**Context:** Testing with two containers
**Requirements:**
- ClientServerContainers.Scopes()
- Test client/server communication

## Platform-Specific Samples

### Blazor WASM
- Client setup in `getting-started-client-program`
- Component usage in `getting-started-usage`
- Validation in `serialization-validation`

### ASP.NET Core
- Server setup in `getting-started-server-program`
- Middleware in `aspnetcore-middleware-order`
- Authorization in `authorization-policy-config`

### Entity Framework Core
- DbContext integration in all Fetch/Insert/Update/Delete examples
- Transactions in `events-scope-example`
- Concurrency in `save-optimistic-concurrency`

### Testing
- Two-container pattern in `aspnetcore-testing`
- Event testing in `events-testing`
- Authorization testing in `authorization-testing`

## Code Sample Guidelines

1. **Use real domain concepts** - Person, Order, Product (not Foo, Bar)
2. **Show complete examples** - Include namespaces, using statements
3. **Include error handling** - Try/catch where appropriate
4. **Demonstrate best practices** - Async/await, cancellation, disposal
5. **No placeholder comments** - Code should be compilable
6. **DDD terminology** - Use aggregate, entity, value object terms naturally
7. **Target expert developers** - Don't explain basic C# or DDD

## Next Steps for docs-code-samples Agent

1. Create a test project structure for sample code
2. Implement each snippet with compilable C# code
3. Ensure all samples build against RemoteFactory packages
4. Run MarkdownSnippets to inject code into documentation
5. Verify all snippets are present in generated docs

## Total Snippet Count

- README: 7
- getting-started: 6
- factory-operations: 23
- service-injection: 11
- authorization: 12
- serialization: 14
- save-operation: 17
- factory-modes: 13
- events: 19
- attributes-reference: 20
- interfaces-reference: 13
- aspnetcore-integration: 18

**Total: 173 snippets**

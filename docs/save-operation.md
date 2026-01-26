# Save Operation

The Save operation provides automatic routing to Insert, Update, or Delete based on entity state.

## IFactorySave Interface

Classes implementing `IFactorySaveMeta` get an `IFactorySave<T>` interface on their factory with a `Save()` method.

### Step 1: Implement IFactorySaveMeta

In the Domain layer, define an Employee entity that implements `IFactorySaveMeta` for state tracking:

<!-- snippet: save-ifactorysavemeta -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity class with [Factory] attribute implementing IFactorySaveMeta
- Properties: Id (Guid), FirstName (string), LastName (string), Email (string?)
- IFactorySaveMeta properties: IsNew (bool, default true), IsDeleted (bool)
- [Create] constructor that generates new Guid for Id
- [Remote, Fetch] method that loads from IEmployeeRepository and sets IsNew = false
- Show how IsNew defaults to true for new instances and becomes false after fetch
-->
<!-- endSnippet -->

`IFactorySaveMeta` requires two properties:
- `IsNew`: true for new entities not yet persisted
- `IsDeleted`: true for entities marked for deletion

### Step 2: Implement Write Operations

In the Domain layer, add Insert, Update, and Delete operations to the Employee entity:

<!-- snippet: save-write-operations -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity with full CRUD operations
- Properties: Id (Guid), FirstName (string), LastName (string), DepartmentId (Guid), IsNew, IsDeleted
- [Create] constructor that generates new Id
- [Remote, Insert] method that:
  - Creates EmployeeEntity, maps properties
  - Sets Created and Modified timestamps
  - Calls repository.AddAsync and SaveChangesAsync
  - Sets IsNew = false after successful insert
- [Remote, Update] method that:
  - Fetches entity by Id, throws if not found
  - Maps updated properties
  - Updates Modified timestamp
  - Calls repository.UpdateAsync and SaveChangesAsync
- [Remote, Delete] method that:
  - Calls repository.DeleteAsync(Id) and SaveChangesAsync
- Use [Service] IEmployeeRepository for all data access
-->
<!-- endSnippet -->

### Step 3: Use Save Method

The Application layer uses the generated factory's Save method. The factory routes to the appropriate operation:

<!-- snippet: save-usage -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Demonstrate IEmployeeFactory.Save() usage
- Show three scenarios with actual executable code (not comments):
  1. Create new employee, set properties, call Save (routes to Insert because IsNew = true)
  2. Modify saved employee's name, call Save again (routes to Update because IsNew = false)
  3. Mark employee.IsDeleted = true, call Save (routes to Delete)
- Demonstrate how IsNew changes from true to false after first Save
- Use async/await properly
-->
<!-- endSnippet -->

The factory's `Save()` method examines `IsNew` and `IsDeleted` to determine which operation to call.

## Routing Logic

| IsNew | IsDeleted | Operation | Description |
|-------|-----------|-----------|-------------|
| true | false | **Insert** | New entity, persist to database |
| false | false | **Update** | Existing entity, persist changes |
| false | true | **Delete** | Existing entity, remove from database |
| true | true | **None** | New entity deleted before save, no-op |

Generated Save method:

<!-- snippet: save-generated -->
<!--
SNIPPET REQUIREMENTS:
- Show the conceptual routing logic of the generated Save method (as pseudocode/comments)
- Demonstrate the decision tree:
  - If IsDeleted: if also IsNew return null (no-op), else route to Delete
  - If IsNew: route to Insert
  - Else: route to Update
- This is explanatory pseudocode showing what the generator produces
- Use LocalSave, LocalInsert, LocalUpdate, LocalDelete method names
-->
<!-- endSnippet -->

## State Management

Track state in your domain model:

### Constructor Sets IsNew

In the Application layer, demonstrate how Create initializes state:

<!-- snippet: save-state-new -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Show state after factory.Create()
- Actual executable code demonstrating:
  - Create employee via factory
  - Assert/show IsNew = true, IsDeleted = false
  - Set properties and call Save
  - Assert/show IsNew = false after Save (Insert was called)
- Show the state transition from new to persisted
-->
<!-- endSnippet -->

### Fetch Clears IsNew

In the Application layer, demonstrate how Fetch sets state for existing entities:

<!-- snippet: save-state-fetch -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Show state after factory.Fetch()
- Actual executable code demonstrating:
  - Fetch existing employee by Id
  - Assert/show IsNew = false (fetched entities already exist)
  - Modify a property and call Save
  - Demonstrate that Update is called (not Insert) because IsNew = false
-->
<!-- endSnippet -->

### MarkDeleted Sets IsDeleted

In the Application layer, demonstrate deletion workflow:

<!-- snippet: save-state-delete -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Show deletion state and Save routing
- Actual executable code demonstrating:
  - Start with an existing employee (IsNew = false)
  - Set IsDeleted = true
  - Assert/show IsNew = false, IsDeleted = true
  - Call Save (routes to Delete)
  - Show that Save returns the deleted entity
-->
<!-- endSnippet -->

## Complete Example

In the Domain layer, here's a complete Department entity with all CRUD operations:

<!-- snippet: save-complete-example -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Complete Department entity with full Save implementation
- Properties: Id (Guid), Name (string), Code (string), ManagerId (Guid?), Budget (decimal), IsActive (bool), IsNew, IsDeleted
- [Create] constructor that:
  - Generates new Id
  - Sets IsActive = true by default
- [Remote, Fetch] method with IDepartmentRepository
- [Remote, Insert] method that:
  - Creates DepartmentEntity
  - Maps all properties
  - Sets timestamps
  - Saves and sets IsNew = false
- [Remote, Update] method that:
  - Fetches by Id with error handling
  - Updates all mutable properties
  - Updates Modified timestamp
  - Saves changes
- [Remote, Delete] method that removes the department
- Use Employee Management domain terminology
-->
<!-- endSnippet -->

Usage:

<!-- snippet: save-complete-usage -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Full CRUD workflow with Department
- Actual executable code showing:
  - CREATE: factory.Create(), set properties, Save (returns created department with Id)
  - READ: factory.Fetch(departmentId)
  - UPDATE: modify Budget property, Save
  - DELETE: set IsDeleted = true, Save
- Show property values at each step
- Use async/await properly
-->
<!-- endSnippet -->

## Return Values

Save returns the entity or null:

```csharp
Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
```

**Returns null when:**
- Insert/Update/Delete operation returns false (not authorized or not found)
- IsNew = true and IsDeleted = true (new entity deleted before save)

**Returns entity when:**
- Operation succeeds (void or returns true)

## Partial Save Methods

You don't need to implement all three operations. Save routes based on what you've defined:

In the Domain layer, create an entity that only supports Insert (immutable after creation):

<!-- snippet: save-partial-methods -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: AuditLog entity that is read-only after creation
- Properties: Id (Guid), Action (string), EntityType (string), EntityId (Guid), Timestamp (DateTime), UserId (Guid), IsNew, IsDeleted
- [Create] constructor that sets Id and Timestamp = DateTime.UtcNow
- [Remote, Insert] method ONLY - no Update or Delete
- Comments explaining:
  - No Update method = entity becomes read-only after creation
  - No Delete method = audit records cannot be deleted
  - Save behavior: calls Insert when IsNew, no-op otherwise
- Use for audit/compliance scenarios where records are immutable
-->
<!-- endSnippet -->

Common patterns:

| Operations Defined | Pattern | Save Behavior |
|-------------------|---------|---------------|
| Insert only | Create-once | Routes new entities to Insert, no-op for updates |
| Insert + Update | Full write | Routes to Insert or Update based on IsNew |
| Insert + Update + Delete | Full CRUD | Routes to all three based on state |
| Update + Delete | Modify/remove | No Insert allowed, can only modify or delete |

For Upsert (same method for Insert and Update), mark a single method with both `[Insert, Update]` attributes. See [Insert, Update, Delete Operations](factory-operations.md#insert-update-delete-operations) for details.

## Authorization with Save

Apply authorization to individual operations:

In the Domain layer, define authorization for Employee operations with granular control:

<!-- snippet: save-authorization -->
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeWriteAuth interface with:
  - [AuthorizeFactory(AuthorizeFactoryOperation.Create)] bool CanCreate()
  - [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite()
- Implement EmployeeWriteAuth class that:
  - Injects IUserContext
  - CanCreate() returns true if user is authenticated
  - CanWrite() returns true if user has "HR" or "Admin" role
- Define Employee entity with [Factory] and [AuthorizeFactory<IEmployeeWriteAuth>]
- Include [Remote, Insert], [Remote, Update], [Remote, Delete] methods
- Show how different operations can have different authorization rules
-->
<!-- endSnippet -->

Or to Save as a whole:

In the Domain layer, use a single authorization check for all write operations:

<!-- snippet: save-authorization-combined -->
<!--
SNIPPET REQUIREMENTS:
- Define ICombinedWriteAuth interface with:
  - Single [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite() method
  - Comment: Write = Insert | Update | Delete
- Implement CombinedWriteAuth class that:
  - Injects IUserContext
  - CanWrite() returns true if user has "Editor" or "Admin" role
- Define Department entity with [Factory] and [AuthorizeFactory<ICombinedWriteAuth>]
- Include all three write operations
- Show how one authorization check covers all write operations
-->
<!-- endSnippet -->

The factory checks authorization before routing.

## Validation Before Save

Validate state before saving:

In the Domain layer, use data annotations for validation:

<!-- snippet: save-validation -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity with validation attributes
- Properties with validation:
  - FirstName: [Required(ErrorMessage = "First name is required")]
  - LastName: [Required(ErrorMessage = "Last name is required")]
  - Email: [EmailAddress(ErrorMessage = "Invalid email format")]
  - Salary: [Range(0, double.MaxValue, ErrorMessage = "Salary must be non-negative")]
- IFactorySaveMeta implementation
- [Remote, Insert] method with comment that validation happens before save
- Also show Application layer helper method SaveWithValidation that:
  - Uses Validator.TryValidateObject before calling factory.Save
  - Returns null with validation errors if invalid
  - Calls factory.Save if valid
-->
<!-- endSnippet -->

Throw exceptions for validation failures:

In the Domain layer, perform server-side validation in the Insert method:

<!-- snippet: save-validation-throw -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity with server-side validation in Insert
- [Remote, Insert] method that:
  - Validates FirstName is not null or whitespace
  - Validates LastName is not null or whitespace
  - Validates Email format if provided
  - Throws ValidationException with descriptive message if invalid
  - Only persists if all validations pass
- Show usage pattern (as comments) demonstrating try/catch for ValidationException
-->
<!-- endSnippet -->

## Optimistic Concurrency

Use version tokens or timestamps:

In the Domain layer, implement optimistic concurrency with row versioning:

<!-- snippet: save-optimistic-concurrency -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity with concurrency handling
- Properties: include byte[]? RowVersion for concurrency token
- [Remote, Fetch] method that loads RowVersion from database
- [Remote, Update] method that:
  - Fetches current entity from database
  - Compares RowVersion using SequenceEqual
  - Throws InvalidOperationException with message "modified by another user" if versions don't match
  - Updates entity and saves if versions match
  - Comment that RowVersion is updated by database automatically
- Show the full concurrency check pattern
-->
<!-- endSnippet -->

EF Core DbUpdateConcurrencyException automatically becomes a 409 response when called remotely.

## Save Without Delete

If you don't implement Delete, IFactorySave still generates but throws `NotImplementedException` for deleted entities:

In the Domain layer, create an entity without Delete support:

<!-- snippet: save-no-delete -->
<!--
SNIPPET REQUIREMENTS:
- Domain layer: Employee entity without Delete method
- Properties: Id, FirstName, LastName, IsNew, IsDeleted
- [Create] constructor
- [Remote, Insert] method that sets IsNew = false
- [Remote, Update] method
- NO Delete method
- Comment explaining: setting IsDeleted = true and calling Save throws NotImplementedException
- Use case: soft-delete pattern where actual deletion is not allowed
-->
<!-- endSnippet -->

Save throws `NotImplementedException` when `IsDeleted = true`.

## Alternative: Explicit Methods

Save is optional. You can always call Insert/Update/Delete directly:

In the Application layer, demonstrate explicit method calls vs Save:

<!-- snippet: save-explicit -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: Show both explicit methods and Save for comparison
- Demonstrate (as executable code or clear comments):
  - Using Save with state flags for automatic routing
  - Create employee, Save routes to Insert
  - Modify employee, Save routes to Update
  - Set IsDeleted, Save routes to Delete
- Explain when to use Save vs explicit methods:
  - Save: UI doesn't track state, single save button, simpler client code
  - Explicit: granular control, different UI actions per operation
-->
<!-- endSnippet -->

Use Save when:
- UI doesn't track entity state changes
- You want a single save button
- State-based routing simplifies client code

Use explicit methods when:
- Client knows the exact operation needed
- You want granular control
- Different UI actions map to different operations

## IFactorySaveMeta Extensions

Extend IFactorySave for batch operations:

In the Application layer, create utility methods for common save patterns:

<!-- snippet: save-extensions -->
<!--
SNIPPET REQUIREMENTS:
- Application layer: SaveUtilities class with helper methods
- SaveWithCancellation<T> method:
  - Takes IFactorySave<T> factory, T entity, CancellationToken
  - Checks cancellation before save
  - Returns Task<T?> with properly cast result
- SaveBatch<T> method:
  - Takes IFactorySave<T> factory, IEnumerable<T> entities, CancellationToken
  - Iterates and saves each entity
  - Checks cancellation between each save
  - Collects and returns List<T> of successfully saved entities
- Show extension method pattern (as comments) for IFactorySave<T>
- Use where T : class, IFactorySaveMeta constraint
-->
<!-- endSnippet -->

Track additional state without affecting Save routing.

## Testing Save Routing

Test routing logic:

In the Tests layer, verify Save routes to the correct operation:

<!-- snippet: save-testing -->
<!--
SNIPPET REQUIREMENTS:
- Tests layer: Unit test demonstrating Save routing verification
- Test Insert routing:
  - Create employee via factory
  - Assert IsNew = true
  - Set properties and call Save
  - Assert IsNew = false (proves Insert was called)
- Test Update routing:
  - After Insert, modify a property
  - Call Save
  - Verify the modification persisted (proves Update was called)
- Test Delete routing:
  - Set IsDeleted = true
  - Call Save
  - Verify result is returned (proves Delete was called)
- Use xUnit or similar test assertions
-->
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - Insert, Update, Delete details
- [Authorization](authorization.md) - Secure save operations
- [Serialization](serialization.md) - Entity state serialization

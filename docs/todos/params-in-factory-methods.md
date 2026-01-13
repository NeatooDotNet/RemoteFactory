# What if they try to define params in their factory method?

**Priority:** Low
**Category:** Investigation
**Created:** 2026-01-12
**Status:** Not Started

## Question

What happens if a user defines a factory method with a `params` parameter?

```csharp
[Fetch]
public void Fetch(params int[] ids)
{
    // ...
}
```

## Investigation Needed

- [ ] Does the generator handle this correctly?
- [ ] Does serialization work for params arrays?
- [ ] Should this be explicitly supported, blocked, or documented?

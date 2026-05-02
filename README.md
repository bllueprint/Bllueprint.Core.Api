# Bllueprint.Core.Domain

A lightweight .NET library for **Domain-Driven Design (DDD)** that provides a structured, expressive way to model domain aggregate state transitions with built-in validation and notification support.

## Features

- **Aggregate base class** — a clean foundation for your domain entities
- **Transition pipeline** — fluent builder for defining guard-validated state transitions
- **Notification context** — scoped, async-safe collection of domain errors and warnings
- **DI integration** — one-liner registration with `IServiceCollection`
- **Zero mutation on failure** — actions only execute after all guards pass

---

## Installation

> Package not yet published. To use locally, reference the project directly:

```xml
<ProjectReference Include="../Bllueprint.Core.Domain/Bllueprint.Core.Domain.csproj" />
```

Or once published to NuGet:

```bash
dotnet add package Bllueprint.Core.Domain
```

---

## Getting Started

### 1. Register services

In your `Program.cs` or startup configuration:

```csharp
builder.Services.AddBllueprintDomainServices();
```

This registers `INotificationContext` as a scoped service and wires up the `AsyncLocal` domain notification state.

---

### 2. Create an aggregate

Inherit from `Aggregate<T>` and define transitions using the built-in builder:

```csharp
public class Order : Aggregate<Order>
{
    private static readonly ITransition<Order> _confirm =
        GetTransitionBuilder()
            .Requires(o => o.Status == OrderStatus.Draft, "Order must be in Draft to confirm.")
            .Requires(o => o.Items.Count > 0, "Order must have at least one item.")
            .Do(o => o.Status = OrderStatus.Confirmed)
            .Create();

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public List<string> Items { get; } = new();

    public void Confirm() => _confirm.Invoke(this);
}
```

---

### 3. Use transitions with arguments

For transitions that require external data, use the generic `ITransition<T, TArgs>` variant:

```csharp
private static readonly ITransition<Order, string> _addItem =
    GetTransitionBuilder<string>()
        .Requires((o, item) => !string.IsNullOrWhiteSpace(item), "Item name cannot be empty.")
        .Do((o, item) => o.Items.Add(item))
        .Create();

public void AddItem(string itemName) => _addItem.Invoke(this, itemName);
```

---

### 4. Check notifications

Inject `INotificationContext` wherever you handle domain results:

```csharp
public class OrderService
{
    private readonly INotificationContext _notifications;

    public OrderService(INotificationContext notifications)
    {
        _notifications = notifications;
    }

    public void ProcessOrder(Order order)
    {
        order.Confirm();

        if (_notifications.HasErrors)
        {
            foreach (var error in _notifications.ValidationErrors)
                Console.WriteLine($"[{error.TransitionName}] {error.Message}");
        }
    }
}
```

---

## Core Concepts

### `Aggregate<T>`

The base class for all domain aggregates. Provides:

- `GetTransitionBuilder()` — creates a `ITransitionBuilder<T>` for no-arg transitions
- `GetTransitionBuilder<TArgs>()` — creates a `ITransitionBuilder<T, TArgs>` for transitions with arguments
- `AddViolationMessage(string message)` — manually adds an error notification (uses caller name automatically)

---

### Transition Pipeline

Transitions are built as immutable pipelines using a fluent API:

| Method | Description |
|---|---|
| `.Requires(guard, message)` | Adds a single guard. Stops execution and logs an error if the guard returns `false`. |
| `.RequiresAll(params guards)` | Adds multiple guards evaluated together. All failures are reported before stopping. |
| `.Do(action)` | Queues a state mutation. Only runs after all preceding guards pass. |
| `.Create()` | Builds and returns the immutable `ITransition<T>` or `ITransition<T, TArgs>`. |

> Actions defined before a guard are deferred and only executed once the guard passes. This ensures the aggregate is never partially mutated.

---

### `INotificationContext`

The notification context collects domain errors and warnings produced during a request scope.

| Member | Description |
|---|---|
| `All` | All notifications collected so far |
| `ValidationErrors` | Only `NotificationKind.Error` entries |
| `Warnings` | Only `NotificationKind.Warning` entries |
| `HasErrors` | `true` if any errors exist |
| `IsEmpty` | `true` if no notifications have been added |
| `Add(Notification)` | Manually add a notification |
| `Add(transitionName, message, kind)` | Convenience overload |

---

### `NotificationKind`

```csharp
public enum NotificationKind
{
    Warning,
    Error
}
```

---

## Architecture Overview

```
Aggregate<T>
│
├── GetTransitionBuilder()         ──► ITransitionBuilder<T>
│                                        ├── .Requires(guard, msg)
│                                        ├── .RequiresAll(guards)
│                                        ├── .Do(action)
│                                        └── .Create() ──► ITransition<T>
│                                                              └── .Invoke(target)
│
└── DomainNotifications (AsyncLocal)
         └── NotificationContext  ──► INotificationContext
                  ├── ValidationErrors
                  ├── Warnings
                  └── HasErrors
```

---

## Project Structure

```
Bllueprint.Core.Domain/
├── Aggregate.cs                          # Base class for domain aggregates
├── INotificationContext.cs               # Public interface for notification access
├── NotificationContext.cs                # Internal scoped implementation
├── DomainNotifications.cs                # AsyncLocal state holder
├── NotificationScope.cs                  # Disposable scope for isolated notification contexts
├── Notification.cs                       # Notification record struct
├── NotificationKind.cs                   # Error / Warning enum
├── ITransition{T}.cs                     # Transition contract (no args)
├── ITransition{T,TArgs}.cs               # Transition contract (with args)
├── ITransitionBuilder{T}.cs              # Fluent builder interface (no args)
├── ITransitionBuilder{T,TArgs}.cs        # Fluent builder interface (with args)
├── ITransitionStep{T}.cs                 # Internal step marker interface
├── Transition{T}.cs                      # Transition execution engine (no args)
├── Transition{T,TArgs}.cs                # Transition execution engine (with args)
├── TransitionBuilder{T}.cs               # Builder implementation (no args)
├── TransitionBuilder{T,TArgs}.cs         # Builder implementation (with args)
├── ActionStep{T}.cs                      # Internal action step
├── GuardStep{T}.cs                       # Internal guard step
├── BllueprintServiceCollectionExtensions  # DI registration
└── Bllueprint.Core.Domain.csproj
```

---

## License

MIT — see [LICENSE](LICENSE) for details.

---

## Author

**Nubaum** — [github.com/bllueprint](https://github.com/bllueprint)
# Bllueprint.Core.Api

A lightweight ASP.NET Core base layer that bridges MediatR commands with HTTP responses. It removes the boilerplate of translating `ICommandResult<T>` outcomes into `IActionResult` in every controller action.

## Installation

```bash
dotnet add package Bllueprint.Core.Api
```

## What it does

The package provides two things:

**`AppController`** — an abstract base controller that exposes a lazy `Mediator` property and a `SendAsync` helper. Inherit from it instead of `ControllerBase` and dispatch commands in one line.

**`ToActionResultAsync`** — an internal extension that maps the three possible `ICommandResult<T>` outcomes to the appropriate HTTP response automatically:

| `ICommandResult<T>` state | HTTP response |
|---|---|
| `NotFound == true` | `404 Not Found` |
| `HasErrors == false` | `200 OK` with `Entity` as the body |
| `HasErrors == true` | `400 Bad Request` with a structured error body |

The `400` error body shape:

```json
{
  "errors": [
    {
      "transition": "Submit",
      "message": "Name is required",
      "kind": "Validation"
    }
  ]
}
```

Each entry maps directly from a `Notification` in `ICommandResult<T>.Errors`.

## Usage

Inherit `AppController` and call `SendAsync` with any `IRequest<ICommandResult<T>>`:

```csharp
[Route("api/orders")]
public class OrdersController : AppController
{
    [HttpPost]
    public Task<IActionResult> Create(CreateOrderCommand command)
        => SendAsync(command);

    [HttpPut("{id}/submit")]
    public Task<IActionResult> Submit(SubmitOrderCommand command)
        => SendAsync(command);
}
```

That's all. No manual `if (result.NotFound)` checks, no error mapping — `SendAsync` handles it end to end.

## Requirements

- .NET 9 or later (uses the `field` keyword for the backing field of `Mediator`)
- `MediatR` registered in your DI container
- Your command handlers must return `ICommandResult<T>` from `Bllueprint.Core.Application`

## How `Mediator` is resolved

`AppController` resolves `IMediator` lazily from `HttpContext.RequestServices` on first access. No constructor injection is needed in derived controllers, which keeps them free of DI ceremony.

```csharp
protected IMediator Mediator
{
    get => field ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
}
```

## Related packages

| Package | Role |
|---|---|
| `Bllueprint.Core.Application` | Defines `ICommandResult<T>`, `Notification`, and `NotificationKind` |
| `MediatR` | Command dispatching |
using System.Reflection;
using Bllueprint.Core.Application;
using Bllueprint.Core.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Bllueprint.Core.Api.Tests;

public class CommandResultExtensionsTests(ControllerFixture fixture) : IClassFixture<ControllerFixture>
{
    private readonly ControllerBase _controller = fixture.Controller;

    [Fact]
    public async Task ToActionResultAsync_WhenSuccessful_ReturnsOkWithEntity()
    {
        var entity = new SampleEntity(1, "Stand");
        Task<ICommandResult<SampleEntity>> resultTask = Task.FromResult(CommandResultBuilder.Success(entity));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(entity);
    }

    [Fact]
    public async Task ToActionResultAsync_WhenSuccessful_Returns200StatusCode()
    {
        Task<ICommandResult<string>> resultTask = Task.FromResult(CommandResultBuilder.Success("payload"));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToActionResultAsync_WhenEntityIsNull_ReturnsOkWithNullValue()
    {
        Task<ICommandResult<string?>> resultTask = Task.FromResult(CommandResultBuilder.Success<string?>(null));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeNull();
    }

    [Fact]
    public async Task ToActionResultAsync_WhenNotFound_ReturnsNotFoundResult()
    {
        Task<ICommandResult<string>> resultTask = Task.FromResult(CommandResultBuilder.NotFound<string>());

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ToActionResultAsync_WhenNotFound_Returns404StatusCode()
    {
        Task<ICommandResult<int>> resultTask = Task.FromResult(CommandResultBuilder.NotFound<int>());

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<NotFoundResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ToActionResultAsync_WhenHasErrors_ReturnsBadRequest()
    {
        Task<ICommandResult<string>> resultTask = Task.FromResult(CommandResultBuilder.WithErrors<string>(
            new Notification
            {
                TransitionName = "Approve",
                Message = "Invalid state",
                Kind = NotificationKind.Error
            }));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ToActionResultAsync_WhenHasErrors_Returns400StatusCode()
    {
        Task<ICommandResult<string>> resultTask = Task.FromResult(CommandResultBuilder.WithErrors<string>(
            new Notification
            {
                TransitionName = "Approve",
                Message = "Invalid state",
                Kind = NotificationKind.Error
            }));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ToActionResultAsync_WhenHasErrors_ErrorsShapeContainsExpectedFields()
    {
        Task<ICommandResult<string>> resultTask = Task.FromResult(CommandResultBuilder.WithErrors<string>(
            new Notification
            {
                TransitionName = "Submit",
                Message = "Name is required",
                Kind = NotificationKind.Error
            },
            new Notification
            {
                TransitionName = "Approve",
                Message = "Insufficient permissions",
                Kind = NotificationKind.Error
            }));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        object? body = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject.Value;
        body.Should().NotBeNull();

        PropertyInfo? errorsProperty = body!.GetType().GetProperty("errors");
        errorsProperty.Should().NotBeNull("response body should have an 'errors' property");

        var errors = (errorsProperty!.GetValue(body) as IEnumerable<object>)!.ToList();
        errors.Should().HaveCount(2);

        AssertErrorShape(errors[0], "Submit", "Name is required", nameof(NotificationKind.Error));
        AssertErrorShape(errors[1], "Approve", "Insufficient permissions", nameof(NotificationKind.Error));
    }

    [Fact]
    public async Task ToActionResultAsync_WhenHasMultipleErrors_AllErrorsAreIncluded()
    {
        Task<ICommandResult<int>> resultTask = Task.FromResult(CommandResultBuilder.WithErrors<int>(
            new Notification { TransitionName = "A", Message = "Error 1", Kind = NotificationKind.Error },
            new Notification { TransitionName = "B", Message = "Error 2", Kind = NotificationKind.Error },
            new Notification { TransitionName = "C", Message = "Error 3", Kind = NotificationKind.Error }));

        IActionResult actionResult = await resultTask.ToActionResultAsync(_controller);

        object body = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject.Value!;
        var errors = (body.GetType().GetProperty("errors")!.GetValue(body) as IEnumerable<object>)!.ToList();

        errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task ToActionResultAsync_WhenNotFoundAndHasErrors_ReturnsNotFound()
    {
        ICommandResult<string> result = Substitute.For<ICommandResult<string>>();
        result.NotFound.Returns(true);
        result.HasErrors.Returns(true);
        result.Errors.Returns(new List<Notification>());

        IActionResult actionResult = await Task.FromResult(result).ToActionResultAsync(_controller);

        actionResult.Should().BeOfType<NotFoundResult>();
    }

    private static void AssertErrorShape(object error, string expectedTransition, string expectedMessage, string expectedKind)
    {
        Type type = error.GetType();

        type.GetProperty("transition")!.GetValue(error).Should().Be(expectedTransition);
        type.GetProperty("message")!.GetValue(error).Should().Be(expectedMessage);
        type.GetProperty("kind")!.GetValue(error).Should().Be(expectedKind);
    }
}

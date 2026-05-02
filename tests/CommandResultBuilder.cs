using Bllueprint.Core.Application;
using Bllueprint.Core.Domain;
using NSubstitute;

namespace Bllueprint.Core.Api.Tests;

public static class CommandResultBuilder
{
    public static ICommandResult<T> Success<T>(T entity)
    {
        ICommandResult<T> result = Substitute.For<ICommandResult<T>>();
        result.NotFound.Returns(false);
        result.HasErrors.Returns(false);
        result.Entity.Returns(entity);
        result.Errors.Returns(new List<Notification>());
        return result;
    }

    public static ICommandResult<T> NotFound<T>()
    {
        ICommandResult<T> result = Substitute.For<ICommandResult<T>>();
        result.NotFound.Returns(true);
        result.HasErrors.Returns(false);
        result.Errors.Returns(new List<Notification>());
        return result;
    }

    public static ICommandResult<T> WithErrors<T>(params Notification[] errors)
    {
        ICommandResult<T> result = Substitute.For<ICommandResult<T>>();
        result.NotFound.Returns(false);
        result.HasErrors.Returns(true);
        result.Errors.Returns(errors.ToList());
        return result;
    }
}

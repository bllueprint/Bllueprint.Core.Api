using Bllueprint.Core.Domain;
using Bllueprint.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bllueprint.Core.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseBllueprint<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services = services.AddBllueprintDomainServices();
        services = services.AddBllueprintInfrastructure<TContext>();
        return services;
    }
}

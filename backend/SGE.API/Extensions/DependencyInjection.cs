using Microsoft.EntityFrameworkCore;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Persistence.Contexts;
using SGE.Persistence.Repositories.Catalog;
using SGE.API.Services;

namespace SGE.API.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SgeDbContext>(options =>
            options.UseNpgsql(PostgresConnectionString.Resolve(configuration)));

        // Repositories
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        return services;
    }
}

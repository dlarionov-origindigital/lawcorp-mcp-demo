using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawCorp.Mcp.Data;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="LawCorpDbContext"/> with SQL Server using the provided connection string.
    /// </summary>
    public static IServiceCollection AddLawCorpDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<LawCorpDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}

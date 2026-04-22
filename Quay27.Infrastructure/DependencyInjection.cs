using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql;
using Quay27.Application.Abstractions;
using Quay27.Application.Repositories;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Repositories;
using Quay27.Infrastructure.Services;

namespace Quay27.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                serverVersion,
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    );
                }
            )
        );

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IColumnPermissionRepository, ColumnPermissionRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerGroupRepository, CustomerGroupRepository>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductGroupRepository, ProductGroupRepository>();
        services.AddScoped<IPriceListRepository, PriceListRepository>();
        services.AddScoped<IPriceListItemRepository, PriceListItemRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICustomerQueueRepository, CustomerQueueRepository>();
        services.AddScoped<IDuplicateFlagRepository, DuplicateFlagRepository>();
        services.AddScoped<ICustomerVersionRepository, CustomerVersionRepository>();
        services.AddScoped<ISheetPickerDraftStaffNameRepository, SheetPickerDraftStaffNameRepository>();
        services.AddScoped<IEndOfDayService, EndOfDayService>();

        return services;
    }
}

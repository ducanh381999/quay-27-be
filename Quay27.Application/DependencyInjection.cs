using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Quay27.Application.Abstractions;
using Quay27.Application.Services;
using Quay27.Domain.Entities;

namespace Quay27.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ICustomerColumnPermissionService, CustomerColumnPermissionService>();
        return services;
    }
}

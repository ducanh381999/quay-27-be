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
        services.AddScoped<ICustomerGroupService, CustomerGroupService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<ICustomerDeliveryAddressService, CustomerDeliveryAddressService>();
        services.AddScoped<ICustomerReceivableService, CustomerReceivableService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductUploadService, ProductUploadService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ICustomerColumnPermissionService, CustomerColumnPermissionService>();
        services.AddScoped<ISupplierPayableService, SupplierPayableService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISupplierGroupService, SupplierGroupService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IImportOrderService, ImportOrderService>();
        services.AddScoped<IReturnReceiptService, ReturnReceiptService>();
        services.AddScoped<IReceivingAccountService, ReceivingAccountService>();
        services.AddScoped<ITreasurySettingsService, TreasurySettingsService>();
        services.AddScoped<ISaleChannelService, SaleChannelService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<ISalesReturnService, SalesReturnService>();
        services.AddScoped<IPaymentCategoryService, PaymentCategoryService>();
        services.AddScoped<ICashbookService, CashbookService>();
        services.AddScoped<ICashbookSyncService, CashbookSyncService>();
        return services;
    }
}

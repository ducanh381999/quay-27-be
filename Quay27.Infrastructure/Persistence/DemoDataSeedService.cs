using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Setup;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;

namespace Quay27.Infrastructure.Persistence;

public class DemoDataSeedService : IDemoDataSeedService
{
    public const string DemoCreatedByMarker = "demo-seed-api";

    private static readonly Guid Staff1Id = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid Staff2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Staff3Id = Guid.Parse("22222222-2222-2222-2222-222222222223");
    private static readonly Guid Staff4Id = Guid.Parse("22222222-2222-2222-2222-222222222224");

    /// <summary>Data entry: edit sheet fields, not queue approval flags or sheet day/status.</summary>
    private static readonly HashSet<string> Staff1EditableColumns = new(StringComparer.Ordinal)
    {
        SchemaConstants.CustomerColumns.SortOrder,
        SchemaConstants.CustomerColumns.InvoiceCode,
        SchemaConstants.CustomerColumns.BillCreatedAt,
        SchemaConstants.CustomerColumns.NameAddress,
        SchemaConstants.CustomerColumns.CreateMachine,
        SchemaConstants.CustomerColumns.DraftStaff,
        SchemaConstants.CustomerColumns.Quantity,
        SchemaConstants.CustomerColumns.InstallStaffCm,
        SchemaConstants.CustomerColumns.Notes,
        SchemaConstants.CustomerColumns.GoodsSenderNote,
        SchemaConstants.CustomerColumns.AdditionalNotes
    };

    /// <summary>Notes-focused: only text/status touch-up.</summary>
    private static readonly HashSet<string> Staff2EditableColumns = new(StringComparer.Ordinal)
    {
        SchemaConstants.CustomerColumns.Notes,
        SchemaConstants.CustomerColumns.AdditionalNotes,
        SchemaConstants.CustomerColumns.Status,
        SchemaConstants.CustomerColumns.DraftStaff
    };

    /// <summary>Counter: approval checkboxes only.</summary>
    private static readonly HashSet<string> Staff3EditableColumns = new(StringComparer.Ordinal)
    {
        SchemaConstants.CustomerColumns.ManagerApproved,
        SchemaConstants.CustomerColumns.Kio27Received,
        SchemaConstants.CustomerColumns.Export27
    };

    /// <summary>Scheduling: row order and sheet date only (contrast with staff1).</summary>
    private static readonly HashSet<string> Staff4EditableColumns = new(StringComparer.Ordinal)
    {
        SchemaConstants.CustomerColumns.SortOrder,
        SchemaConstants.CustomerColumns.SheetDate
    };

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeedService> _logger;

    public DemoDataSeedService(
        ApplicationDbContext db,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger<DemoDataSeedService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DemoSeedResponse> SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        var staffPassword = _configuration["DemoSeed:StaffPassword"] ?? "Demo@123";
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "ChangeMe!123";

        var usersInfo = new List<SeededUserInfo>
        {
            new("admin", adminPassword, SchemaConstants.Roles.Admin, "Ba Hiền"),
            new("staff1", staffPassword, SchemaConstants.Roles.Staff, "Nguyễn Thị Lan"),
            new("staff2", staffPassword, SchemaConstants.Roles.Staff, "Trần Văn Minh"),
            new("staff3", staffPassword, SchemaConstants.Roles.Staff, "Lê Hoàng Nam"),
            new("staff4", staffPassword, SchemaConstants.Roles.Staff, "Phạm Thu Hà")
        };

        if (await _db.Customers.AnyAsync(c => c.CreatedBy == DemoCreatedByMarker, cancellationToken))
        {
            return new DemoSeedResponse(
                true,
                "Demo data was already applied. Credentials below are unchanged (use configured passwords).",
                usersInfo,
                0);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var staffIds = new[] { Staff1Id, Staff2Id, Staff3Id, Staff4Id };
            var staffDefs = new[]
            {
                ("staff1", "Nguyễn Thị Lan"),
                ("staff2", "Trần Văn Minh"),
                ("staff3", "Lê Hoàng Nam"),
                ("staff4", "Phạm Thu Hà")
            };

            for (var i = 0; i < staffDefs.Length; i++)
            {
                var (username, fullName) = staffDefs[i];
                var id = staffIds[i];
                if (await _db.Users.AnyAsync(u => u.Id == id || u.Username == username, cancellationToken))
                    continue;

                var u = new User
                {
                    Id = id,
                    Username = username,
                    FullName = fullName,
                    IsActive = true,
                    CreatedDate = now,
                    PasswordHash = string.Empty
                };
                u.PasswordHash = _passwordHasher.HashPassword(u, staffPassword);
                _db.Users.Add(u);
                _db.UserRoles.Add(new UserRole { UserId = id, RoleId = 2 });
            }

            await _db.SaveChangesAsync(cancellationToken);

            var customerColumnNames = GetCustomerColumnNames();
            foreach (var staffId in staffIds)
            {
                if (!await _db.Users.AnyAsync(u => u.Id == staffId, cancellationToken))
                    continue;

                foreach (var col in customerColumnNames)
                {
                    if (await _db.ColumnPermissions.AnyAsync(
                            p => p.UserId == staffId && p.TableName == SchemaConstants.CustomersTable && p.ColumnName == col,
                            cancellationToken))
                        continue;

                    _db.ColumnPermissions.Add(new ColumnPermission
                    {
                        Id = Guid.NewGuid(),
                        UserId = staffId,
                        TableName = SchemaConstants.CustomersTable,
                        ColumnName = col,
                        CanView = true,
                        CanEdit = IsColumnEditableForDemoStaff(staffId, col)
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            var today = DateOnly.FromDateTime(now);
            var yesterday = today.AddDays(-1);

            var customers = BuildDemoCustomers(today, yesterday, now);
            var dupName = "Nguyễn Văn Trùng - 12 Lê Lợi, Q1";
            var dupGroupId = Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(dupName.Trim()));
            foreach (var c in customers.Where(x => x.NameAddress == dupName))
                c.IsDuplicate = true;

            foreach (var c in customers)
                _db.Customers.Add(c);

            foreach (var c in customers.Where(x => x.NameAddress == dupName))
            {
                _db.DuplicateFlags.Add(new DuplicateFlag
                {
                    Id = Guid.NewGuid(),
                    CustomerId = c.Id,
                    DuplicateGroupId = dupGroupId
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            var inQueue = customers.Take(4).Select(c => c.Id).ToList();
            foreach (var customerId in inQueue)
            {
                _db.CustomerQueues.Add(new CustomerQueue
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    QueueId = SchemaConstants.Quay27QueueId,
                    CreatedDate = now,
                    CreatedBy = DemoCreatedByMarker
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("Demo sheet data seeded: {Count} customers.", customers.Count);

            return new DemoSeedResponse(
                false,
                "Demo users, per-staff column permissions, customers, queue rows, and duplicate sample created.",
                usersInfo,
                customers.Count);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsColumnEditableForDemoStaff(Guid staffId, string columnName)
    {
        if (staffId == Staff1Id) return Staff1EditableColumns.Contains(columnName);
        if (staffId == Staff2Id) return Staff2EditableColumns.Contains(columnName);
        if (staffId == Staff3Id) return Staff3EditableColumns.Contains(columnName);
        if (staffId == Staff4Id) return Staff4EditableColumns.Contains(columnName);
        return false;
    }

    private static IReadOnlyList<string> GetCustomerColumnNames() => SchemaConstants.GetAllCustomerColumnNames();

    private static List<Customer> BuildDemoCustomers(DateOnly today, DateOnly yesterday, DateTime nowUtc)
    {
        int order = 1;
        Customer Row(
            string invoice,
            DateTime billAt,
            string nameAddr,
            string machine,
            string draft,
            int qty,
            string install,
            bool ql,
            bool kio,
            bool xuat,
            string notes,
            string sender,
            string extra,
            DateOnly sheet,
            string status)
        {
            return new Customer
            {
                Id = Guid.NewGuid(),
                SortOrder = order++,
                InvoiceCode = invoice,
                BillCreatedAt = billAt,
                NameAddress = nameAddr,
                CreateMachine = machine,
                DraftStaff = draft,
                Quantity = qty,
                InstallStaffCm = install,
                ManagerApproved = ql,
                Kio27Received = kio,
                Export27 = xuat,
                Notes = notes,
                GoodsSenderNote = sender,
                AdditionalNotes = extra,
                SheetDate = sheet,
                Status = status,
                IsDuplicate = false,
                CreatedDate = nowUtc,
                CreatedBy = DemoCreatedByMarker,
                IsDeleted = false
            };
        }

        var dup = "Nguyễn Văn Trùng - 12 Lê Lợi, Q1";

        return new List<Customer>
        {
            Row("HD-DEMO-001", nowUtc.AddHours(-3), "Chị Lan - 45 Nguyễn Huệ, Q1", "maythanh", "Hà", 2, "Tuấn", true, false, false, "Giao sáng", "Anh Bình", "", today, "Mới"),
            Row("HD-DEMO-002", nowUtc.AddHours(-2), "Anh Hùng - 88 Võ Văn Tần, Q3", "maythanh", "Lan", 1, "Tuấn", false, true, false, "", "Shop X", "Khách lấy tại quầy", today, "Đang xử lý"),
            Row("HD-DEMO-003", nowUtc.AddHours(-1), dup, "may2", "Minh", 5, "Hùng", false, false, false, "SL cập nhật từ sheet", "", "", today, "Mới"),
            Row("HD-DEMO-004", yesterday.ToDateTime(TimeOnly.MinValue), dup, "maythanh", "Nam", 3, "Tuấn", true, true, true, "Đủ hàng", "Kho A", "", yesterday, "Hoàn tất"),
            Row("HD-DEMO-005", nowUtc.AddDays(-1).AddHours(-5), "Cô Mai - 9 Điện Biên Phủ, Q1", "may2", "Hà", 1, "", false, false, false, "Ghi chú ngắn", "Nội bộ", "Ưu tiên", yesterday, "Mới"),
            Row("HD-DEMO-006", nowUtc.AddHours(-4), "Anh Tuấn - CC Sunrise, Q7", "maythanh", "Lan", 4, "Hùng", false, false, false, "", "", "Gọi trước khi giao", today, "Mới"),
            Row("HD-DEMO-007", nowUtc.AddHours(-5), "Chị Hương - 30 Pasteur, Q1", "may2", "Minh", 2, "Tuấn", true, false, false, "Đổi địa chỉ", "Grab", "", today, "Đang xử lý"),
            Row("HD-DEMO-008", nowUtc.AddDays(-2), "Khách lẻ - 100 Hai Bà Trưng, Q1", "maythanh", "Nam", 1, "", false, false, true, "Xuất kho", "", "Đã xuất", yesterday.AddDays(-1), "Hoàn tất")
        };
    }
}

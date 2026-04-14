using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Common;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Customers;
using Quay27.Application.Repositories;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class CustomerService : ICustomerService
{
    private static readonly string[] AllCustomerColumns =
    {
        SchemaConstants.CustomerColumns.SortOrder,
        SchemaConstants.CustomerColumns.InvoiceCode,
        SchemaConstants.CustomerColumns.BillCreatedAt,
        SchemaConstants.CustomerColumns.NameAddress,
        SchemaConstants.CustomerColumns.CreateMachine,
        SchemaConstants.CustomerColumns.DraftStaff,
        SchemaConstants.CustomerColumns.Quantity,
        SchemaConstants.CustomerColumns.TotalAmount,
        SchemaConstants.CustomerColumns.InstallStaffCm,
        SchemaConstants.CustomerColumns.ManagerApproved,
        SchemaConstants.CustomerColumns.Kio27Received,
        SchemaConstants.CustomerColumns.Export27,
        SchemaConstants.CustomerColumns.FullSelfExport,
        SchemaConstants.CustomerColumns.Notes,
        SchemaConstants.CustomerColumns.GoodsSenderNote,
        SchemaConstants.CustomerColumns.AdditionalNotes,
        SchemaConstants.CustomerColumns.SheetDate,
        SchemaConstants.CustomerColumns.Status
    };

    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly ICustomerQueueRepository _customerQueues;
    private readonly IQueueRepository _queues;
    private readonly IColumnPermissionRepository _columnPermissions;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IDuplicateFlagRepository _duplicateFlags;
    private readonly ICustomerVersionRepository _customerVersions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICustomerSheetRealtimeNotifier _realtime;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customers,
        IUserRepository users,
        ICustomerQueueRepository customerQueues,
        IQueueRepository queues,
        IColumnPermissionRepository columnPermissions,
        IAuditLogRepository auditLogs,
        IDuplicateFlagRepository duplicateFlags,
        ICustomerVersionRepository customerVersions,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICustomerSheetRealtimeNotifier realtime,
        ILogger<CustomerService> logger)
    {
        _customers = customers;
        _users = users;
        _customerQueues = customerQueues;
        _queues = queues;
        _columnPermissions = columnPermissions;
        _auditLogs = auditLogs;
        _duplicateFlags = duplicateFlags;
        _customerVersions = customerVersions;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CustomerDto>> ListBySheetDateAsync(DateOnly? sheetDate, int? queueId, bool pendingExport27 = false,
        string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (queueId is null && sheetDate is { } sd)
        {
            var todayVn = VietnamDate.TodayInVietnam();
            if (sd == todayVn)
                return await _customers.ListTodayFullSheetWithCarryoverAsync(sd, pendingExport27, searchTerm, cancellationToken);
        }

        return await _customers.ListBySheetDateAsync(sheetDate, queueId, pendingExport27, searchTerm, cancellationToken);
    }

    public async Task<ImportCustomersExcelResult> ImportExcelAsync(
        ImportCustomersExcelRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (request.FileBytes.Length == 0)
            throw new InvalidOperationException("File import rỗng.");

        var ext = Path.GetExtension(request.FileName ?? string.Empty);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx.");

        var rows = SimpleXlsxReader.ReadMappedRows(request.FileBytes);
        var imported = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();
        var sortOrder = 1;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.InvoiceCode))
            {
                skipped++;
                continue;
            }

            var billCreatedAt = ParseImportedDateTime(row.TimeRaw);
            var createReq = new CreateCustomerRequest(
                sortOrder++,
                row.InvoiceCode.Trim(),
                billCreatedAt,
                row.CustomerRaw.Trim(),
                row.CreatorRaw.Trim(),
                string.Empty, // DraftStaff
                row.QuantityRaw, // Quantity
                row.TotalAmountRaw, // TotalAmount
                string.Empty, // InstallStaffCm
                false,        // ManagerApproved
                false,        // Kio27Received
                false,        // Export27
                string.Empty, // Notes
                string.Empty, // GoodsSenderNote
                string.Empty, // AdditionalNotes
                request.SheetDate,
                "Mới",
                false         // FullSelfExport
            );

            try
            {
                // Khi import, chỉ tạo 1 audit log cho mỗi bản ghi
                await CreateInternalAsync(createReq, fromImport: true, cancellationToken);
                imported++;
            }
            catch (Exception ex)
            {
                failed++;
                if (errors.Count < 30)
                    errors.Add($"Row {row.RowNumber}: {ex.Message}");
            }
        }

        return new ImportCustomersExcelResult(rows.Count, imported, skipped, failed, errors);
    }

    public Task<byte[]> ExportGridAsync(ExportGridRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(string.IsNullOrWhiteSpace(request.SheetName) ? "Data" : request.SheetName);

        for (var i = 0; i < request.Columns.Count; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = request.Columns[i].HeaderName;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var r = 0; r < request.Rows.Count; r++)
        {
            var rowData = request.Rows[r];
            for (var c = 0; c < request.Columns.Count; c++)
            {
                var field = request.Columns[c].Field;
                if (!rowData.TryGetValue(field, out var element))
                    continue;

                var cell = ws.Cell(r + 2, c + 1);
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        cell.Value = element.GetString();
                        break;
                    case JsonValueKind.Number:
                        cell.Value = element.GetDouble();
                        break;
                    case JsonValueKind.True:
                        cell.Value = true;
                        break;
                    case JsonValueKind.False:
                        cell.Value = false;
                        break;
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        cell.Value = string.Empty;
                        break;
                    default:
                        cell.Value = element.GetRawText();
                        break;
                }
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return await _customers.GetProjectedByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerAuditLogEntryDto>> GetAuditLogsForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (await _customers.GetProjectedByIdAsync(customerId, cancellationToken) is null)
            throw new NotFoundException("Customer not found.");

        return await _auditLogs.ListByRecordIdAsync(customerId, cancellationToken);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
        => await CreateInternalAsync(request, fromImport: false, cancellationToken);

    private async Task<CustomerDto> CreateInternalAsync(
        CreateCustomerRequest request,
        bool fromImport,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var userId = _currentUser.UserId!.Value;
        var username = _currentUser.Username;

        await EnsureCanEditColumnsAsync(userId, AllCustomerColumns, cancellationToken);

        var dto = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var entity = new Customer
            {
                Id = id,
                SortOrder = request.SortOrder,
                InvoiceCode = request.InvoiceCode.Trim(),
                BillCreatedAt = request.BillCreatedAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.BillCreatedAt, DateTimeKind.Utc)
                    : request.BillCreatedAt.ToUniversalTime(),
                NameAddress = request.NameAddress.Trim(),
                CreateMachine = request.CreateMachine.Trim(),
                DraftStaff = request.DraftStaff.Trim(),
                Quantity = request.Quantity,
                TotalAmount = request.TotalAmount,
                InstallStaffCm = request.InstallStaffCm.Trim(),
                ManagerApproved = request.ManagerApproved,
                Kio27Received = request.Kio27Received,
                Export27 = request.Export27,
                FullSelfExport = request.FullSelfExport,
                Notes = request.Notes.Trim(),
                GoodsSenderNote = request.GoodsSenderNote.Trim(),
                AdditionalNotes = request.AdditionalNotes.Trim(),
                SheetDate = request.SheetDate,
                Status = request.Status.Trim(),
                IsDuplicate = false,
                CreatedDate = now,
                CreatedBy = username,
                IsDeleted = false
            };

            await _customers.AddAsync(entity, cancellationToken);

            if (fromImport)
            {
                await _auditLogs.AddRangeAsync(new[]
                {
                    new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        TableName = SchemaConstants.CustomersTable,
                        RecordId = id,
                        ColumnName = "Import",
                        OldValue = null,
                        NewValue = "Imported",
                        ActionType = "Import",
                        ChangedBy = username,
                        ChangedDate = now
                    }
                }, cancellationToken);
            }
            else
            {
                var audits = BuildInsertAudits(id, entity, username, now);
                await _auditLogs.AddRangeAsync(audits, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await RecomputeDuplicatesForNameAddressAsync(entity.NameAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (await _customers.GetProjectedByIdAsync(id, cancellationToken))!;
        }, cancellationToken);

        _logger.LogInformation("Customer {CustomerId} created by {User}", dto.Id, username);

        await _realtime.NotifyAsync(
            new CustomerSheetChangeNotification(dto.SheetDate, dto.Id, "created"), cancellationToken);

        return dto;
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var userId = _currentUser.UserId!.Value;
        var username = _currentUser.Username;

        var (resultDto, auditCount, sheetDateBeforeSnap, sheetDateAfter) =
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
            var entity = await _customers.GetTrackedAsync(id, cancellationToken);
            if (entity is null)
                throw new NotFoundException("Customer not found.");

            var sheetDateBefore = entity.SheetDate;
            var oldNameAddress = entity.NameAddress;
            var snapshot = JsonSerializer.Serialize(CustomerSnapshot.FromEntity(entity));
            await _customerVersions.AddAsync(new CustomerVersion
            {
                Id = Guid.NewGuid(),
                CustomerId = entity.Id,
                SnapshotData = snapshot,
                CreatedBy = username,
                CreatedDate = DateTime.UtcNow
            }, cancellationToken);

            var audits = new List<AuditLog>();
            var now = DateTime.UtcNow;

            if (request.SortOrder is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.SortOrder, cancellationToken);
                var v = request.SortOrder.Value;
                if (v != entity.SortOrder)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.SortOrder, entity.SortOrder.ToString(), v.ToString(), "Update", username, now));
                    entity.SortOrder = v;
                }
            }

            if (request.InvoiceCode is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.InvoiceCode, cancellationToken);
                var v = request.InvoiceCode.Trim();
                if (v != entity.InvoiceCode)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.InvoiceCode, entity.InvoiceCode, v, "Update", username, now));
                    entity.InvoiceCode = v;
                }
            }

            if (request.BillCreatedAt is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.BillCreatedAt, cancellationToken);
                var v = request.BillCreatedAt.Value;
                var utc = v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime();
                if (utc != entity.BillCreatedAt)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.BillCreatedAt, entity.BillCreatedAt.ToString("O"), utc.ToString("O"), "Update", username, now));
                    entity.BillCreatedAt = utc;
                }
            }

            if (request.NameAddress is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.NameAddress, cancellationToken);
                var v = request.NameAddress.Trim();
                if (v != entity.NameAddress)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.NameAddress, entity.NameAddress, v, "Update", username, now));
                    entity.NameAddress = v;
                }
            }

            if (request.CreateMachine is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.CreateMachine, cancellationToken);
                var v = request.CreateMachine.Trim();
                if (v != entity.CreateMachine)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.CreateMachine, entity.CreateMachine, v, "Update", username, now));
                    entity.CreateMachine = v;
                }
            }

            if (request.DraftStaff is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.DraftStaff, cancellationToken);
                var v = request.DraftStaff.Trim();
                if (v != entity.DraftStaff)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.DraftStaff, entity.DraftStaff, v, "Update", username, now));
                    entity.DraftStaff = v;
                }
            }

            if (request.Quantity is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.Quantity, cancellationToken);
                var v = request.Quantity;
                if (v != entity.Quantity)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.Quantity, entity.Quantity, v, "Update", username, now));
                    entity.Quantity = v;
                }
            }
            
            if (request.TotalAmount is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.TotalAmount, cancellationToken);
                var v = request.TotalAmount;
                if (v != entity.TotalAmount)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.TotalAmount, entity.TotalAmount, v, "Update", username, now));
                    entity.TotalAmount = v;
                }
            }

            if (request.InstallStaffCm is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.InstallStaffCm, cancellationToken);
                var v = request.InstallStaffCm.Trim();
                if (v != entity.InstallStaffCm)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.InstallStaffCm, entity.InstallStaffCm, v, "Update", username, now));
                    entity.InstallStaffCm = v;
                }
            }

            if (request.ManagerApproved is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.ManagerApproved, cancellationToken);
                var v = request.ManagerApproved.Value;
                if (v != entity.ManagerApproved)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.ManagerApproved, entity.ManagerApproved.ToString(), v.ToString(), "Update", username, now));
                    entity.ManagerApproved = v;
                }
            }

            if (request.Kio27Received is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.Kio27Received, cancellationToken);
                var v = request.Kio27Received.Value;
                if (v != entity.Kio27Received)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.Kio27Received, entity.Kio27Received.ToString(), v.ToString(), "Update", username, now));
                    entity.Kio27Received = v;
                }
            }

            if (request.Export27 is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.Export27, cancellationToken);
                var v = request.Export27.Value;
                if (v != entity.Export27)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.Export27, entity.Export27.ToString(), v.ToString(), "Update", username, now));
                    entity.Export27 = v;
                }
            }

            if (request.FullSelfExport is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.FullSelfExport, cancellationToken);
                var v = request.FullSelfExport.Value;
                if (v != entity.FullSelfExport)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.FullSelfExport, entity.FullSelfExport.ToString(), v.ToString(), "Update", username, now));
                    entity.FullSelfExport = v;
                }
            }

            if (request.Notes is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.Notes, cancellationToken);
                var v = request.Notes.Trim();
                if (v != entity.Notes)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.Notes, entity.Notes, v, "Update", username, now));
                    entity.Notes = v;
                }
            }

            if (request.GoodsSenderNote is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.GoodsSenderNote, cancellationToken);
                var v = request.GoodsSenderNote.Trim();
                if (v != entity.GoodsSenderNote)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.GoodsSenderNote, entity.GoodsSenderNote, v, "Update", username, now));
                    entity.GoodsSenderNote = v;
                }
            }

            if (request.AdditionalNotes is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.AdditionalNotes, cancellationToken);
                var v = request.AdditionalNotes.Trim();
                if (v != entity.AdditionalNotes)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.AdditionalNotes, entity.AdditionalNotes, v, "Update", username, now));
                    entity.AdditionalNotes = v;
                }
            }

            if (request.SheetDate is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.SheetDate, cancellationToken);
                var v = request.SheetDate.Value;
                if (v != entity.SheetDate)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.SheetDate, entity.SheetDate.ToString("O"), v.ToString("O"), "Update", username, now));
                    entity.SheetDate = v;
                }
            }

            if (request.Status is not null)
            {
                await EnsureCanEditColumnAsync(userId, SchemaConstants.CustomerColumns.Status, cancellationToken);
                var v = request.Status.Trim();
                if (v != entity.Status)
                {
                    audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.Status, entity.Status, v, "Update", username, now));
                    entity.Status = v;
                }
            }

            await ApplyCancelledInvoiceNormalizationAsync(id, entity, username, now, audits, cancellationToken);

            if (audits.Count > 0)
            {
                entity.UpdatedBy = username;
                entity.UpdatedDate = now;
                await _auditLogs.AddRangeAsync(audits, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (request.NameAddress is not null && !string.Equals(oldNameAddress.Trim(), entity.NameAddress.Trim(), StringComparison.Ordinal))
                await RecomputeDuplicatesForNameAddressAsync(oldNameAddress, cancellationToken);

            await RecomputeDuplicatesForNameAddressAsync(entity.NameAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = (await _customers.GetProjectedByIdAsync(id, cancellationToken))!;
            return (dto, audits.Count, sheetDateBefore, entity.SheetDate);
        }, cancellationToken);

        _logger.LogInformation("Customer {CustomerId} updated by {User}", id, username);

        if (auditCount > 0)
        {
            await _realtime.NotifyAsync(
                new CustomerSheetChangeNotification(sheetDateAfter, id, "updated"), cancellationToken);
            if (sheetDateBeforeSnap != sheetDateAfter)
            {
                await _realtime.NotifyAsync(
                    new CustomerSheetChangeNotification(sheetDateBeforeSnap, id, "updated"), cancellationToken);
            }
        }

        return resultDto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await BulkDeleteAsync(new[] { id }, cancellationToken);
    }

    public async Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (!_currentUser.IsAdmin)
        {
            if (_currentUser.UserId is null)
                throw new ForbiddenException("You are not allowed to delete customers.");
            var actor = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
            if (actor is null || !actor.CanDeleteCustomers)
                throw new ForbiddenException("You are not allowed to delete customers.");
        }

        var username = _currentUser.Username;
        var affectedSheetDates = new HashSet<DateOnly>();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var nameAddressesToRecompute = new HashSet<string>();

            foreach (var id in ids)
            {
                var entity = await _customers.GetTrackedAsync(id, cancellationToken);
                if (entity is null)
                    continue;

                var inQuay27 = await _customerQueues.FindAsync(id, SchemaConstants.Quay27QueueId, cancellationToken);
                if (inQuay27 is not null)
                    throw new ConflictException($"Cannot delete customer '{entity.InvoiceCode}' because it is already on Quầy 27.");

                affectedSheetDates.Add(entity.SheetDate);
                nameAddressesToRecompute.Add(entity.NameAddress);

                var ok = await _customers.SoftDeleteAsync(id, username, cancellationToken);
                if (ok)
                {
                    await _auditLogs.AddRangeAsync(new[]
                    {
                        CreateAudit(SchemaConstants.CustomersTable, id, "IsDeleted", "false", "true", "SoftDelete", username, now)
                    }, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            foreach (var na in nameAddressesToRecompute)
            {
                await RecomputeDuplicatesForNameAddressAsync(na, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        foreach (var sd in affectedSheetDates)
        {
            await _realtime.NotifyAsync(new CustomerSheetChangeNotification(sd, Guid.Empty, "deleted"), cancellationToken);
        }
    }

    public async Task SetQueueAsync(Guid customerId, int queueId, SetCustomerQueueRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var username = _currentUser.Username;

        var customer = await _customers.GetProjectedByIdAsync(customerId, cancellationToken);
        if (customer is null)
            throw new NotFoundException("Customer not found.");

        if (!await _queues.ExistsAsync(queueId, cancellationToken))
            throw new NotFoundException("Queue not found.");

        var queueChanged = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var changed = false;
            var link = await _customerQueues.FindAsync(customerId, queueId, cancellationToken);
            var now = DateTime.UtcNow;

            if (request.Enrolled)
            {
                if (link is null)
                {
                    changed = true;
                    await _customerQueues.AddAsync(new CustomerQueue
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        QueueId = queueId,
                        CreatedDate = now,
                        CreatedBy = username
                    }, cancellationToken);

                    await _auditLogs.AddRangeAsync(new[]
                    {
                        new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            TableName = "CustomerQueues",
                            RecordId = customerId,
                            ColumnName = "QueueId",
                            OldValue = null,
                            NewValue = queueId.ToString(),
                            ActionType = "Enroll",
                            ChangedBy = username,
                            ChangedDate = now
                        }
                    }, cancellationToken);
                }
            }
            else
            {
                if (link is not null)
                {
                    changed = true;
                    _customerQueues.Remove(link);
                    await _auditLogs.AddRangeAsync(new[]
                    {
                        new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            TableName = "CustomerQueues",
                            RecordId = customerId,
                            ColumnName = "QueueId",
                            OldValue = queueId.ToString(),
                            NewValue = null,
                            ActionType = "Remove",
                            ChangedBy = username,
                            ChangedDate = now
                        }
                    }, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return changed;
        }, cancellationToken);

        if (queueChanged)
        {
            await _realtime.NotifyAsync(
                new CustomerSheetChangeNotification(customer.SheetDate, customerId, "queue"), cancellationToken);
        }
    }

    private static bool IsCancelledInvoiceNotes(string? notes) =>
        string.Equals(notes?.Trim(), SchemaConstants.CancelledInvoiceNotes, StringComparison.Ordinal);

    private async Task ApplyCancelledInvoiceNormalizationAsync(Guid id, Customer entity, string username,
        DateTime now, List<AuditLog> audits, CancellationToken cancellationToken)
    {
        if (!IsCancelledInvoiceNotes(entity.Notes))
            return;

        if (entity.FullSelfExport)
        {
            audits.Add(CreateAudit(SchemaConstants.CustomersTable, id, SchemaConstants.CustomerColumns.FullSelfExport,
                bool.TrueString, bool.FalseString, "Update", username, now));
            entity.FullSelfExport = false;
        }

        var q27 = await _customerQueues.FindAsync(id, SchemaConstants.Quay27QueueId, cancellationToken);
        if (q27 is null)
            return;

        _customerQueues.Remove(q27);
        audits.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TableName = "CustomerQueues",
            RecordId = id,
            ColumnName = "QueueId",
            OldValue = SchemaConstants.Quay27QueueId.ToString(),
            NewValue = null,
            ActionType = "Remove",
            ChangedBy = username,
            ChangedDate = now
        });
    }

    private static List<AuditLog> BuildInsertAudits(Guid id, Customer e, string username, DateTime now)
    {
        void Add(string col, string? newVal, List<AuditLog> list) =>
            list.Add(CreateAudit(SchemaConstants.CustomersTable, id, col, null, newVal, "Insert", username, now));

        var audits = new List<AuditLog>();
        Add(SchemaConstants.CustomerColumns.SortOrder, e.SortOrder.ToString(), audits);
        Add(SchemaConstants.CustomerColumns.InvoiceCode, e.InvoiceCode, audits);
        Add(SchemaConstants.CustomerColumns.BillCreatedAt, e.BillCreatedAt.ToString("O"), audits);
        Add(SchemaConstants.CustomerColumns.NameAddress, e.NameAddress, audits);
        Add(SchemaConstants.CustomerColumns.CreateMachine, e.CreateMachine, audits);
        Add(SchemaConstants.CustomerColumns.DraftStaff, e.DraftStaff, audits);
        Add(SchemaConstants.CustomerColumns.Quantity, e.Quantity, audits);
        Add(SchemaConstants.CustomerColumns.TotalAmount, e.TotalAmount, audits);
        Add(SchemaConstants.CustomerColumns.InstallStaffCm, e.InstallStaffCm, audits);
        Add(SchemaConstants.CustomerColumns.ManagerApproved, e.ManagerApproved.ToString(), audits);
        Add(SchemaConstants.CustomerColumns.Kio27Received, e.Kio27Received.ToString(), audits);
        Add(SchemaConstants.CustomerColumns.Export27, e.Export27.ToString(), audits);
        Add(SchemaConstants.CustomerColumns.FullSelfExport, e.FullSelfExport.ToString(), audits);
        Add(SchemaConstants.CustomerColumns.Notes, e.Notes, audits);
        Add(SchemaConstants.CustomerColumns.GoodsSenderNote, e.GoodsSenderNote, audits);
        Add(SchemaConstants.CustomerColumns.AdditionalNotes, e.AdditionalNotes, audits);
        Add(SchemaConstants.CustomerColumns.SheetDate, e.SheetDate.ToString("O"), audits);
        Add(SchemaConstants.CustomerColumns.Status, e.Status, audits);
        return audits;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private async Task EnsureCanEditColumnAsync(Guid userId, string column, CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin)
            return;

        var allowed = await _columnPermissions.CanEditColumnAsync(userId, SchemaConstants.CustomersTable, column, cancellationToken);
        if (!allowed)
            throw new ForbiddenException($"No permission to edit column '{column}'.");
    }

    private async Task EnsureCanEditColumnsAsync(Guid userId, IReadOnlyList<string> columns, CancellationToken cancellationToken)
    {
        if (_currentUser.IsAdmin)
            return;

        foreach (var column in columns)
            await EnsureCanEditColumnAsync(userId, column, cancellationToken);
    }

    private async Task RecomputeDuplicatesForNameAddressAsync(string nameAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nameAddress))
            return;

        var ids = (await _customers.GetActiveCustomerIdsWithSameNameAddressAsync(nameAddress, cancellationToken)).ToList();
        var isDup = ids.Count > 1;
        var groupId = isDup ? Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(nameAddress.Trim())) : 0;

        await _customers.UpdateDuplicateStateAsync(ids, isDup, cancellationToken);

        if (isDup)
            await _duplicateFlags.ReplaceFlagsForCustomersAsync(ids, groupId, cancellationToken);
        else
            await _duplicateFlags.ClearFlagsForCustomersAsync(ids, cancellationToken);
    }

    private static AuditLog CreateAudit(string table, Guid recordId, string column, string? oldVal, string? newVal, string action, string user, DateTime when) =>
        new()
        {
            Id = Guid.NewGuid(),
            TableName = table,
            RecordId = recordId,
            ColumnName = column,
            OldValue = oldVal,
            NewValue = newVal,
            ActionType = action,
            ChangedBy = user,
            ChangedDate = when
        };

    private static DateTime ParseImportedDateTime(string raw)
    {
        var text = raw.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return DateTime.UtcNow;

        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var excelSerial))
        {
            try
            {
                return DateTime.SpecifyKind(DateTime.FromOADate(excelSerial), DateTimeKind.Utc);
            }
            catch
            {
                // fall through to string parsing
            }
        }

        if (DateTime.TryParse(
                text,
                CultureInfo.GetCultureInfo("vi-VN"),
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out var vi))
            return vi.ToUniversalTime();

        if (DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                out var inv))
            return inv;

        return DateTime.UtcNow;
    }

    private sealed record CustomerSnapshot(
        int SortOrder,
        string InvoiceCode,
        DateTime BillCreatedAt,
        string NameAddress,
        string CreateMachine,
        string DraftStaff,
        string Quantity,
        string InstallStaffCm,
        bool ManagerApproved,
        bool Kio27Received,
        bool Export27,
        bool FullSelfExport,
        string Notes,
        string GoodsSenderNote,
        string AdditionalNotes,
        DateOnly SheetDate,
        string Status,
        string TotalAmount)
    {
        public static CustomerSnapshot FromEntity(Customer c) =>
            new(
                c.SortOrder,
                c.InvoiceCode,
                c.BillCreatedAt,
                c.NameAddress,
                c.CreateMachine,
                c.DraftStaff,
                c.Quantity,
                c.InstallStaffCm,
                c.ManagerApproved,
                c.Kio27Received,
                c.Export27,
                c.FullSelfExport,
                c.Notes,
                c.GoodsSenderNote,
                c.AdditionalNotes,
                c.SheetDate,
                c.Status,
                c.TotalAmount);
    }
}

using System.Globalization;
using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class SupplierService : ISupplierService
{
    private const string SupplierCodePrefix = "NCC";
    private const int SupplierCodePadding = 6;

    private readonly ISupplierRepository _suppliers;
    private readonly ISupplierGroupRepository _groups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SupplierService> _logger;

    public SupplierService(
        ISupplierRepository suppliers,
        ISupplierGroupRepository groups,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<SupplierService> logger)
    {
        _suppliers = suppliers;
        _groups = groups;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<IReadOnlyList<SupplierDto>> ListAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _suppliers.ListAsync(query, cancellationToken);
    }

    public Task<SupplierDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _suppliers.GetProjectedByIdAsync(id, cancellationToken);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var username = _currentUser.Username;

        var dto = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var code = string.IsNullOrWhiteSpace(request.Code)
                ? await GenerateSupplierCodeAsync(cancellationToken)
                : request.Code!.Trim();

            if (await _suppliers.CodeExistsAsync(code, excludeId: null, cancellationToken))
                throw new ConflictException($"Mã nhà cung cấp '{code}' đã tồn tại.");

            if (request.SupplierGroupId is { } gid)
            {
                var g = await _groups.GetTrackedAsync(gid, cancellationToken);
                if (g is null) throw new NotFoundException("Nhóm nhà cung cấp không tồn tại.");
            }

            var initial = request.InitialDebt ?? 0m;
            var current = request.CurrentDebt ?? initial;

            var entity = new Supplier
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = request.Name.Trim(),
                Phone = request.Phone?.Trim() ?? string.Empty,
                Email = request.Email?.Trim() ?? string.Empty,
                Address = request.Address?.Trim() ?? string.Empty,
                Region = request.Region?.Trim() ?? string.Empty,
                Ward = request.Ward?.Trim() ?? string.Empty,
                SupplierGroupId = request.SupplierGroupId,
                Notes = request.Notes?.Trim() ?? string.Empty,
                CompanyName = request.CompanyName?.Trim() ?? string.Empty,
                TaxCode = request.TaxCode?.Trim() ?? string.Empty,
                InitialDebt = initial,
                CurrentDebt = current,
                TotalPurchase = 0m,
                TotalReturn = 0m,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = username,
                IsDeleted = false,
            };
            await _suppliers.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (await _suppliers.GetProjectedByIdAsync(entity.Id, cancellationToken))!;
        }, cancellationToken);

        _logger.LogInformation("Supplier {Id} ({Code}) created by {User}", dto.Id, dto.Code, username);
        return dto;
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var username = _currentUser.Username;

        var dto = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _suppliers.GetTrackedAsync(id, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");

            if (request.Name is not null)
            {
                var v = request.Name.Trim();
                if (string.IsNullOrEmpty(v)) throw new ConflictException("Tên không được để trống.");
                entity.Name = v;
            }
            if (request.Phone is not null) entity.Phone = request.Phone.Trim();
            if (request.Email is not null) entity.Email = request.Email.Trim();
            if (request.Address is not null) entity.Address = request.Address.Trim();
            if (request.Region is not null) entity.Region = request.Region.Trim();
            if (request.Ward is not null) entity.Ward = request.Ward.Trim();

            if (request.ClearSupplierGroup == true)
            {
                entity.SupplierGroupId = null;
            }
            else if (request.SupplierGroupId is { } gid)
            {
                var g = await _groups.GetTrackedAsync(gid, cancellationToken);
                if (g is null) throw new NotFoundException("Nhóm nhà cung cấp không tồn tại.");
                entity.SupplierGroupId = gid;
            }

            if (request.Notes is not null) entity.Notes = request.Notes.Trim();
            if (request.CompanyName is not null) entity.CompanyName = request.CompanyName.Trim();
            if (request.TaxCode is not null) entity.TaxCode = request.TaxCode.Trim();
            if (request.InitialDebt is { } ini) entity.InitialDebt = ini;
            if (request.CurrentDebt is { } cur) entity.CurrentDebt = cur;
            if (request.IsActive is { } act) entity.IsActive = act;

            entity.UpdatedDate = DateTime.UtcNow;
            entity.UpdatedBy = username;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (await _suppliers.GetProjectedByIdAsync(id, cancellationToken))!;
        }, cancellationToken);

        _logger.LogInformation("Supplier {Id} updated by {User}", id, username);
        return dto;
    }

    public async Task<SupplierDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entity = await _suppliers.GetTrackedAsync(id, cancellationToken)
            ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");
        entity.IsActive = isActive;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.Username;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await _suppliers.GetProjectedByIdAsync(id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var ok = await _suppliers.SoftDeleteAsync(id, _currentUser.Username, cancellationToken);
        if (!ok) throw new NotFoundException("Nhà cung cấp không tồn tại.");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportSuppliersExcelResult> ImportExcelAsync(
        ImportSuppliersExcelRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var username = _currentUser.Username;

        if (request.FileBytes.Length == 0)
            throw new InvalidOperationException("File import rỗng.");

        var ext = Path.GetExtension(request.FileName ?? string.Empty);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx.");

        var rows = SupplierExcelReader.ReadMappedRows(request.FileBytes);
        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;
        var errors = new List<string>();

        // Cache group lookup theo tên (case-insensitive) — tự tạo mới khi cần.
        var groupCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                skipped++;
                continue;
            }

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    Guid? groupId = null;
                    if (!string.IsNullOrWhiteSpace(row.GroupName))
                    {
                        if (!groupCache.TryGetValue(row.GroupName, out var gid))
                        {
                            var existing = await _groups.GetByNameAsync(row.GroupName, cancellationToken);
                            if (existing is null)
                            {
                                existing = new SupplierGroup
                                {
                                    Id = Guid.NewGuid(),
                                    Name = row.GroupName.Trim(),
                                    CreatedDate = DateTime.UtcNow,
                                    CreatedBy = username,
                                };
                                await _groups.AddAsync(existing, cancellationToken);
                                await _unitOfWork.SaveChangesAsync(cancellationToken);
                            }
                            gid = existing.Id;
                            groupCache[row.GroupName] = gid;
                        }
                        groupId = gid;
                    }

                    var isActive = !string.Equals(row.StatusRaw.Trim(), "0", StringComparison.Ordinal)
                                   && !string.Equals(row.StatusRaw.Trim(), "false", StringComparison.OrdinalIgnoreCase)
                                   && !string.Equals(row.StatusRaw.Trim(), "ngừng hoạt động", StringComparison.OrdinalIgnoreCase);

                    Supplier? existingSupplier = null;
                    if (!string.IsNullOrWhiteSpace(row.Code))
                    {
                        existingSupplier = await _suppliers.GetTrackedByCodeAsync(row.Code.Trim(), cancellationToken);
                    }

                    if (existingSupplier is null)
                    {
                        var code = string.IsNullOrWhiteSpace(row.Code)
                            ? await GenerateSupplierCodeAsync(cancellationToken)
                            : row.Code.Trim();

                        if (await _suppliers.CodeExistsAsync(code, excludeId: null, cancellationToken))
                            throw new InvalidOperationException($"Mã '{code}' đã tồn tại.");

                        var debt = request.UpdateClosingDebt ? ParseDecimal(row.CurrentDebtRaw) : 0m;

                        var entity = new Supplier
                        {
                            Id = Guid.NewGuid(),
                            Code = code,
                            Name = row.Name.Trim(),
                            Phone = row.Phone.Trim(),
                            Email = row.Email.Trim(),
                            Address = row.Address.Trim(),
                            Region = row.Region.Trim(),
                            Ward = row.Ward.Trim(),
                            SupplierGroupId = groupId,
                            Notes = row.Notes.Trim(),
                            CompanyName = row.CompanyName.Trim(),
                            TaxCode = row.TaxCode.Trim(),
                            InitialDebt = debt,
                            CurrentDebt = debt,
                            TotalPurchase = 0m,
                            TotalReturn = 0m,
                            IsActive = isActive,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = username,
                            IsDeleted = false,
                        };
                        await _suppliers.AddAsync(entity, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        imported++;
                    }
                    else
                    {
                        existingSupplier.Name = row.Name.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Phone)) existingSupplier.Phone = row.Phone.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Email)) existingSupplier.Email = row.Email.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Address)) existingSupplier.Address = row.Address.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Region)) existingSupplier.Region = row.Region.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Ward)) existingSupplier.Ward = row.Ward.Trim();
                        if (groupId is not null) existingSupplier.SupplierGroupId = groupId;
                        if (!string.IsNullOrWhiteSpace(row.Notes)) existingSupplier.Notes = row.Notes.Trim();
                        if (!string.IsNullOrWhiteSpace(row.CompanyName)) existingSupplier.CompanyName = row.CompanyName.Trim();
                        if (!string.IsNullOrWhiteSpace(row.TaxCode)) existingSupplier.TaxCode = row.TaxCode.Trim();
                        existingSupplier.IsActive = isActive;
                        if (request.UpdateClosingDebt)
                        {
                            var debt = ParseDecimal(row.CurrentDebtRaw);
                            existingSupplier.InitialDebt = debt;
                            existingSupplier.CurrentDebt = debt;
                        }
                        existingSupplier.UpdatedDate = DateTime.UtcNow;
                        existingSupplier.UpdatedBy = username;
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        updated++;
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                if (errors.Count < 30)
                    errors.Add($"Dòng {row.RowNumber}: {ex.Message}");
            }
        }

        return new ImportSuppliersExcelResult(rows.Count, imported, updated, skipped, failed, errors);
    }

    public async Task<byte[]> ExportExcelAsync(ListSuppliersQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var rows = await _suppliers.ListAsync(query, cancellationToken);
        return SupplierExcelExporter.Build(rows, DateTime.Now);
    }

    private async Task<string> GenerateSupplierCodeAsync(CancellationToken cancellationToken)
    {
        var maxSuffixRaw = await _suppliers.GetMaxNumericCodeSuffixAsync(SupplierCodePrefix, cancellationToken);
        var next = 1;
        if (!string.IsNullOrWhiteSpace(maxSuffixRaw) && int.TryParse(maxSuffixRaw, out var parsed))
            next = parsed + 1;
        return SupplierCodePrefix + next.ToString().PadLeft(SupplierCodePadding, '0');
    }

    private static decimal ParseDecimal(string raw)
    {
        var t = raw?.Trim();
        if (string.IsNullOrWhiteSpace(t)) return 0m;
        // Cho phép có dấu chấm/phẩy phân cách hàng nghìn.
        var clean = t.Replace(",", string.Empty).Replace(" ", string.Empty);
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        return 0m;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}

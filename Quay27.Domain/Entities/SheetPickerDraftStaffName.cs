namespace Quay27.Domain.Entities;

/// <summary>NV soạn không đăng nhập — tên do admin cấu hình cho dropdown.</summary>
public class SheetPickerDraftStaffName
{
    public int Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

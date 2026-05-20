# End-of-day report templates

## Sales (`concern=sales`)

| File | `displayMode` | Data start row |
|------|---------------|----------------|
| `EndOfDayDocument.xlsx` | `vertical` | 10 |
| `EndOfDayDocumentLC.xlsx` | `horizontal` | 12 |

See `EndOfDayExcelTemplateFiller.cs` for column mapping.

## Cashflow (`concern=cashflow`)

| File | `displayMode` | Layout |
|------|---------------|--------|
| `EndOfDayCashFlow.xlsx` | `vertical` | Tổng kết thu/chi — header row 9, data row 11+ (cols: Thu/Chi, Tiền mặt, CK, Thẻ) |
| `EndOfDayCashFlowLC.xlsx` | `horizontal` | Chi tiết phiếu — header row 8, data row 11+ |

Horizontal columns: 2 Mã phiếu, 3 Loại thu chi, 5 Nhân viên, 8 Đối tác, 9 Thu/Chi, 10 Thời gian, 12 T.Toán, 13 Mã chứng từ nguồn.

## Products (`concern=products`)

| File | `displayMode` | Data start row |
|------|---------------|----------------|
| `EndOfDayProduct.xlsx` | `vertical` | 9 |
| `EndOfDayProductLC.xlsx` | `horizontal` | 10 |

Vertical: 2 Mã, 4 Tên, 8 SL bán, 9 Doanh thu, 10 SL trả, 11 Giá trị trả, 13 Doanh thu thuần.

Horizontal: 2 Mã, 5 Tên, 9 SL bán, 10 Giá niêm yết, 11 Doanh thu, 13 Chênh lệch, 15 SL trả.

## Summary (`concern=summary`)

Always `EndOfDayCashFlow.xlsx` (vertical). Fills cashflow summary (row 11+), sales summary (row 18+), transaction counts (row 27+).

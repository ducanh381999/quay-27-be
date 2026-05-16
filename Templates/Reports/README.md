# End-of-day sales report templates

| File | `displayMode` | Layout |
|------|---------------|--------|
| `EndOfDayDocument.xlsx` | `vertical` | Aggregated columns per transaction |
| `EndOfDayDocumentLC.xlsx` | `horizontal` | Per-invoice detail rows |

## Vertical (`EndOfDayDocument.xlsx`)

| Cell | Content |
|------|---------|
| (1,2) | `Ngày lập: {dd/MM/yyyy HH:mm}` |
| (4,2) | `Ngày bán: {range}` |
| (5,2) | `Ngày thanh toán: {range}` |
| (6,2) | `Chi nhánh: {branch}` |
| Row 10+ | Data (clears placeholder row 10) |

| Col | Field |
|-----|-------|
| 2 | Mã giao dịch (`Code`) |
| 3 | Thời gian (`CreatedAtUtc`) |
| 5 | SL (sum item qty) |
| 7 | Doanh thu (`SubtotalAmount`) |
| 8 | Thu khác (0) |
| 9 | VAT (0) |
| 10 | Làm tròn (0) |
| 11 | Phí trả hàng (0) |
| 13 | Thực thu (`PaidAmount`) |

## Horizontal (`EndOfDayDocumentLC.xlsx`)

| Cell | Content |
|------|---------|
| (2,2) | `Ngày lập: …` |
| (5,2) | `Ngày bán: …` |
| (6,2) | `Ngày thanh toán: …` |
| (7,2) | `Chi nhánh: …` |
| Row 12+ | Data |

| Col | Field |
|-----|-------|
| 2 | Mã chứng từ |
| 3 | Khách hàng |
| 4 | Nhân viên |
| 7 | Thời gian |
| 8 | SL |
| 9 | Tổng tiền hàng |
| 10 | Giảm giá |
| 12 | Thu khác (0) |
| 13 | VAT (0) |
| 14 | Làm tròn (0) |

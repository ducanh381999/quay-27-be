# Quay27 backend

ASP.NET Core 8 Web API for the customer sheet system described in `.cursor/rules/quay27.mdc`.

## Structure

- **Quay27.Domain** – entities and schema constants  
- **Quay27.Application** – DTOs, FluentValidation, services, repository interfaces  
- **Quay27.Infrastructure** – EF Core (Pomelo MySQL), repository implementations, end-of-day service  
- **Quay27-Be** – host, JWT, Swagger, middleware, background job  

## Engineering Governance

- Project governance and quality gates are defined in
  `.specify/memory/constitution.md`.
- Backend changes MUST align with API contract, security/permission rules, test gates,
  and data integrity requirements from the constitution.

## Prerequisites

- .NET 8 SDK  
- MySQL 8 (local or remote)  

## Configuration

Edit `appsettings.json` (or user secrets / environment variables):

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | MySQL connection string |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` | JWT (signing key must be long enough for HMAC, e.g. 32+ characters) |
| `Jwt:AccessTokenMinutes` | Access token lifetime |
| `Seed:AdminPassword` | Password for the seeded `admin` user (development; change in production) |
| `DemoSeed:StaffPassword` | Password for demo staff users `staff1`–`staff4` (used by `POST /api/setup/demo-data`) |
| `Cors:AllowedOrigins` | Browser origins for SignalR + credentialed cross-origin calls (e.g. `http://localhost:3000` for Next.js dev) |

### Demo sheet data (FE / local)

After logging in as **admin**, call:

`POST /api/setup/demo-data` (Bearer token required; **Admin role** only)

This is **idempotent**: if demo customers already exist (`CreatedBy = demo-seed-api`), the response returns `alreadySeeded: true` and does not duplicate rows. It seeds **4 staff** users (`staff1`…`staff4`), **8 sample customers** (today / yesterday, duplicate pair, flags), and enrolls the **first 4** customers in queue **Quầy 27** (id `1`). The response lists usernames and configured passwords for wiring the FE.

**Column permissions (demo staff)** — every staff has `CanView` on all sheet columns; `CanEdit` differs:

| User | Role in demo | Editable columns (`Customers` table) |
|------|----------------|--------------------------------------|
| **staff1** | Nhập liệu | `SortOrder`, `InvoiceCode`, `BillCreatedAt`, `NameAddress`, `CreateMachine`, `DraftStaff`, `Quantity`, `InstallStaffCm`, `Notes`, `GoodsSenderNote`, `AdditionalNotes` (not the three approval flags, `SheetDate`, or `Status`) |
| **staff2** | Ghi chú | `Notes`, `AdditionalNotes`, `Status`, `DraftStaff` |
| **staff3** | Quầy / tick | `ManagerApproved`, `Kio27Received`, `Export27` |
| **staff4** | Thứ tự & ngày sheet | `SortOrder`, `SheetDate` only |

If you already ran `demo-data` with an older build (full edit for everyone), drop or clear demo rows / use a fresh database, then call `POST /api/setup/demo-data` again to get these profiles.

For EF CLI design-time, `ApplicationDbContextFactory` resolves the connection in this order: **`QUAY27_CONNECTION_STRING`** (if set), then **`Quay27-Be/appsettings.json`** + optional **`appsettings.{Environment}.json`** (walks up from the current directory to find `Quay27-Be.csproj`). Run `dotnet ef` from the **solution root** so that lookup succeeds.

### EF Core CLI tools (version warning)

If you see *“Entity Framework tools version '7.x' is older than runtime '8.x'”*, update the global tool:

```bash
dotnet tool update --global dotnet-ef
```

Ensure `dotnet ef` is on your PATH (restart the terminal / Visual Studio after installing).

## Database

Apply schema (also runs on startup via `DatabaseSeeder`):

```bash
dotnet ef database update --project Quay27.Infrastructure --startup-project Quay27-Be.csproj
```

### MySQL: `Access denied for user 'root'@'...'` (e.g. `172.17.0.1`)

That host is often **Docker’s bridge** (app or tool runs in a container, or MySQL is in Docker). Typical fixes:

1. **Password** – In `appsettings.json`, set `Password=` to the real password for `root` (or the user you use). Wrong password produces the same error as “wrong host”.

2. **User allowed from your client host** – MySQL may have `root` only for `localhost`, while the server sees you as `172.17.0.1`. In MySQL (as an admin), either:
   - Create/grant a user for remote/Docker clients, e.g.  
     `CREATE USER 'quay27'@'%' IDENTIFIED BY 'your_password';`  
     `GRANT ALL ON quay27.* TO 'quay27'@'%';`  
     `FLUSH PRIVILEGES;`  
     then point the connection string at `User=quay27` and that password, **or**
   - Adjust `root` for your environment (dev only): grant from `'%'` or from that IP — only if you accept the security trade-off.

3. **Try `127.0.0.1` instead of `localhost`** – Sometimes the client resolves to IPv6 or a different path; `Server=127.0.0.1` can change which MySQL account matches.

4. **Package Manager Console** – Default directory may not be the solution folder. Either `cd` to the repo root first, or use full paths to the `.csproj` files.

Visual Studio **Package Manager Console** can also run (with **Default project** = `Quay27.Infrastructure`):

```powershell
Update-Database -StartupProject Quay27-Be
```

(`Add-Migration Name` works here only when `Microsoft.EntityFrameworkCore.Tools` is referenced and the PMC default project is correct.)

### MySQL: `Row size too large` (65535)

With **utf8mb4**, several wide `VARCHAR` columns in one row can exceed MySQL’s row-size limit. This project maps **`AuditLogs.OldValue` / `NewValue`** to **`longtext`** so migrations apply cleanly. If you changed migrations locally and see this error again, prefer `longtext`/`TEXT` for large string columns instead of huge `VARCHAR`s.

If a failed migration left the database half-created, drop the database (or remove the partial tables) and run `dotnet ef database update` again.

On first run, roles **Admin** / **Staff**, queue **Quầy 27** (id `1`), and user **admin** are seeded if the database is empty.

## Run

```bash
dotnet run --project Quay27-Be.csproj
```

Swagger UI (Development): `/swagger`

## SignalR (customer sheet realtime)

- **Hub URL:** `/hubs/customer-sheet` (class `CustomerSheetHub`).
- **Auth:** JWT — browser clients pass `access_token` as a query string during negotiate/WebSocket (see `JwtBearerEvents.OnMessageReceived` in `Program.cs`); same token as `Authorization: Bearer` for REST.
- **Groups:** `sheet:{yyyy-MM-dd}` — call hub methods `JoinSheet(sheetDate)` / `LeaveSheet(sheetDate)` after connecting.
- **Server event:** `CustomerSheetChanged` with payload `{ sheetDate, customerId, changeType }` where `changeType` is `created` | `updated` | `deleted` | `queue` (emitted after successful customer create/update/delete and queue enrollment changes).

Next.js front end uses `GET /api/auth/signalr-access` (same app origin) to read the httpOnly login cookie into JS for `accessTokenFactory`, then connects to this hub URL on the API host. **CORS** must list the front-end origin.

## Customer columns (sheet Quầy 27)

`Customers` maps to the operational sheet (14 cột): `SortOrder` (stt), `InvoiceCode` (Mã HĐ), `BillCreatedAt` (TG lên bill), `NameAddress` (Tên khách + địa chỉ, **longtext**), `CreateMachine`, `DraftStaff`, `Quantity`, `InstallStaffCm` (NV Lắp CM), `ManagerApproved` (QL duyệt), `Kio27Received` (Kio27 nhận), `Export27` (27 Xuất), `Notes`, `GoodsSenderNote`, `AdditionalNotes`, plus `SheetDate`, `Status`, và metadata hệ thống. Trường dài dùng **longtext** để tránh giới hạn row size MySQL.

**File full thông tin — cột CẤP 27:** không lưu boolean riêng; khi tick, gọi `PUT /api/customers/{id}/queues/1` với `{ "enrolled": true }` (queue **Quầy 27** có id `1` sau seed). Bỏ tick → `enrolled: false`.

Migration `Quay27CustomerSheetColumns` chuyển dữ liệu cũ: `NameAddress` ← `Name` + `Address`, `Notes` ← `Note`, `InvoiceCode` ← cắt `Phone` (64 ký tự), `BillCreatedAt` ← `CreatedDate`, rồi xóa cột `Name`, `Address`, `Note`, `Phone`. **Breaking** cho client cũ dùng `name` / `phone` / `address` / `note`.

## API overview

- `POST /api/auth/login` – JWT (anonymous)  
- `GET /api/customers?sheetDate=yyyy-MM-dd&queueId=` – list theo ngày; `queueId=1` = chỉ khách đang trong **Quầy 27**. **Full sheet** (không `queueId`): nếu `sheetDate` trùng **hôm nay theo lịch Việt Nam** (Asia/Ho_Chi_Minh), trả về cả dòng của ngày đó và các đơn **ngày cũ hơn** chưa xử lý xong (chưa vào queue Quầy 27, không `fullSelfExport`, `notes` khác `"Hủy hóa đơn"`). Ngày khác: chỉ đúng `sheetDate` đã chọn.  
- `GET /api/customers/{id}`, `POST`, `PATCH`, `DELETE` – customer CRUD (soft delete; admin only for delete)  
- `PUT /api/customers/{id}/queues/{queueId}` – body `{ "enrolled": true|false }` (CẤP 27 → `queueId=1`)  
- `GET /api/queues` – active queues  
- `GET /api/products`, `GET /api/products/{id}`, `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}` – CRUD hàng hóa
- `POST /api/products/uploads/images` – upload ảnh hàng hóa lên Cloudflare R2 (multipart form-data, field `file`)
- `POST /api/products/{id}/duplicate`, `PATCH /api/products/{id}/status`, `PATCH /api/products/{id}/group` – thao tác nhanh trên hàng hóa
- `GET /api/products/groups`, `POST /api/products/groups` – danh mục nhóm hàng
- `GET /api/products/groups/tree` – cây nhóm hàng hóa (cha-con) để map theo nhóm
- `GET /api/products/price-lists`, `POST /api/products/price-lists`, `PUT /api/products/price-lists/{id}` – quản lý thông tin bảng giá
- `GET /api/products/price-lists/items?priceListIds=` – lấy danh sách sản phẩm theo một hoặc nhiều bảng giá; hỗ trợ filter tùy chọn `priceOperator`, `comparePrice`, `compareValue`
- `POST /api/products/price-lists/{id}/items/add-all` – thêm toàn bộ hàng hóa vào bảng giá (body tùy chọn `{ "confirmed": true }`, bắt buộc khi bảng giá đang rỗng)
- `POST /api/products/price-lists/{id}/items/add-by-groups` – thêm hàng theo nhóm (body: `{ "groupIds": ["..."], "includeDescendants": true }`)
- `POST /api/products/price-lists/{id}/apply-formula` – áp công thức giá cho toàn bộ hoặc 1 sản phẩm (body: `{ \"applyTo\": \"all|single\", \"productId\": \"...\" }`)
- `GET /api/products/price-lists/import/template` – tải file mẫu import bảng giá (`MauFileBangGia.xlsx`)
- `POST /api/products/price-lists/import` – import bảng giá từ file `.xlsx` (multipart form-data, field `file`), trả về `totalRows/successfulRows/failedRows/errors`
- `GET /api/products/price-lists/export` – export dữ liệu bảng giá theo filter hiện tại (`priceListIds`, `search`, `groupId`, `stock`, `priceOperator`, `comparePrice`, `compareValue`)
- `GET /api/customer-groups`, `POST /api/customer-groups`, `PUT /api/customer-groups/{id}`, `DELETE /api/customer-groups/{id}` – master nhóm khách hàng (phục vụ map phạm vi áp dụng bảng giá)

### Users and column permissions (Admin JWT)

All routes below require **Bearer** token with role **Admin**, except `GET /api/users/sheet-pickers` and `GET /api/me/customer-column-permissions` (any authenticated user).

| Method | Path | Body / notes |
|--------|------|----------------|
| `GET` | `/api/users/sheet-pickers` | Sheet dropdowns: mọi user **Admin** và **Staff** hoạt động (`username`, `fullName`) cộng các **tên NV soạn** cấu hình (`username` rỗng, `fullName` = tên). |
| `GET` | `/api/users/sheet-picker-members` | **Admin.** Danh sách tên NV soạn đã cấu hình: `string[]`. |
| `PUT` | `/api/users/sheet-picker-members` | **Admin.** Body `{ "names": ["Tên A", "Tên B"] }` — thay toàn bộ danh sách tên NV soạn (không cần user đăng nhập). |
| `GET` | `/api/users` | List users (`username`, `fullName`, `isActive`, `roles`). |
| `GET` | `/api/users/{id}` | Single user. |
| `POST` | `/api/users` | `{ "username", "password", "fullName", "roleNames": ["Staff"], "isActive": true }`. New **Staff** users get default customer column permissions: all columns `canView: true`, `canEdit: false` until you `PUT` overrides. |
| `PATCH` | `/api/users/{id}` | Optional `username`, `fullName`, `isActive`, `roleNames` (replaces roles when provided). |
| `POST` | `/api/users/{id}/password` | `{ "newPassword" }`. |
| `GET` | `/api/users/{id}/column-permissions` | Response `{ "columns": [ { "name", "canView", "canEdit" }, ... ] }` for `Customers`. Users with role **Admin** return synthetic full allow (same as `/api/me` for admin). |
| `PUT` | `/api/users/{id}/column-permissions` | JSON array of `{ "columnName", "canView", "canEdit" }` — must include **every** customer sheet column exactly once (property names: `SortOrder`, `InvoiceCode`, …). |

### Current user column permissions (FE / AG Grid)

- `GET /api/me/customer-column-permissions` – `{ "columns": [ { "name", "canView", "canEdit" }, ... ] }`. **Admin:** all `true`/`true`. **Staff:** from `ColumnPermissions` for table `Customers`; missing rows → `canView`/`canEdit` **false**.

Staff users need `ColumnPermissions` rows (`TableName` = `Customers`, `ColumnName` matching **C# property names**, e.g. `NameAddress`, `InvoiceCode`, …) with `CanEdit` to change fields via `PATCH /api/customers`. **Admin** bypasses column checks on the server.

A background service runs at **local midnight** and moves active customers with **no** queue rows to the next `SheetDate`.

## Migrations

Add a migration after model changes (replace `YourMigrationName` with a real name — do **not** use angle brackets in PowerShell):

```bash
dotnet ef migrations add YourMigrationName --project Quay27.Infrastructure --startup-project Quay27-Be.csproj --output-dir Persistence/Migrations
```

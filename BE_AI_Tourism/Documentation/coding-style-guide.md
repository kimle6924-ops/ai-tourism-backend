# Coding Style Guide

---

## Đặt tên

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Class | PascalCase | `UserService`, `PaymentController` |
| Method | PascalCase | `CreateUser()`, `GetOrders()` |
| Variable | camelCase | `userId`, `orderList` |
| Interface | `I` + PascalCase | `IUserRepository`, `IPaymentService` |
| Constant | PascalCase | `MaxPageSize`, `DefaultTimeout` |

---

## Giới hạn kích thước

| Loại | Giới hạn |
|------|---------|
| Method | ≤ 30 dòng |
| Class | ≤ 300 dòng |
| File | Lý tưởng 100-200 dòng |

---

## Phân chia trách nhiệm

| Layer | Chỉ làm | KHÔNG làm |
|-------|---------|-----------|
| Controller | Nhận request → gọi service → trả response | Business logic |
| Service | Business logic, mapping Entity ↔ DTO (dùng Mapster) | Truy cập DB trực tiếp |
| Repository | Query/Save/Update/Delete database | Business logic |

---

## API Design

- RESTful: `GET /api/users`, `POST /api/users`, `PUT /api/users/{id}`, `DELETE /api/users/{id}`
- KHÔNG dùng: `/getUser`, `/createUser`
- Response luôn dùng Result pattern: `{ success, data, error, statusCode }`

---

## Không Hard-code

| Loại giá trị | Nơi quản lý |
|---|---|
| Hằng số cố định | `Shared/Constants/AppConstants.cs` |
| Config theo môi trường | `appsettings.json` + Options class |
| Secrets / keys | `.env` (dev) / Env vars (prod) |
| Validation rules | FluentValidation validators |
| Error messages dùng chung | Constants class |

Quy tắc: giá trị xuất hiện > 1 lần → constants/config. Khác nhau giữa môi trường → appsettings. Secret → KHÔNG đưa vào code.

---

## Validation (FluentValidation)

- Mỗi Request DTO cần validator: `{RequestName}Validator` trong `Application/Validators/`
- Auto-register qua DI, lỗi trả về qua Result pattern (400)
- KHÔNG validate trong controller hay service

---

## Mapping (Mapster)

- Dùng `IMapper` (inject qua DI): `mapper.Map<UserResponse>(entity)`
- Custom rules tập trung tại `Application/Mapping/MappingConfig.cs`
- KHÔNG map tay khi Mapster xử lý được

---

## Các quy tắc khác

- **Folder:** Không tạo quá nhiều folder nhỏ. `Services/UserService.cs` thay vì `Services/User/Create/`
- **DI:** Constructor injection, KHÔNG new object trong service
- **Error handling:** KHÔNG try/catch trong controller, dùng ExceptionMiddleware
- **Logging:** Log error, external API call, important workflow. KHÔNG log spam
- **Comment:** Chỉ comment khi cần, KHÔNG comment hiển nhiên
- **Formatting:** Indent 4 spaces, brace style new line (C# convention)
- **Entity:** KHÔNG expose trực tiếp ra API, KHÔNG trả sensitive data trong Response DTO

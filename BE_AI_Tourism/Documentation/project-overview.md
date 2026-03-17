# BackendBase - Project Overview

ASP.NET Core 8.0 backend template. MVC + Layered Architecture, single project, tách layer bằng folders.

---

## Folder Structure

| Folder | Vai trò |
|--------|---------|
| `Controllers/` | API endpoints (thin controllers) |
| `Middlewares/` | ExceptionMiddleware, RequestLoggingMiddleware |
| `Filters/` | ASP.NET Core action filters |
| `Configuration/` | Options classes bind từ appsettings.json |
| `Application/DTOs/` | Request/Response models (tách riêng Request DTO và Response DTO) |
| `Application/Interfaces/` | Service contracts |
| `Application/Services/` | Business logic |
| `Application/Validators/` | FluentValidation validators + ValidatorFilter |
| `Application/Mapping/` | Mapster config (Entity ↔ DTO) |
| `Domain/Entities/` | Database models, kế thừa BaseEntity (Id, CreatedAt, UpdatedAt) |
| `Domain/Enums/` | Enums |
| `Domain/Interfaces/` | IRepository\<T\> |
| `Infrastructure/Database/` | IDatabaseContext |
| `Infrastructure/Repositories/` | Repository implementations |
| `Infrastructure/Providers/` | External service implementations |
| `Infrastructure/DependencyInjection.cs` | DI registration: AddInfrastructure(), AddApplicationServices() |
| `Shared/Core/` | Result\<T\> pattern, BaseEntity |
| `Shared/Pagination/` | PaginationRequest, PaginationResponse\<T\> |
| `Shared/Constants/` | AppConstants |
| `Shared/Utils/` | Utility helpers |

---

## Data Flow

```
Frontend → RequestDTO → Controller → Service → Entity → Repository → Database
Database → Entity → Service → ResponseDTO → Controller → Frontend
```

| Layer | Model | Mục đích |
|-------|-------|----------|
| Frontend → API | Request DTO | Nhận input từ client |
| Service | Entity ↔ DTO | Xử lý logic, mapping (dùng Mapster) |
| Repository → DB | Entity | Lưu/đọc database |
| API → Frontend | Response DTO | Trả kết quả cho client |

KHÔNG dùng Entity trực tiếp cho API response. KHÔNG trả sensitive data trong Response DTO.

---

## Key Components

**Result\<T\>** — Chuẩn hóa API response: `{ success, data, error, statusCode }`
- `Result.Ok()`, `Result.Fail()`, `Result.NotFound()`, `Result.Unauthorized()`
- `Result.Ok<T>(data)`, `Result.Fail<T>(error)`

**ExceptionMiddleware** — Global error handler, controller không cần try/catch.
- `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 401, `ArgumentException` → 400, còn lại → 500

**IRepository\<T\>** — Generic repository (where T : BaseEntity):
GetByIdAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync

**Database** — MongoDB. Chỉ có interface (IDatabaseContext, IRepository\<T\>), chưa implement.

**Pagination** — PaginationRequest (PageNumber, PageSize) + PaginationResponse\<T\> (Items, TotalCount, TotalPages, HasPreviousPage, HasNextPage)

**FluentValidation** — Auto-register validators qua DI. Lỗi trả về qua Result pattern (400).

**Mapster** — Mapping Entity ↔ DTO. Config tập trung tại `Application/Mapping/MappingConfig.cs`.

**CORS** — Cấu hình qua appsettings.json section `Cors`. Thêm origin mới chỉ sửa config.

---

## Configuration & Secrets

### Cơ chế

Options pattern (`IOptions<T>`) — các class trong `Configuration/` bind từ `appsettings.json`, đăng ký tại `DependencyInjection.cs`.

### Phân loại

| Loại | Nơi lưu | Ví dụ |
|------|---------|-------|
| Config không nhạy cảm | `appsettings.json` | CORS origins, JWT Issuer/Audience, ExpirationInMinutes, base URLs |
| Secrets | User Secrets (dev) / Env vars (prod) | JWT Secret, ConnectionString, API keys |

### Options classes

| Class | Section trong appsettings | Fields |
|-------|--------------------------|--------|
| `DatabaseOptions` | `Database` | Provider (MongoDB), ConnectionString |
| `JwtOptions` | `Jwt` | Secret, Issuer, Audience, ExpirationInMinutes |
| `ExternalServiceOptions` | `ExternalServices` | PaymentApiKey, AiServiceKey, PaymentBaseUrl, AiServiceBaseUrl |
| `CorsOptions` | `Cors` | AllowedOrigins, AllowedMethods, AllowedHeaders, AllowCredentials |

### Setup User Secrets (development)

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your-secret-key"
dotnet user-secrets set "Database:ConnectionString" "mongodb://username:password@localhost:27017/BackendBase"
dotnet user-secrets set "ExternalServices:PaymentApiKey" "your-key"
dotnet user-secrets set "ExternalServices:AiServiceKey" "your-key"
```

---

## Middleware Pipeline (thứ tự trong Program.cs)

1. ExceptionMiddleware
2. RequestLoggingMiddleware
3. Swagger (Development only)
4. HTTPS Redirection
5. CORS
6. MapControllers

---

## NuGet Packages

- `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
- `Mapster` + `Mapster.DependencyInjection`
- `Swashbuckle.AspNetCore`

---

## Khi thêm module mới

```
Application/DTOs/            → CreateFeatureRequest.cs, FeatureResponse.cs
Application/Interfaces/      → IFeatureService.cs
Application/Services/        → FeatureService.cs
Application/Validators/      → CreateFeatureRequestValidator.cs
Domain/Entities/             → Feature.cs
Infrastructure/Repositories/ → FeatureRepository.cs
Controllers/                 → FeatureController.cs
```

Đăng ký DI tại `Infrastructure/DependencyInjection.cs`.

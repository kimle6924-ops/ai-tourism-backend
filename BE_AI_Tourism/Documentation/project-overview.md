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
| `Application/Services/` | Business logic (interface + implementation cùng folder) |
| `Application/Validators/` | FluentValidation validators |
| `Application/Mapping/` | Mapster config (Entity ↔ DTO) |
| `Domain/Entities/` | Database models, kế thừa BaseEntity (Id, CreatedAt, UpdatedAt) |
| `Domain/Enums/` | Enums |
| `Domain/Interfaces/` | IRepository\<T\> |
| `Infrastructure/Database/` | AppDbContext (EF Core), EfRepository\<T\> |
| `Infrastructure/Authorization/` | Custom authorization handlers |
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
GetByIdAsync, FindOneAsync, FindAsync, GetAllAsync, GetPagedAsync, AddAsync, UpdateAsync, DeleteAsync, ExistsAsync

**Database** — PostgreSQL via EF Core + Npgsql. AppDbContext quản lý DbSets, indexes, enum conversions.

**Pagination** — PaginationRequest (PageNumber, PageSize) + PaginationResponse\<T\> (Items, TotalCount, TotalPages, HasPreviousPage, HasNextPage)

**FluentValidation** — Auto-register validators qua DI. Lỗi trả về qua Result pattern (400).

**Mapster** — Mapping Entity ↔ DTO. Config tập trung tại `Application/Mapping/MappingConfig.cs`.

**CORS** — Cấu hình qua appsettings.json section `Cors`. Thêm origin mới chỉ sửa config.

**JWT Authentication** — Bearer token, cấu hình qua `Jwt` section. Swagger UI hỗ trợ Authorization header.

---

## Configuration & Secrets

### Cơ chế

Options pattern (`IOptions<T>`) — các class trong `Configuration/` bind từ `appsettings.json`, đăng ký tại `DependencyInjection.cs`.

### Phân loại

| Loại | Nơi lưu | Ví dụ |
|------|---------|-------|
| Config không nhạy cảm | `appsettings.json` | CORS origins, JWT Issuer/Audience, ExpirationInMinutes, base URLs |
| Secrets | `.env` / User Secrets (dev) / Env vars (prod) | JWT Secret, ConnectionString, API keys |

### Options classes

| Class | Section trong appsettings | Fields |
|-------|--------------------------|--------|
| `DatabaseOptions` | `Database` | ConnectionString |
| `JwtOptions` | `Jwt` | Secret, Issuer, Audience, ExpirationInMinutes |
| `CloudinaryOptions` | `Cloudinary` | CloudName, ApiKey, ApiSecret, Folder |
| `GeminiOptions` | `Gemini` | ApiKey, Model |
| `SecurityOptions` | `Security` | AllowPlaintextPassword, EnvironmentMode |
| `CorsOptions` | `Cors` | AllowedOrigins, AllowedMethods, AllowedHeaders, AllowCredentials |

### Setup .env (development)

```env
Database__ConnectionString=Host=localhost;Port=5432;Database=ai_tourism;Username=postgres;Password=your_password
Jwt__Secret=your-secret-key-at-least-32-characters-long
Cloudinary__ApiKey=your-key
Cloudinary__ApiSecret=your-secret
Gemini__ApiKey=your-key
```

---

## Middleware Pipeline (thứ tự trong Program.cs)

1. ExceptionMiddleware
2. RequestLoggingMiddleware
3. Swagger (Development only)
4. HTTPS Redirection
5. CORS
6. Authentication
7. Authorization
8. MapControllers

---

## NuGet Packages

- `FluentValidation` + `FluentValidation.DependencyInjectionExtensions`
- `Mapster` + `Mapster.DependencyInjection`
- `Swashbuckle.AspNetCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL` + `Microsoft.EntityFrameworkCore.Tools`
- `BCrypt.Net-Next`
- `Microsoft.AspNetCore.Authentication.JwtBearer` + `System.IdentityModel.Tokens.Jwt`
- `DotNetEnv`

---

## EF Core Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Khi thêm module mới

```
Application/DTOs/            → CreateFeatureRequest.cs, FeatureResponse.cs
Application/Services/        → IFeatureService.cs + FeatureService.cs
Application/Validators/      → CreateFeatureRequestValidator.cs
Domain/Entities/             → Feature.cs (thêm DbSet vào AppDbContext)
Controllers/                 → FeatureController.cs
```

Đăng ký DI tại `Infrastructure/DependencyInjection.cs`.

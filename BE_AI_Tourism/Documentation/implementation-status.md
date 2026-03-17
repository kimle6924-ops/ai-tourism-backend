# Implementation Status

Trạng thái các tính năng trong hệ thống.

**Ký hiệu:** Done | In Progress | Not Started | Planned

---

## Base Template

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Folder structure | Done | MVC + Layered Architecture |
| Result\<T\> pattern | Done | Shared/Core/Result.cs |
| BaseEntity | Done | Shared/Core/BaseEntity.cs |
| ExceptionMiddleware | Done | Middlewares/ExceptionMiddleware.cs |
| RequestLoggingMiddleware | Done | Middlewares/RequestLoggingMiddleware.cs |
| Configuration Options | Done | DatabaseOptions, JwtOptions, CloudinaryOptions, GeminiOptions, SecurityOptions, CorsOptions |
| CORS | Done | Cấu hình qua appsettings.json |
| FluentValidation setup | Done | Auto-register validators |
| Mapster setup | Done | MappingConfig.cs, DI registered |
| Pagination models | Done | PaginationRequest, PaginationResponse\<T\> |
| AppConstants | Done | Shared/Constants/AppConstants.cs |
| DI registration | Done | Infrastructure/DependencyInjection.cs |
| Health check endpoint | Done | GET /api/health |
| Swagger UI | Done | Development only, JWT Bearer support |
| IRepository\<T\> interface | Done | Domain/Interfaces/IRepository.cs |

## Phase 1: Foundation

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Domain Enums | Done | 9 enums: UserRole, AdministrativeLevel, ModerationStatus, EventStatus, ReviewStatus, ResourceType, ConversationStatus, MessageRole, UserStatus |
| Domain Entities | Done | 12 entities, User có RefreshToken/RefreshTokenExpiryTime |
| PostgreSQL + EF Core | Done | AppDbContext với Npgsql, snake_case tables, enum→string, array/jsonb support |
| EfRepository\<T\> | Done | Infrastructure/Database/EfRepository.cs (FindOneAsync, FindAsync) |
| Database Indexes | Done | Cấu hình trong AppDbContext.OnModelCreating() |
| .env setup | Done | Tất cả config keys |

## Phase 2: Auth & User + RBAC

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Auth DTOs | Done | RegisterRequest, LoginRequest, RefreshTokenRequest, AuthResponse |
| User DTOs | Done | UserResponse, UpdateUserRequest, UpdatePreferencesRequest, PreferencesResponse |
| Auth Validators | Done | RegisterRequestValidator, LoginRequestValidator |
| User Validators | Done | UpdateUserRequestValidator, UpdatePreferencesRequestValidator |
| PasswordService | Done | BCrypt hash/verify, AllowPlaintextPassword support |
| JwtService | Done | GenerateAccessToken, GenerateRefreshToken, GetPrincipalFromExpiredToken |
| AuthService | Done | Register, Login, RefreshToken |
| UserService | Done | GetCurrentUser, UpdateProfile, GetPreferences, UpdatePreferences |
| AdminUserService | Done | GetUsers (paged), LockUser, UnlockUser |
| JWT Authentication | Done | Program.cs — AddAuthentication().AddJwtBearer() |
| Authorization pipeline | Done | UseAuthentication + UseAuthorization |
| AuthController | Done | POST register/login/refresh — AllowAnonymous |
| UserController | Done | GET/PUT /me, GET/PUT /me/preferences — Authorize |
| AdminController | Done | GET users, PATCH lock/unlock — Authorize(Roles=Admin) |
| ScopeAuthorization | Done | ScopeRequirement + ScopeAuthorizationHandler |
| AppConstants Auth | Done | Auth error messages, JWT claim types |

---

## Business Modules

| Module | Trạng thái | Ghi chú |
|--------|-----------|---------|
| Auth & User (Phase 2) | Done | Register/Login/Refresh, RBAC |
| Administrative + Category (Phase 3) | Not Started | CRUD + Seed |
| Place/Event + Moderation (Phase 4) | Not Started | CRUD + workflow duyệt |
| Media Cloudinary (Phase 5) | Not Started | Upload/finalize/manage |
| Review + Discovery (Phase 6) | Not Started | CRUD review, list/filter/search |
| AI Chat (Phase 7) | Not Started | Gemini + context memory |
| Admin Stats (Phase 8) | Not Started | Dashboard API |
| Docs + Hardening (Phase 9) | Not Started | Swagger, rate limit, logging |

---

## Ghi chú

- Phase 1 Foundation: đã chuyển từ MongoDB sang PostgreSQL (EF Core + Npgsql).
- Phase 2 Auth & User + RBAC: hoàn thành.
- Tiếp theo: Phase 3 — Administrative + Category CRUD.

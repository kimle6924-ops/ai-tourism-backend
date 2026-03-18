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
| Administrative + Category (Phase 3) | Done | CRUD + Seed data (63 tỉnh/thành, categories) |
| Place/Event + Moderation (Phase 4) | Done | CRUD + workflow duyệt + scope check |
| Media Cloudinary (Phase 5) | Done | Upload signature/finalize, set-primary, reorder, delete |
| Review + Discovery (Phase 6) | Not Started | CRUD review, list/filter/search |
| AI Chat (Phase 7) | Not Started | Gemini + context memory |
| Admin Stats (Phase 8) | Not Started | Dashboard API |
| Docs + Hardening (Phase 9) | Not Started | Swagger, rate limit, logging |

---

## Phase 3: Administrative + Category

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Administrative DTOs | Done | CreateAdministrativeUnitRequest, UpdateAdministrativeUnitRequest, AdministrativeUnitResponse |
| Administrative Validators | Done | CreateAdministrativeUnitRequestValidator, UpdateAdministrativeUnitRequestValidator |
| AdministrativeUnitService | Done | CRUD, GetByLevel, GetChildren, hierarchy validation |
| AdministrativeUnitController | Done | GET (public), POST/PUT/DELETE (Admin only) |
| Category DTOs | Done | CreateCategoryRequest, UpdateCategoryRequest, CategoryResponse |
| Category Validators | Done | CreateCategoryRequestValidator, UpdateCategoryRequestValidator |
| CategoryService | Done | CRUD, GetActive, GetByType, slug uniqueness |
| CategoryController | Done | GET (public), POST/PUT/DELETE (Admin only) |
| Seed Data | Done | 63 tỉnh/thành, quận/huyện Đà Nẵng, phường Hải Châu, 28 categories (6 types) |
| AppConstants Phase 3 | Done | Administrative + Category error messages |

## Phase 4: Place/Event + Moderation

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Place DTOs | Done | CreatePlaceRequest, UpdatePlaceRequest, PlaceResponse |
| Place Validators | Done | CreatePlaceRequestValidator, UpdatePlaceRequestValidator |
| PlaceService | Done | CRUD, scope check, approved/all listing |
| PlaceController | Done | GET public (approved), POST/PUT/DELETE (Admin/Contributor with scope) |
| Event DTOs | Done | CreateEventRequest, UpdateEventRequest, EventResponse |
| Event Validators | Done | CreateEventRequestValidator, UpdateEventRequestValidator |
| EventService | Done | CRUD, scope check, approved/all listing, EventStatus management |
| EventController | Done | GET public (approved), POST/PUT/DELETE (Admin/Contributor with scope) |
| Moderation DTOs | Done | ModerationActionRequest, ModerationLogResponse |
| Moderation Validator | Done | ModerationActionRequestValidator |
| ModerationService | Done | Approve/Reject Place/Event, log actions, scope-based permission |
| ModerationController | Done | PATCH approve/reject, GET logs (Admin/Contributor) |
| ScopeService | Done | Reusable scope checking (IsInScopeAsync) |
| AppConstants Phase 4 | Done | Forbidden error message |

---

## Ghi chú

- Phase 1 Foundation: đã chuyển từ MongoDB sang PostgreSQL (EF Core + Npgsql).
- Phase 2 Auth & User + RBAC: hoàn thành.
- Phase 3 Administrative + Category: hoàn thành.
- Phase 4 Place/Event + Moderation: hoàn thành.
- Phase 5 Media Cloudinary: hoàn thành.
- Tiếp theo: Phase 6 — Review + Discovery.

## Phase 5: Media Cloudinary

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Media DTOs | Done | UploadSignatureRequest/Response, FinalizeUploadRequest, MediaAssetResponse, ReorderMediaRequest |
| Media Validators | Done | UploadSignatureRequestValidator, FinalizeUploadRequestValidator, ReorderMediaRequestValidator |
| CloudinaryProvider | Done | GenerateSignature (HMAC-SHA1), DestroyAsync |
| MediaService | Done | Signature, finalize, get by resource, set-primary, reorder, delete (DB + Cloudinary) |
| MediaController | Done | 6 endpoints, scope-based permission |
| Auto isPrimary | Done | Ảnh đầu tiên tự động là primary, xóa primary thì promote ảnh tiếp theo |
| NuGet package | Done | CloudinaryDotNet 1.28.0 |

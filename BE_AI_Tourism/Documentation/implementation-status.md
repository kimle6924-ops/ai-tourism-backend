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
| Domain Enums | Done | 10 enums: UserRole, AdministrativeLevel, ModerationStatus, EventStatus, ScheduleType, ReviewStatus, ResourceType, ConversationStatus, MessageRole, UserStatus (Active/Locked/PendingApproval) |
| Domain Entities | Done | 17 entities (bao gồm CommunityGroup/Post/PostMedia/Comment/Reaction), User có RefreshToken/RefreshTokenExpiryTime |
| PostgreSQL + EF Core | Done | AppDbContext với Npgsql, snake_case tables, enum→string, array/jsonb support |
| EfRepository\<T\> | Done | Infrastructure/Database/EfRepository.cs (FindOneAsync, FindAsync) |
| Database Indexes | Done | Cấu hình trong AppDbContext.OnModelCreating() |
| .env setup | Done | Tất cả config keys |

## Phase 2: Auth & User + RBAC

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Auth DTOs | Done | RegisterRequest, LoginRequest, RefreshTokenRequest, AuthResponse |
| User DTOs | Done | UserResponse, UpdateUserRequest, UpdateAccountRequest, FinalizeAvatarUploadRequest, AvatarUploadSignatureResponse, UpdatePreferencesRequest, PreferencesResponse |
| Auth Validators | Done | RegisterRequestValidator, LoginRequestValidator, RefreshTokenRequestValidator |
| User Validators | Done | UpdateUserRequestValidator, UpdateAccountRequestValidator, FinalizeAvatarUploadRequestValidator, UpdatePreferencesRequestValidator |
| PasswordService | Done | BCrypt hash/verify, AllowPlaintextPassword support |
| JwtService | Done | GenerateAccessToken, GenerateRefreshToken, GetPrincipalFromExpiredToken |
| AuthService | Done | Register, Login, RefreshToken |
| UserService | Done | GetCurrentUser, UpdateProfile, UpdateAccount (email duplicate check), Avatar upload signature/finalize, GetPreferences, UpdatePreferences |
| AdminUserService | Done | GetUsers (paged), LockUser, UnlockUser |
| JWT Authentication | Done | Program.cs — AddAuthentication().AddJwtBearer() |
| Authorization pipeline | Done | UseAuthentication + UseAuthorization |
| AuthController | Done | POST register/login/refresh — AllowAnonymous |
| UserController | Done | GET/PUT /me, PUT /me/account, POST /me/avatar/upload-signature, POST /me/avatar/finalize, GET/PUT /me/preferences — Authorize |
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
| Review + Discovery (Phase 6) | Done | CRUD review (rating bắt buộc, comment/image tùy chọn), search/filter places+events, simple search API, review stats |
| Community (Phase 5 AddDesign) | Done | 1 public group, post + media + comment + reaction |
| Leaderboard (Phase 2 mở rộng) | Done | Xếp hạng user theo điểm review Active |
| Seed toàn tỉnh (Phase 6 AddDesign) | Done | Seed idempotent theo toàn bộ đơn vị cấp Province hiện có: mỗi tỉnh 2 places + 2 events + review mẫu (rating luôn có, comment/image linh hoạt) và bổ sung bộ Sa Pa gốc theo title |
| AI Chat (Phase 7) | Done | Gemini streaming SSE + context memory (summary + key facts) |
| Admin Stats (Phase 8) | Done | Overview + daily time-series, aggregate query tối ưu DB-side |
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
| Event DTOs | Done | CreateEventRequest, UpdateEventRequest, EventResponse, EventOccurrenceResponse, EventOccurrencesQueryRequest (hỗ trợ recurrence) |
| Event Validators | Done | CreateEventRequestValidator, UpdateEventRequestValidator (validate theo ScheduleType) |
| EventService | Done | CRUD, scope check, recurrence + occurrences API, EventStatus động theo now |
| EventController | Done | GET public (approved), GET `/{id}/occurrences`, POST/PUT/DELETE (Admin/Contributor with scope), POST seed |
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
- Phase 5 (AddDesign) Community: hoàn thành.
- Phase 6 Review + Discovery: hoàn thành.
- Phase 6 (AddDesign) Seed toàn tỉnh + review mẫu: hoàn thành.
- Phase 7 AI Chat: hoàn thành.
- Phase 8 Admin Stats: đã hoàn thiện overview + time-series và tối ưu truy vấn aggregate.

## Phase 8: Admin Stats

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Stats DTOs | Done | Bổ sung `StatsRange`, `ModerationStats`, `TimeSeriesStats`, `DailyCountPoint`, query DTO `StatsOverviewQueryRequest` |
| AdminStatsService | Done | Aggregate query DB-side (COUNT/GROUP BY/AVG), không load full bảng vào memory |
| IAdminStatsService | Done | Contract cho stats service |
| AdminController endpoint | Done | `GET /api/admin/stats/overview` (Admin only) hỗ trợ `fromUtc`, `toUtc` |
| DI registration | Done | `IAdminStatsService -> AdminStatsService` |
| Query optimization | Done | Đã thay `GetAllAsync()` bằng aggregate query trực tiếp trên `AppDbContext` |

## Phase 7: AI Chat

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Chat DTOs | Done | CreateConversationRequest, ConversationResponse, SendMessageRequest, MessageResponse |
| Chat Validators | Done | CreateConversationRequestValidator, SendMessageRequestValidator |
| GeminiProvider | Done | GenerateContentAsync (non-stream), StreamContentAsync (SSE stream) |
| ChatService | Done | CRUD conversations, send message, stream message, context memory |
| ChatController | Done | 5 endpoints, SSE streaming endpoint |
| System Prompt | Done | Tiếng Việt, grounding data (places/events), user preferences |
| Context Memory | Done | Summary + Key Facts, auto-trigger mỗi 10 messages |
| Grounding | Done | Load approved places/events vào system prompt |
| SSE Streaming | Done | `text/event-stream`, real-time chunks |

## Phase 6: Review + Discovery

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Review DTOs | Done | CreateReviewRequest, UpdateReviewRequest, ReviewResponse, ReviewListResponse, ReviewHistoryItemResponse (`rating` bắt buộc trong create/update request) |
| Review Validators | Done | CreateReviewRequestValidator, UpdateReviewRequestValidator (`rating` bắt buộc 1-5, `comment`/`imageUrl` tùy chọn) |
| ReviewService | Done | Create/update/delete (owner+Admin), create/update mặc định `Active`, admin chuyển `Active/Hidden`, get by resource (kèm thống kê), get mine, get my history (tổng hợp) |
| ReviewController | Done | POST upload-signature, POST/PATCH/DELETE, GET by resource, GET mine, GET me/history |
| Leaderboard DTO | Done | UserLeaderboardItemResponse |
| Leaderboard Service | Done | Tính điểm từ review `Active` (image + rating + comment), phân trang + rank |
| Leaderboard Controller | Done | GET `/api/leaderboard/users` (public) |
| Discovery DTO | Done | DiscoveryRequest, SimpleSearchRequest, RecommendRequest, RecommendMixRequest, PlaceByLocationTagRequest (multi-tags), PlaceByTagsRequest, EventTimelineRequest, DiscoveryMixItemResponse |
| DiscoveryService | Done | Search places/events, simple search, recommend places/events, recommend mix, by-location-tag (multi-tags), by-tags, events timeline; chuẩn hóa lỗi `NO_LOCATION` cho API cần vị trí |
| DiscoveryController | Done | GET places, events, search/*, recommend/places, recommend/events, recommend/mix, places/by-location-tag, places/by-tags, events/timeline |

## Phase 5 (AddDesign): Community

| Tính năng | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| Community Entities | Done | CommunityGroup, CommunityPost, CommunityPostMedia, CommunityComment, CommunityReaction |
| Community DTOs | Done | Group/Post/Comment/Media responses + create/comment/react/upload/finalize requests |
| Community Validators | Done | Validate create post, comment, reaction, upload signature, finalize media |
| CommunityService | Done | Public group/posts, create post, get post, comment, reaction toggle, upload signature, finalize media |
| CommunityController | Done | `/api/community/group/public`, `/posts/*` đầy đủ theo AddDesign phase 5 |
| Seed public group | Done | Seed/init 1 group slug `public` idempotent trong SeedData |

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

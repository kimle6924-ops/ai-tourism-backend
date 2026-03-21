# Kế Hoạch Full Backend A-Z (ASP.NET Core 8 + PostgreSQL + EF Core + Cloudinary + Gemini)

## 1. Mục tiêu sản phẩm
- Xây backend cho hệ thống du lịch địa phương với 3 nhóm người dùng: `Admin`, `Contributor`, `User`.
- Contributor quản lý dữ liệu theo phân cấp hành chính: `Province`, `Ward` (2 cấp theo API v2 post-2025 merger).
- Frontend xử lý GPS, backend chỉ cung cấp dữ liệu để frontend lọc/đề xuất theo vị trí.
- AI Chatbot dùng Gemini, có truy xuất dữ liệu thực trong PostgreSQL và lưu context theo từng người dùng.
- Ảnh upload qua Cloudinary, PostgreSQL chỉ lưu link và metadata.
- Giai đoạn test/dev cho phép mật khẩu plain text có kiểm soát; production bắt buộc hash.

## 2. Kiến trúc tổng thể
- Kiến trúc giữ theo template hiện tại: `Controllers -> Application Services -> Repositories -> PostgreSQL`.
- Layer `Domain` chứa Entity, Enum, business rule model.
- Layer `Application` chứa DTO, Service interface/implementation, Validator, Mapper.
- Layer `Infrastructure` chứa PostgreSQL context/repositories, Cloudinary provider, Gemini provider.
- Response chuẩn hóa với `Result<T>`.
- Xử lý lỗi tập trung qua `ExceptionMiddleware`.
- Logging request/response qua `RequestLoggingMiddleware`.

## 3. Module nghiệp vụ cần có
- Module `Auth & User`.
- Module `Administrative Scope`.
- Module `Category`.
- Module `Place`.
- Module `Event`.
- Module `Media Asset` (Cloudinary).
- Module `Review`.
- Module `Moderation`.
- Module `Discovery` (list/filter/search, không làm GPS server-side).
- Module `AI Chat + Context Memory`.
- Module `Admin Statistics`.

## 4. Thiết kế CSDL PostgreSQL
- Table `users`.
- Table `administrative_units`.
- Table `categories`.
- Table `user_preferences`.
- Table `places`.
- Table `events`.
- Table `media_assets`.
- Table `reviews`.
- Table `moderation_logs`.
- Table `ai_conversations`.
- Table `ai_messages`.
- Table `ai_context_memory`.

### 4.1 `users`
- Trường: `_id`, `email`, `password`, `fullName`, `phone`, `avatarUrl`, `role`, `administrativeUnitId`, `status`, `createdAt`, `updatedAt`.
- Index: `email unique`, `role`, `administrativeUnitId`, `status`.

### 4.2 `administrative_units`
- Trường: `_id`, `name`, `level`, `parentId`, `code`, `createdAt`, `updatedAt`.
- Index: `code unique`, `level`, `parentId`.

### 4.3 `categories`
- Trường: `_id`, `name`, `slug`, `type`, `isActive`, `createdAt`, `updatedAt`.
- Index: `slug unique`, `type`, `isActive`.

### 4.4 `user_preferences`
- Trường: `_id`, `userId`, `categoryIds`, `updatedAt`.
- Index: `userId unique`, `categoryIds`.

### 4.5 `places`
- Trường: `_id`, `name`, `description`, `address`, `administrativeUnitId`, `categoryIds`, `tags`, `moderationStatus`, `createdBy`, `approvedBy`, `approvedAt`, `createdAt`, `updatedAt`.
- Index: `administrativeUnitId`, `categoryIds`, `moderationStatus`, text index `name+description+tags`.

### 4.6 `events`
- Trường: `_id`, `title`, `description`, `address`, `administrativeUnitId`, `categoryIds`, `tags`, `startAt`, `endAt`, `eventStatus`, `moderationStatus`, `createdBy`, `approvedBy`, `approvedAt`, `createdAt`, `updatedAt`.
- Index: `administrativeUnitId`, `categoryIds`, `startAt`, `endAt`, `eventStatus`, `moderationStatus`, text index.

### 4.7 `media_assets`
- Trường: `_id`, `resourceType`, `resourceId`, `url`, `secureUrl`, `publicId`, `format`, `mimeType`, `bytes`, `width`, `height`, `isPrimary`, `sortOrder`, `uploadedBy`, `createdAt`.
- Index: `(resourceType, resourceId)`, `(resourceId, isPrimary)`, `(resourceType, resourceId, sortOrder)`.

### 4.8 `reviews`
- Trường: `_id`, `resourceType`, `resourceId`, `userId`, `rating`, `comment`, `status`, `createdAt`, `updatedAt`.
- Index: `(resourceType, resourceId)`, `userId`, `status`.

### 4.9 `moderation_logs`
- Trường: `_id`, `resourceType`, `resourceId`, `action`, `note`, `actedBy`, `actedAt`.
- Index: `(resourceType, resourceId)`, `actedBy`, `actedAt`.

### 4.10 `ai_conversations`
- Trường: `_id`, `userId`, `title`, `model`, `status`, `lastMessageAt`, `createdAt`, `updatedAt`.
- Index: `(userId, lastMessageAt desc)`, `status`.

### 4.11 `ai_messages`
- Trường: `_id`, `conversationId`, `userId`, `role`, `content`, `tokenCount`, `citations`, `createdAt`.
- Index: `(conversationId, createdAt)`, `(userId, createdAt)`.

### 4.12 `ai_context_memory`
- Trường: `_id`, `userId`, `conversationId`, `summary`, `keyFacts`, `preferenceSnapshot`, `version`, `updatedAt`.
- Index: `(userId, updatedAt desc)`, `(conversationId, updatedAt desc)`.

## 5. Phân quyền và phạm vi dữ liệu
- `Admin` có toàn quyền toàn hệ thống.
- `Contributor` có quyền CRUD nội dung trong phạm vi hành chính được cấp.
- `User` chỉ xem dữ liệu đã duyệt và tạo/chỉnh review của chính mình.
- Rule scope:
- `Province` quản lý province của mình và các ward bên dưới.
- `Ward` quản lý dữ liệu ward của mình.
- Mọi thao tác sửa/xóa/duyệt đều kiểm tra `role` và `administrative scope` trong service layer.

## 6. Thiết kế API
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/users/me`
- `PUT /api/users/me`
- `PUT /api/users/me/preferences`
- `GET /api/admin/users`
- `PATCH /api/admin/users/{id}/lock`
- `PATCH /api/admin/users/{id}/unlock`
- `POST /api/places`
- `GET /api/places`
- `GET /api/places/{id}`
- `PUT /api/places/{id}`
- `DELETE /api/places/{id}`
- `POST /api/events`
- `GET /api/events`
- `GET /api/events/{id}`
- `PUT /api/events/{id}`
- `DELETE /api/events/{id}`
- `PATCH /api/moderation/{resourceType}/{id}/approve`
- `PATCH /api/moderation/{resourceType}/{id}/reject`
- `POST /api/reviews`
- `GET /api/reviews`
- `PATCH /api/reviews/{id}`
- `DELETE /api/reviews/{id}`
- `POST /api/media/upload-signature`
- `POST /api/media/finalize`
- `GET /api/media/by-resource`
- `PATCH /api/media/{id}/set-primary`
- `PATCH /api/media/reorder`
- `DELETE /api/media/{id}`
- `POST /api/chat/conversations`
- `GET /api/chat/conversations`
- `GET /api/chat/conversations/{id}/messages`
- `POST /api/chat/conversations/{id}/messages`
- `GET /api/admin/stats/overview`

## 7. Luồng lưu ảnh Cloudinary
- Backend cấp chữ ký upload qua `upload-signature`.
- Frontend upload file trực tiếp lên Cloudinary.
- Frontend gọi `finalize` gửi `publicId`, `secureUrl`, metadata file.
- Backend kiểm tra quyền trên `resourceId` rồi lưu `media_assets`.
- Backend đảm bảo 1 resource chỉ có 1 ảnh `isPrimary = true`.
- Xóa ảnh sẽ xóa metadata PostgreSQL và gọi Cloudinary destroy theo `publicId`.

## 8. Luồng AI Chat + Context
- User gửi message vào conversation.
- Backend nạp `summary` mới nhất từ `ai_context_memory`.
- Backend nạp 10-20 message gần nhất từ `ai_messages`.
- Backend truy vấn dữ liệu `places/events` đã duyệt để làm grounding.
- Backend tạo prompt cho Gemini gồm `system rule + context + dữ liệu nghiệp vụ`.
- Backend nhận response rồi lưu cả user message và assistant message.
- Mỗi 8-10 lượt chat tạo lại summary, upsert vào `ai_context_memory`.
- Context tách biệt tuyệt đối theo `userId`.

## 9. Cấu hình môi trường
- `DATABASE_URL` (PostgreSQL connection string)
- `Jwt:Secret`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpirationInMinutes`
- `Cloudinary:CloudName`
- `Cloudinary:ApiKey`
- `Cloudinary:ApiSecret`
- `Cloudinary:Folder`
- `Gemini:ApiKey`
- `Gemini:Model`
- `Security:AllowPlaintextPassword`
- `Security:EnvironmentMode`

## 10. Kế hoạch triển khai theo giai đoạn
- Giai đoạn 1: Foundation
- Hoàn thiện EF Core DbContext, base repository, index config.
- Hoàn thiện DI cho PostgreSQL, JWT, Cloudinary, Gemini.
- Giai đoạn 2: Auth + User + RBAC
- Register/login/refresh.
- Role policy + scope policy.
- Giai đoạn 3: Administrative + Category
- CRUD đơn vị hành chính và danh mục.
- Seed dữ liệu ban đầu.
- Giai đoạn 4: Place/Event + Moderation
- CRUD nội dung.
- Workflow duyệt và log duyệt.
- Giai đoạn 5: Media Cloudinary
- Upload-signature/finalize.
- Set primary, reorder, delete.
- Giai đoạn 6: Review + Discovery
- CRUD review.
- API list/filter/search cho places/events.
- Giai đoạn 7: AI Chat
- Conversation/messages/context memory.
- Tích hợp Gemini + grounding dữ liệu PostgreSQL.
- Giai đoạn 8: Admin Stats + Hardening
- Dashboard stats API.
- Rate limit, logging nâng cao, chuẩn hóa lỗi.
- Giai đoạn 9: Tài liệu + bàn giao
- Swagger hoàn chỉnh.
- Tài liệu DB, API, runbook vận hành.

## 11. Test plan
- Unit test cho service auth, scope check, moderation transitions, media logic, chat context logic.
- Integration test cho auth flow, CRUD modules, moderation, media finalize, chat flow.
- Security test cho unauthorized/forbidden theo role và scope.
- Data integrity test cho index unique, primary image uniqueness, moderation visibility.
- Performance smoke test cho list/filter/search và chat endpoint.

## 12. Tiêu chí nghiệm thu
- Người dùng đăng ký/đăng nhập thành công, nhận JWT hợp lệ.
- Contributor chỉ thao tác dữ liệu trong đúng phạm vi hành chính.
- Admin duyệt/từ chối được place/event và có log hành động.
- User chỉ thấy dữ liệu `Approved`.
- Ảnh upload Cloudinary thành công, PostgreSQL lưu link/metadata đúng.
- AI chat nhớ ngữ cảnh theo từng user/conversation qua nhiều lượt.
- Không có rò rỉ context giữa các user.

## 13. Kế hoạch chuyển từ test sang production
- Tắt `AllowPlaintextPassword`.
- Chạy migration script chuyển password cũ sang hash.
- Bật HTTPS bắt buộc và rotate toàn bộ secrets.
- Bật logging/audit mức production.
- Bật backup PostgreSQL định kỳ (pg_dump) và alert lỗi API/AI.

## 14. Giả định mặc định đã chốt
- Dùng PostgreSQL (EF Core + Npgsql) cho toàn bộ hệ thống.
- GPS không xử lý ở backend.
- Ảnh lưu Cloudinary, PostgreSQL chỉ lưu link/metadata.
- Gemini là LLM chính cho chatbot.
- Plain password chỉ tồn tại trong dev/test, không cho production.

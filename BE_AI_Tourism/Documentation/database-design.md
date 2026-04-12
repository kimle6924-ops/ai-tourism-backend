# Database Design

Thiết kế database cho hệ thống AI Tourism.

**Database:** PostgreSQL (via EF Core + Npgsql)

**Connection String:** Lưu trong `.env`
```
Host=localhost;Port=5432;Database=ai_tourism;Username=postgres;Password=your_password
```

---

## Base Entity

Mọi entity kế thừa `BaseEntity`:

| Field | Type | Mô tả |
|-------|------|-------|
| Id | Guid | Primary key |
| CreatedAt | DateTime | Thời điểm tạo |
| UpdatedAt | DateTime | Thời điểm cập nhật |

---

## Tables

### users

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| email | string | unique index | Email đăng nhập |
| password | string | required | Mật khẩu (hash hoặc plain tùy env) |
| full_name | string | required | Họ tên |
| phone | string | | Số điện thoại |
| avatar_url | string | | URL ảnh đại diện |
| avatar_public_id | string? | | Cloudinary public ID của avatar hiện tại |
| role | string (enum) | index | Admin / Contributor / User |
| administrative_unit_id | Guid? | index | Đơn vị hành chính (cho Contributor) |
| status | string (enum) | index | Active / Locked / PendingApproval |
| refresh_token | string? | | JWT refresh token |
| refresh_token_expiry_time | DateTime? | | Thời hạn refresh token |

### administrative_units

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| name | string | required | Tên đơn vị |
| level | string (enum) | index | Province / Ward |
| parent_id | Guid? | index | Đơn vị cha |
| code | string | unique index | Mã đơn vị |

### categories

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| name | string | required | Tên danh mục |
| slug | string | unique index | Slug URL |
| type | string | index | Loại danh mục |
| is_active | bool | index | Trạng thái hoạt động |

### user_preferences

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| user_id | Guid | unique index | FK → users |
| category_ids | Guid[] | | Danh sách sở thích (PostgreSQL array) |

### places

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| title | string | | Tiêu đề địa điểm |
| description | string | | Mô tả |
| address | string | | Địa chỉ |
| administrative_unit_id | Guid | index | FK → administrative_units |
| category_ids | Guid[] | | Danh mục (PostgreSQL array) |
| latitude | double? | | Vĩ độ (nullable) |
| longitude | double? | | Kinh độ (nullable) |
| tags | text[] | | Tags tìm kiếm (PostgreSQL array) |
| moderation_status | string (enum) | index | Pending / Approved / Rejected |
| created_by | Guid | | FK → users |
| approved_by | Guid? | | FK → users |
| approved_at | DateTime? | | Thời điểm duyệt |

### events

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| title | string | | Tiêu đề sự kiện |
| description | string | | Mô tả |
| address | string | | Địa chỉ |
| administrative_unit_id | Guid | index | FK → administrative_units |
| category_ids | Guid[] | | Danh mục (PostgreSQL array) |
| latitude | double? | | Vĩ độ (nullable) |
| longitude | double? | | Kinh độ (nullable) |
| tags | text[] | | Tags tìm kiếm (PostgreSQL array) |
| schedule_type | string (enum) | index | ExactDate / YearlyRecurring / MonthlyRecurring |
| start_at | DateTime? | index | Thời gian bắt đầu (dùng cho ExactDate) |
| end_at | DateTime? | index | Thời gian kết thúc (dùng cho ExactDate) |
| start_month | int? | | Tháng bắt đầu (dùng cho YearlyRecurring) |
| start_day | int? | | Ngày bắt đầu (dùng cho YearlyRecurring/MonthlyRecurring) |
| end_month | int? | | Tháng kết thúc (dùng cho YearlyRecurring) |
| end_day | int? | | Ngày kết thúc (dùng cho YearlyRecurring/MonthlyRecurring) |
| event_status | string (enum) | index | Upcoming / Ongoing / Ended |
| moderation_status | string (enum) | index | Pending / Approved / Rejected |
| created_by | Guid | | FK → users |
| approved_by | Guid? | | FK → users |
| approved_at | DateTime? | | Thời điểm duyệt |

### media_assets

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| resource_type | string (enum) | compound index | Place / Event |
| resource_id | Guid | compound index | FK → places/events |
| url | string | | URL Cloudinary |
| secure_url | string | | HTTPS URL |
| public_id | string | | Cloudinary public ID |
| format | string | | jpg, png, etc. |
| mime_type | string | | MIME type |
| bytes | long | | Kích thước file |
| width | int | | Chiều rộng |
| height | int | | Chiều cao |
| is_primary | bool | compound index | Ảnh chính |
| sort_order | int | compound index | Thứ tự sắp xếp |
| uploaded_by | Guid | | FK → users |

### reviews

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| resource_type | string (enum) | compound index | Place / Event |
| resource_id | Guid | compound index | FK → places/events |
| user_id | Guid | index | FK → users |
| rating | int? | | Điểm đánh giá (nullable) |
| comment | string? | | Nội dung đánh giá (nullable) |
| image_url | string? | | Ảnh review (nullable) |
| status | string (enum) | index | Active / Hidden |

### moderation_logs

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| resource_type | string (enum) | compound index | Place / Event |
| resource_id | Guid | compound index | FK → places/events |
| action | string | | Hành động (approve/reject) |
| note | string | | Ghi chú |
| acted_by | Guid | index | FK → users |
| acted_at | DateTime | index | Thời điểm thực hiện |

### community_groups

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| name | string | | Tên nhóm |
| slug | string | unique index | Slug nhóm (phase đầu: `public`) |
| description | string | | Mô tả nhóm |
| is_public | bool | index | Nhóm public/private |
| is_active | bool | index | Trạng thái hoạt động |

### community_posts

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| group_id | Guid | compound index | FK → community_groups |
| user_id | Guid | index | FK → users |
| content | string | | Nội dung bài viết/trải nghiệm |

### community_post_media

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| post_id | Guid | compound index | FK → community_posts |
| url | string | | URL media |
| secure_url | string | | HTTPS URL media |
| public_id | string | | Cloudinary public ID |
| format | string | | jpg, png, ... |
| mime_type | string | | MIME type |
| bytes | long | | Kích thước file |
| width | int | | Chiều rộng |
| height | int | | Chiều cao |
| sort_order | int | compound index | Thứ tự media trong post |

### community_comments

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| post_id | Guid | compound index | FK → community_posts |
| user_id | Guid | index | FK → users |
| content | string | | Nội dung bình luận |

### community_reactions

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| post_id | Guid | index + unique(post_id,user_id) | FK → community_posts |
| user_id | Guid | unique(post_id,user_id) | FK → users |
| reaction_type | string | | Loại reaction (ví dụ: like) |

### ai_conversations

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| user_id | Guid | compound index | FK → users |
| title | string | | Tiêu đề cuộc trò chuyện |
| model | string | | Model AI sử dụng |
| status | string (enum) | index | Active / Archived |
| last_message_at | DateTime | compound index | Tin nhắn cuối |

### ai_messages

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| conversation_id | Guid | compound index | FK → ai_conversations |
| user_id | Guid | compound index | FK → users |
| role | string (enum) | | User / Assistant / System |
| content | string | | Nội dung tin nhắn |
| token_count | int | | Số token |
| citations | text[] | | Trích dẫn nguồn (PostgreSQL array) |

### ai_context_memory

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| user_id | Guid | compound index | FK → users |
| conversation_id | Guid | compound index | FK → ai_conversations |
| summary | string | | Tóm tắt ngữ cảnh |
| key_facts | text[] | | Các sự kiện quan trọng (PostgreSQL array) |
| preference_snapshot | jsonb | | Snapshot sở thích user |
| version | int | | Phiên bản summary |

---

## Ghi chú

- Enums lưu dưới dạng **string** trong database (có `HasConversion<string>()`).
- `List<Guid>` và `List<string>` sử dụng PostgreSQL **native array** (Npgsql hỗ trợ tự động).
- `Dictionary<string, object>` sử dụng PostgreSQL **jsonb**.
- Table names sử dụng **snake_case** convention.
- Indexes được cấu hình trong `AppDbContext.OnModelCreating()`.
- Luồng seed phase 6 dùng toàn bộ `administrative_units` level `Province` hiện có để tạo dữ liệu idempotent: mỗi tỉnh 2 `places`, 2 `events`, mỗi resource có tối thiểu 1 media primary và 1 review mẫu `Active` (luôn có `rating`, còn `image_url`/`comment` được phối hợp linh hoạt).
- Migrations quản lý schema: `dotnet ef migrations add <Name>` → `dotnet ef database update`.

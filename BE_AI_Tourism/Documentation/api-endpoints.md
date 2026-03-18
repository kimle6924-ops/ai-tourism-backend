# API Endpoints

Danh sách các API endpoints trong hệ thống.

---

## Auth

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| POST | `/api/auth/register` | Đăng ký tài khoản | No |
| POST | `/api/auth/login` | Đăng nhập | No |
| POST | `/api/auth/refresh` | Refresh JWT token | No |

---

## User

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/user/me` | Lấy thông tin user hiện tại | Yes |
| PUT | `/api/user/me` | Cập nhật profile | Yes |
| GET | `/api/user/me/preferences` | Lấy sở thích | Yes |
| PUT | `/api/user/me/preferences` | Cập nhật sở thích | Yes |

---

## Admin

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/admin/users` | Danh sách users (phân trang) | Admin |
| PATCH | `/api/admin/users/{id}/lock` | Khóa tài khoản user | Admin |
| PATCH | `/api/admin/users/{id}/unlock` | Mở khóa tài khoản user | Admin |

---

## Administrative Units

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/administrative-units` | Danh sách đơn vị hành chính (phân trang) | No |
| GET | `/api/administrative-units/{id}` | Chi tiết đơn vị hành chính | No |
| GET | `/api/administrative-units/by-level/{level}` | Lấy theo cấp (Central/Province/Ward/Neighborhood) | No |
| GET | `/api/administrative-units/{id}/children` | Lấy đơn vị con | No |
| POST | `/api/administrative-units` | Tạo đơn vị hành chính | Admin |
| PUT | `/api/administrative-units/{id}` | Cập nhật đơn vị hành chính | Admin |
| DELETE | `/api/administrative-units/{id}` | Xóa đơn vị hành chính | Admin |

---

## Categories

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/categories` | Danh sách danh mục (phân trang) | No |
| GET | `/api/categories/active` | Danh sách danh mục đang hoạt động | No |
| GET | `/api/categories/by-type/{type}` | Lấy theo loại (tourism/food/entertainment/event/accommodation/shopping) | No |
| GET | `/api/categories/{id}` | Chi tiết danh mục | No |
| POST | `/api/categories` | Tạo danh mục | Admin |
| PUT | `/api/categories/{id}` | Cập nhật danh mục | Admin |
| DELETE | `/api/categories/{id}` | Xóa danh mục | Admin |

---

## Places

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/places` | Danh sách địa điểm đã duyệt (phân trang) | No |
| GET | `/api/places/all` | Tất cả địa điểm (phân trang) | Admin, Contributor |
| GET | `/api/places/{id}` | Chi tiết địa điểm | No |
| POST | `/api/places` | Tạo địa điểm | Admin, Contributor |
| PUT | `/api/places/{id}` | Cập nhật địa điểm | Admin, Contributor (scope) |
| DELETE | `/api/places/{id}` | Xóa địa điểm | Admin, Contributor (scope) |

---

## Events

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/events` | Danh sách sự kiện đã duyệt (phân trang) | No |
| GET | `/api/events/all` | Tất cả sự kiện (phân trang) | Admin, Contributor |
| GET | `/api/events/{id}` | Chi tiết sự kiện | No |
| POST | `/api/events` | Tạo sự kiện | Admin, Contributor |
| PUT | `/api/events/{id}` | Cập nhật sự kiện | Admin, Contributor (scope) |
| DELETE | `/api/events/{id}` | Xóa sự kiện | Admin, Contributor (scope) |

---

## Moderation

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| PATCH | `/api/moderation/{resourceType}/{id}/approve` | Duyệt Place/Event | Admin, Contributor (scope cấp trên) |
| PATCH | `/api/moderation/{resourceType}/{id}/reject` | Từ chối Place/Event | Admin, Contributor (scope cấp trên) |
| GET | `/api/moderation/{resourceType}/{id}/logs` | Lịch sử duyệt | Admin, Contributor |

> `resourceType`: `Place` hoặc `Event`

---

## Test / Dev

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/dbtest` | Test kết nối database | No |
| POST | `/api/dbtest/create-tables` | Tạo toàn bộ tables | No |
| POST | `/api/geminitest` | Test Gemini AI (body: `{"prompt": "..."}`) | No |

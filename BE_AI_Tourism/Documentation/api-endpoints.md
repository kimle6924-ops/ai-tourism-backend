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

## Test / Dev

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/dbtest` | Test kết nối database | No |
| POST | `/api/dbtest/create-tables` | Tạo toàn bộ tables | No |
| POST | `/api/geminitest` | Test Gemini AI (body: `{"prompt": "..."}`) | No |

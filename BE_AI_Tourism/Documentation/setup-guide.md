# Hướng dẫn cài đặt & chạy Backend

Hướng dẫn dành cho developer (frontend/backend) muốn chạy backend trên máy local.

---

## Yêu cầu

| Phần mềm | Phiên bản | Link tải |
|-----------|-----------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 |
| PostgreSQL | 15+ | https://www.postgresql.org/download/ |
| Git | bất kỳ | https://git-scm.com/ |

---

## Bước 1: Clone project

```bash
git clone <repo-url>
cd ai-tourism-backend
```

---

## Bước 2: Cài đặt PostgreSQL

### Windows
1. Tải PostgreSQL installer từ https://www.postgresql.org/download/windows/
2. Chạy installer, nhớ **ghi lại password** cho user `postgres`
3. Port mặc định: `5432`
4. Sau khi cài xong, mở **pgAdmin** (cài kèm PostgreSQL) hoặc dùng terminal

### Tạo database
Mở pgAdmin hoặc terminal PostgreSQL, chạy:
```sql
CREATE DATABASE ai_tourism;
```

Hoặc dùng command line:
```bash
psql -U postgres -c "CREATE DATABASE ai_tourism;"
```

---

## Bước 3: Cấu hình .env

Mở file `BE_AI_Tourism/.env` và điền thông tin:

```env
# PostgreSQL - điền password PostgreSQL của bạn
DATABASE_URL=Host=localhost;Port=5432;Database=ai_tourism;Username=postgres;Password=mat_khau_cua_ban

# JWT - giữ nguyên hoặc đổi tùy ý
JWT__SECRET=b8f4c9d8a61e3f5b4c2d9e7f81a6b3c9d0e2f4a6b8c1d3e5f7a9b0c2d4e6f8a2
JWT__ISSUER=AITourism
JWT__AUDIENCE=AITourism
JWT__EXPIRATIONINMINUTES=60

# Security - dev mode cho phép password plain text
SECURITY__ALLOWPLAINTEXTPASSWORD=true
SECURITY__ENVIRONMENTMODE=Development

# CORS - URL frontend
CORS__ALLOWEDORIGINS=http://localhost:3000
```

> **Lưu ý:** File `.env` chứa secrets, KHÔNG commit lên git. File `.env.example` là template mẫu.

---

## Bước 4: Chạy project

```bash
cd BE_AI_Tourism
dotnet run
```

Mặc định chạy tại: `https://localhost:5001` hoặc `http://localhost:5000`

---

## Bước 5: Tạo tables trong database

Sau khi project đã chạy, gọi API này để tạo toàn bộ tables:

```
POST http://localhost:5000/api/dbtest/create-tables
```

Hoặc mở Swagger UI tại `http://localhost:5000/swagger`, tìm **DbTest** → **POST /api/dbtest/create-tables** → Execute.

Kết quả thành công:
```json
{
  "success": true,
  "data": {
    "status": "OK",
    "message": "All tables created successfully"
  }
}
```

---

## Bước 6: Kiểm tra kết nối

```
GET http://localhost:5000/api/dbtest
```

Kết quả:
```json
{
  "success": true,
  "data": {
    "status": "Connected",
    "database": "ai_tourism"
  }
}
```

---

## Bước 7: Test nhanh Auth

### Đăng ký
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "123456",
  "fullName": "Test User",
  "phone": "0123456789"
}
```

### Đăng nhập
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "123456"
}
```

Response sẽ trả về `accessToken` — dùng token này cho các API cần auth.

---

## Swagger UI

Truy cập `http://localhost:5000/swagger` để xem và test tất cả API.

Để test API cần auth:
1. Đăng nhập lấy `accessToken`
2. Click nút **Authorize** (ổ khóa) trên Swagger
3. Nhập token (không cần prefix "Bearer ")
4. Click **Authorize** → Done

---

## Cấu trúc API hiện tại

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/dbtest` | Test kết nối database | No |
| POST | `/api/dbtest/create-tables` | Tạo tables | No |
| POST | `/api/geminitest` | Test Gemini AI | No |
| POST | `/api/auth/register` | Đăng ký | No |
| POST | `/api/auth/login` | Đăng nhập | No |
| POST | `/api/auth/refresh` | Refresh token | No |
| GET | `/api/user/me` | Thông tin user | Yes |
| PUT | `/api/user/me` | Cập nhật profile | Yes |
| GET | `/api/user/me/preferences` | Lấy sở thích | Yes |
| PUT | `/api/user/me/preferences` | Cập nhật sở thích | Yes |
| GET | `/api/admin/users` | Danh sách users | Admin |
| PATCH | `/api/admin/users/{id}/lock` | Khóa user | Admin |
| PATCH | `/api/admin/users/{id}/unlock` | Mở khóa user | Admin |

---

## Xử lý lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|------------|----------|
| Cannot connect to database | Sai connection string hoặc PostgreSQL chưa chạy | Kiểm tra `.env`, kiểm tra PostgreSQL service đang chạy |
| Database "ai_tourism" does not exist | Chưa tạo database | Chạy `CREATE DATABASE ai_tourism;` trong pgAdmin |
| 401 Unauthorized | Thiếu hoặc sai token | Đăng nhập lại lấy token mới |
| 403 Forbidden | Không đủ quyền (vd: User gọi API Admin) | Dùng account có role phù hợp |

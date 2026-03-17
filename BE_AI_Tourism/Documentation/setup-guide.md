# Hướng dẫn cài đặt & chạy Backend

Hướng dẫn dành cho developer (frontend/backend) muốn chạy backend trên máy local.

---

## Yêu cầu

| Phần mềm | Link tải |
|-----------|----------|
| Docker Desktop | https://www.docker.com/products/docker-desktop/ |
| Git | https://git-scm.com/ |

> Không cần cài .NET SDK hay PostgreSQL — Docker lo hết.

---

## Bước 1: Clone project

```bash
git clone <repo-url>
cd ai-tourism-backend
```

---

## Bước 2: Cấu hình .env

Hỏi BE để lấy file .env

## Bước 3: Chạy project

khởi động docker desktop sau đó chạy lệnh dưới trong dự án

```bash
docker compose up
```

Docker sẽ tự động:
1. Tải và khởi động PostgreSQL
2. Tạo database `ai_tourism`
3. Build và chạy backend

Lần đầu sẽ mất vài phút để tải image + build. Các lần sau chạy nhanh hơn.

Khi thấy dòng này là thành công:
```
api-1  | Now listening on: http://[::]:8080
```

Backend chạy tại: **http://localhost:5000**

---

## Bước 4: Tạo tables trong database

Sau khi project đã chạy, mở Swagger UI tại `http://localhost:5000/swagger`:

1. Tìm **DbTest** → **POST /api/dbtest/create-tables** → Execute
2. Kết quả thành công:
```json
{
  "success": true,
  "data": {
    "status": "OK",
    "message": "All tables created successfully"
  }
}
```

> Chỉ cần làm 1 lần. Data được lưu trong Docker volume, không mất khi restart.

---

## Bước 5: Test nhanh

### Kiểm tra kết nối database
```
GET http://localhost:5000/api/dbtest
```

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

Response trả về `accessToken` — dùng token này cho các API cần auth.

---

## Swagger UI

Truy cập `http://localhost:5000/swagger` để xem và test tất cả API.

Để test API cần auth:
1. Đăng nhập lấy `accessToken`
2. Click nút **Authorize** (ổ khóa) trên Swagger
3. Nhập token (không cần prefix "Bearer ")
4. Click **Authorize** → Done

---

## Các lệnh Docker thường dùng

| Lệnh | Mô tả |
|-------|-------|
| `docker compose up` | Chạy backend + database |
| `docker compose up -d` | Chạy ngầm (không chiếm terminal) |
| `docker compose up --build` | Build lại khi có thay đổi code |
| `docker compose down` | Dừng và xóa containers |
| `docker compose down -v` | Dừng + xóa cả database (reset toàn bộ) |
| `docker compose logs api` | Xem log backend |

---

## Cấu trúc API hiện tại

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| GET | `/api/dbtest` | Test kết nối database | No |
| POST | `/api/dbtest/create-tables` | Tạo tables | No |
| POST | `/api/geminitest` | Test Gemini AI (body: `{"prompt": "..."}`) | No |
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
| `docker_engine: The system cannot find the file` | Docker Desktop chưa chạy | Mở Docker Desktop, đợi "Running" |
| `POSTGRES_PASSWORD is not specified` | Volume cũ conflict | `docker compose down -v` rồi `docker compose up` |
| Swagger trả 404 | Backend chưa ở mode Development | Kiểm tra `ASPNETCORE_ENVIRONMENT=Development` trong docker-compose.yml |
| Cannot connect to database | Database chưa sẵn sàng | Đợi vài giây, thử lại |
| 401 Unauthorized | Thiếu hoặc sai token | Đăng nhập lại lấy token mới |
| 403 Forbidden | Không đủ quyền | Dùng account có role phù hợp |

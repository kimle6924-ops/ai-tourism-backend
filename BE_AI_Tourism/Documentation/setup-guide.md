# Hướng dẫn cài đặt & tích hợp Backend

Hướng dẫn dành cho developer (frontend/backend) muốn chạy backend trên máy local và tích hợp frontend.

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

Hỏi BE để lấy file `.env`. File cần có các key sau:

```env
# PostgreSQL
DATABASE_URL=Host=localhost;Port=5432;Database=ai_tourism;Username=postgres;Password=your_password

# JWT
JWT__SECRET=your_jwt_secret_key
JWT__ISSUER=AITourism
JWT__AUDIENCE=AITourism
JWT__EXPIRATIONINMINUTES=60

# Cloudinary (lấy từ https://cloudinary.com/console)
CLOUDINARY__CLOUDNAME=your_cloud_name
CLOUDINARY__APIKEY=your_api_key
CLOUDINARY__APISECRET=your_api_secret
CLOUDINARY__FOLDER=ai-tourism

# Gemini (lấy từ https://aistudio.google.com/apikey)
GEMINI__APIKEY=your_gemini_api_key
GEMINI__MODEL=gemini-2.0-flash

# Security
SECURITY__ALLOWPLAINTEXTPASSWORD=false
SECURITY__ENVIRONMENTMODE=Production

# CORS — thêm URL frontend vào đây
CORS__ALLOWEDORIGINS=http://localhost:3000
```

---

## Bước 3: Chạy project

Khởi động Docker Desktop, sau đó:

```bash
docker compose up
```

Docker sẽ tự động:
1. Tải và khởi động PostgreSQL
2. Tạo database `ai_tourism`
3. Build và chạy backend
4. Seed dữ liệu mẫu (63 tỉnh/thành, 28 danh mục)

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

# Hướng dẫn tích hợp Frontend

## Base URL & Response format

```
Base URL: http://localhost:5000/api
```

Mọi response đều có format:
```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "statusCode": 200
}
```

Khi lỗi:
```json
{
  "success": false,
  "data": null,
  "error": "Error message",
  "statusCode": 400
}
```

---

## Authentication (JWT)

### Luồng auth

```
1. POST /api/auth/register hoặc /api/auth/login
   → Nhận: { accessToken, refreshToken, expiresAt, user }

2. Lưu accessToken vào localStorage/cookie

3. Mọi request cần auth: thêm header
   Authorization: Bearer <accessToken>

4. Khi token hết hạn (401):
   POST /api/auth/refresh { refreshToken: "..." }
   → Nhận token mới
```

### Axios interceptor mẫu (React/Next.js)
```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      const refreshToken = localStorage.getItem('refreshToken');
      const { data } = await axios.post('/api/auth/refresh', { refreshToken });
      localStorage.setItem('accessToken', data.data.accessToken);
      localStorage.setItem('refreshToken', data.data.refreshToken);
      error.config.headers.Authorization = `Bearer ${data.data.accessToken}`;
      return axios(error.config);
    }
    return Promise.reject(error);
  }
);

export default api;
```

---

## Phân quyền (3 roles)

| Role | Mô tả |
|------|-------|
| `User` | Xem dữ liệu approved, tạo review, chat AI |
| `Contributor` | Tạo/sửa/xóa places/events trong phạm vi hành chính, upload ảnh |
| `Admin` | Toàn quyền: quản lý users, duyệt nội dung, CRUD mọi thứ |

Đăng ký với role:
```json
{
  "email": "contributor@test.com",
  "password": "123456",
  "fullName": "Contributor",
  "role": "Contributor",
  "administrativeUnitId": "<guid của đơn vị hành chính>"
}
```

---

## Upload ảnh (Cloudinary)

### Luồng upload 3 bước

```
Frontend                    Backend                     Cloudinary
   │                           │                            │
   │ 1. Lấy signature          │                            │
   │  POST /api/media/         │                            │
   │    upload-signature       │                            │
   │  { resourceType, id }     │                            │
   │──────────────────────────>│                            │
   │  { signature, timestamp,  │                            │
   │    apiKey, cloudName,     │                            │
   │    folder }               │                            │
   │<──────────────────────────│                            │
   │                                                        │
   │ 2. Upload trực tiếp lên Cloudinary                    │
   │  POST https://api.cloudinary.com/v1_1/{cloud}/upload  │
   │  FormData: file, signature, timestamp, api_key, folder │
   │───────────────────────────────────────────────────────>│
   │  { public_id, secure_url, format, bytes, ... }         │
   │<───────────────────────────────────────────────────────│
   │                                                        │
   │ 3. Lưu metadata           │                            │
   │  POST /api/media/finalize │                            │
   │  { resourceType, id,      │                            │
   │    publicId, secureUrl,   │                            │
   │    format, bytes, ... }   │                            │
   │──────────────────────────>│                            │
   │  { mediaAsset }           │                            │
   │<──────────────────────────│                            │
```

### Code mẫu upload (React)
```javascript
async function uploadImage(file, resourceType, resourceId) {
  // Bước 1: Lấy signature từ backend
  const { data: sig } = await api.post('/media/upload-signature', {
    resourceType,
    resourceId,
  });

  // Bước 2: Upload lên Cloudinary
  const formData = new FormData();
  formData.append('file', file);
  formData.append('signature', sig.data.signature);
  formData.append('timestamp', sig.data.timestamp);
  formData.append('api_key', sig.data.apiKey);
  formData.append('folder', sig.data.folder);

  const cloudinaryUrl = `https://api.cloudinary.com/v1_1/${sig.data.cloudName}/image/upload`;
  const { data: cloudRes } = await axios.post(cloudinaryUrl, formData);

  // Bước 3: Lưu metadata vào backend
  const { data: media } = await api.post('/media/finalize', {
    resourceType,
    resourceId,
    publicId: cloudRes.public_id,
    url: cloudRes.url,
    secureUrl: cloudRes.secure_url,
    format: cloudRes.format,
    mimeType: `image/${cloudRes.format}`,
    bytes: cloudRes.bytes,
    width: cloudRes.width,
    height: cloudRes.height,
  });

  return media.data;
}
```

### Quản lý ảnh
```
GET    /api/media/by-resource?resourceType=Place&resourceId=xxx  → Lấy danh sách ảnh
PATCH  /api/media/{id}/set-primary                               → Đặt ảnh chính
PATCH  /api/media/reorder  { orderedIds: [id1, id2, id3] }       → Sắp xếp thứ tự
DELETE /api/media/{id}                                           → Xóa ảnh
```

---

## AI Chat (SSE Streaming)

### Luồng chat

```
1. Tạo conversation
   POST /api/chat/conversations { title: "..." }
   → { id, title, model, ... }

2. Gửi tin nhắn (streaming)
   POST /api/chat/conversations/{id}/messages/stream
   { content: "Chiều nay rảnh, đề xuất gì đi" }

3. Nhận response qua SSE (Server-Sent Events)
   data: {"content":"Chiều"}
   data: {"content":" nay"}
   data: {"content":" bạn"}
   data: {"content":" có thể..."}
   data: [DONE]
```

### Code mẫu SSE (React)
```javascript
async function sendMessageStream(conversationId, content, onChunk, onDone) {
  const token = localStorage.getItem('accessToken');

  const response = await fetch(
    `http://localhost:5000/api/chat/conversations/${conversationId}/messages/stream`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify({ content }),
    }
  );

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (!line.startsWith('data: ')) continue;
      const data = line.slice(6);

      if (data === '[DONE]') {
        onDone();
        return;
      }

      try {
        const parsed = JSON.parse(data);
        onChunk(parsed.content);
      } catch {}
    }
  }
}

// Sử dụng
const [message, setMessage] = useState('');

sendMessageStream(
  conversationId,
  'Chiều nay rảnh, đề xuất gì đi',
  (chunk) => setMessage(prev => prev + chunk),  // Mỗi chunk append vào
  () => console.log('Done!')
);
```

### Non-streaming (đơn giản hơn)
```javascript
// Nếu không muốn streaming, dùng endpoint này:
const { data } = await api.post(
  `/chat/conversations/${conversationId}/messages`,
  { content: 'Chiều nay rảnh, đề xuất gì đi' }
);
// data.data = { id, role, content, ... } — response đầy đủ
```

### Lấy lịch sử chat
```javascript
// Danh sách conversations
const { data } = await api.get('/chat/conversations');

// Tin nhắn trong 1 conversation (phân trang, mới nhất trước)
const { data } = await api.get(`/chat/conversations/${id}/messages?pageNumber=1&pageSize=20`);
```

---

## Discovery (Tìm kiếm / Lọc)

### Tìm kiếm địa điểm
```
GET /api/discovery/places?search=biển&categoryId=xxx&administrativeUnitId=xxx&tag=cafe&sortBy=rating&pageNumber=1&pageSize=10
```

### Tìm kiếm sự kiện
```
GET /api/discovery/events?search=lễ hội&sortBy=startdate&pageNumber=1&pageSize=10
```

### Các giá trị sortBy

| Giá trị | Mô tả |
|---------|-------|
| `newest` | Mới nhất (mặc định) |
| `oldest` | Cũ nhất |
| `rating` | Đánh giá cao nhất |
| `name` | Theo tên A-Z |
| `startdate` | Theo ngày bắt đầu (chỉ events) |

---

## Review (Đánh giá)

### Tạo/cập nhật review (upsert)
Mỗi user chỉ có 1 review cho mỗi place/event. Gọi lại sẽ đè review cũ.

```javascript
await api.post('/reviews', {
  resourceType: 'Place',  // hoặc 'Event'
  resourceId: '<place-id>',
  rating: 5,              // 1-5
  comment: 'Rất tuyệt!'
});
```

### Lấy reviews
```
GET /api/reviews?resourceType=Place&resourceId=xxx    → Tất cả reviews (public)
GET /api/reviews/mine?resourceType=Place&resourceId=xxx → Review của mình
```

---

## Phân trang

Mọi endpoint có phân trang đều dùng chung format:

**Request:**
```
?pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Enum values

Frontend cần biết các giá trị enum khi gọi API:

| Enum | Giá trị |
|------|---------|
| UserRole | `User`, `Contributor`, `Admin` |
| AdministrativeLevel | `Province`, `Ward` |
| ModerationStatus | `Pending`, `Approved`, `Rejected` |
| EventStatus | `Upcoming`, `Ongoing`, `Ended` |
| ReviewStatus | `Active`, `Hidden`, `Deleted` |
| ResourceType | `Place`, `Event` |
| ConversationStatus | `Active`, `Archived` |
| MessageRole | `User`, `Assistant`, `System` |

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
| CORS blocked | Frontend URL chưa được allow | Thêm URL vào `CORS__ALLOWEDORIGINS` trong .env |
| SSE không nhận data | Thiếu header hoặc sai Content-Type | Dùng `fetch` thay vì `axios` cho SSE |

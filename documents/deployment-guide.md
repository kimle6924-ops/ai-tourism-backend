# Hướng dẫn Deploy AI Tourism (Free Tier)

> **Phương án:** Frontend (Vercel) + Backend (Render) + Database (Neon PostgreSQL)

## Mục lục

- [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
- [Bước 1: Tạo Database trên Neon](#bước-1-tạo-database-trên-neon)
- [Bước 2: Deploy Backend trên Render](#bước-2-deploy-backend-trên-render)
- [Bước 3: Deploy Frontend trên Vercel](#bước-3-deploy-frontend-trên-vercel)
- [Bước 4: Cấu hình kết nối](#bước-4-cấu-hình-kết-nối)
- [Bước 5: Kiểm tra & Xử lý lỗi](#bước-5-kiểm-tra--xử-lý-lỗi)
- [Lưu ý quan trọng](#lưu-ý-quan-trọng)

---

## Tổng quan kiến trúc

```
┌─────────────────┐     HTTPS      ┌──────────────────┐     TCP/SSL     ┌─────────────────┐
│   Vercel (FE)   │ ──────────────>│   Render (BE)    │ ──────────────> │  Neon (Postgres) │
│  React 19 SPA   │   /api/*       │  .NET 8 API      │                 │  PostgreSQL 16   │
│  CDN toàn cầu   │                │  Port 8080       │                 │  Free 0.5GB      │
└─────────────────┘                └──────────────────┘                 └─────────────────┘
     *.vercel.app                    *.onrender.com                      *.neon.tech
```

**Yêu cầu trước khi bắt đầu:**
- Tài khoản GitHub (push cả 2 repo BE và FE lên GitHub)
- Tài khoản Cloudinary (đã có - giữ nguyên API keys)
- Tài khoản Google AI Studio (đã có - giữ nguyên Gemini API key)

---

## Bước 1: Tạo Database trên Neon

### 1.1. Đăng ký tài khoản

1. Truy cập [https://neon.tech](https://neon.tech)
2. Đăng ký bằng GitHub (nhanh nhất)
3. Chọn **Free Tier** (0.5 GB storage, không giới hạn thời gian)

### 1.2. Tạo Project & Database

1. Click **"New Project"**
2. Điền thông tin:
   - **Project name:** `ai-tourism`
   - **Database name:** `ai_tourism`
   - **Region:** `Singapore` (gần Việt Nam nhất)
   - **Postgres version:** `16`
3. Click **"Create Project"**

### 1.3. Lấy Connection String

Sau khi tạo xong, Neon sẽ hiển thị connection string. Copy lại dạng:

```
postgresql://username:password@ep-xxx-xxx-123456.ap-southeast-1.aws.neon.tech/ai_tourism?sslmode=require
```

**Chuyển đổi sang format cho .NET:**

```
Host=ep-xxx-xxx-123456.ap-southeast-1.aws.neon.tech;Port=5432;Database=ai_tourism;Username=username;Password=password;SSL Mode=Require;Trust Server Certificate=true
```

> Lưu lại connection string này, sẽ dùng ở Bước 2.

---

## Bước 2: Deploy Backend trên Render

### 2.1. Đăng ký tài khoản

1. Truy cập [https://render.com](https://render.com)
2. Đăng ký bằng GitHub

### 2.2. Push code lên GitHub

Đảm bảo repo BE đã được push lên GitHub (public hoặc private đều được).

```bash
# Nếu chưa có remote
cd ai-tourism-backend
git remote add origin https://github.com/<username>/ai-tourism-backend.git
git push -u origin main
```

### 2.3. Tạo Web Service

1. Vào Render Dashboard → Click **"New +"** → **"Web Service"**
2. Chọn **"Build and deploy from a Git repository"**
3. Connect repo `ai-tourism-backend`
4. Cấu hình:

| Trường | Giá trị |
|--------|---------|
| **Name** | `ai-tourism-api` |
| **Region** | `Singapore` (gần Neon DB nhất) |
| **Branch** | `main` |
| **Runtime** | `Docker` |
| **Dockerfile Path** | `./BE_AI_Tourism/Dockerfile` |
| **Docker Context Directory** | `./BE_AI_Tourism` |
| **Instance Type** | **Free** |

### 2.4. Thiết lập Environment Variables

Trong phần **"Environment Variables"**, thêm các biến sau:

```env
# App
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Database (connection string từ Neon - Bước 1.3)
DATABASE_URL=Host=ep-xxx.ap-southeast-1.aws.neon.tech;Port=5432;Database=ai_tourism;Username=xxx;Password=xxx;SSL Mode=Require;Trust Server Certificate=true

# JWT
JWT__SECRET=<chuỗi-secret-dài-ít-nhất-32-ký-tự>
JWT__ISSUER=AITourism
JWT__AUDIENCE=AITourism
JWT__EXPIRATIONINMINUTES=120

# Cloudinary (giữ nguyên keys hiện tại)
CLOUDINARY__CLOUDNAME=<your-cloud-name>
CLOUDINARY__APIKEY=<your-api-key>
CLOUDINARY__APISECRET=<your-api-secret>
CLOUDINARY__FOLDER=ai-tourism

# Gemini AI
GEMINI__APIKEY=<your-gemini-api-key>
GEMINI__MODEL=gemini-2.0-flash

# Security
SECURITY__ALLOWPLAINTEXTPASSWORD=false
SECURITY__ENVIRONMENTMODE=Production

# CORS (sẽ cập nhật sau khi có domain Vercel ở Bước 3)
CORS__ALLOWEDORIGINS=https://<your-app>.vercel.app
```

### 2.5. Deploy

1. Click **"Create Web Service"**
2. Render sẽ tự động build Docker image và deploy
3. Chờ build xong (~5-10 phút lần đầu)
4. Sau khi deploy thành công, bạn sẽ có URL dạng: `https://ai-tourism-api.onrender.com`

### 2.6. Kiểm tra Backend

Truy cập `https://ai-tourism-api.onrender.com/swagger` để kiểm tra API hoạt động.

> **Lưu ý:** Lần đầu truy cập sẽ mất ~30-60s do server cold start (free tier).

---

## Bước 3: Deploy Frontend trên Vercel

### 3.1. Chuẩn bị code FE

#### 3.1.1. Cập nhật `src/config/config.ts`

Thay đổi để đọc API URL từ environment variable:

```typescript
const config = {
  baseUrl: import.meta.env.VITE_API_URL || '',
};

export default config;
```

#### 3.1.2. Thêm file `vercel.json` ở thư mục gốc FE

```json
{
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

File này cấu hình SPA fallback routing (tương tự `try_files` trong Nginx).

#### 3.1.3. Push code lên GitHub

```bash
cd FE_Tourism
git add -A
git commit -m "config for Vercel deployment"
git push origin main
```

### 3.2. Đăng ký & Deploy trên Vercel

1. Truy cập [https://vercel.com](https://vercel.com)
2. Đăng ký bằng GitHub
3. Click **"Add New..."** → **"Project"**
4. Import repo FE từ GitHub
5. Cấu hình:

| Trường | Giá trị |
|--------|---------|
| **Framework Preset** | `Vite` |
| **Root Directory** | `.` (hoặc đường dẫn đến thư mục FE nếu monorepo) |
| **Build Command** | `npm run build` |
| **Output Directory** | `dist` |

### 3.3. Thiết lập Environment Variables

Trong phần **"Environment Variables"**:

```env
VITE_API_URL=https://ai-tourism-api.onrender.com
```

> **Quan trọng:** Biến env cho Vite **bắt buộc** phải có prefix `VITE_` mới được inject vào client code.

### 3.4. Deploy

1. Click **"Deploy"**
2. Vercel sẽ tự build và deploy (~1-2 phút)
3. Sau khi xong, bạn sẽ có URL dạng: `https://ai-tourism-xxx.vercel.app`

---

## Bước 4: Cấu hình kết nối

### 4.1. Cập nhật CORS trên Render

Sau khi có domain Vercel, quay lại Render Dashboard:

1. Vào service `ai-tourism-api` → **Environment**
2. Cập nhật biến `CORS__ALLOWEDORIGINS`:

```
CORS__ALLOWEDORIGINS=https://ai-tourism-xxx.vercel.app
```

> Nếu cần nhiều origins (ví dụ thêm custom domain), phân cách bằng dấu `,`:
> ```
> CORS__ALLOWEDORIGINS=https://ai-tourism-xxx.vercel.app,https://custom-domain.com
> ```

3. Render sẽ tự động redeploy sau khi thay đổi env.

### 4.2. Seed dữ liệu

Nếu database Neon trống, bạn có thể:

**Cách 1:** Đổi tạm `ASPNETCORE_ENVIRONMENT=Development` trên Render để app tự seed data khi khởi động, sau đó đổi lại `Production`.

**Cách 2:** Dùng Neon SQL Editor (trên web) để chạy script seed thủ công.

---

## Bước 5: Kiểm tra & Xử lý lỗi

### 5.1. Checklist kiểm tra

- [ ] Truy cập `https://<be-url>/swagger` → Swagger UI hiện
- [ ] Truy cập `https://<fe-url>` → Giao diện FE hiện
- [ ] Đăng ký/Đăng nhập hoạt động
- [ ] Xem danh sách địa điểm/sự kiện
- [ ] Upload ảnh (Cloudinary)
- [ ] Chat AI (Gemini)

### 5.2. Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| FE gọi API bị **CORS error** | CORS chưa cấu hình đúng | Kiểm tra `CORS__ALLOWEDORIGINS` trên Render có đúng domain Vercel không (phải có `https://`) |
| FE gọi API bị **Network Error** | `VITE_API_URL` sai | Kiểm tra biến env trên Vercel, redeploy sau khi sửa |
| BE trả **500 Internal Server Error** | DB connection lỗi | Kiểm tra connection string Neon, thêm `SSL Mode=Require;Trust Server Certificate=true` |
| BE build fail trên Render | Dockerfile path sai | Đảm bảo Dockerfile Path = `./BE_AI_Tourism/Dockerfile`, Docker Context = `./BE_AI_Tourism` |
| Trang trắng sau deploy FE | SPA routing lỗi | Kiểm tra `vercel.json` có rewrite rule không |
| Cold start quá lâu (~60s) | Render free tier sleep | Bình thường, dùng [UptimeRobot](https://uptimerobot.com) ping mỗi 14 phút để giữ server (xem mục 6) |

### 5.3. Xem logs

- **Render:** Dashboard → Service → Logs (realtime)
- **Vercel:** Dashboard → Project → Deployments → Functions logs
- **Neon:** Dashboard → Monitoring

---

## Lưu ý quan trọng

### Free Tier Limits

| Service | Giới hạn | Lưu ý |
|---------|----------|-------|
| **Vercel** | 100GB bandwidth/tháng, 6000 build phút | Quá đủ cho project học tập |
| **Render** | 750 giờ/tháng, sleep sau 15 phút idle | Đủ cho 1 service chạy liên tục 24/7 |
| **Neon** | 0.5 GB storage, 190 compute hours/tháng | Đủ cho vài nghìn records |

### Chống Server Sleep bằng UptimeRobot (BẮT BUỘC)

Render free tier sẽ **tắt server sau 15 phút** không có request. Dùng UptimeRobot (free) để ping endpoint `/api/health/ping` giữ server luôn sống.

> Backend đã có sẵn endpoint `GET /api/health/ping` → trả về `{ status: "alive", timestamp: "..." }`, rất nhẹ, không tốn tài nguyên.

#### Hướng dẫn setup UptimeRobot

1. Truy cập [https://uptimerobot.com](https://uptimerobot.com) → **Register for FREE**
2. Đăng ký bằng email (hoặc Google)
3. Xác nhận email → Đăng nhập vào Dashboard
4. Click **"+ Add New Monitor"** (nút xanh lá, góc trên bên trái)
5. Điền thông tin:

   | Trường | Giá trị |
   |--------|---------|
   | **Monitor Type** | `HTTP(s)` |
   | **Friendly Name** | `AI Tourism API - Keep Alive` |
   | **URL (or IP)** | `https://ai-tourism-api.onrender.com/api/health/ping` |
   | **Monitoring Interval** | `Every 5 minutes` *(chọn 5 phút cho chắc, free tier cho phép)* |

6. Phần **"Alert Contacts To Notify"** → tick email của bạn (sẽ nhận thông báo khi server down)
7. Click **"Create Monitor"**

#### Kiểm tra hoạt động

- Sau khi tạo, UptimeRobot sẽ bắt đầu ping ngay lập tức
- Vào Dashboard → monitor vừa tạo → kiểm tra status hiện **"Up"** (xanh lá)
- Tab **"Response Times"** sẽ hiển thị thời gian phản hồi mỗi lần ping
- Nếu status **"Down"** (đỏ) → kiểm tra lại URL hoặc xem Render logs

#### Lưu ý

- UptimeRobot free tier: **50 monitors**, interval tối thiểu **5 phút** → quá đủ
- Nếu server vẫn cold start lần đầu sau deploy: bình thường, chờ UptimeRobot ping lần đầu
- Render free tier giới hạn **750 giờ/tháng** (~31 ngày liên tục) → đủ chạy 1 service 24/7

### Custom Domain (tùy chọn)

Nếu có tên miền riêng:

- **Vercel:** Settings → Domains → Add domain → Trỏ DNS (CNAME)
- **Render:** Settings → Custom Domains → Add domain → Trỏ DNS

### Bảo mật Production

- `SECURITY__ALLOWPLAINTEXTPASSWORD` phải là `false`
- `SECURITY__ENVIRONMENTMODE` phải là `Production`
- `JWT__SECRET` phải đủ dài (>= 32 ký tự) và random
- Không commit file `.env` lên GitHub

---

## Tóm tắt các URL sau khi deploy

| Service | URL |
|---------|-----|
| Frontend | `https://ai-tourism-xxx.vercel.app` |
| Backend API | `https://ai-tourism-api.onrender.com` |
| Swagger Docs | `https://ai-tourism-api.onrender.com/swagger` |
| Database | Neon Dashboard → `console.neon.tech` |
| Monitoring | UptimeRobot Dashboard |

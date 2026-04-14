# AddDesign - Thiết kế triển khai tổng hợp (đã áp dụng 10 ý chốt)

Mục tiêu: đây là tài liệu triển khai chuẩn để AI/coder đọc vào và thực hiện đúng yêu cầu. Bao gồm hiện trạng, gap, thiết kế API/DB, lộ trình làm dần và ràng buộc đã chốt.

## Trạng thái thực thi
- Phase 1: **Done** (hoàn thành ngày 2026-04-12)
- Phase 2: **Done** (hoàn thành ngày 2026-04-12)
- Phase 3: **Done** (hoàn thành ngày 2026-04-12)
- Phase 4: **Done** (hoàn thành ngày 2026-04-12)
- Phase 5: **Done** (hoàn thành ngày 2026-04-12)
- Phase 6: Not Started

---

## A. 10 quyết định đã chốt (ưu tiên cao nhất)

1. Không thêm `username`, hệ thống dùng `email` để đăng nhập.
2. Cho phép đổi `email` trong profile, có check trùng và trả lỗi đầy đủ (`409`, `EMAIL_ALREADY_EXISTS`).
3. Avatar user triển khai nhanh bằng Cloudinary signature/finalize, lưu `avatarUrl + avatarPublicId` trên `users`.
4. Review hợp lệ khi có ít nhất 1 trong 3: `image` hoặc `rating` hoặc `comment`.
5. Leaderboard chỉ tính review `Active`.
6. API mix event+place không ép tỉ lệ, xếp theo điểm tổng.
7. API yêu cầu vị trí user: user chưa lưu vị trí thì trả lỗi rõ ràng (`400`, ví dụ `NO_LOCATION`).
8. Event recurrence hỗ trợ đủ 3 loại: `ExactDate`, `YearlyRecurring`, `MonthlyRecurring`.
9. Community phase đầu chỉ cần 1 group public chung.
10. Seed toàn bộ tỉnh theo danh sách administrative units hiện có; đầy đủ tỉnh, đúng `administrativeUnitId`, dữ liệu đẹp, idempotent.

---

## B. Đánh giá hiện trạng dự án (as-is)

### B1. User account
- `PUT /api/user/me` hiện chỉ cập nhật `fullName`, `phone`, `avatarUrl`.
- Chưa cho đổi email tại profile.
- Chưa có duplicate-check email trong update profile.

### B2. Review
- Đã có `POST/PATCH/DELETE /api/reviews` và `GET /api/reviews/mine`.
- `GET /api/reviews/mine` đang lọc theo từng resource, chưa có lịch sử tổng user.
- Model review hiện chưa có ảnh.
- `rating` đang bắt buộc, `comment` chưa thiết kế nullable theo logic mới.

### B3. Discovery
- Đã có filter/search cho places/events và recommend riêng cho place/event.
- Chưa có endpoint mix place+event 9 item.
- Chưa có endpoint place theo vị trí+tag 16 item theo rule mới.
- Chưa có endpoint event timeline ongoing/upcoming đúng yêu cầu.

### B4. Event
- Event đang dùng `startAt/endAt` cố định, chưa có recurrence.

### B5. Community
- Chưa có module community/forum.

### B6. Leaderboard
- Chưa có API xếp hạng user theo chất lượng review.

### B7. Seed
- Có seed places/events mẫu (khu vực Sa Pa), chưa đạt yêu cầu seed toàn bộ tỉnh với review mẫu liên kết user.

---

## C. Thiết kế mục tiêu (to-be)

## C1. User: đổi email + avatar upload

### C1.1 API đổi account
- Endpoint: `PUT /api/user/me/account`
- Body:
  - `email?`
  - `fullName?`
  - `phone?`
- Rule:
  - Nếu có `email`: normalize `trim + lowercase`, check trùng user khác.
  - Trùng -> `409 EMAIL_ALREADY_EXISTS`.
- Response: `Result<UserResponse>`.

### C1.2 API avatar
- `POST /api/user/me/avatar/upload-signature`
- `POST /api/user/me/avatar/finalize`
- Lưu vào `users`:
  - `avatar_url`
  - `avatar_public_id` (mới)

### C1.3 DB change
- `users` thêm `avatar_public_id` (nullable string).

---

## C2. Review: image + rule 1/3 + history tổng

### C2.1 DB change
- `reviews` thêm `image_url` (nullable).
- `rating` -> nullable.
- `comment` -> nullable.

### C2.2 Validation mới
- Create/Update review hợp lệ nếu thỏa:
  - có `imageUrl`, hoặc
  - có `rating` (1..5), hoặc
  - có `comment` (không rỗng, <=1000).
- Nếu cả 3 rỗng -> 400 validation fail.

### C2.3 API history tổng
- `GET /api/reviews/me/history`
- Query:
  - `pageNumber`, `pageSize`
  - `resourceType?`
  - `status?` (nếu cần)
- Trả toàn bộ review của user, mới nhất trước.
- Mỗi item nên enrich thêm:
  - `resourceTitle`
  - `resourceAddress`
  - `resourceImage`

---

## C3. Leaderboard user

### C3.1 Công thức điểm
- Mỗi review `Active`:
  - có image: +1
  - có rating: +1
  - có comment: +1
- `reviewScore` từ 0..3.
- `userTotalScore = sum(reviewScore)`.

### C3.2 API
- `GET /api/leaderboard/users`
- Query: `pageNumber`, `pageSize`.
- Response item:
  - `rank`
  - `userId`
  - `email`
  - `fullName`
  - `avatarUrl`
  - `totalScore`
  - `totalReviews`
  - `avgScorePerReview`

---

## C4. Discovery mở rộng

### C4.1 API mix 9 item
- `GET /api/discovery/recommend/mix` (Authorize)
- Query:
  - `pageNumber`
  - `pageSize` mặc định 9
  - `maxDistanceKm?`
- Điều kiện bắt buộc:
  - user phải có `latitude/longitude` đã lưu.
  - chưa có -> trả `400 NO_LOCATION`.
- Dữ liệu nguồn:
  - place approved
  - event approved + chưa ended (theo logic timeline/recurrence mới)
- Score đề xuất:
  - `preferenceMatchScore` (cao nhất)
  - `distanceScore`
  - `ratingScore`
- Sort giảm dần theo `totalScore`.

### C4.2 API place theo vị trí + tag (16)
- `GET /api/discovery/places/by-location-tag` (Authorize)
- Query:
  - `tags` (required, string[]; hỗ trợ `tags=a&tags=b` hoặc `tags=a,b`)
  - `radiusKm?`
  - `pageNumber`
  - `pageSize` mặc định 16
- Vị trí lấy từ profile user đã lưu.
- Match theo nhiều tag (OR): place hợp lệ nếu chứa ít nhất 1 tag được truyền vào.

### C4.4 API place theo nhiều tag (public)
- `GET /api/discovery/places/by-tags` (Public)
- Query:
  - `tags` (required, string[]; hỗ trợ `tags=a&tags=b` hoặc `tags=a,b`)
  - `pageNumber`
  - `pageSize` mặc định 16
- Lọc place Approved theo nhiều tag (OR), không yêu cầu vị trí user.

### C4.3 API event ongoing/upcoming (16)
- `GET /api/discovery/events/timeline` (Authorize)
- Query:
  - `timeline=ongoing|upcoming|both`
  - `radiusKm?`
  - `pageNumber`
  - `pageSize` mặc định 16
- Vị trí lấy từ profile user đã lưu.

---

## C5. Event recurrence

### C5.1 DB change trên `events`
- Thêm:
  - `schedule_type` enum: `ExactDate | YearlyRecurring | MonthlyRecurring`
  - `start_month` (nullable int)
  - `start_day` (nullable int)
  - `end_month` (nullable int)
  - `end_day` (nullable int)
- Giữ `start_at/end_at` cho `ExactDate`.

### C5.2 API create/update event
- Mở rộng `CreateEventRequest`/`UpdateEventRequest` để nhận schedule fields.
- Rule validate:
  - `ExactDate`: bắt buộc `startAt/endAt`.
  - `YearlyRecurring`: bắt buộc `startMonth/startDay/endMonth/endDay`.
  - `MonthlyRecurring`: bắt buộc `startDay/endDay`.

### C5.3 API phụ trợ
- `GET /api/events/{id}/occurrences?from=...&to=...`
- Trả các khoảng occurrence đã resolve trong range.

### C5.4 Trạng thái ongoing/upcoming
- Tính động theo `now` và recurrence thay vì set cứng thủ công.

---

## C6. Community (1 public group)

### C6.1 Bảng mới
- `community_groups`
- `community_posts`
- `community_post_media`
- `community_comments`
- `community_reactions`

### C6.2 Quy ước phase đầu
- Tạo sẵn 1 group public (seed/init).
- User login có thể đăng bài + ảnh + trải nghiệm.
- Có comment và reaction cơ bản.
- Chưa làm private group/invite.

### C6.3 API đề xuất
- `GET /api/community/group/public`
- `GET /api/community/group/public/posts`
- `POST /api/community/group/public/posts`
- `GET /api/community/posts/{postId}`
- `POST /api/community/posts/{postId}/comments`
- `POST /api/community/posts/{postId}/reactions`
- `POST /api/community/posts/upload-signature`
- `POST /api/community/posts/finalize-media`

---

## C7. Seed data mở rộng toàn tỉnh

### C7.1 Nguồn tỉnh
- Dựa trên bảng `administrative_units` level Province đã có sau seed API hiện hành.
- Không hardcode thiếu tỉnh.

### C7.2 Khối lượng seed
- Mỗi tỉnh:
  - 2 places
  - 2 events
- Mỗi place/event:
  - 1 review mẫu gắn `userId` có thật từ seed account.

### C7.3 Review mẫu
- Random tổ hợp trường:
  - 1/3, 2/3 hoặc 3/3 (image/rating/comment).
- Ảnh default bắt buộc dùng:
  - `https://res.cloudinary.com/dhwljelir/image/upload/v1775384907/ai-tourism/Place/2e54b997-5494-442b-9d29-53f2480e2aff/uwyqbbcd6hphz0r31c65.jpg`

### C7.4 Yêu cầu chất lượng
- Dữ liệu "fake đẹp": title/description/tag phù hợp từng tỉnh, tránh trùng lặp máy móc.
- Liên kết đúng `administrativeUnitId` của tỉnh.
- Seed idempotent: chạy lại không tạo trùng.

---

## D. Thứ tự triển khai khuyến nghị (để chạy dần)

## Phase 1
1. User account update email + duplicate check.
2. Avatar upload APIs + DB field `avatarPublicId`.
3. Review history tổng (`/api/reviews/me/history`).

## Phase 2
1. Review schema/DTO/validator theo rule 1/3 + image.
2. Leaderboard API.

## Phase 3
1. Discovery mới: mix 9, place by location+tag 16, event timeline 16.
2. Chuẩn hóa lỗi `NO_LOCATION` cho API cần vị trí.

## Phase 4
1. Event recurrence (DB + DTO + service + occurrence API).
2. Đồng bộ logic ongoing/upcoming.

## Phase 5
1. Community module (1 group public).

## Phase 6
1. Seed data toàn tỉnh + review mẫu đủ tỉnh.
2. Verify luồng `seed-all` / `reset-and-seed-all`.

---

## E. Danh sách file chính sẽ bị ảnh hưởng
- `Domain/Entities`: `User`, `Review`, `Event`, entity Community mới.
- `Infrastructure/Database/AppDbContext.cs`.
- `Application/DTOs/*`: User/Review/Discovery/Event/Community/Leaderboard.
- `Application/Validators/*` tương ứng.
- `Application/Services/*`: User/Review/Discovery/Event + service mới.
- `Controllers/*`: User/Review/Discovery/Event + controller mới.
- `Infrastructure/Database/SeedData.cs`, `Controllers/DbTestController.cs`.
- Docs:
  - `BE_AI_Tourism/Documentation/api-endpoints.md`
  - `BE_AI_Tourism/Documentation/database-design.md`
  - `BE_AI_Tourism/Documentation/implementation-status.md`

---

## F. Ràng buộc implementation
- Tuân thủ response wrapper `Result<T>`.
- Không mở rộng ngoài scope nếu chưa được chốt thêm.
- Mọi thay đổi schema/API phải cập nhật docs tương ứng.


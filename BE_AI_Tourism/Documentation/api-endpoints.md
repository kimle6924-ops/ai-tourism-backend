# API Endpoints

## Quy ước chung

**Response wrapper:** Mọi API đều trả về `Result<T>` gồm: `success` (bool), `data` (T), `message`, `errorCode`, `statusCode`. Lỗi validation trả thêm `errors[]`.

**Phân trang:** Các API có phân trang nhận query `pageNumber` (int), `pageSize` (int). Response: `items[]`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`, `hasPreviousPage`, `hasNextPage`.

**Quyền truy cập (Auth):**
- `Public` — Không cần đăng nhập, ai cũng gọi được.
- `Login` — Cần đăng nhập (gửi header `Authorization: Bearer <accessToken>`). Mọi role (Admin, Contributor, User) đều được.
- `Admin` — Chỉ role Admin (0) mới được gọi. Trả 403 nếu không đủ quyền.
- `Admin/Contributor` — Role Admin (0) hoặc Contributor (1). Contributor bị giới hạn scope theo đơn vị hành chính được gán khi đăng ký (chỉ thao tác với dữ liệu thuộc đơn vị hành chính của mình). Admin không bị giới hạn scope.
- Không có token hoặc token hết hạn → trả 401 Unauthorized.

**Enums (gửi/nhận dạng số int):**
- UserRole: 0=Admin, 1=Contributor, 2=User
- UserStatus: 0=Active, 1=Locked, 2=PendingApproval
- AdministrativeLevel: 0=Province, 1=Ward
- ModerationStatus: 0=Pending, 1=Approved, 2=Rejected
- EventStatus: 0=Upcoming, 1=Ongoing, 2=Ended
- ScheduleType: 0=ExactDate, 1=YearlyRecurring, 2=MonthlyRecurring
- ReviewStatus: 0=Active, 1=Hidden
- ResourceType: 0=Place, 1=Event
- ConversationStatus: 0=Active, 1=Archived
- MessageRole: 0=User, 1=Assistant, 2=System

---

## Auth (`/api/auth`) — Public

**POST `/register`** — Public
Body: `email`* (string), `password`* (string), `fullName`* (string), `phone`* (string), `role?` (UserRole: 0=Admin/1=Contributor/2=User, mặc định 2), `administrativeUnitId?` (guid, bắt buộc nếu role=Contributor), `categoryIds` (guid[], sở thích ban đầu)
→ AuthResponse: `accessToken`, `refreshToken`, `expiresAt`, `user` (UserResponse)

**POST `/login`** — Public
Body: `email`* (string), `password`* (string)
→ AuthResponse

**POST `/refresh`** — Public
Body: `refreshToken`* (string)
→ AuthResponse

---

## User (`/api/user`) — Login (mọi role)

**GET `/me`** — Login → UserResponse: `id`, `email`, `fullName`, `phone`, `avatarUrl`, `role` (0=Admin/1=Contributor/2=User), `status` (0=Active/1=Locked/2=PendingApproval), `latitude?` (double), `longitude?` (double)

**PUT `/me/account`** — Login
Body: `email?` (string), `fullName?` (string), `phone?` (string)
→ UserResponse
- Nếu đổi `email` sang email đã tồn tại → `409 EMAIL_ALREADY_EXISTS`
- `email` được normalize `trim + lowercase` trước khi lưu

**PUT `/me`** — Login
Body: `fullName?` (string), `phone?` (string), `avatarUrl?` (string)
→ UserResponse

**POST `/me/avatar/upload-signature`** — Login
→ AvatarUploadSignatureResponse: `signature`, `timestamp`, `apiKey`, `cloudName`, `folder`
(Frontend dùng signature này để upload avatar trực tiếp lên Cloudinary)

**POST `/me/avatar/finalize`** — Login
Body: `publicId`* (string), `url`* (string), `secureUrl`* (string)
→ UserResponse (cập nhật `avatarUrl` + `avatarPublicId` cho user hiện tại)

**GET `/me/preferences`** — Login → PreferencesResponse: `categoryIds` (guid[])

**PUT `/me/location`** — Login
Body: `latitude`* (double, -90 đến 90), `longitude`* (double, -180 đến 180)
→ UserResponse

**PUT `/me/preferences`** — Login
Body: `categoryIds`* (guid[])
→ PreferencesResponse

---

## Admin (`/api/admin`) — Admin only

**GET `/users`** — Admin, phân trang → UserResponse[]

**PATCH `/users/{id}/lock`** — Admin → UserResponse (khóa tài khoản, chuyển status→1=Locked)

**PATCH `/users/{id}/unlock`** — Admin → UserResponse (mở khóa, chuyển status→0=Active)

**PATCH `/users/{id}/approve`** — Admin → UserResponse (duyệt Contributor: status 2=PendingApproval → 0=Active)

**GET `/stats/overview`** — Admin
Query: `fromUtc?` (ISO-8601, mặc định now-29d), `toUtc?` (ISO-8601, mặc định now)
→ StatsOverviewResponse gồm: `users` (total, byRole, byStatus), `places` (total, byModerationStatus), `events` (total, byModerationStatus, byEventStatus), `reviews` (total, byStatus, averageRating), `moderation` (pendingPlaces, pendingEvents), `chat` (totalConversations, totalMessages, newInRange), `content` (categories, administrativeUnits, mediaAssets, totalMediaBytes), `timeSeries` (daily count users/places/events/reviews)
Lỗi 400 nếu fromUtc > toUtc.

---

## Administrative Units (`/api/administrative-units`)

**GET `/`** — Public, phân trang → AdministrativeUnitResponse[]: `id`, `name`, `level` (0=Province/1=Ward), `parentId?`, `code`, `createdAt`, `updatedAt`

**GET `/{id}`** — Public → AdministrativeUnitResponse

**GET `/by-level/{level}`** — Public, level: 0=Province/1=Ward → AdministrativeUnitResponse[]

**GET `/{id}/children`** — Public → AdministrativeUnitResponse[]

**POST `/`** — Admin
Body: `name`* (string), `level`* (int: 0=Province/1=Ward), `parentId?` (guid, bắt buộc nếu level=Ward), `code`* (string)
→ AdministrativeUnitResponse

**PUT `/{id}`** — Admin
Body: `name`* (string), `code`* (string)
→ AdministrativeUnitResponse

**DELETE `/{id}`** — Admin → Result

---

## Categories (`/api/categories`)

**GET `/`** — Public, phân trang → CategoryResponse[]: `id`, `name`, `slug`, `type`, `isActive`, `createdAt`, `updatedAt`

**GET `/active`** — Public → CategoryResponse[] (chỉ isActive=true)

**GET `/by-type/{type}`** — Public, type = theme/style/activity/budget/companion → CategoryResponse[]

**GET `/{id}`** — Public → CategoryResponse

**POST `/`** — Admin
Body: `name`* (string, max 100), `slug`* (string, max 100, format: a-z0-9 và dấu -), `type`* (string, max 50)
→ CategoryResponse. Lỗi 409 nếu slug đã tồn tại.

**PUT `/{id}`** — Admin
Body: `name`* (string), `slug`* (string), `type`* (string), `isActive` (bool, default true)
→ CategoryResponse

**DELETE `/{id}`** — Admin → Result

**POST `/seed`** — Admin
Tạo sẵn 18 category, bỏ qua nếu slug đã tồn tại:
- theme: Thiên nhiên, Thành phố, Ẩm thực, Văn hóa – Lịch sử, Lễ hội – sự kiện
- style: Chill – thư giãn, Vui vẻ – năng động, Sôi động – náo nhiệt, Phiêu lưu – khám phá
- activity: Trekking / khám phá, Du lịch sinh thái, Check-in sống ảo, Giải trí / vui chơi
- budget: Giá rẻ – tiết kiệm, Cao cấp – sang chảnh
- companion: Gia đình, Cặp đôi, Nhóm bạn

---

## Places (`/api/places`)

PlaceResponse: `id`, `title`, `description`, `address`, `administrativeUnitId`, `latitude?`, `longitude?`, `categoryIds` (guid[]), `tags` (string[]), `moderationStatus` (0=Pending/1=Approved/2=Rejected), `createdBy`, `approvedBy?`, `approvedAt?`, `createdAt`, `updatedAt`, `images` (MediaAssetResponse[]: `id`, `resourceType`, `resourceId`, `url`, `altText`, `sortOrder`, `createdAt` — load trực tiếp từ bảng media_assets)

**GET `/`** — Public, phân trang → PlaceResponse[] (chỉ moderationStatus=1 Approved)

**GET `/all`** — Admin/Contributor, phân trang → PlaceResponse[] (tất cả status, Contributor chỉ thấy dữ liệu trong scope đơn vị hành chính của mình)

**GET `/{id}`** — Public → PlaceResponse

**POST `/`** — Admin/Contributor (Contributor chỉ tạo trong scope đơn vị hành chính của mình)
Body: `title`* (string), `description`* (string), `address`* (string), `administrativeUnitId`* (guid), `latitude?` (double), `longitude?` (double), `categoryIds` (guid[]), `tags` (string[])
→ PlaceResponse (tự động moderationStatus=0 Pending, cần Admin/Contributor cấp trên duyệt)

**PUT `/{id}`** — Admin/Contributor (Admin sửa tất cả, Contributor chỉ sửa place mình tạo trong scope)
Body: giống POST
→ PlaceResponse

**DELETE `/{id}`** — Admin/Contributor (Admin xóa tất cả, Contributor chỉ xóa place mình tạo trong scope) → Result

**POST `/seed`** — Admin
Tạo sẵn 16 place mẫu (khu vực Sa Pa, Lào Cai), tự động Approved + tạo ảnh mặc định. Bỏ qua nếu place cùng title đã tồn tại. Tự tạo đơn vị hành chính Lào Cai/Sa Pa nếu chưa có. Yêu cầu đã seed accounts và seed categories trước.

---

## Events (`/api/events`)

EventResponse: `id`, `title`, `description`, `address`, `administrativeUnitId`, `latitude?`, `longitude?`, `categoryIds` (guid[]), `tags` (string[]), `scheduleType` (0=ExactDate/1=YearlyRecurring/2=MonthlyRecurring), `startAt?`, `endAt?`, `startMonth?`, `startDay?`, `endMonth?`, `endDay?`, `eventStatus` (0=Upcoming/1=Ongoing/2=Ended, tính động theo `now` + recurrence), `moderationStatus` (0=Pending/1=Approved/2=Rejected), `createdBy`, `approvedBy?`, `approvedAt?`, `createdAt`, `updatedAt`, `images` (MediaAssetResponse[]: `id`, `resourceType`, `resourceId`, `url`, `altText`, `sortOrder`, `createdAt` — load trực tiếp từ bảng media_assets)

**GET `/`** — Public, phân trang → EventResponse[] (chỉ moderationStatus=1 Approved)

**GET `/all`** — Admin/Contributor, phân trang → EventResponse[] (tất cả status, Contributor chỉ thấy trong scope)

**GET `/{id}`** — Public → EventResponse

**GET `/{id}/occurrences`** — Public
Query: `from`* (datetime), `to`* (datetime), `to >= from`
→ EventOccurrenceResponse[]: `startAt`, `endAt`

**POST `/`** — Admin/Contributor (Contributor chỉ tạo trong scope đơn vị hành chính của mình)
Body: `title`* (string), `description`* (string), `address`* (string), `administrativeUnitId`* (guid), `latitude?` (double), `longitude?` (double), `categoryIds` (guid[]), `tags` (string[]), `scheduleType`* (int)
- Nếu `scheduleType=ExactDate`: bắt buộc `startAt`, `endAt` và `endAt > startAt`
- Nếu `scheduleType=YearlyRecurring`: bắt buộc `startMonth/startDay/endMonth/endDay`
- Nếu `scheduleType=MonthlyRecurring`: bắt buộc `startDay/endDay`
→ EventResponse (tự động moderationStatus=0 Pending)

**PUT `/{id}`** — Admin/Contributor (Admin sửa tất cả, Contributor chỉ sửa event mình tạo trong scope)
Body: giống POST (không cập nhật tay `eventStatus`, hệ thống tự tính động)
→ EventResponse

**DELETE `/{id}`** — Admin/Contributor (Admin xóa tất cả, Contributor chỉ xóa event mình tạo trong scope) → Result

**POST `/seed`** — Admin
Tạo sẵn 16 event mẫu (khu vực Sa Pa, Lào Cai), tự động Approved + tạo ảnh mặc định cho mỗi event. Bỏ qua nếu event cùng title đã tồn tại (gọi nhiều lần không sao). Tự tạo đơn vị hành chính Lào Cai/Sa Pa nếu chưa có. Yêu cầu: phải seed admin (POST /api/dbtest/seed-admin) và seed categories (POST /api/categories/seed) trước khi gọi API này. Seed data gồm các lễ hội, sự kiện thể thao, ẩm thực, văn hóa, trekking với trạng thái Upcoming/Ongoing.

---

## Moderation (`/api/moderation`) — Admin/Contributor

resourceType trong URL: `Place` hoặc `Event`. Contributor chỉ duyệt/từ chối dữ liệu thuộc scope đơn vị hành chính cấp dưới của mình. Admin duyệt tất cả.

**PATCH `/{resourceType}/{id}/approve`** — Admin/Contributor
Body: `note`* (string)
→ ModerationLogResponse: `id`, `resourceType`, `resourceId`, `action`, `note`, `actedBy`, `actedAt`
(Chuyển moderationStatus → 1=Approved)

**PATCH `/{resourceType}/{id}/reject`** — Admin/Contributor
Body: `note`* (string)
→ ModerationLogResponse (chuyển moderationStatus → 2=Rejected)

**GET `/{resourceType}/{id}/logs`** — Admin/Contributor → ModerationLogResponse[]

---

## Media (`/api/media`)

MediaAssetResponse: `id`, `resourceType` (0=Place/1=Event), `resourceId`, `url`, `secureUrl`, `publicId`, `format`, `mimeType`, `bytes`, `width`, `height`, `isPrimary`, `sortOrder`, `uploadedBy`, `createdAt`

**POST `/upload-signature`** — Admin/Contributor (Contributor chỉ upload cho resource trong scope)
Body: `resourceType`* (int: 0=Place/1=Event), `resourceId`* (guid)
→ UploadSignatureResponse: `signature`, `timestamp` (long), `apiKey`, `cloudName`, `folder`
(Frontend dùng signature này để upload trực tiếp lên Cloudinary)

**POST `/finalize`** — Admin/Contributor (Contributor chỉ finalize cho resource trong scope)
Body: `resourceType`* (int: 0=Place/1=Event), `resourceId`* (guid), `publicId`* (string), `url`* (string), `secureUrl`* (string), `format`* (string), `mimeType`* (string), `bytes`* (long), `width`* (int), `height`* (int)
→ MediaAssetResponse (lưu metadata sau khi upload Cloudinary thành công)

**GET `/by-resource?resourceType=Place&resourceId=xxx`** — Public → MediaAssetResponse[]

**PATCH `/{id}/set-primary`** — Admin/Contributor → MediaAssetResponse

**PATCH `/reorder`** — Admin/Contributor
Body: `orderedIds`* (guid[]) — danh sách media ID theo thứ tự mong muốn
→ Result

**DELETE `/{id}`** — Admin/Contributor → Result (xóa cả DB và Cloudinary)

---

## Reviews (`/api/reviews`)

ReviewResponse: `id`, `resourceType` (0=Place/1=Event), `resourceId`, `userId`, `userFullName` (string), `userAvatarUrl` (string), `rating?`, `comment?`, `imageUrl?`, `status` (0=Active/1=Hidden), `createdAt`, `updatedAt`

**POST `/`** — Login (mỗi lần gọi tạo 1 review mới, 1 user có thể đánh giá nhiều lần cho cùng 1 resource)
Body: `resourceType`* (int: 0=Place/1=Event), `resourceId`* (guid), `rating`* (int 1-5), `comment?` (string), `imageUrl?` (string)
→ ReviewResponse
- Validation: `rating` là bắt buộc, `comment` và `imageUrl` là tùy chọn.
- Trạng thái mặc định sau khi tạo: `Active`.

**PATCH `/{id}`** — Login (chỉ chủ review mới sửa được, người khác → 403)
Body: `rating`* (int 1-5), `comment?` (string), `imageUrl?` (string)
→ ReviewResponse
- Validation: `rating` là bắt buộc, `comment` và `imageUrl` là tùy chọn.
- Sau khi user cập nhật review, trạng thái được đặt lại `Active`.

**DELETE `/{id}`** — Login (chủ review hoặc Admin mới xóa được) → Result

**GET `/?resourceType=Place&resourceId=xxx`** — Public, phân trang
→ ReviewListResponse: `averageRating` (double, làm tròn 1 chữ số), `totalReviews` (int), `reviews` (PaginationResponse\<ReviewResponse\>)

**GET `/mine?resourceType=Place&resourceId=xxx`** — Login, phân trang → ReviewResponse[] (danh sách review của user hiện tại cho resource đó, mới nhất trước)

**GET `/me/history`** — Login, phân trang
Query: `resourceType?` (int: 0=Place/1=Event)
→ ReviewHistoryItemResponse[] (lịch sử review tổng của user hiện tại, mới nhất trước, mỗi item có thêm `resourceTitle`, `resourceAddress`, `resourceImageUrl`)

**GET `/all`** — Admin, phân trang
Query: `status?` (int: 0=Active, 1=Hidden)
→ ReviewResponse[] (danh sách review toàn hệ thống, có thể filter theo trạng thái)

**PATCH `/{id}/approve`** — Admin
→ Result (chuyển trạng thái review sang `Active`)

**PATCH `/{id}/hide`** — Admin
→ Result (chuyển trạng thái review sang `Hidden`)

---

## Leaderboard (`/api/leaderboard`) — Public

**GET `/users`** — Public, phân trang
→ UserLeaderboardItemResponse[]: `rank`, `userId`, `email`, `fullName`, `avatarUrl`, `totalScore`, `totalReviews`, `avgScorePerReview`

Quy tắc tính điểm (chỉ tính review `Active`):
- `rating` có giá trị: +1 điểm
- `comment` có nội dung: +1 điểm
- `imageUrl` có giá trị: +1 điểm
- Mỗi review tối đa 3 điểm

---

## Discovery (`/api/discovery`) — Public

**GET `/tags`** — Public
→ string[] (danh sách tag distinct từ `Place.Tags` + `Event.Tags` đã Approved, đã loại rỗng, sort A-Z, không phân biệt hoa thường)

**GET `/places`** — Public, phân trang (chỉ trả place đã Approved)
Query: `search?` (string), `categoryId?` (guid), `administrativeUnitId?` (guid), `tag?` (string), `sortBy` (newest/oldest/rating/name, default: newest)
→ PlaceResponse[]

**GET `/events`** — Public, phân trang (chỉ trả event đã Approved)
Query: `search?` (string), `categoryId?` (guid), `administrativeUnitId?` (guid), `tag?` (string), `sortBy` (newest/oldest/rating/name/startdate, default: newest)
→ EventResponse[]

### Simple Search (`/api/discovery/search`) — Public

API tìm kiếm đơn giản, hỗ trợ lọc theo khoảng sao trung bình.

**GET `/search/places`** — Public, phân trang (chỉ trả place đã Approved)
Query: `search?` (string), `sortBy` (newest/oldest/rating/name, default: newest), `averageRating?` (int: 5→đúng 5.0, 4→4.0–4.99, 3→3.0–3.99, 2→2.0–2.99, 1→1.0–1.99)
→ PlaceResponse[]

**GET `/search/events`** — Public, phân trang (chỉ trả event đã Approved)
Query: `search?` (string), `sortBy` (newest/oldest/rating/name/startdate, default: newest), `averageRating?` (int: 5→đúng 5.0, 4→4.0–4.99, 3→3.0–3.99, 2→2.0–2.99, 1→1.0–1.99)
→ EventResponse[]

### Gợi ý theo vị trí + sở thích (`/api/discovery/recommend`) — Login

Tự lấy vị trí (latitude/longitude) và sở thích (categoryIds) của user đang đăng nhập. Yêu cầu user phải cập nhật vị trí trước (`PUT /api/user/me/location`), nếu chưa trả `400 NO_LOCATION`.

**GET `/recommend/places`** — Login, phân trang
Query: `maxDistanceKm?` (double, lọc khoảng cách tối đa km, không truyền → trả tất cả)
→ PlaceResponse[] (mỗi item có thêm `distanceKm`)

**GET `/recommend/events`** — Login, phân trang
Query: `maxDistanceKm?` (double)
Chỉ trả event chưa kết thúc (Ended), trạng thái `Upcoming/Ongoing/Ended` được tính động theo `now` + recurrence.
Ưu tiên match sở thích, sắp theo khoảng cách.
→ EventResponse[] (mỗi item có thêm `distanceKm`)

**GET `/recommend/mix`** — Login, phân trang (default `pageSize=9`)
Query: `maxDistanceKm?` (double)
→ DiscoveryMixItemResponse[] (mix place + event, không ép tỉ lệ, sort giảm dần theo `totalScore`; mỗi item có: `resourceType`, `resourceId`, `title`, `address`, `averageRating`, `distanceKm`, `primaryImageUrl`, `preferenceMatched`, `preferenceMatchScore`, `distanceScore`, `ratingScore`, `totalScore`)

### Discovery theo vị trí + tag/timeline (`/api/discovery`) — Login

**GET `/places/by-location-tag`** — Login, phân trang (default `pageSize=16`)
Query: `tags`* (string[], bắt buộc; hỗ trợ lặp query `tags=a&tags=b` hoặc dạng comma `tags=a,b`), `radiusKm?` (double)
Lấy vị trí từ profile user, lọc place `Approved` theo nhiều tag (match bất kỳ tag nào) + bán kính (nếu có), sort gần → xa.
→ PlaceResponse[] (mỗi item có thêm `distanceKm`)

**GET `/events/timeline`** — Login, phân trang (default `pageSize=16`)
Query: `timeline` (ongoing|upcoming|both, default `both`), `radiusKm?` (double)
Lấy vị trí từ profile user, lọc event `Approved` theo timeline + bán kính (nếu có):
- `ongoing` → chỉ event `Ongoing`
- `upcoming` → chỉ event `Upcoming`
- `both` → `Ongoing` + `Upcoming`
(`EventStatus` trong API này được tính động theo recurrence)
→ EventResponse[] (mỗi item có thêm `distanceKm`)

### Discovery theo nhiều tag (`/api/discovery`) — Public

**GET `/places/by-tags`** — Public, phân trang (default `pageSize=16`)
Query: `tags`* (string[], bắt buộc; hỗ trợ lặp query `tags=a&tags=b` hoặc dạng comma `tags=a,b`)
Lọc place `Approved` theo nhiều tag (match bất kỳ tag nào), sort mới nhất → cũ hơn.
→ PlaceResponse[]

---

## Community (`/api/community`)

Community phase đầu chỉ có 1 group public chung.

**GET `/group/public`** — Public  
→ CommunityGroupResponse: `id`, `name`, `slug`, `description`, `isPublic`, `isActive`, `createdAt`, `updatedAt`

**GET `/group/public/posts`** — Public, phân trang  
→ CommunityPostResponse[]: `id`, `groupId`, `userId`, `userFullName`, `userAvatarUrl`, `content`, `reactionCount`, `commentCount`, `media` (CommunityPostMediaResponse[]), `createdAt`, `updatedAt`

**POST `/group/public/posts`** — Login  
Body: `content`* (string, max 5000)  
→ CommunityPostResponse

**GET `/posts/{postId}`** — Public  
→ CommunityPostResponse (bao gồm `comments` (CommunityCommentResponse[]))

**POST `/posts/{postId}/comments`** — Login  
Body: `content`* (string, max 1000)  
→ CommunityCommentResponse

**POST `/posts/{postId}/reactions`** — Login  
Body: `reactionType`* (string, mặc định `like`)  
Rule: nếu user đã có cùng `reactionType` trên post thì gọi lại sẽ bỏ reaction (toggle off).  
→ CommunityPostResponse (đã cập nhật `reactionCount`)

**POST `/posts/upload-signature`** — Login  
Body: `postId`* (guid, chỉ chủ post mới upload)  
→ CommunityPostUploadSignatureResponse: `signature`, `timestamp`, `apiKey`, `cloudName`, `folder`

**POST `/posts/finalize-media`** — Login  
Body: `postId`* (guid), `publicId`* (string), `url`* (string), `secureUrl`* (string), `format`* (string), `mimeType`* (string), `bytes`* (long), `width`* (int), `height`* (int)  
Rule: chỉ chủ post mới finalize media.  
→ CommunityPostMediaResponse

---

## Chat AI (`/api/chat`) — Login (mọi role)

ConversationResponse: `id`, `title`, `model`, `status` (0=Active/1=Archived), `lastMessageAt`, `createdAt`
MessageResponse: `id`, `conversationId`, `role` (0=User/1=Assistant/2=System), `content`, `tokenCount`, `citations`, `createdAt`

**POST `/conversations`** — Login
Body: `title`* (string)
→ ConversationResponse

**GET `/conversations`** — Login, phân trang → ConversationResponse[] (chỉ conversation của user hiện tại)

**GET `/conversations/{id}/messages`** — Login, phân trang → MessageResponse[] (chỉ xem conversation của mình)

**POST `/conversations/{id}/messages`** — Login
Body: `content`* (string)
→ MessageResponse (AI trả lời dựa trên dữ liệu thực: places, events, preferences của user)

**POST `/conversations/{id}/messages/stream`** — Login
Body: `content`* (string)
→ SSE stream, mỗi chunk: `data: {"content":"..."}`, kết thúc: `data: [DONE]`

---

## Test / Dev

**GET `/api/dbtest`** — No auth, test kết nối database

**POST `/api/dbtest/create-tables`** — No auth, tạo toàn bộ tables (`?reset=true` để xóa schema cũ và tạo lại từ đầu)

**POST `/api/dbtest/seed-accounts`** — No auth, tạo 4 tài khoản mặc định (bỏ qua nếu đã tồn tại):
- Admin: admin@aitourism.vn / admin123
- Contributor (Province - Đà Nẵng): contributor.province@aitourism.vn / contributor123
- Contributor (Ward - Hải Châu): contributor.ward@aitourism.vn / contributor123
- User: user@aitourism.vn / user123

**POST `/api/dbtest/seed-all`** — No auth, seed toàn bộ dữ liệu mà không reset DB: đảm bảo bảng tồn tại → seed đơn vị hành chính + categories + users + 1 community public group + dữ liệu toàn tỉnh (mỗi tỉnh 2 place + 2 event + review mẫu) + bộ dữ liệu Sa Pa gốc (idempotent theo title) → seed thêm 4 tài khoản mặc định (skip nếu trùng email). Trả về danh sách từng bước và trạng thái.

**POST `/api/dbtest/reset-and-seed-all`** — No auth, reset toàn bộ database và seed lại tất cả: reset/create bảng → seed đơn vị hành chính + categories + users + 1 community public group + dữ liệu toàn tỉnh (mỗi tỉnh 2 place + 2 event + review mẫu) + bộ dữ liệu Sa Pa gốc (idempotent theo title) → seed thêm 4 tài khoản mặc định (skip nếu trùng email). Trả về danh sách từng bước và trạng thái.

**POST `/api/geminitests`** — No auth
Body: `prompt`* (string)
→ `response` (string) — gửi prompt thẳng cho Gemini

**POST `/api/geminitests/ask-api`** — No auth
Body: `question`* (string)
→ `question`, `answer` — hỏi Gemini về các API trong dự án (dùng file này làm knowledge base)

**POST `/api/geminitests/test-prompt`** — No auth
Body: `userMessage`* (string), `userPreferences?` (string[]), `userLatitude?` (double), `userLongitude?` (double), `fakePlaces?` (object[]: title, category?, description?, address?, rating?, tags?, latitude?, longitude?), `fakeEvents?` (object[]: title, description?, address?, status?, startAt?, endAt?)
→ `systemPrompt`, `userMessage`, `aiResponse` — test base prompt AI với fake data

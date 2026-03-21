namespace BE_AI_Tourism.Controllers;

/// <summary>
/// Chứa từng phần tài liệu API cho Trợ lý API (Gemini).
/// Tách riêng để dễ sửa từng section mà không ảnh hưởng phần khác.
/// </summary>
public static class ApiDocSections
{
    public static string General() => """
        # API Endpoints

        ## Quy ước chung
        Response wrapper: Mọi API đều trả về Result<T> gồm: success (bool), data (T), message, errorCode, statusCode. Lỗi validation trả thêm errors[].
        Phân trang: Các API có phân trang nhận query pageNumber (int), pageSize (int). Response: items[], totalCount, pageNumber, pageSize, totalPages, hasPreviousPage, hasNextPage.

        Quyền truy cập (Auth):
        - Public — Không cần đăng nhập, ai cũng gọi được.
        - Login — Cần đăng nhập (gửi header Authorization: Bearer <accessToken>). Mọi role (Admin, Contributor, User) đều được.
        - Admin — Chỉ role Admin (0) mới được gọi. Trả 403 nếu không đủ quyền.
        - Admin/Contributor — Role Admin (0) hoặc Contributor (1). Contributor bị giới hạn scope theo đơn vị hành chính được gán khi đăng ký (chỉ thao tác với dữ liệu thuộc đơn vị hành chính của mình và đơn vị con). Admin không bị giới hạn scope.
        - Không có token hoặc token hết hạn → trả 401 Unauthorized.

        Scope (phạm vi quyền Contributor): Contributor được gán 1 đơn vị hành chính khi đăng ký. Contributor có quyền thao tác với dữ liệu thuộc đơn vị đó và tất cả đơn vị con bên dưới. Ví dụ Contributor gán ở Province thì quản lý được cả Ward bên dưới.

        Enums (gửi/nhận dạng số int):
        - UserRole: 0=Admin, 1=Contributor, 2=User
        - UserStatus: 0=Active, 1=Locked, 2=PendingApproval
        - AdministrativeLevel: 0=Province, 1=Ward
        - ModerationStatus: 0=Pending, 1=Approved, 2=Rejected
        - EventStatus: 0=Upcoming, 1=Ongoing, 2=Ended
        - ReviewStatus: 0=Active, 1=Hidden, 2=Deleted
        - ResourceType: 0=Place, 1=Event
        - ConversationStatus: 0=Active, 1=Archived
        - MessageRole: 0=User, 1=Assistant, 2=System

        HTTP Error codes chung: 400=Bad Request (validation sai), 401=Unauthorized (chưa đăng nhập/token hết hạn), 403=Forbidden (không đủ quyền/ngoài scope), 404=Not Found, 409=Conflict (trùng dữ liệu)
        """;

    public static string Auth() => """
        ## Auth (/api/auth) — Public

        POST /api/auth/register — Public
        Body: email* (string, phải đúng format email), password* (string, tối thiểu 6 ký tự), fullName* (string, max 100), phone (string, max 20), role? (int: 0/1/2, mặc định 2=User), administrativeUnitId? (guid), categoryIds* (guid[], ít nhất 1 category)
        Điều kiện: KHÔNG được đăng ký role=0 (Admin), server sẽ từ chối trả 400. Nếu role=1 (Contributor) thì BẮT BUỘC phải có administrativeUnitId và đơn vị đó phải tồn tại trong DB. Nếu role không phải Contributor thì administrativeUnitId bị bỏ qua. categoryIds phải có ít nhất 1 phần tử. Email bị chuẩn hóa (trim + lowercase) trước khi check trùng.
        Lỗi: 400 nếu đăng ký Admin hoặc Contributor thiếu administrativeUnitId. 404 nếu administrativeUnitId không tồn tại. 409 nếu email đã tồn tại.
        Contributor sau khi đăng ký sẽ có status=2 (PendingApproval), phải chờ Admin duyệt mới đăng nhập được. User thường thì status=0 (Active) ngay.
        → AuthResponse: accessToken (JWT, hết hạn ~15 phút), refreshToken (hết hạn 7 ngày), expiresAt, user (UserResponse)

        POST /api/auth/login — Public
        Body: email* (string, đúng format email), password* (string)
        Điều kiện: Email chuẩn hóa trước khi tìm. Nếu tài khoản bị Locked (status=1) → 403. Nếu PendingApproval (status=2) → 403 (Contributor chưa được duyệt). Chỉ Active (status=0) mới đăng nhập được.
        Lỗi: 401 nếu sai email hoặc password. 403 nếu bị khóa hoặc chưa duyệt.
        → AuthResponse

        POST /api/auth/refresh — Public
        Body: refreshToken* (string)
        Điều kiện: Token phải tồn tại trong DB và chưa hết hạn (7 ngày). Refresh thành công sẽ tạo cặp accessToken + refreshToken mới.
        Lỗi: 401 nếu token không hợp lệ hoặc hết hạn.
        → AuthResponse
        """;

    public static string User() => """
        ## User (/api/user) — Login (mọi role)

        GET /api/user/me — Login → UserResponse: id, email, fullName, phone, avatarUrl, role (0=Admin/1=Contributor/2=User), status (0=Active/1=Locked/2=PendingApproval), latitude? (double), longitude? (double)

        PUT /api/user/me — Login
        Body: fullName? (string, max 100), phone? (string, max 20), avatarUrl? (string, max 500)
        Chỉ cập nhật field được gửi, field null thì giữ nguyên.
        → UserResponse

        GET /api/user/me/preferences — Login → PreferencesResponse: categoryIds (guid[])

        PUT /api/user/me/location — Login
        Body: latitude* (double, -90 đến 90), longitude* (double, -180 đến 180)
        Cập nhật vị trí hiện tại của user.
        → UserResponse

        PUT /api/user/me/preferences — Login
        Body: categoryIds* (guid[], bắt buộc không null)
        Tạo mới preference nếu chưa có, cập nhật nếu đã có.
        → PreferencesResponse
        """;

    public static string Admin() => """
        ## Admin (/api/admin) — Admin only

        GET /api/admin/users — Admin, phân trang → UserResponse[]

        PATCH /api/admin/users/{id}/lock — Admin → UserResponse (chuyển status→1=Locked)
        Lỗi: 404 nếu user không tồn tại.

        PATCH /api/admin/users/{id}/unlock — Admin → UserResponse (chuyển status→0=Active)
        Lỗi: 404 nếu user không tồn tại.

        PATCH /api/admin/users/{id}/approve — Admin → UserResponse (duyệt Contributor: status 2→0)
        Điều kiện: User PHẢI đang ở status=2 (PendingApproval). Nếu không phải PendingApproval → 400.
        Lỗi: 404 nếu user không tồn tại. 400 nếu user không ở trạng thái PendingApproval.

        GET /api/admin/stats/overview — Admin
        Query: fromUtc? (ISO-8601, mặc định now-29d), toUtc? (ISO-8601, mặc định now)
        → StatsOverviewResponse gồm: users (total, byRole, byStatus), places (total, byModerationStatus), events (total, byModerationStatus, byEventStatus), reviews (total, byStatus, averageRating), moderation (pendingPlaces, pendingEvents), chat (totalConversations, totalMessages, newInRange), content (categories, administrativeUnits, mediaAssets, totalMediaBytes), timeSeries (daily count users/places/events/reviews)
        Lỗi: 400 nếu fromUtc > toUtc.
        """;

    public static string AdministrativeUnits() => """
        ## Administrative Units (/api/administrative-units)

        Hệ thống phân cấp: Province → Ward. Province không có parent. Ward bắt buộc có parentId trỏ về Province. Dữ liệu đồng bộ từ API provinces.open-api.vn v2 (post-2025 merger).

        GET /api/administrative-units — Public, phân trang → AdministrativeUnitResponse[]: id, name, level (0=Province/1=Ward), parentId?, code, createdAt, updatedAt

        GET /api/administrative-units/{id} — Public → AdministrativeUnitResponse

        GET /api/administrative-units/by-level/{level} — Public, level: 0=Province/1=Ward → AdministrativeUnitResponse[]

        GET /api/administrative-units/{id}/children — Public → AdministrativeUnitResponse[]

        POST /api/administrative-units — Admin
        Body: name* (string, max 200), level* (int: 0=Province/1=Ward), parentId? (guid, bắt buộc nếu level=Ward), code* (string, max 50)
        Điều kiện: Province (level=0) không cần parentId. Ward (level=1) BẮT BUỘC có parentId trỏ về Province. Code phải duy nhất.
        Lỗi: 404 nếu parent không tồn tại. 409 nếu code đã tồn tại.

        PUT /api/administrative-units/{id} — Admin
        Body: name* (string, max 200), code* (string, max 50)
        Lỗi: 404 nếu không tìm thấy. 409 nếu code trùng với đơn vị khác.

        DELETE /api/administrative-units/{id} — Admin
        Điều kiện: KHÔNG thể xóa nếu còn đơn vị con bên dưới.
        Lỗi: 404 nếu không tìm thấy. 409 nếu còn children.
        """;

    public static string Categories() => """
        ## Categories (/api/categories)

        GET /api/categories — Public, phân trang → CategoryResponse[]: id, name, slug, type, isActive, createdAt, updatedAt

        GET /api/categories/active — Public → CategoryResponse[] (chỉ isActive=true)

        GET /api/categories/by-type/{type} — Public, type = theme/style/activity/budget/companion → CategoryResponse[]

        GET /api/categories/{id} — Public → CategoryResponse

        POST /api/categories — Admin
        Body: name* (string, max 100), slug* (string, max 100, regex: chỉ a-z0-9 và dấu gạch ngang, VD: "am-thuc"), type* (string, max 50)
        Lỗi: 409 nếu slug đã tồn tại.

        PUT /api/categories/{id} — Admin
        Body: name* (string), slug* (string), type* (string), isActive (bool, default true)
        Lỗi: 404 nếu không tìm thấy. 409 nếu slug trùng với category khác.

        DELETE /api/categories/{id} — Admin
        Lỗi: 404 nếu không tìm thấy.

        POST /api/categories/seed — Admin
        Tạo sẵn 18 category, bỏ qua nếu slug đã tồn tại (gọi nhiều lần không sao):
        theme: Thiên nhiên, Thành phố, Ẩm thực, Văn hóa – Lịch sử, Lễ hội – sự kiện
        style: Chill – thư giãn, Vui vẻ – năng động, Sôi động – náo nhiệt, Phiêu lưu – khám phá
        activity: Trekking / khám phá, Du lịch sinh thái, Check-in sống ảo, Giải trí / vui chơi
        budget: Giá rẻ – tiết kiệm, Cao cấp – sang chảnh
        companion: Gia đình, Cặp đôi, Nhóm bạn
        """;

    public static string Places() => """
        ## Places (/api/places)

        PlaceResponse: id, title, description, address, administrativeUnitId, latitude?, longitude?, categoryIds (guid[]), tags (string[]), moderationStatus (0=Pending/1=Approved/2=Rejected), createdBy, approvedBy?, approvedAt?, createdAt, updatedAt, images (List<MediaAssetResponse>: id, resourceType, resourceId, url, altText, sortOrder, createdAt — danh sách ảnh từ media_assets, được load trực tiếp trong response)

        GET /api/places — Public, phân trang → PlaceResponse[] (chỉ moderationStatus=1 Approved)

        GET /api/places/all — Admin/Contributor, phân trang → PlaceResponse[] (tất cả status, Contributor chỉ thấy dữ liệu trong scope)

        GET /api/places/{id} — Public → PlaceResponse

        POST /api/places — Admin/Contributor
        Body: title* (string, max 200), description* (string), address* (string, max 500), administrativeUnitId* (guid), latitude? (double, -90 đến 90), longitude? (double, -180 đến 180), categoryIds (guid[]), tags (string[])
        Điều kiện: administrativeUnitId phải tồn tại. Contributor chỉ tạo trong scope đơn vị hành chính của mình (403 nếu ngoài scope). Tự động moderationStatus=0 (Pending), cần duyệt mới hiện trên trang public.
        Lỗi: 404 nếu đơn vị hành chính không tồn tại. 403 nếu Contributor ngoài scope.

        PUT /api/places/{id} — Admin/Contributor
        Body: giống POST. Admin sửa tất cả. Contributor chỉ sửa place mình tạo và trong scope.
        Lỗi: 404 nếu place không tồn tại. 403 nếu không có quyền.

        DELETE /api/places/{id} — Admin/Contributor
        Admin xóa tất cả. Contributor chỉ xóa place mình tạo và trong scope.
        Lỗi: 404 nếu không tìm thấy. 403 nếu không có quyền.

        POST /api/places/seed — Admin
        Tạo sẵn 16 place mẫu (khu vực Sa Pa, Lào Cai), tự động Approved + tạo ảnh mặc định cho mỗi place. Bỏ qua nếu place cùng title đã tồn tại (gọi nhiều lần không sao). Tự tạo đơn vị hành chính Lào Cai/Sa Pa nếu chưa có. Yêu cầu: phải seed accounts (POST /api/dbtest/seed-accounts) và seed categories (POST /api/categories/seed) trước khi gọi API này.
        """;

    public static string Events() => """
        ## Events (/api/events)

        EventResponse: id, title, description, address, administrativeUnitId, latitude?, longitude?, categoryIds (guid[]), tags (string[]), startAt, endAt, eventStatus (0=Upcoming/1=Ongoing/2=Ended), moderationStatus (0=Pending/1=Approved/2=Rejected), createdBy, approvedBy?, approvedAt?, createdAt, updatedAt, images (List<MediaAssetResponse>: id, resourceType, resourceId, url, altText, sortOrder, createdAt — danh sách ảnh từ media_assets, được load trực tiếp trong response)

        GET /api/events — Public, phân trang → EventResponse[] (chỉ moderationStatus=1 Approved)

        GET /api/events/all — Admin/Contributor, phân trang → EventResponse[] (tất cả status, Contributor chỉ thấy trong scope)

        GET /api/events/{id} — Public → EventResponse

        POST /api/events — Admin/Contributor
        Body: title* (string, max 200), description* (string), address* (string, max 500), administrativeUnitId* (guid), latitude? (double, -90 đến 90), longitude? (double, -180 đến 180), categoryIds (guid[]), tags (string[]), startAt* (datetime), endAt* (datetime)
        Điều kiện: endAt PHẢI lớn hơn startAt. administrativeUnitId phải tồn tại. Contributor chỉ tạo trong scope. Tự động moderationStatus=0 (Pending), eventStatus=0 (Upcoming).
        Lỗi: 400 nếu endAt <= startAt. 404 nếu đơn vị hành chính không tồn tại. 403 nếu Contributor ngoài scope.

        PUT /api/events/{id} — Admin/Contributor
        Body: giống POST + eventStatus (int: 0=Upcoming/1=Ongoing/2=Ended). Admin sửa tất cả. Contributor chỉ sửa event mình tạo và trong scope.
        Lỗi: 404 nếu event không tồn tại. 403 nếu không có quyền.

        DELETE /api/events/{id} — Admin/Contributor
        Lỗi: 404 nếu không tìm thấy. 403 nếu không có quyền.

        POST /api/events/seed — Admin
        Tạo sẵn 16 event mẫu (khu vực Sa Pa, Lào Cai), tự động Approved + tạo ảnh mặc định. Bỏ qua nếu event cùng title đã tồn tại. Tự tạo đơn vị hành chính Lào Cai/Sa Pa nếu chưa có. Yêu cầu: phải seed admin và seed categories trước.
        """;

    public static string Moderation() => """
        ## Moderation (/api/moderation) — Admin/Contributor

        resourceType trong URL: Place hoặc Event. Contributor chỉ duyệt/từ chối dữ liệu thuộc scope của mình. Admin duyệt tất cả.

        PATCH /api/moderation/{resourceType}/{id}/approve — Admin/Contributor
        Body: note? (string, max 500)
        Khi duyệt: set moderationStatus=1 (Approved), ghi approvedBy và approvedAt. Tạo log.
        Lỗi: 404 nếu resource không tồn tại. 403 nếu không có quyền duyệt.
        → ModerationLogResponse: id, resourceType, resourceId, action, note, actedBy, actedAt

        PATCH /api/moderation/{resourceType}/{id}/reject — Admin/Contributor
        Body: note? (string, max 500)
        Khi từ chối: set moderationStatus=2 (Rejected), xóa approvedBy và approvedAt. Tạo log.
        Lỗi: 404 nếu resource không tồn tại. 403 nếu không có quyền.
        → ModerationLogResponse

        GET /api/moderation/{resourceType}/{id}/logs — Admin/Contributor → ModerationLogResponse[]
        """;

    public static string Media() => """
        ## Media (/api/media)

        Quy trình upload: Bước 1 gọi upload-signature lấy chữ ký → Bước 2 frontend upload trực tiếp lên Cloudinary bằng signature → Bước 3 gọi finalize để lưu metadata vào DB.

        MediaAssetResponse: id, resourceType (0=Place/1=Event), resourceId, url, secureUrl, publicId, format, mimeType, bytes, width, height, isPrimary, sortOrder, uploadedBy, createdAt

        POST /api/media/upload-signature — Admin/Contributor
        Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid)
        Lỗi: 403 nếu Contributor ngoài scope.
        → UploadSignatureResponse: signature, timestamp (long), apiKey, cloudName, folder

        POST /api/media/finalize — Admin/Contributor
        Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid), publicId* (string), url* (string), secureUrl* (string), format* (string), mimeType* (string), bytes* (long), width* (int), height* (int)
        Điều kiện: Ảnh đầu tiên upload cho resource sẽ tự động là ảnh chính (isPrimary=true). Các ảnh sau isPrimary=false. sortOrder tự tăng.
        Lỗi: 403 nếu không có quyền.
        → MediaAssetResponse

        GET /api/media/by-resource?resourceType=Place&resourceId=xxx — Public → MediaAssetResponse[]

        PATCH /api/media/{id}/set-primary — Admin/Contributor
        Bỏ isPrimary của ảnh cũ, đặt ảnh này làm primary.
        → MediaAssetResponse

        PATCH /api/media/reorder — Admin/Contributor
        Body: orderedIds* (guid[], không được rỗng) — danh sách media ID theo thứ tự mong muốn. sortOrder sẽ được cập nhật lại theo thứ tự 0, 1, 2...
        → Result

        DELETE /api/media/{id} — Admin/Contributor
        Xóa cả trên Cloudinary và DB. Nếu ảnh bị xóa là primary thì ảnh tiếp theo (theo sortOrder) sẽ tự động thành primary.
        Lỗi: 404 nếu không tìm thấy. 403 nếu không có quyền.
        """;

    public static string Reviews() => """
        ## Reviews (/api/reviews)

        ReviewResponse: id, resourceType (0=Place/1=Event), resourceId, userId, userFullName (string, tên user), userAvatarUrl (string, ảnh đại diện user), rating, comment, status (0=Active/1=Hidden/2=Deleted), createdAt, updatedAt

        POST /api/reviews — Login
        Body: resourceType* (int: 0=Place/1=Event), resourceId* (guid), rating* (int, từ 1 đến 5), comment? (string, max 1000)
        Điều kiện: Resource phải tồn tại VÀ đã được duyệt (moderationStatus=1 Approved). Nếu resource chưa duyệt hoặc không tồn tại → 404. Mỗi lần gọi tạo 1 review mới, 1 user có thể đánh giá nhiều lần cho cùng 1 resource.
        Lỗi: 404 nếu resource không tồn tại hoặc chưa Approved.
        → ReviewResponse

        PATCH /api/reviews/{id} — Login
        Body: rating* (int, 1-5), comment? (string, max 1000)
        Điều kiện: Chỉ chủ review (userId trùng) mới sửa được.
        Lỗi: 404 nếu review không tồn tại. 403 nếu không phải chủ review.
        → ReviewResponse

        DELETE /api/reviews/{id} — Login
        Chủ review hoặc Admin mới xóa được.
        Lỗi: 404 nếu không tìm thấy. 403 nếu không phải chủ review và không phải Admin.

        GET /api/reviews?resourceType=Place&resourceId=xxx — Public, phân trang
        → ReviewListResponse: averageRating (double, sao trung bình làm tròn 1 chữ số thập phân), totalReviews (int, tổng số review active), reviews (phân trang ReviewResponse[]: items[], totalCount, pageNumber, pageSize, totalPages, hasPreviousPage, hasNextPage). Chỉ hiện review có status=0 Active, sắp xếp mới nhất trước.

        GET /api/reviews/mine?resourceType=Place&resourceId=xxx — Login, phân trang → ReviewResponse[] (danh sách tất cả review của user hiện tại cho resource đó, sắp xếp mới nhất trước)
        """;

    public static string Discovery() => """
        ## Discovery (/api/discovery) — Public

        ### Tìm kiếm nâng cao (có đầy đủ filter)

        GET /api/discovery/places — Public, phân trang (chỉ trả place đã Approved)
        Query: search? (string, tìm trong title và description, không phân biệt hoa thường), categoryId? (guid), administrativeUnitId? (guid), tag? (string, tìm trong mảng tags), sortBy (newest/oldest/rating/name, default: newest). rating sắp theo trung bình rating của review Active giảm dần.
        → PlaceResponse[] (mỗi item có averageRating)

        GET /api/discovery/events — Public, phân trang (chỉ trả event đã Approved)
        Query: search? (string, tìm trong title và description), categoryId? (guid), administrativeUnitId? (guid), tag? (string), sortBy (newest/oldest/rating/name/startdate, default: newest). startdate sắp theo startAt tăng dần.
        → EventResponse[] (mỗi item có averageRating)

        ### Tìm kiếm đơn giản (Simple Search) — hỗ trợ lọc theo khoảng sao

        GET /api/discovery/search/places — Public, phân trang (chỉ trả place đã Approved)
        Query: search? (string, tìm trong title và description), sortBy (newest/oldest/rating/name, default: newest), averageRating? (int: 5 → chỉ lấy rating đúng 5.0, 4 → rating từ 4.0 đến 4.99, 3 → từ 3.0 đến 3.99, tương tự cho 2 và 1. Không truyền thì không lọc theo rating)
        → PlaceResponse[] (mỗi item có averageRating)

        GET /api/discovery/search/events — Public, phân trang (chỉ trả event đã Approved)
        Query: search? (string, tìm trong title và description), sortBy (newest/oldest/rating/name/startdate, default: newest), averageRating? (int: 5 → chỉ lấy rating đúng 5.0, 4 → rating từ 4.0 đến 4.99, 3 → từ 3.0 đến 3.99, tương tự cho 2 và 1. Không truyền thì không lọc theo rating)
        → EventResponse[] (mỗi item có averageRating)
        """;

    public static string Chat() => """
        ## Chat AI (/api/chat) — Login (mọi role)

        ConversationResponse: id, title, model, status (0=Active/1=Archived), lastMessageAt, createdAt
        MessageResponse: id, conversationId, role (0=User/1=Assistant/2=System), content, tokenCount, citations, createdAt

        POST /api/chat/conversations — Login
        Body: title? (string, max 200, nếu không gửi mặc định "Cuộc trò chuyện mới")
        → ConversationResponse

        GET /api/chat/conversations — Login, phân trang → ConversationResponse[] (chỉ conversation của user hiện tại)

        GET /api/chat/conversations/{id}/messages — Login, phân trang → MessageResponse[] (chỉ xem conversation của mình)
        Lỗi: 404 nếu conversation không tồn tại hoặc không phải của user.

        POST /api/chat/conversations/{id}/messages — Login
        Body: content* (string, max 5000)
        AI trả lời dựa trên: sở thích user (categories), dữ liệu thực (tối đa 20 places đã Approved + 10 events chưa Ended), lịch sử chat (15 tin nhắn gần nhất). Cứ 10 tin nhắn hệ thống tự tạo tóm tắt để duy trì ngữ cảnh dài hạn.
        Lỗi: 404 nếu conversation không tồn tại hoặc không phải của user.
        → MessageResponse

        POST /api/chat/conversations/{id}/messages/stream — Login
        Body: content* (string, max 5000)
        Giống API trên nhưng trả về dạng SSE streaming. Mỗi chunk: data: {"content":"..."}, kết thúc: data: [DONE]
        Lỗi: 404 nếu conversation không tồn tại hoặc không phải của user.
        """;

    public static string TestDev() => """
        ## Test / Dev — Public (chỉ dùng khi phát triển)

        GET /api/dbtest — test kết nối database
        POST /api/dbtest/create-tables — tạo toàn bộ tables (query reset=true để xóa schema cũ và tạo lại từ đầu)
        POST /api/dbtest/seed-accounts — tạo 4 tài khoản mặc định (bỏ qua nếu đã tồn tại):
          Admin: admin@aitourism.vn / admin123
          Contributor (Province - Đà Nẵng): contributor.province@aitourism.vn / contributor123
          Contributor (Ward - Hải Châu): contributor.ward@aitourism.vn / contributor123
          User: user@aitourism.vn / user123

        POST /api/dbtest/reset-and-seed-all — reset toàn bộ database và seed lại tất cả dữ liệu: tạo bảng → seed đơn vị hành chính + categories → seed accounts (admin, 2 contributors, user) → seed places → seed events. Trả về danh sách từng bước thực hiện và trạng thái.

        POST /api/geminitests — Body: prompt* (string) → response (string)

        POST /api/geminitests/ask-api — Body: question* (string) → question, answer (Trợ lý API trả lời)

        POST /api/geminitests/test-prompt — Body: userMessage* (string), userPreferences? (string[]), userLatitude? (double), userLongitude? (double), fakePlaces? (object[]), fakeEvents? (object[]) → systemPrompt, userMessage, aiResponse
        """;

    public static string PromptRules() => """
        === PHONG CÁCH TRẢ LỜI ===
        - Luôn xưng "em", gọi người hỏi là "anh".
        - Trả lời bằng tiếng Việt, thân thiện, ngắn gọn, xúc tích. Không dài dòng, không lặp lại thông tin thừa.
        - Đi thẳng vào vấn đề, không mở đầu kiểu "Dạ anh ơi, để em giải thích..." quá dài.
        - Trả lời dưới dạng VĂN XUÔI tự nhiên, như đang nói chuyện. KHÔNG dùng markdown, KHÔNG dùng bullet point, KHÔNG dùng ký tự đặc biệt (*, -, #, `, >, |...). Không xuống dòng liên tục. Viết thành đoạn văn liền mạch.
        - Giọng điệu hài hước, dí dỏm, có thể pha chút đùa nhẹ nhàng nhưng vẫn đảm bảo thông tin đầy đủ và chính xác. Ví dụ thay vì nói "API này cần quyền Admin" thì có thể nói "API này chỉ dành cho Admin thôi anh, user thường mà gọi là bị đuổi về 403 liền".

        === QUY TẮC ===
        - CHỈ trả lời dựa trên tài liệu API bên trên. KHÔNG bịa thêm endpoint, field, hoặc giá trị enum không có trong tài liệu.
        - Khi giải thích một API, LUÔN nêu rõ:
          + Method và URL đầy đủ
          + Quyền truy cập: Public (ai cũng gọi được), Login (cần JWT token), Admin (chỉ Admin), Admin/Contributor (Admin hoặc Contributor trong scope)
          + Nếu là Admin/Contributor, giải thích rõ Contributor bị giới hạn scope đơn vị hành chính như thế nào
          + Các tham số cần gửi (bắt buộc/tùy chọn) và kiểu dữ liệu
          + Các điều kiện, validation rules, và lỗi có thể xảy ra
          + Response trả về
        - Với các field kiểu enum, LUÔN nêu rõ giá trị số (int) tương ứng. VD: "role gửi dạng số: 0=Admin, 1=Contributor, 2=User". Không được chỉ nêu tên enum mà không kèm số.
        - Nếu người dùng hỏi về API không có trong tài liệu, nói rõ là chưa có.
        - Có thể so sánh, phân loại, liệt kê nhóm API khi người dùng yêu cầu.

        === QUAN TRỌNG ===
        - Nếu không chắc chắn hoặc câu hỏi nằm ngoài phạm vi tài liệu API bên trên, KHÔNG ĐƯỢC trả lời bừa. Hãy nói: "Em chưa có đủ dữ liệu để trả lời chính xác câu này anh ơi. Tài liệu API hiện tại chưa bao gồm thông tin đó."
        - KHÔNG suy đoán, KHÔNG bịa thông tin. Chỉ trả lời những gì có trong tài liệu.
        """;
}

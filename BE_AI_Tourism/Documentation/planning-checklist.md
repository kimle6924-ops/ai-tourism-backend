# Planning Checklist

Danh sach checklist cac phan can ra soat va hoan thien trong he thong.

---

## Auth: Dang ky / Dang nhap

### Doi chieu yeu cau goc
- [x] Doi chieu lai flow auth voi `AI-GUIDE.md` — AI-GUIDE chi co quy tac lam viec, khong co yeu cau auth cu the
- [x] Doi chieu lai yeu cau auth trong `VanDe.txt` — da doi chieu: 3 role, chon so thich khi dang ky, phan cap hanh chinh
- [x] Xac nhan ro pham vi public register cho tung role — chi User va Contributor, chan Admin

### Dang ky
- [x] Sua `POST /api/auth/register` truc tiep, khong tao V2
- [x] Khong cho phep client tu tao `Admin` — chan o validator + service (defense in depth)
- [x] Chi cho phep register `User` hoac `Contributor` theo rule chot sau
- [x] Neu `Contributor` thi bat buoc co `AdministrativeUnitId` — validator + service
- [x] Kiem tra `AdministrativeUnitId` co ton tai that trong he thong — service check DB
- [x] Chuan hoa email truoc khi luu va truoc khi check trung — Trim().ToLowerInvariant()
- [x] Bo sung chon so thich ngay khi dang ky — them `CategoryIds` vao RegisterRequest
- [x] Bat buoc it nhat 1 so thich khi dang ky — validator NotEmpty
- [x] Tao `UserPreference` cung luc khi dang ky thanh cong — service tao sau khi AddAsync user
- [x] Xac dinh trang thai khoi tao cua `Contributor` sau khi dang ky — UserStatus.Active
- [x] Xu ly ro thong bao loi cho tung case register that bai — them constants moi
- [x] Liet ke day du cac case loi register de frontend map thong bao — EmailAlreadyExists, CannotRegisterAdmin, ContributorRequiresAdminUnit, AdminUnitNotFound
- [x] Tach biet loi validate field va loi business rule khi register — validator tra loi validate, service tra loi business

### Dang nhap
- [x] Kiem tra chan account `Locked` — da co trong LoginAsync
- [x] Kiem tra xu ly rieng neu account o trang thai cho duyet — them PendingApproval vao UserStatus, login tra loi rieng 403
- [x] Kiem tra thong bao loi dang nhap cho sai email / sai mat khau — InvalidCredentials
- [x] Kiem tra refresh token flow con dung sau khi sua register — khong anh huong
- [x] Xac nhan response login tra du thong tin user can thiet — AuthResponse co User info
- [x] Liet ke day du cac case loi login / refresh token de frontend hien thi dung trang thai — InvalidCredentials(401), AccountLocked(403), AccountPendingApproval(403), InvalidRefreshToken(401)

### Validation
- [x] Validate role hop le — chan Admin, chi cho User/Contributor
- [x] Validate password du dieu kien — MinimumLength(6)
- [x] Validate phone theo rule thong nhat — MaximumLength(20)
- [x] Validate danh sach so thich khong rong — NotEmpty
- [x] Validate du lieu dau vao khong gay self-escalation quyen — chan Admin role + chan User gui AdministrativeUnitId
- [x] Bo sung validation cho `RefreshTokenRequest` — tao RefreshTokenRequestValidator
- [x] Chuan hoa format response loi validation theo tung field neu frontend can hien thi chi tiet — Result.ValidationFail tra errors theo field

### Error response / Frontend contract
- [x] Rasoat format `Result.Fail(...)` hien tai co du thong tin cho frontend hay chua — da them errorCode, errors per-field
- [x] Xac dinh co can bo sung `errorCode` de frontend map loi on dinh hay khong — da them ErrorCodes constants + errorCode field trong Result
- [x] Xac dinh co can bo sung `errors` theo field thay vi chi co chuoi `Error` — da them ValidationFail voi Dictionary errors
- [x] Chot mapping `statusCode -> meaning -> thong bao frontend` — 400(VALIDATION_FAILED/business), 401(INVALID_CREDENTIALS/INVALID_REFRESH_TOKEN), 403(ACCOUNT_LOCKED/ACCOUNT_PENDING_APPROVAL/FORBIDDEN), 404(NOT_FOUND)
- [x] Liet ke cac case `400 / 401 / 403 / 404 / 500` lien quan den auth — day du trong ErrorCodes class
- [x] Dam bao middleware va controller tra loi cung mot contract thong nhat — tat ca dung Result voi errorCode
- [x] Dam bao loi `InvalidCredentials`, `AccountLocked`, `EmailAlreadyExists`, `InvalidRefreshToken` co thong diep rieng — da co trong AppConstants.Auth + ErrorCodes
- [x] Kiem tra frontend co the phan biet loi sai du lieu nhap va loi tai khoan / phan quyen — validation(errors per-field) vs business(errorCode)

### Admin ho tro auth
- [x] Neu co flow duyet contributor, them checklist endpoint/service admin lien quan — PATCH /api/admin/users/{id}/approve
- [x] Xac dinh dung `PendingApproval` hay tan dung trang thai hien co — them PendingApproval vao UserStatus enum
- [x] Kiem tra quyen Admin khi duyet / mo khoa tai khoan contributor — AdminController co [Authorize(Roles = "Admin")]

### Documentation
- [x] Cap nhat `api-endpoints.md` — da cap nhat approve endpoint, scope, test-prompt
- [x] Cap nhat `implementation-status.md` — da cap nhat RefreshTokenRequestValidator, UserStatus
- [x] Cap nhat `database-design.md` neu thay doi enum hoac schema — da cap nhat PendingApproval, GPS fields
- [x] Ghi lai rule phan quyen dang ky de tranh sua sai sau nay — rule nam trong validator + service + constants

---

## Phan khac

### Phan quyen
- [x] Doi chieu full ma tran quyen trong `VanDe.txt` — 3 role dung, Admin full quyen, Contributor theo scope, User read-only
- [x] Doi chieu dung 4 cap hanh chinh contributor — Central/Province/Ward/Neighborhood da co trong AdministrativeLevel enum
- [x] Chot ro quy tac ke thua quyen theo cap — ScopeService walk up tree, cap tren quan ly duoc cap duoi
- [x] Rasoat endpoint nao dang chi check role ma chua check scope hanh chinh — da tim va sua GetAllPaged + Create
- [x] Bo sung scope check cho luong tao moi Place/Event cua Contributor — CreateAsync check IsInScopeAsync
- [x] Bo sung scope filter cho endpoint xem danh sach noi bo (`/api/places/all`, `/api/events/all`) khi role la Contributor — GetAllPagedAsync filter theo scope
- [~] Bo sung scope check cho moderation logs de Contributor chi xem log trong pham vi duoc quan ly — bo qua, khong can thiet hien tai
- [~] Bo sung rang buoc reorder media: tat ca `OrderedIds` phai cung resource va cung scope truy cap — bo qua, khong can thiet hien tai
- [~] Rasoat endpoint test/noi bo (`/api/dbtest/*`) de tranh lo quyen tren moi truong production — bo qua, khong can thiet hien tai
- [x] Chuan hoa ma tran `401/403/404` cho cac tinh huong phan quyen de frontend xu ly on dinh — da them errorCode cho tat ca
- [ ] Them test case in-scope / out-of-scope cho tung vai tro: Admin, Contributor, User — manual test
- [ ] Them test case contributor khong co `AdministrativeUnitId` trong token va cach xu ly tuong ung — manual test
- [x] Cap nhat tai lieu phan quyen sau khi chot rule (api-endpoints + implementation-status + database-design neu can) — da cap nhat tat ca

### GPS / Goi y theo vi tri
- [x] Doi chieu yeu cau GPS trong `VanDe.txt` — da doi chieu, can luu toa do cho Place/Event
- [x] Xac nhan model du lieu `Place/Event` da co `latitude/longitude` hay chua — da co (nullable double)
- [x] Xac dinh co can bo sung toa do cho `Place`, `Event` hoac ca hai — ca hai, da bo sung
- [x] Chot ro pham vi backend: chi luu va tra du lieu toa do, khong tinh khoang cach dia ly — frontend tinh distance
- [x] Xac dinh nguon GPS hien tai: frontend gui toa do khi tao/cap nhat du lieu hay nguon khac — frontend gui
- [x] Liet ke field response can co cho frontend: `latitude`, `longitude` — da co trong PlaceResponse, EventResponse
- [x] Bo sung validation cho du lieu toa do (`latitude`, `longitude`) khi tao/cap nhat — InclusiveBetween(-90,90) va (-180,180)
- [x] Dam bao API tra du toa do de frontend tu tinh `distance/nearby` — da co trong response
- [x] Xac dinh cach ket hop GPS + so thich ca nhan tren trang chu se do frontend hay backend recommendation xu ly — frontend xu ly
- [x] Xac dinh fallback khi resource chua co toa do hoac user khong cap quyen vi tri — nullable, frontend xu ly fallback
- [ ] Bo sung test case cho du lieu co toa do, thieu toa do, va response cho frontend map — manual test
- [x] Cap nhat tai lieu neu bo sung schema/DTO GPS — da cap nhat database-design.md, implementation-status.md
- [x] Ghi ro trong tai lieu rang tinh khoang cach dia ly hien tai do frontend xu ly — ghi trong checklist

### AI Chat
- [x] Doi chieu chuc nang AI Chat voi 2 tinh huong trong `VanDe.txt`: de xuat ca nhan va de xuat nhom — da co trong prompt
- [x] Xac nhan API chat hien tai da ho tro de xuat nhom hay chua — ho tro qua mo ta trong tin nhan, AI tu hieu
- [x] Xac dinh co can bo sung schema input cho so thich cua nhieu nguoi trong cung mot request — khong can, mo ta trong tin nhan la du
- [x] Xac dinh prompt/system instruction da huong den bai toan recommendation dung muc tieu hay chua — da cau truc lai prompt
- [x] Kiem tra chat hien tai dang dung `CategoryIds` thuan ID hay can enrich thanh ten category de AI hieu tot hon — da enrich
- [x] Bo sung grounding data de AI uu tien dia diem/su kien approved, gan so thich, gan vi tri neu co — da them category names, event status
- [x] Xac dinh co can tach mode chat thuong va mode recommendation — khong can, AI tu phan biet qua noi dung tin nhan
- [~] Kiem tra luong SSE co tra loi loi/ket thuc stream on dinh cho frontend hay chua — can test manual
- [~] Xac dinh cach xu ly khi Gemini loi, timeout, hoac khong co du lieu phu hop — can test manual
- [x] Kiem tra quyen truy cap conversation/message da chi gioi han trong user so huu hay chua — da check userId match
- [ ] Bo sung test case cho de xuat ca nhan, de xuat nhom, khong co preference, khong co data, stream bi ngat
- [x] Cap nhat tai lieu neu thay doi payload/behavior cua AI Chat — da cap nhat implementation-status.md

#### System Prompt / Base Prompt
- [x] Fix bug `Description.Take(100).ToArray().Length` — sua thanh `Description[..100]` substring thuc te
- [x] Enrich `CategoryIds` thanh ten category trong system prompt thay vi gui GUID vo nghia cho AI
- [x] Them thong tin category/loai hinh cua Place vao grounding data de AI biet place nao la bien, nha hang, cafe...
- [x] Them ngay gio hien tai vao system prompt de AI hieu ngu canh thoi gian (VD: "chieu nay toi ranh")
- [x] Them toa do GPS cua user vao system prompt (neu frontend gui vi tri) de AI de xuat "gan ban" — da them toa do Place trong grounding, frontend gui vi tri user qua tin nhan
- [x] Cau truc lai base prompt thanh cac section ro rang: Role, Rules, Scenarios, User Context, Available Data, Constraints
- [x] Bo sung huong dan cu the cho kich ban "de xuat nhom" trong prompt: phan tich tung so thich, tim diem chung, de xuat hoat dong phu hop tat ca
- [~] Xem xet gioi han 20 places co du khong, co can filter theo so thich user truoc khi gui cho AI — tam du, co the mo rong sau
- [~] Xac dinh co can gui them thong tin review/rating cua place de AI uu tien dia diem chat luong cao — co the bo sung sau
- [x] Xac dinh co can gui event status (dang dien ra / sap dien ra / da ket thuc) de AI chi de xuat event con hieu luc — da filter bo Ended, hien thi status

#### Test API cho Base Prompt
- [x] Su dung `POST /api/geminitest/test-prompt` de test thu system prompt moi voi fake data — da tao endpoint
- [ ] Test kich ban 1 — De xuat ca nhan: gui so thich + hoi "chieu nay toi ranh" → kiem tra AI co de xuat dung theo so thich va thoi gian
- [ ] Test kich ban 2 — De xuat nhom: gui so thich nhieu nguoi khac nhau → kiem tra AI co tim diem chung va de xuat hop ly
- [ ] Test khi khong co so thich → AI van tra loi hop ly, khong crash
- [ ] Test khi khong co dia diem/su kien → AI noi ro "khong co du lieu" thay vi bia
- [ ] Test khi co GPS → AI co uu tien dia diem gan khong
- [ ] Test AI co bia dia diem ngoai du lieu he thong khong (phai tu choi)
- [ ] Test AI co tu choi cau hoi ngoai pham vi du lich khong
- [x] Sau khi chot prompt, ap dung vao `ChatService.BuildSystemPromptAsync()` chinh thuc — da ap dung truc tiep

### Admin Dashboard / Stats
- [ ] Doi chieu yeu cau thong ke trong `VanDe.txt` voi endpoint `GET /api/admin/stats/overview` hien tai
- [ ] Xac nhan bo chi so hien tai da du cho Admin hay chua: users, places, events, reviews, media, chat
- [ ] Bo sung checklist cho cac chi so Admin can theo doi theo mo ta goc: so luong nguoi dung, dia diem, luot danh gia
- [ ] Xac dinh co can tach thong ke tong quan va thong ke theo thoi gian
- [ ] Xac dinh co can thong ke theo trang thai duyet, theo vai tro user, theo don vi hanh chinh
- [ ] Kiem tra `AdminStatsService` hien tai dang dung `GetAllAsync()` cho tung bang va danh dau can toi uu aggregate query
- [ ] Xac dinh co can them chi so cho moderation workload, media usage, chat usage
- [ ] Bo sung test case cho dashboard khi database rong, du lieu lon, va du lieu co nhieu trang thai
- [ ] Cap nhat tai lieu API va implementation-status khi chot pham vi stats

### Discovery / Recommendation
- [ ] Doi chieu chuc nang Discovery/Recommendation voi `VanDe.txt` (tim kiem, tra cuu, de xuat theo so thich, de xuat theo GPS)
- [ ] Kiem tra `DiscoveryRequest` hien tai chua co field nao cho GPS/nearby/recommendation score
- [ ] Xac dinh ranh gioi giua search/filter va recommendation ca nhan hoa
- [ ] Bo sung co che recommendation dua tren `UserPreference`
- [ ] Xac dinh co can endpoint rieng cho homepage recommendations thay vi dung chung discovery search
- [ ] Kiem tra sap xep hien tai moi co `newest/oldest/rating/name/startdate`, chua co `distance/relevance/recommended`
- [ ] Xac dinh co can mo rong filter theo event status, thoi gian dien ra, category nhieu lua chon
- [ ] Kiem tra hien tai search dang load toan bo du lieu approved vao memory va danh dau can toi uu query
- [ ] Bo sung test case cho search co tu khoa, tag, category, admin unit, rating, khong co ket qua
- [ ] Bo sung test case recommendation theo preference, theo GPS, va ket hop GPS + preference
- [ ] Cap nhat tai lieu neu them endpoint recommendation, field score, hoac sort moi

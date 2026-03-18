# Planning Checklist

Danh sach checklist cac phan can ra soat va hoan thien trong he thong.

---

## Auth: Dang ky / Dang nhap

### Doi chieu yeu cau goc
- [ ] Doi chieu lai flow auth voi `AI-GUIDE.md`
- [ ] Doi chieu lai yeu cau auth trong `D:\1.University\Job\DuLichDiaPhuong\VanDe.txt`
- [ ] Xac nhan ro pham vi public register cho tung role

### Dang ky
- [ ] Sua `POST /api/auth/register` truc tiep, khong tao V2
- [ ] Khong cho phep client tu tao `Admin`
- [ ] Chi cho phep register `User` hoac `Contributor` theo rule chot sau
- [ ] Neu `Contributor` thi bat buoc co `AdministrativeUnitId`
- [ ] Kiem tra `AdministrativeUnitId` co ton tai that trong he thong
- [ ] Chuan hoa email truoc khi luu va truoc khi check trung
- [ ] Bo sung chon so thich ngay khi dang ky
- [ ] Bat buoc it nhat 1 so thich khi dang ky
- [ ] Tao `UserPreference` cung luc khi dang ky thanh cong
- [ ] Xac dinh trang thai khoi tao cua `Contributor` sau khi dang ky
- [ ] Xu ly ro thong bao loi cho tung case register that bai
- [ ] Liet ke day du cac case loi register de frontend map thong bao
- [ ] Tach biet loi validate field va loi business rule khi register

### Dang nhap
- [ ] Kiem tra chan account `Locked`
- [ ] Kiem tra xu ly rieng neu account o trang thai cho duyet
- [ ] Kiem tra thong bao loi dang nhap cho sai email / sai mat khau
- [ ] Kiem tra refresh token flow con dung sau khi sua register
- [ ] Xac nhan response login tra du thong tin user can thiet
- [ ] Liet ke day du cac case loi login / refresh token de frontend hien thi dung trang thai

### Validation
- [ ] Validate role hop le
- [ ] Validate password du dieu kien
- [ ] Validate phone theo rule thong nhat
- [ ] Validate danh sach so thich khong rong
- [ ] Validate du lieu dau vao khong gay self-escalation quyen
- [ ] Bo sung validation cho `RefreshTokenRequest`
- [ ] Chuan hoa format response loi validation theo tung field neu frontend can hien thi chi tiet

### Error response / Frontend contract
- [ ] Rasoat format `Result.Fail(...)` hien tai co du thong tin cho frontend hay chua
- [ ] Xac dinh co can bo sung `errorCode` de frontend map loi on dinh hay khong
- [ ] Xac dinh co can bo sung `errors` theo field thay vi chi co chuoi `Error`
- [ ] Chot mapping `statusCode -> meaning -> thong bao frontend`
- [ ] Liet ke cac case `400 / 401 / 403 / 404 / 500` lien quan den auth
- [ ] Dam bao middleware va controller tra loi cung mot contract thong nhat
- [ ] Dam bao loi `InvalidCredentials`, `AccountLocked`, `PendingApproval`, `EmailAlreadyExists`, `InvalidRefreshToken` co thong diep rieng
- [ ] Kiem tra frontend co the phan biet loi sai du lieu nhap va loi tai khoan / phan quyen

### Admin ho tro auth
- [ ] Neu co flow duyet contributor, them checklist endpoint/service admin lien quan
- [ ] Xac dinh dung `PendingApproval` hay tan dung trang thai hien co
- [ ] Kiem tra quyen Admin khi duyet / mo khoa tai khoan contributor

### Documentation
- [ ] Cap nhat `api-endpoints.md`
- [ ] Cap nhat `implementation-status.md`
- [ ] Cap nhat `database-design.md` neu thay doi enum hoac schema
- [ ] Ghi lai rule phan quyen dang ky de tranh sua sai sau nay

---

## Phan khac

### Phan quyen
- [ ] Doi chieu full ma tran quyen trong `D:\1.University\Job\DuLichDiaPhuong\VanDe.txt` (Admin / Nguoi dung / Nguoi cung cap thong tin)
- [ ] Doi chieu dung 4 cap hanh chinh contributor trong `VanDe.txt` (Trung uong -> Tinh/Thanh pho -> Phuong/Xa -> To dan pho)
- [ ] Chot ro quy tac ke thua quyen theo cap (cap tren quan ly duoc tat ca cap duoi) theo mo ta trong `VanDe.txt`
- [ ] Rasoat endpoint nao dang chi check role ma chua check scope hanh chinh
- [ ] Bo sung scope check cho luong tao moi Place/Event cua Contributor
- [ ] Bo sung scope filter cho endpoint xem danh sach noi bo (`/api/places/all`, `/api/events/all`) khi role la Contributor
- [ ] Bo sung scope check cho moderation logs de Contributor chi xem log trong pham vi duoc quan ly
- [ ] Bo sung rang buoc reorder media: tat ca `OrderedIds` phai cung resource va cung scope truy cap
- [ ] Rasoat endpoint test/noi bo (`/api/dbtest/*`) de tranh lo quyen tren moi truong production
- [ ] Chuan hoa ma tran `401/403/404` cho cac tinh huong phan quyen de frontend xu ly on dinh
- [ ] Them test case in-scope / out-of-scope cho tung vai tro: Admin, Contributor, User
- [ ] Them test case contributor khong co `AdministrativeUnitId` trong token va cach xu ly tuong ung
- [ ] Cap nhat tai lieu phan quyen sau khi chot rule (api-endpoints + implementation-status + database-design neu can)

### GPS / Goi y theo vi tri
- [ ] Doi chieu yeu cau GPS trong `D:\1.University\Job\DuLichDiaPhuong\VanDe.txt`
- [ ] Xac nhan model du lieu `Place/Event` da co `latitude/longitude` hay chua
- [ ] Xac dinh co can bo sung toa do cho `Place`, `Event` hoac ca hai
- [ ] Chot ro pham vi backend: chi luu va tra du lieu toa do, khong tinh khoang cach dia ly
- [ ] Xac dinh nguon GPS hien tai: frontend gui toa do khi tao/cap nhat du lieu hay nguon khac
- [ ] Liet ke field response can co cho frontend: `latitude`, `longitude`
- [ ] Bo sung validation cho du lieu toa do (`latitude`, `longitude`) khi tao/cap nhat
- [ ] Dam bao API tra du toa do de frontend tu tinh `distance/nearby`
- [ ] Xac dinh cach ket hop GPS + so thich ca nhan tren trang chu se do frontend hay backend recommendation xu ly
- [ ] Xac dinh fallback khi resource chua co toa do hoac user khong cap quyen vi tri
- [ ] Bo sung test case cho du lieu co toa do, thieu toa do, va response cho frontend map
- [ ] Cap nhat tai lieu neu bo sung schema/DTO GPS
- [ ] Ghi ro trong tai lieu rang tinh khoang cach dia ly hien tai do frontend xu ly

### AI Chat
- [ ] Doi chieu chuc nang AI Chat voi 2 tinh huong trong `VanDe.txt`: de xuat ca nhan va de xuat nhom
- [ ] Xac nhan API chat hien tai da ho tro de xuat nhom hay chua
- [ ] Xac dinh co can bo sung schema input cho so thich cua nhieu nguoi trong cung mot request
- [ ] Xac dinh prompt/system instruction da huong den bai toan recommendation dung muc tieu hay chua
- [ ] Kiem tra chat hien tai dang dung `CategoryIds` thuan ID hay can enrich thanh ten category de AI hieu tot hon
- [ ] Bo sung grounding data de AI uu tien dia diem/su kien approved, gan so thich, gan vi tri neu co
- [ ] Xac dinh co can tach mode chat thuong va mode recommendation
- [ ] Kiem tra luong SSE co tra loi loi/ket thuc stream on dinh cho frontend hay chua
- [ ] Xac dinh cach xu ly khi Gemini loi, timeout, hoac khong co du lieu phu hop
- [ ] Kiem tra quyen truy cap conversation/message da chi gioi han trong user so huu hay chua
- [ ] Bo sung test case cho de xuat ca nhan, de xuat nhom, khong co preference, khong co data, stream bi ngat
- [ ] Cap nhat tai lieu neu thay doi payload/behavior cua AI Chat

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

# API cho Admin & Người cung cấp thông tin

## 1. Admin

### Quản lý tài khoản
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/admin/users` | Danh sách người dùng (phân trang) |
| PATCH | `/api/admin/users/{id}/lock` | Khóa tài khoản |
| PATCH | `/api/admin/users/{id}/unlock` | Mở khóa tài khoản |
| PATCH | `/api/admin/users/{id}/approve` | Duyệt tài khoản người cung cấp thông tin |

### Thống kê
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/admin/stats/overview` | Thống kê tổng quan hệ thống |

### Quản lý đơn vị hành chính
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/administrative-units` | Danh sách đơn vị hành chính |
| GET | `/api/administrative-units/{id}` | Chi tiết đơn vị hành chính |
| GET | `/api/administrative-units/by-level/{level}` | Lấy theo cấp (0=Tỉnh, 1=Phường/Xã) |
| GET | `/api/administrative-units/{id}/children` | Lấy đơn vị con |
| POST | `/api/administrative-units` | Thêm đơn vị hành chính |
| PUT | `/api/administrative-units/{id}` | Sửa đơn vị hành chính |
| DELETE | `/api/administrative-units/{id}` | Xóa đơn vị hành chính |

### Quản lý danh mục
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/categories` | Danh sách danh mục (phân trang) |
| GET | `/api/categories/active` | Danh mục đang hoạt động |
| GET | `/api/categories/{id}` | Chi tiết danh mục |
| POST | `/api/categories` | Thêm danh mục |
| PUT | `/api/categories/{id}` | Sửa danh mục |
| DELETE | `/api/categories/{id}` | Xóa danh mục |

### Quản lý địa điểm
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/places` | Danh sách địa điểm đã duyệt |
| GET | `/api/places/all` | Tất cả địa điểm (mọi trạng thái) |
| GET | `/api/places/{id}` | Chi tiết địa điểm |
| POST | `/api/places` | Thêm địa điểm |
| PUT | `/api/places/{id}` | Sửa địa điểm |
| DELETE | `/api/places/{id}` | Xóa địa điểm |

### Quản lý sự kiện
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/events` | Danh sách sự kiện đã duyệt |
| GET | `/api/events/all` | Tất cả sự kiện (mọi trạng thái) |
| GET | `/api/events/{id}` | Chi tiết sự kiện |
| POST | `/api/events` | Thêm sự kiện |
| PUT | `/api/events/{id}` | Sửa sự kiện |
| DELETE | `/api/events/{id}` | Xóa sự kiện |

### Kiểm duyệt
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| PATCH | `/api/moderation/{resourceType}/{id}/approve` | Duyệt địa điểm/sự kiện |
| PATCH | `/api/moderation/{resourceType}/{id}/reject` | Từ chối địa điểm/sự kiện |
| GET | `/api/moderation/{resourceType}/{id}/logs` | Lịch sử kiểm duyệt |

### Quản lý hình ảnh
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/media/upload-signature` | Tạo chữ ký upload Cloudinary |
| POST | `/api/media/finalize` | Hoàn tất upload & lưu DB |
| GET | `/api/media/by-resource?resourceType={type}&resourceId={id}` | Lấy hình ảnh theo tài nguyên |
| PATCH | `/api/media/{id}/set-primary` | Đặt ảnh chính |
| PATCH | `/api/media/reorder` | Sắp xếp thứ tự ảnh |
| DELETE | `/api/media/{id}` | Xóa hình ảnh |

### Quản lý đánh giá (kiểm duyệt)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| DELETE | `/api/reviews/{id}` | Xóa đánh giá vi phạm |

---

## 2. Người cung cấp thông tin (Contributor)

> Contributor chỉ thao tác được trong phạm vi đơn vị hành chính được phân công.

### Quản lý địa điểm
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/places/all` | Xem địa điểm trong phạm vi quản lý |
| GET | `/api/places/{id}` | Chi tiết địa điểm |
| POST | `/api/places` | Thêm địa điểm (trạng thái Pending) |
| PUT | `/api/places/{id}` | Sửa địa điểm |
| DELETE | `/api/places/{id}` | Xóa địa điểm |

### Quản lý sự kiện
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/events/all` | Xem sự kiện trong phạm vi quản lý |
| GET | `/api/events/{id}` | Chi tiết sự kiện |
| POST | `/api/events` | Thêm sự kiện (trạng thái Pending) |
| PUT | `/api/events/{id}` | Sửa sự kiện |
| DELETE | `/api/events/{id}` | Xóa sự kiện |

### Kiểm duyệt (cấp trên duyệt cấp dưới)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| PATCH | `/api/moderation/{resourceType}/{id}/approve` | Duyệt địa điểm/sự kiện |
| PATCH | `/api/moderation/{resourceType}/{id}/reject` | Từ chối địa điểm/sự kiện |
| GET | `/api/moderation/{resourceType}/{id}/logs` | Xem lịch sử kiểm duyệt |

### Quản lý hình ảnh
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/media/upload-signature` | Tạo chữ ký upload |
| POST | `/api/media/finalize` | Hoàn tất upload |
| GET | `/api/media/by-resource?resourceType={type}&resourceId={id}` | Xem hình ảnh |
| PATCH | `/api/media/{id}/set-primary` | Đặt ảnh chính |
| PATCH | `/api/media/reorder` | Sắp xếp ảnh |
| DELETE | `/api/media/{id}` | Xóa hình ảnh |

### API dùng chung (cả Admin & Contributor đều dùng)
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/auth/login` | Đăng nhập |
| POST | `/api/auth/refresh` | Làm mới token |
| GET | `/api/user/me` | Xem thông tin cá nhân |
| PUT | `/api/user/me` | Cập nhật thông tin cá nhân |

---

## Ghi chú

- **resourceType**: `place` hoặc `event`
- **Phân cấp Contributor**: Trung ương > Tỉnh/TP > Phường/Xã > Tổ dân phố. Cấp trên quản lý được dữ liệu cấp dưới.
- Admin có toàn quyền, Contributor bị giới hạn theo scope đơn vị hành chính.

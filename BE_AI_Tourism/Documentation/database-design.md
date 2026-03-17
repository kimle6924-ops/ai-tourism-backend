# Database Design

Thiết kế database cho hệ thống.

**Database:** MongoDB

**Connection String:** Lưu trong User Secrets (dev) / Env vars (prod)
```
mongodb://username:password@localhost:27017/BackendBase
```

---

## Base Entity

Mọi entity kế thừa `BaseEntity`:

| Field | Type | Mô tả |
|-------|------|-------|
| Id | Guid | Primary key |
| CreatedAt | DateTime | Thời điểm tạo |
| UpdatedAt | DateTime | Thời điểm cập nhật |

---

## Tables / Collections

<!-- Thêm table mới theo format:

### [TableName]

| Field | Type | Constraints | Mô tả |
|-------|------|------------|-------|
| Id | Guid | PK | Primary key |
| FieldName | Type | NOT NULL, FK, ... | Mô tả |

**Relationships:** Mô tả quan hệ với table khác

-->

# ATOZA – Kế Hoạch Xây Dựng Module Admin

> Tài liệu thiết kế chi tiết cho việc bổ sung hệ thống quản trị (Admin) vào dự án ATOZA.

---

## 1. Mục tiêu

Thêm role **Admin** (`Admin = 2`) vào hệ thống hiện tại (chỉ có `Student = 0`, `Teacher = 1`) để quản trị tập trung: quản lý tài khoản, giám sát đề thi, theo dõi thống kê toàn hệ thống.

---

## 2. Nhiệm vụ của Admin

| Chức năng | Mô tả |
|---|---|
| **Dashboard thống kê** | Xem tổng quan: số Teacher, số Student, số đề thi, số lớp học, số bài nộp |
| **Quản lý tài khoản** | Xem danh sách User, lọc theo role, khóa/mở khóa tài khoản (`IsActive`) |
| **Quản lý đề thi** | Xem tất cả đề thi, duyệt/gỡ đề công khai, xóa đề vi phạm |
| **Quản lý lớp học** | Xem tất cả lớp, số học sinh mỗi lớp, giáo viên phụ trách |
| **Lịch sử hoạt động** | (Tùy chọn) Theo dõi đăng nhập, thao tác quan trọng |

---

## 3. Thiết kế file theo kiến trúc Clean Architecture

### 3.1. ATOZA.Domain – Tầng Domain

**File sửa:**

| File | Thay đổi |
|---|---|
| `Enums/UserRole.cs` | Thêm `Admin = 2` |
| `Entities/User.cs` | Thêm property `IsActive` (bool, default `true`) |

**Chi tiết code cần sửa:**

```csharp
// UserRole.cs
public enum UserRole
{
    Student = 0,
    Teacher = 1,
    Admin = 2       // MỚI
}
```

```csharp
// User.cs – thêm property
public bool IsActive { get; set; } = true;
```

---

### 3.2. ATOZA.Application – Tầng Application

**File mới:**

```
ATOZA.Application/
├── Abstractions/Services/
│   └── IAdminService.cs          # Interface cho Admin
└── DTOs/
    └── AdminDtos.cs              # DTOs cho Admin
```

**`IAdminService.cs`:**

```csharp
public interface IAdminService
{
    // Dashboard
    Task<DashboardStatsDto> GetDashboardStatsAsync();

    // Quản lý tài khoản
    Task<List<UserListDto>> GetAllUsersAsync(UserRole? roleFilter = null);
    Task<bool> SetUserActiveStatusAsync(int userId, bool isActive);

    // Quản lý đề thi
    Task<List<Exam>> GetAllExamsWithCreatorAsync();
    Task<bool> DeleteExamAsync(int examId);
    Task<bool> SetExamPublicStatusAsync(int examId, bool isPublic);

    // Quản lý lớp học
    Task<List<ClassOverviewDto>> GetAllClassesOverviewAsync();
}
```

**`AdminDtos.cs`:**

```csharp
public class DashboardStatsDto
{
    public int TotalTeachers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalExams { get; set; }
    public int TotalClasses { get; set; }
    public int TotalSubmissions { get; set; }
    public int ActiveUsers { get; set; }        // User có IsActive = true
    public int InactiveUsers { get; set; }      // User bị khóa
}

public class UserListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClassOverviewDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int AssignmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

### 3.3. ATOZA.Infrastructure – Tầng Infrastructure

**File mới:**

```
ATOZA.Infrastructure/
├── Services/
│   └── AdminService.cs           # Implementation IAdminService
└── Migrations/
    └── XXXXXXXX_AddAdminSupport.cs  # Migration tự sinh
```

**File sửa:**

| File | Thay đổi |
|---|---|
| `DependencyInjection.cs` | Thêm `services.AddScoped<IAdminService, AdminService>()` |
| `Persistence/ATOZADbContext.cs` | Cấu hình `IsActive` default value, seed tài khoản Admin |

**Đăng ký DI:**

```csharp
// DependencyInjection.cs – thêm dòng:
services.AddScoped<IAdminService, AdminService>();
```

**Seed tài khoản Admin (trong `ATOZADbContext.OnModelCreating`):**

```csharp
modelBuilder.Entity<User>().HasData(new User
{
    Id = 1,
    FullName = "System Admin",
    Email = "admin@atoza.vn",
    UserName = "admin",
    PasswordHash = "<PBKDF2 hash của mật khẩu mặc định>",
    Role = UserRole.Admin,
    IsActive = true,
    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
});
```

---

### 3.4. Atoza – Tầng Presentation

**File mới:**

```
Atoza/
├── Controllers/
│   └── AdminController.cs        # [Authorize(Roles = "Admin")]
├── Views/admin/
│   ├── Index.cshtml              # Dashboard thống kê
│   ├── UserList.cshtml           # Danh sách tài khoản
│   ├── ExamList.cshtml           # Quản lý đề thi
│   └── ClassList.cshtml          # Quản lý lớp học
└── wwwroot/css/
    └── admin.css                 # Style riêng cho Admin
```

**File sửa:**

| File | Thay đổi |
|---|---|
| `Controllers/HomeController.cs` | Thêm redirect: `if (role == "Admin") return RedirectToAction("Index", "Admin")` |
| `Controllers/AccountController.cs` | Chặn Register với role Admin |
| `Views/Shared/_Layout.cshtml` | Thêm menu Admin trong navbar nếu role = Admin |

---

## 4. Routes của AdminController

| Action | Route | Method | Mô tả |
|---|---|---|---|
| Dashboard | `GET /Admin/Index` | GET | Hiển thị thống kê tổng quan |
| Danh sách User | `GET /Admin/UserList` | GET | Xem tất cả tài khoản, lọc theo role |
| Khóa/Mở tài khoản | `POST /Admin/ToggleUserStatus` | POST | Đổi `IsActive` của User |
| Danh sách đề thi | `GET /Admin/ExamList` | GET | Xem tất cả đề thi |
| Xóa đề thi | `POST /Admin/DeleteExam` | POST | Xóa đề thi vi phạm |
| Đổi trạng thái đề | `POST /Admin/ToggleExamVisibility` | POST | Duyệt/gỡ đề công khai |
| Danh sách lớp | `GET /Admin/ClassList` | GET | Xem tất cả lớp học |

**Guard:** `[Authorize(Roles = "Admin")]` trên toàn bộ Controller.

---

## 5. Thay đổi Database

### Migration mới: `AddAdminSupport`

| Bảng | Thay đổi |
|---|---|
| `Users` | Thêm cột `IsActive` (`bit`, NOT NULL, DEFAULT `true`) |
| `Users` | Seed 1 record Admin mặc định |

**Lệnh tạo migration:**

```bash
cd ATOZA.Infrastructure
dotnet ef migrations add AddAdminSupport --startup-project ../Atoza
dotnet ef database update --startup-project ../Atoza
```

---

## 6. Ảnh hưởng đến code hiện tại

### Cần kiểm tra lại:

| Vị trí | Lý do |
|---|---|
| `AuthService.LoginAsync()` | Nếu `IsActive = false` → từ chối đăng nhập |
| `AuthService.RegisterAsync()` | Chặn role `Admin` khi đăng ký |
| `HomeController.Index()` | Thêm redirect cho Admin |
| `_Layout.cshtml` | Hiển thị menu Admin |
| `ClassService.AssignExamAsync()` | Không ảnh hưởng (Admin không giao bài) |

### Logic kiểm tra `IsActive` khi đăng nhập:

```csharp
// AuthService.LoginAsync() – thêm kiểm tra:
if (!user.IsActive)
    return null; // hoặc trả về thông báo "Tài khoản đã bị khóa"
```

---

## 7. Thứ tự triển khai

| Bước | Công việc | Tầng |
|---|---|---|
| 1 | Sửa `UserRole.cs` (thêm `Admin = 2`) | Domain |
| 2 | Sửa `User.cs` (thêm `IsActive`) | Domain |
| 3 | Tạo `AdminDtos.cs` | Application |
| 4 | Tạo `IAdminService.cs` | Application |
| 5 | Tạo `AdminService.cs` | Infrastructure |
| 6 | Sửa `DependencyInjection.cs` | Infrastructure |
| 7 | Sửa `ATOZADbContext.cs` (cấu hình `IsActive`, seed Admin) | Infrastructure |
| 8 | Tạo Migration + Update DB | Infrastructure |
| 9 | Sửa `AuthService.cs` (kiểm tra `IsActive`, chặn Register Admin) | Infrastructure |
| 10 | Tạo `AdminController.cs` | Presentation |
| 11 | Tạo Views (`Index`, `UserList`, `ExamList`, `ClassList`) | Presentation |
| 12 | Tạo `admin.css` | Presentation |
| 13 | Sửa `HomeController.cs`, `_Layout.cshtml` | Presentation |

---

## 8. Tài khoản Admin mặc định

| Trường | Giá trị |
|---|---|
| UserName | `admin` |
| Email | `admin@atoza.vn` |
| Password | *(đặt trong seed, cần đổi sau lần đầu đăng nhập)* |
| Role | `Admin (2)` |
| IsActive | `true` |

> ⚠️ **Lưu ý bảo mật:** Mật khẩu mặc định cần được đổi ngay sau khi deploy. Cân nhắc thêm flag `MustChangePassword` nếu muốn bắt buộc đổi mật khẩu lần đầu.

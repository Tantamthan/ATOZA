# ATOZA – Hệ Thống Quản Lý Thi Trực Tuyến

> Nền tảng thi trắc nghiệm trực tuyến dành cho Giáo viên, Học sinh và Quản trị viên, xây dựng trên **ASP.NET Core MVC** theo chuẩn **Clean Architecture**.

---

## Mục Lục

1. [Giới thiệu](#giới-thiệu)
2. [Tính năng chính](#tính-năng-chính)
3. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
4. [Cấu trúc solution](#cấu-trúc-solution)
5. [Hướng dẫn cài đặt](#hướng-dẫn-cài-đặt)
6. [Cấu hình](#cấu-hình)
7. [Tài khoản mặc định](#tài-khoản-mặc-định)
8. [Tiến trình phát triển](#tiến-trình-phát-triển)

---

## Giới thiệu

**ATOZA** là ứng dụng web cho phép:

- **Quản trị viên (Admin)** quản lý tập trung: dashboard thống kê, quản lý tài khoản (khóa/mở khóa), giám sát đề thi (duyệt/gỡ/xóa), xem tổng quan lớp học.
- **Giáo viên** tạo và quản lý lớp học, soạn đề thi (thủ công hoặc import từ file `.docx`/`.pdf`), chỉnh sửa đề thi, giao bài, xuất đề thi ra file Word, quản lý quyền truy cập đề (công khai / riêng tư) và theo dõi kết quả.
- **Học sinh** tham gia lớp học bằng mã `JoinCode`, làm bài thi trực tuyến (có kiểm tra thời hạn), xem lại kết quả và lịch sử nộp bài.

---

## Tính năng chính

### Dành cho Quản trị viên (Admin)
| Tính năng | Mô tả |
|---|---|
| Dashboard thống kê | Tổng quan: số Teacher, Student, đề thi, lớp học, bài nộp, user active/inactive |
| Quản lý tài khoản | Xem danh sách User, lọc theo role, khóa/mở khóa tài khoản (`IsActive`) |
| Quản lý đề thi | Xem tất cả đề thi, duyệt/gỡ đề công khai, xóa đề vi phạm (cascade xóa câu hỏi, bài nộp, bài giao) |
| Quản lý lớp học | Xem tất cả lớp, số học sinh, số bài giao, giáo viên phụ trách |

### Dành cho Giáo viên
| Tính năng | Mô tả |
|---|---|
| Quản lý lớp học | Tạo lớp, xem danh sách lớp, xuất danh sách học sinh ra CSV (UTF-8 BOM) |
| Soạn đề thi | Tạo đề thi thủ công hoặc import từ file Word/PDF |
| Chỉnh sửa đề thi | Sửa metadata và câu hỏi của đề thi đã tạo |
| Xuất đề thi | Export đề thi ra file `.docx` (đáp án đúng in đỏ + tóm tắt đáp án cuối trang) |
| Giao bài | Gán đề thi vào lớp với thời hạn `AvailableFrom` / `DueDate` |
| Quản lý truy cập đề | Chuyển đề thi giữa chế độ **công khai** (public) và **riêng tư** (private) |
| Báo cáo kết quả | Xem điểm và trạng thái nộp bài của từng học sinh theo lớp/đề |

### Dành cho Học sinh
| Tính năng | Mô tả |
|---|---|
| Tham gia lớp | Nhập mã `JoinCode` 6 ký tự để vào lớp |
| Làm bài thi | Thi trắc nghiệm 4 đáp án (A/B/C/D) – kiểm tra thời hạn `AvailableFrom`/`DueDate` |
| Lịch sử nộp bài | Xem điểm và chi tiết từng bài đã nộp |
| Xem lại đáp án | Đối chiếu đáp án đúng/sai sau khi thi |

---

## Công nghệ sử dụng

| Thành phần | Phiên bản / Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10.0) |
| Language | C# |
| ORM | Entity Framework Core 9.0.14 |
| Database | SQL Server (LocalDB `(localdb)\MSSQLLocalDB`) |
| Frontend | Razor Views (`.cshtml`), HTML5, CSS3, JavaScript |
| File Parsing | DocumentFormat.OpenXml 3.5.1 (Word), PdfPig 0.1.14 (PDF) |
| Password Hashing | **PBKDF2-SHA256** (100,000 iterations) – tự động nâng cấp từ MD5 legacy |
| Authentication | **Cookie Authentication** (ASP.NET Core) + Claims-based identity |
| Authorization | `[Authorize]` attribute với role-based (`Admin` / `Teacher` / `Student`) |

---

## Cấu trúc solution

```
Atoza.slnx
├── ATOZA.Domain/           # Tầng Domain – Entities, Enums, Common, Exceptions
├── ATOZA.Application/      # Tầng Application – Abstractions, DTOs, Features
├── ATOZA.Infrastructure/   # Tầng Infrastructure – DbContext, Services, Migrations
└── Atoza/                  # Tầng Presentation – Controllers, Views, wwwroot
```

### Chi tiết từng tầng

**ATOZA.Domain** – Entities: `User`, `Class`, `ClassStudent`, `ClassAssignment`, `Exam`, `Question`, `Submission`, `SubmissionDetail` | Enums: `UserRole` (Student/Teacher/Admin), `ExamMode` (Assessment/Practice) | Exceptions: `DomainExceptions` | Common: `BaseEntity`

**ATOZA.Application** – Interfaces: `IAuthService`, `IClassService`, `IExamService`, `ISubmissionService`, `IFileParserService`, `IAdminService` | DTOs: `AuthDtos`, `ClassDtos`, `ExamDtos`, `SubmissionDtos`, `AdminDtos`

**ATOZA.Infrastructure** – Services: `AuthService`, `ClassService`, `ExamService`, `SubmissionService`, `FileParserService`, `AdminService` | Persistence: `ATOZADbContext` | Migrations: `InitialCreate`, `AddExamVisibility`, `AddAdminSupport`

**Atoza (Presentation)** – Controllers: `HomeController`, `AccountController`, `TeacherController`, `StudentController`, `ExamController`, `AdminController` | Views: `account/`, `teacher/`, `student/`, `exam/`, `admin/` | CSS: `site.css`, `exam.css`, `CreateExam.css`, `admin.css`

Xem chi tiết kiến trúc tại [ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Hướng dẫn cài đặt

### Yêu cầu hệ thống
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (cài cùng Visual Studio) hoặc SQL Server bất kỳ

### Các bước thực hiện

**1. Clone repository**
```bash
git clone <repository-url>
cd Atoza
```

**2. Cấu hình connection string**

Mở file `Atoza/appsettings.json`, chỉnh `DefaultConnection` nếu cần:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ATOZA_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**3. Áp dụng Database Migration**
```bash
cd ATOZA.Infrastructure
dotnet ef database update --startup-project ../Atoza
```

> **Lưu ý:** Migration `AddAdminSupport` sẽ tự động tạo tài khoản Admin mặc định (xem mục [Tài khoản mặc định](#tài-khoản-mặc-định)).

**4. Chạy ứng dụng**
```bash
cd Atoza
dotnet run
```

Ứng dụng sẽ chạy tại `https://localhost:...` (xem terminal để biết port chính xác).

---

## Cấu hình

| Key | Vị trí | Mô tả |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` | Chuỗi kết nối SQL Server |
| `Cookie Authentication ExpireTimeSpan` | `Program.cs` | 30 phút (sliding), 30 ngày nếu RememberMe |
| `Session:IdleTimeout` | `Program.cs` | Thời gian hết phiên: **30 phút** |
| `AntiForgery Header` | `Program.cs` | Header name: `RequestVerificationToken` |

---

## Tài khoản mặc định

Hệ thống có sẵn tài khoản Admin được seed từ migration `AddAdminSupport`:

| Trường | Giá trị |
|---|---|
| UserName | `admin` |
| Email | `admin@atoza.vn` |
| Password | `admin123` |
| Role | `Admin (2)` |
| IsActive | `true` |

> ⚠️ **Bảo mật:** Mật khẩu mặc định cần được đổi ngay sau khi deploy.

Ngoài tài khoản Admin, cần đăng ký tài khoản Teacher/Student thông qua `/Account/Register` (role Admin bị chặn đăng ký).

---

## Tiến trình phát triển

### ✅ Đã hoàn thành

- **Clean Architecture**: 4 tầng tách biệt (Domain → Application → Infrastructure → Presentation)
- **Authentication & Authorization**: Cookie Authentication + Claims-based identity + `[Authorize(Roles)]`
- **Password Hashing**: PBKDF2-SHA256 (100K iterations) với auto-migration từ MD5 legacy
- **Module Giáo viên**: Quản lý lớp, soạn/sửa đề, giao bài, báo cáo kết quả, xuất CSV/Word
- **Module Học sinh**: Tham gia lớp, làm bài thi (kiểm tra thời hạn), lịch sử nộp bài, xem lại đáp án
- **Import đề thi**: Parse từ file `.docx` (OpenXml, đáp án đỏ → `*`) và `.pdf` (PdfPig)
- **Xuất đề thi Word**: File `.docx` đáp án đúng in đỏ + tóm tắt đáp án cuối trang
- **Exam Visibility**: Đề thi công khai / riêng tư (`IsPublic`)
- **Exam Editing**: Chỉnh sửa đề thi đã tạo (EditExam / UpdateExamApi)
- **Module Admin**: ✅ **Hoàn thành**
  - Dashboard thống kê tổng quan
  - Quản lý tài khoản (xem, lọc theo role, khóa/mở khóa `IsActive`)
  - Quản lý đề thi (xem, duyệt/gỡ công khai, xóa cascade)
  - Quản lý lớp học (xem tổng quan)
  - Seed tài khoản Admin mặc định qua Migration
  - Chặn đăng ký role Admin (cả Controller và Service)
  - Kiểm tra `IsActive` khi đăng nhập
  - Redirect theo role (Teacher / Student / Admin)

### ⚠️ Cần cải thiện

- **ExamMode Practice**: Enum `Practice = 1` đã định nghĩa nhưng logic "xem đáp án sau mỗi câu" chưa triển khai
- **File Upload Validation**: `ProcessExamFile` chưa ràng buộc size/type file ở tầng Controller
- **Domain Exceptions**: Các exception đã định nghĩa nhưng chưa được sử dụng đồng bộ (hiện dùng return value)
- **Timezone**: Cần chuẩn hóa về UTC đồng bộ toàn hệ thống

### 📋 Việc cần làm tiếp theo

- [ ] Triển khai `ExamMode.Practice` (xem đáp án sau từng câu)
- [ ] Thêm validation file size và MIME type trong `ProcessExamFile`
- [ ] Sử dụng Domain Exceptions thống nhất thay vì return tuple
- [ ] Chuẩn hóa timezone về UTC toàn bộ hệ thống
- [ ] Viết unit test cho các Service
- [ ] Cân nhắc bổ sung global exception handler middleware
- [ ] (Tùy chọn) Thêm seed data tạo tài khoản Teacher/Student mẫu để tiện demo/test

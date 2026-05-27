# ATOZA - Hệ thống quản lý thi trực tuyến
## 👥 Thành viên nhóm

| STT | Họ và tên | MSSV |
|---:|---|---|
| 1 | Hoàng Ngọc Nhật Tân | 2324802010346 |
| 2 | Phạm Văn Tùng  | 2324801030079 |
| 3 | Nguyễn Phan Quốc Hưng | 2324802010046|


> Nền tảng thi trắc nghiệm trực tuyến cho Admin, giáo viên và học sinh, xây dựng bằng ASP.NET Core MVC theo hướng Clean Architecture.

## Mục Lục

1. [Tổng quan](#tổng-quan)
2. [Tính năng chính](#tính-năng-chính)
3. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
4. [Cấu trúc solution](#cấu-trúc-solution)
5. [Kiến trúc và luồng phụ thuộc](#kiến-trúc-và-luồng-phụ-thuộc)
6. [Cấu hình](#cấu-hình)
7. [Cài đặt và chạy ứng dụng](#cài-đặt-và-chạy-ứng-dụng)
8. [Database và migration](#database-và-migration)
9. [Tài khoản mặc định](#tài-khoản-mặc-định)
10. [Kiểm thử](#kiểm-thử)
11. [Ghi chú phát triển](#ghi-chú-phát-triển)

## Tổng quan

**ATOZA** là ứng dụng web quản lý thi trắc nghiệm online. Hệ thống chia người dùng theo 3 vai trò:

- **Admin**: quản trị người dùng, duyệt giáo viên, quản lý đề thi, lớp học và thống kê tổng quan.
- **Teacher**: tạo lớp, tạo/import/chỉnh sửa đề thi, giao đề cho lớp, quản lý chế độ công khai/riêng tư, xuất file và xem báo cáo kết quả.
- **Student**: tham gia lớp bằng mã lớp, xem bài được giao, bắt đầu attempt, làm bài, nộp bài và xem lại kết quả.

Solution hiện có gồm 5 project: Domain, Application, Infrastructure, Web MVC và Tests.

## Tính năng chính

### Xác thực và tài khoản

| Tính năng | Mô tả |
|---|---|
| Đăng ký / đăng nhập | Đăng ký tài khoản Student hoặc Teacher, đăng nhập bằng username/password |
| Google login | Hỗ trợ đăng nhập/đăng ký qua Google nếu cấu hình `Authentication:Google` |
| Duyệt giáo viên | Teacher mới đăng ký sẽ ở trạng thái `Pending`, chưa active đến khi Admin phê duyệt |
| Quên mật khẩu | Tạo token reset mật khẩu và gửi email qua SMTP |
| Bảo vệ forgot-password | Rate limit 5 yêu cầu / 15 phút theo địa chỉ IP |
| Password hashing | PBKDF2-SHA256 100,000 iterations, có tự động nâng cấp hash MD5 cũ khi đăng nhập thành công |
| Cookie authentication | Cookie 30 phút, sliding expiration, có remember-me 30 ngày trong luồng đăng nhập |

### Admin

| Tính năng | Mô tả |
|---|---|
| Dashboard | Thống kê tổng số Teacher, Student, đề thi, lớp học, bài nộp, user active/inactive |
| Quản lý tài khoản | Xem/lọc theo role, khóa/mở khóa user bằng `IsActive` |
| Duyệt giáo viên | Chuyển `ApprovalStatus` của Teacher sang `Approved` hoặc `Rejected` |
| Quản lý đề thi | Xem danh sách đề, bật/tắt công khai, xóa đề và dữ liệu liên quan |
| Quản lý lớp học | Xem danh sách lớp, giáo viên phụ trách, số học sinh và số bài giao |

### Teacher

| Tính năng | Mô tả |
|---|---|
| Quản lý lớp | Tạo lớp, xem danh sách lớp, xem chi tiết lớp |
| Mã tham gia lớp | Mỗi lớp có `JoinCode` duy nhất để học sinh tham gia |
| Xuất danh sách học sinh | Export danh sách học sinh của lớp ra CSV UTF-8 BOM |
| Tạo đề thi | Tạo đề bằng form web hoặc import từ file `.docx` / `.pdf` |
| Validate file import | Giới hạn 10 MB, chỉ nhận `.docx` và `.pdf`, kiểm tra MIME type |
| Chỉnh sửa đề thi | Cập nhật metadata và câu hỏi; nếu đề đã có assignment/submission thì tạo version mới và archive version cũ |
| Chế độ đề thi | Hỗ trợ `Assessment` và `Practice` |
| Công khai/riêng tư | Đề public có thể được giao bởi giáo viên khác; đề private chỉ giáo viên tạo đề sử dụng |
| Giao bài | Gán đề thi vào lớp với `AvailableFrom` và `DueDate` theo UTC |
| Báo cáo kết quả | Xem trạng thái nộp bài và điểm của từng học sinh theo lớp/đề |
| Xuất Word | Export đề thi `.docx`, có đáp án đúng và bảng đáp án cuối file |

### Student

| Tính năng | Mô tả |
|---|---|
| Tham gia lớp | Nhập `JoinCode` để vào lớp |
| Xem bài được giao | Xem danh sách assignment theo lớp và trang My Assignments |
| Bắt đầu attempt | Tạo `ExamAttempt`, refresh không tạo attempt mới nếu attempt đang chạy |
| Làm bài | Chỉ truy cập khi là thành viên lớp, đề đã mở, chưa quá hạn và chưa nộp |
| Nộp bài | Chấm điểm tự động, lưu submission và submission detail |
| Hết giờ | Attempt quá hạn bị đánh dấu `Expired` và không tạo submission |
| Practice mode | Kiểm tra đáp án từng câu và trả về feedback đúng/sai |
| Xem lại bài | Xem điểm, đáp án đã chọn và kết quả từng câu |

## Công nghệ sử dụng

| Thành phần | Công nghệ / phiên bản |
|---|---|
| Runtime | .NET 10.0 |
| Web framework | ASP.NET Core MVC, Razor Views |
| Language | C# với nullable reference types |
| ORM | Entity Framework Core 9.0.14 |
| Database | SQL Server / SQL Server LocalDB |
| Authentication | ASP.NET Core Cookie Authentication, Google Authentication 9.0.16 |
| Authorization | Role-based authorization bằng `[Authorize(Roles = "...")]` |
| UI | Razor, Bootstrap, jQuery, jQuery Validation, CSS/JavaScript riêng |
| Word import/export | DocumentFormat.OpenXml 3.5.1 |
| PDF import | PdfPig 0.1.14 |
| Test | xUnit, EF Core InMemory, coverlet.collector |

## Cấu trúc solution

```text
Atoza.slnx
|-- ATOZA.Domain/
|   |-- Common/
|   |   `-- BaseEntity.cs
|   |-- Entities/
|   |   |-- User.cs
|   |   |-- Class.cs
|   |   |-- ClassStudent.cs
|   |   |-- ClassAssignment.cs
|   |   |-- Exam.cs
|   |   |-- Question.cs
|   |   |-- ExamAttempt.cs
|   |   |-- Submission.cs
|   |   `-- SubmissionDetail.cs
|   |-- Enums/
|   |   |-- UserRole.cs
|   |   |-- ApprovalStatus.cs
|   |   |-- ExamMode.cs
|   |   `-- AttemptStatus.cs
|   `-- Exceptions/
|       `-- DomainExceptions.cs
|-- ATOZA.Application/
|   |-- Abstractions/
|   |   |-- Persistence/
|   |   |   `-- IApplicationDbContext.cs
|   |   `-- Services/
|   |       |-- IAdminService.cs
|   |       |-- IAuthService.cs
|   |       |-- IClassService.cs
|   |       |-- IEmailService.cs
|   |       |-- IExamAttemptService.cs
|   |       |-- IExamService.cs
|   |       |-- IFileParserService.cs
|   |       `-- ISubmissionService.cs
|   `-- DTOs/
|       |-- AdminDtos.cs
|       |-- AuthDtos.cs
|       |-- ClassDtos.cs
|       |-- ExamDtos.cs
|       `-- SubmissionDtos.cs
|-- ATOZA.Infrastructure/
|   |-- Persistence/
|   |   `-- ATOZADbContext.cs
|   |-- Services/
|   |   |-- AdminService.cs
|   |   |-- AuthService.cs
|   |   |-- ClassService.cs
|   |   |-- ExamAttemptService.cs
|   |   |-- ExamService.cs
|   |   |-- FileParserService.cs
|   |   |-- SmtpEmailService.cs
|   |   `-- SubmissionService.cs
|   |-- Migrations/
|   `-- DependencyInjection.cs
|-- Atoza/
|   |-- Controllers/
|   |   |-- AccountController.cs
|   |   |-- AdminController.cs
|   |   |-- ExamController.cs
|   |   |-- HomeController.cs
|   |   |-- StudentController.cs
|   |   `-- TeacherController.cs
|   |-- Views/
|   |   |-- account/
|   |   |-- admin/
|   |   |-- exam/
|   |   |-- Home/
|   |   |-- Shared/
|   |   |-- student/
|   |   `-- teacher/
|   |-- wwwroot/
|   |   |-- css/
|   |   |-- js/
|   |   |-- lib/
|   |   `-- data/
|   |-- Program.cs
|   |-- appsettings.json
|   |-- appsettings.Development.json
|   `-- Properties/launchSettings.json
`-- ATOZA.Tests/
    |-- AuthServiceTests.cs
    |-- ExamServiceTests.cs
    `-- HighPriorityWorkflowTests.cs
```

Thư mục `.vs/`, `bin/`, `obj/`, `.claude/`, `.dotnet-home/`, `.codex-run/` và `.codex-build/` là thư mục local/tooling, không phải mã nguồn chính.

## Kiến trúc và luồng phụ thuộc

Solution đang đi theo hướng Clean Architecture:

```text
Atoza (Web MVC)
    -> ATOZA.Application
    -> ATOZA.Infrastructure
        -> ATOZA.Application
        -> ATOZA.Domain
ATOZA.Application
    -> ATOZA.Domain
ATOZA.Domain
    -> không phụ thuộc project nội bộ nào
ATOZA.Tests
    -> Domain, Application, Infrastructure
```

- **Domain** chứa entity, enum, base entity và exception nghiệp vụ.
- **Application** chứa DTO và interface cho persistence/service.
- **Infrastructure** hiện thực EF Core `ATOZADbContext`, service nghiệp vụ, email SMTP, import/export file và migration.
- **Atoza** là presentation layer: routing, controller, view, static assets, cookie/session/rate-limit/error handling.
- **Tests** kiểm thử các luồng ưu tiên bằng xUnit và EF Core InMemory.

Đăng ký DI nằm trong `ATOZA.Infrastructure/DependencyInjection.cs`, được gọi từ `Atoza/Program.cs` bằng `builder.Services.AddInfrastructure(builder.Configuration)`.

## Cấu hình

Ứng dụng đọc cấu hình từ:

- `Atoza/appsettings.json`
- `Atoza/appsettings.Development.json`
- `Atoza/appsettings.Local.json` nếu file này tồn tại

`appsettings.Local.json` phù hợp để đặt secret trên máy local. Nếu dùng file này cho secret thật, cần đảm bảo không commit lên repository.

### Connection string

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ATOZA_DB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Authentication

| Key | Bắt buộc | Mô tả |
|---|---:|---|
| `Authentication:Google:ClientId` | Không | Bật Google login khi có cả ClientId và ClientSecret |
| `Authentication:Google:ClientSecret` | Không | Secret của Google OAuth |
| `Authentication:PasswordReset:Secret` | Có trong non-Development | Secret ký token reset mật khẩu |

Nếu chạy môi trường không phải Development, hệ thống sẽ dừng khởi động nếu thiếu `Authentication:PasswordReset:Secret`.

### Email SMTP

| Key | Mô tả |
|---|---|
| `Email:SmtpHost` | SMTP host, mặc định `smtp.gmail.com` |
| `Email:SmtpPort` | SMTP port, mặc định `587` |
| `Email:EnableSsl` | Bật/tắt SSL |
| `Email:UserName` | Tài khoản SMTP |
| `Email:Password` | Mật khẩu/app password SMTP |
| `Email:FromEmail` | Email người gửi |
| `Email:FromName` | Tên người gửi, mặc định `Atoza` |

### Cấu hình runtime trong `Program.cs`

| Thành phần | Giá trị hiện tại |
|---|---|
| Cookie login path | `/Account/Login` |
| Cookie expire | 30 phút, sliding expiration |
| Remember-me | 30 ngày trong logic đăng nhập |
| External cookie | `Atoza.External`, expire 5 phút |
| Session idle timeout | 30 phút |
| Antiforgery header | `RequestVerificationToken` |
| Forgot password rate limit | 5 request / 15 phút / IP |
| Global exception handler | Map domain exception sang JSON status code, redirect `/Home/Error` cho lỗi 500 non-JSON |

## Cài đặt và chạy ứng dụng

### Yêu cầu

- .NET 10 SDK
- SQL Server LocalDB hoặc SQL Server
- EF Core CLI tool nếu cần chạy migration bằng lệnh `dotnet ef`

Kiểm tra SDK:

```bash
dotnet --version
```

Cài EF tool nếu chưa có:

```bash
dotnet tool install --global dotnet-ef
```

### Khởi động local

```bash
dotnet restore Atoza.slnx
dotnet build Atoza.slnx
dotnet ef database update --project ATOZA.Infrastructure --startup-project Atoza
dotnet run --project Atoza --launch-profile https
```

Theo `Atoza/Properties/launchSettings.json`, profile mặc định:

- HTTPS: `https://localhost:7032`
- HTTP: `http://localhost:5009`

Có thể chạy HTTP:

```bash
dotnet run --project Atoza --launch-profile http
```

## Database và migration

DbContext chính: `ATOZA.Infrastructure/Persistence/ATOZADbContext.cs`.

DbSet hiện có:

- `Users`
- `Exams`
- `Questions`
- `Classes`
- `ClassStudents`
- `ClassAssignments`
- `Submissions`
- `SubmissionDetails`
- `ExamAttempts`

Ràng buộc quan trọng:

- `Users.Email` và `Users.UserName` unique.
- `Classes.JoinCode` unique.
- `ClassStudents` dùng composite key `{ ClassId, StudentId }`.
- `Submissions` unique theo `{ ExamId, StudentId }`.
- `ClassAssignments` unique theo `{ ClassId, ExamId }` để tránh giao trùng đề cho cùng một lớp.
- Nhiều quan hệ dùng `DeleteBehavior.Restrict`; các service/Admin xử lý xóa liên quan khi cần.

Migration hiện có:

| Migration | Mục đích |
|---|---|
| `20260406031100_InitialCreate` | Tạo schema ban đầu |
| `20260426031226_AddExamVisibility` | Thêm trạng thái public/private cho đề |
| `20260427144552_AddAdminSupport` | Bổ sung hỗ trợ Admin |
| `20260515005759_AddExamAttemptsVersioningAndTeacherApproval` | Thêm attempt, versioning đề thi và duyệt Teacher |
| `20260526090356_AddUniqueClassExamAssignment` | Thêm unique index cho assignment theo class/exam |

Tạo migration mới:

```bash
dotnet ef migrations add <TenMigration> --project ATOZA.Infrastructure --startup-project Atoza
```

Cập nhật database:

```bash
dotnet ef database update --project ATOZA.Infrastructure --startup-project Atoza
```

## Tài khoản mặc định

Hệ thống seed tài khoản Admin trong `ATOZADbContext`:

| Trường | Giá trị |
|---|---|
| UserName | `admin` |
| Email | `admin@atoza.vn` |
| Password | `admin123` |
| Role | `Admin` |
| IsActive | `true` |
| ApprovalStatus | `Approved` |

Cần đổi mật khẩu Admin mặc định khi triển khai thật.

Teacher và Student được đăng ký qua `/Account/Register`. Role Admin không được mở đăng ký công khai.

## Kiểm thử

Project test: `ATOZA.Tests`.

Chạy toàn bộ test:

```bash
dotnet test Atoza.slnx
```

Phạm vi test hiện có:

- `AuthServiceTests`: login bằng MD5 legacy và nâng cấp PBKDF2, kiểm tra email/username tồn tại, Google login/register, reset password.
- `ExamServiceTests`: exception khi sửa đề không tồn tại hoặc không thuộc giáo viên.
- `HighPriorityWorkflowTests`: Teacher pending cho đến khi Admin duyệt, versioning khi sửa đề đã giao, attempt reuse khi refresh, attempt expired, view không render raw HTML cho nội dung câu hỏi.

## Ghi chú phát triển

- `ExamMode.Assessment`: thi chính thức, không hiện đáp án ngay.
- `ExamMode.Practice`: cho phép gọi `/Exam/CheckPracticeAnswer` để xem feedback từng câu.
- Khi sửa đề đã có assignment/submission, service tạo đề version mới và archive đề cũ để giữ lịch sử assignment.
- Thời gian assignment và attempt được xử lý theo UTC trong service/controller.
- Static assets nằm trong `Atoza/wwwroot`; các thư viện client vendor nằm trong `wwwroot/lib`.
- Một số chuỗi thông báo trong code hiện đang viết không dấu để tránh lỗi encoding trong runtime/terminal.

# ATOZA – Tài Liệu Kiến Trúc Hệ Thống

> Tài liệu kỹ thuật mô tả kiến trúc, database schema, module và quy trình nghiệp vụ của hệ thống **ATOZA**.

---

## Mục Lục

1. [Kiến trúc tổng thể](#1-kiến-trúc-tổng-thể)
2. [Mô tả các Layer](#2-mô-tả-các-layer)
3. [Mô tả Module / Tính năng](#3-mô-tả-module--tính-năng)
4. [Database Schema](#4-database-schema)
5. [Quy trình nghiệp vụ](#5-quy-trình-nghiệp-vụ)
6. [Dependency Injection & Cấu hình](#6-dependency-injection--cấu-hình)
7. [Ghi chú & Việc cần làm tiếp theo](#7-ghi-chú--việc-cần-làm-tiếp-theo)

---

## 1. Kiến trúc tổng thể

ATOZA áp dụng **Clean Architecture** (Kiến trúc sạch), tổ chức thành 4 project riêng biệt. Nguyên tắc cốt lõi: các layer bên trong **không phụ thuộc** vào layer bên ngoài.

```
┌──────────────────────────────────────────────────────┐
│                  Presentation Layer                  │
│              Atoza (ASP.NET Core MVC)                │
│      Controllers │ Views (.cshtml) │ wwwroot          │
└──────────────┬───────────────────────────────────────┘
               │ gọi Interfaces (DI)
┌──────────────▼───────────────────────────────────────┐
│               Application Layer                      │
│           ATOZA.Application                          │
│   Abstractions (Interfaces) │ DTOs │ Features        │
└──────────────┬───────────────────────────────────────┘
               │ implements
┌──────────────▼───────────────────────────────────────┐
│             Infrastructure Layer                     │
│           ATOZA.Infrastructure                       │
│        Services │ DbContext │ Migrations             │
└──────────────┬───────────────────────────────────────┘
               │ sử dụng Entities từ
┌──────────────▼───────────────────────────────────────┐
│                  Domain Layer                        │
│               ATOZA.Domain                          │
│         Entities │ Enums │ Common (BaseEntity)       │
└──────────────────────────────────────────────────────┘
```

---

## 2. Mô tả các Layer

### 2.1. ATOZA.Domain – Tầng Domain

> Nhân lõi của hệ thống. **Không phụ thuộc** vào bất kỳ project nào khác.

**Cấu trúc thư mục:**
```
ATOZA.Domain/
├── Common/
│   └── BaseEntity.cs          # Lớp cơ sở: Id (int), CreatedAt (DateTime)
├── Entities/
│   ├── User.cs                # Người dùng (Giáo viên / Học sinh)
│   ├── Class.cs               # Lớp học
│   ├── ClassStudent.cs        # Học sinh tham gia lớp (bảng junction)
│   ├── ClassAssignment.cs     # Giáo viên giao đề cho lớp
│   ├── Exam.cs                # Đề thi
│   ├── Question.cs            # Câu hỏi trắc nghiệm
│   ├── Submission.cs          # Bài nộp của học sinh
│   └── SubmissionDetail.cs    # Chi tiết từng đáp án trong bài nộp
└── Enums/
    ├── UserRole.cs             # Student = 0, Teacher = 1
    └── ExamMode.cs             # Assessment = 0, Practice = 1
```

**BaseEntity:**
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### 2.2. ATOZA.Application – Tầng Application

> Chứa các **Interface (Abstractions)** và **DTO** để tầng Presentation giao tiếp với Infrastructure qua DI mà không phụ thuộc trực tiếp.

**Cấu trúc thư mục:**
```
ATOZA.Application/
├── Abstractions/
│   ├── Persistence/
│   │   └── IApplicationDbContext.cs    # Interface DbContext (DbSet<> + SaveChangesAsync)
│   └── Services/
│       ├── IAuthService.cs
│       ├── IClassService.cs
│       ├── IExamService.cs
│       ├── ISubmissionService.cs
│       └── IFileParserService.cs
├── DTOs/
│   ├── AuthDtos.cs        # LoginDto, RegisterDto, UserProfileDto
│   ├── ClassDtos.cs       # CreateClassDto, AssignExamDto
│   ├── ExamDtos.cs        # CreateExamDto, QuestionDto
│   └── SubmissionDtos.cs  # SubmitExamDto, StudentAnswerDto, SubmitResultDto, StudentReportDto
└── Features/              # (Thư mục dự phòng – hiện trống, dành cho CQRS sau này)
```

**Danh sách Interface chính:**

| Interface | Phương thức chính |
|---|---|
| `IAuthService` | `LoginAsync`, `RegisterAsync`, `IsEmailOrUsernameTakenAsync` |
| `IClassService` | `GetClassesByTeacherAsync`, `CreateClassAsync`, `JoinClassAsync`, `AssignExamAsync`, `ExportStudentsCsvAsync` |
| `IExamService` | `CreateExamAsync`, `GetExamWithQuestionsAsync`, `HasSubmittedAsync`, `GetExamsByCreatorAsync` |
| `ISubmissionService` | `SubmitExamAsync`, `GetStudentSubmissionsAsync`, `GetSubmissionDetailAsync`, `GetSubmissionReportAsync` |
| `IFileParserService` | `ExtractFromWord`, `ExtractFromPdf`, `FormatExamText` |

---

### 2.3. ATOZA.Infrastructure – Tầng Infrastructure

> Triển khai cụ thể (implementation) toàn bộ các interface từ Application layer. Kết nối với SQL Server thông qua EF Core.

**Cấu trúc thư mục:**
```
ATOZA.Infrastructure/
├── Persistence/
│   └── ATOZADbContext.cs       # EF Core DbContext – cấu hình mapping và quan hệ
├── Services/
│   ├── AuthService.cs          # Xử lý đăng nhập, đăng ký (MD5 hash)
│   ├── ClassService.cs         # Quản lý lớp học, tham gia lớp, giao bài
│   ├── ExamService.cs          # Tạo đề thi, lấy đề thi kèm câu hỏi
│   ├── SubmissionService.cs    # Nộp bài, chấm điểm, báo cáo
│   └── FileParserService.cs    # Parse file Word/PDF thành text đề thi
├── Migrations/                 # EF Core migration files
└── DependencyInjection.cs      # Extension method: AddInfrastructure()
```

**Ghi chú về `FileParserService`:**
- File **Word (.docx)**: Dùng `DocumentFormat.OpenXml`. Các đáp án đúng được đánh dấu bằng **màu đỏ** (`FF0000`) trong file Word → tự động thêm ký tự `*` vào đầu.
- File **PDF**: Dùng thư viện `PdfPig` để trích xuất text thô.
- Sau khi trích xuất, `FormatExamText()` chuẩn hóa header câu hỏi từ nhiều định dạng (`Câu N`, `Bài N`, `Question N`, `N.`) về dạng thống nhất `Câu N:`.

---

### 2.4. Atoza – Tầng Presentation

> Web Application – ASP.NET Core MVC. Tiếp nhận HTTP request, gọi service, trả về View.

**Cấu trúc thư mục:**
```
Atoza/
├── Controllers/
│   ├── HomeController.cs       # Trang chủ
│   ├── AccountController.cs    # Đăng ký, đăng nhập, đăng xuất
│   ├── TeacherController.cs    # Toàn bộ chức năng Giáo viên
│   ├── StudentController.cs    # Toàn bộ chức năng Học sinh
│   └── ExamController.cs       # Làm bài thi, nộp bài
├── Views/
│   ├── account/                # Login.cshtml, Register.cshtml
│   ├── teacher/                # Index, ClassList, ClassDetail, CreateClass,
│   │                           # AssignExam, ClassAssignmentsList,
│   │                           # ExamSubmissionReport, TeacherCreateExam
│   ├── student/                # Index, JoinClass, ClassDetail,
│   │                           # MyAssignments, ReviewExam
│   └── exam/                   # CreateExam, Index (làm bài), SaveExam,
│                               # QuestionCard, Submit, Sidebar, _IntroScreen
├── wwwroot/
│   ├── css/                    # Stylesheet
│   ├── js/                     # CreateExam.js và các script khác
│   └── lib/                    # Thư viện frontend (jQuery, Bootstrap...)
├── Program.cs
└── appsettings.json
```

---

## 3. Mô tả Module / Tính năng

### 3.1. Module Authentication (`AccountController`)

| Action | Method | Mô tả |
|---|---|---|
| `GET /Account/Register` | GET | Hiển thị form đăng ký |
| `POST /Account/Register` | POST | Tạo tài khoản mới; hash password bằng MD5 |
| `GET /Account/Login` | GET | Hiển thị form đăng nhập; tự redirect nếu đã có Session |
| `POST /Account/Login` | POST | Xác thực; lưu `IdUser`, `FullName`, `Role` vào Session; hỗ trợ "Ghi nhớ đăng nhập" 30 ngày bằng Cookie |
| `GET /Account/Logout` | GET | Xóa Session + Cookie, redirect về trang chủ |

**Cơ chế xác thực:**
- Session-based authentication (không dùng ASP.NET Core Identity).
- Password được hash bằng **MD5** trước khi lưu vào DB.
- "Ghi nhớ đăng nhập": lưu `RememberMe_Username` và `RememberMe_Hash` vào Cookie HttpOnly, hết hạn sau **30 ngày**.

> ⚠️ **Cần bổ sung:** Logic tự động đăng nhập lại từ Cookie "RememberMe" (hiện `AccountController.Login GET` chỉ check Session, chưa đọc Cookie để tự login).

---

### 3.2. Module Giáo viên (`TeacherController`)

| Action | Route | Mô tả |
|---|---|---|
| Dashboard | `GET /Teacher/Index` | Danh sách đề thi đã tạo + số lớp đang quản lý |
| Danh sách lớp | `GET /Teacher/ClassList` | Toàn bộ lớp của giáo viên |
| Tạo lớp | `GET/POST /Teacher/CreateClass` | Tạo lớp mới, tự sinh `JoinCode` 6 ký tự ngẫu nhiên |
| Chi tiết lớp | `GET /Teacher/ClassDetail/{id}` | Xem thông tin lớp + danh sách học sinh |
| Xuất CSV | `GET /Teacher/ExportStudents/{classId}` | Tải file CSV danh sách học sinh (UTF-8 BOM) |
| Giao bài | `GET/POST /Teacher/AssignExam` | Chọn lớp + đề thi + thời hạn để giao |
| Danh sách bài giao | `GET /Teacher/ClassAssignmentsList/{classId}` | Xem các đề đã giao cho lớp |
| Báo cáo kết quả | `GET /Teacher/ExamSubmissionReport` | Xem điểm từng học sinh theo lớp/đề |
| Upload file | `POST /Teacher/ProcessExamFile` | Import đề từ `.docx` hoặc `.pdf` |

**Guard:** Tất cả action đều kiểm tra `Session["Role"] == "Teacher"`, nếu không sẽ redirect về trang Login.

---

### 3.3. Module Học sinh (`StudentController`)

| Action | Route | Mô tả |
|---|---|---|
| Dashboard | `GET /Student/Index` | Danh sách lớp đã tham gia |
| Tham gia lớp | `GET/POST /Student/JoinClass` | Nhập `JoinCode` để tham gia lớp |
| Chi tiết lớp | `GET /Student/ClassDetail/{id}` | Danh sách đề thi được giao trong lớp |
| Lịch sử bài thi | `GET /Student/MyAssignments` | Tất cả bài đã nộp của học sinh |
| Xem lại bài | `GET /Student/ReviewExam/{id}` | Chi tiết câu trả lời đúng/sai của bài nộp |

---

### 3.4. Module Thi (`ExamController`)

| Action | Route | Mô tả |
|---|---|---|
| Soạn đề | `GET /Exam/CreateExam` | Giao diện soạn đề thi (nhận nội dung từ TempData sau khi parse file) |
| Lưu đề | `POST /Exam/SaveExamApi` | JSON API nhận DTO, tạo Exam + Questions vào DB |
| Làm bài | `GET /Exam/Index/{id}` | Hiển thị đề thi; kiểm tra học sinh chưa nộp |
| Nộp bài | `POST /Exam/SubmitExam` | JSON API nhận đáp án, chấm điểm, lưu Submission |

**Logic chấm điểm:**
```
Score = Round((SốCâuĐúng / TổngSốCâu) × 10, 2)
```
Chống nộp lại: kiểm tra bảng `Submissions` trước khi lưu.

---

### 3.5. Module File Parser (`FileParserService`)

Luồng xử lý file import đề thi:

```
[Upload .docx/.pdf]
      ↓
ExtractFromWord() / ExtractFromPdf()
      ↓  (text thô)
FormatExamText()
      ↓  (text chuẩn hóa "Câu N:")
TempData["RawContent"]
      ↓
Redirect → /Exam/CreateExam  (hiển thị để giáo viên review)
      ↓
[Giáo viên xác nhận → JavaScript parse → POST /Exam/SaveExamApi]
```

---

## 4. Database Schema

### Sơ đồ quan hệ (ERD tóm lược)

```
Users ──────< Classes (TeacherId)
Users ──────< Submissions (StudentId)
Users ──────< ClassStudents (StudentId)
Classes ────< ClassStudents (ClassId)
Classes ────< ClassAssignments (ClassId)
Exams ──────< ClassAssignments (ExamId)
Exams ──────< Questions (ExamId)
Exams ──────< Submissions (ExamId)
Submissions < SubmissionDetails (SubmissionId)
Questions ──< SubmissionDetails (QuestionId)
```

---

### Bảng `Users`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK, Auto-increment | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian tạo (UTC) |
| `FullName` | `nvarchar(max)` | NOT NULL | Họ và tên |
| `Email` | `nvarchar(200)` | NOT NULL, UNIQUE | Email đăng nhập |
| `UserName` | `nvarchar(100)` | NOT NULL, UNIQUE | Tên đăng nhập |
| `PasswordHash` | `nvarchar(max)` | NOT NULL | MD5 hash của mật khẩu |
| `Role` | `int` | NOT NULL | `0` = Student, `1` = Teacher |

---

### Bảng `Classes`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian tạo |
| `ClassName` | `nvarchar(max)` | NOT NULL | Tên lớp học |
| `TeacherId` | `int` | FK → Users.Id | Giáo viên chủ nhiệm |
| `JoinCode` | `nvarchar(max)` | NOT NULL, UNIQUE | Mã tham gia lớp (6 ký tự) |

---

### Bảng `ClassStudents` *(junction table)*

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `ClassId` | `int` | PK, FK → Classes.Id | Lớp học |
| `StudentId` | `int` | PK, FK → Users.Id | Học sinh |
| `JoinedAt` | `datetime` | NOT NULL | Thời điểm tham gia |

**Composite PK:** `(ClassId, StudentId)` — `DeleteBehavior.Restrict` trên cả hai FK.

---

### Bảng `Exams`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian tạo |
| `Title` | `nvarchar(max)` | NOT NULL | Tiêu đề đề thi |
| `Description` | `nvarchar(max)` | NULL | Mô tả đề thi |
| `CreatorId` | `int` | FK → Users.Id | Giáo viên tạo đề |
| `DurationMinutes` | `int` | NOT NULL | Thời lượng thi (phút) |
| `ExamMode` | `int` | NOT NULL | `0` = Assessment, `1` = Practice |
| `StartTime` | `datetime` | NOT NULL | Thời gian bắt đầu |
| `EndTime` | `datetime` | NOT NULL | Thời gian kết thúc |

---

### Bảng `Questions`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian tạo |
| `ExamId` | `int` | FK → Exams.Id | Đề thi chứa câu hỏi |
| `OrderNumber` | `int` | NOT NULL | Thứ tự câu hỏi |
| `Content` | `nvarchar(max)` | NOT NULL | Nội dung câu hỏi |
| `OptionA` | `nvarchar(max)` | NOT NULL | Đáp án A |
| `OptionB` | `nvarchar(max)` | NOT NULL | Đáp án B |
| `OptionC` | `nvarchar(max)` | NOT NULL | Đáp án C |
| `OptionD` | `nvarchar(max)` | NOT NULL | Đáp án D |
| `CorrectAnswer` | `nvarchar(max)` | NOT NULL | Đáp án đúng: `"A"`, `"B"`, `"C"`, hoặc `"D"` |

---

### Bảng `ClassAssignments`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian giao bài |
| `ClassId` | `int` | FK → Classes.Id | Lớp được giao |
| `ExamId` | `int` | FK → Exams.Id | Đề thi được giao |
| `AssignedAt` | `datetime` | NOT NULL | Thời điểm giao bài (UTC) |
| `AvailableFrom` | `datetime` | NOT NULL | Thời điểm mở bài |
| `DueDate` | `datetime` | NOT NULL | Hạn nộp bài |

**Ràng buộc nghiệp vụ:** `DueDate > AvailableFrom` (kiểm tra trong `ClassService.AssignExamAsync`).

---

### Bảng `Submissions`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `CreatedAt` | `datetime` | NOT NULL | Thời gian tạo record |
| `ExamId` | `int` | FK → Exams.Id | Đề thi |
| `StudentId` | `int` | FK → Users.Id | Học sinh nộp bài |
| `Score` | `float` | NOT NULL | Điểm (thang 10, làm tròn 2 chữ số) |
| `SubmitTime` | `datetime` | NOT NULL | Thời điểm nộp bài (UTC) |

**Ràng buộc nghiệp vụ:** Mỗi học sinh chỉ được nộp **một lần** cho mỗi đề (kiểm tra trong `SubmissionService`).

---

### Bảng `SubmissionDetails`

| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| `Id` | `int` | PK | Khóa chính |
| `SubmissionId` | `int` | FK → Submissions.Id | Bài nộp |
| `QuestionId` | `int` | FK → Questions.Id | Câu hỏi |
| `Answer` | `nvarchar(max)` | NULL | Đáp án học sinh chọn (`"A"`/`"B"`/`"C"`/`"D"`) |
| `IsCorrect` | `bit` | NOT NULL | `1` = đúng, `0` = sai |

---

## 5. Quy trình nghiệp vụ

### 5.1. Quy trình Đăng ký & Đăng nhập

```
[Người dùng] → Nhập form Register
      ↓
AuthService.RegisterAsync()
  - Kiểm tra Email/UserName đã tồn tại?
  - Hash password bằng MD5
  - Lưu User vào DB
      ↓
Redirect → /Account/Login
      ↓
AuthService.LoginAsync()
  - Hash password → so sánh với DB
  - Lưu Session: IdUser, FullName, Role
  - (Tuỳ chọn) Ghi Cookie RememberMe 30 ngày
      ↓
Redirect theo Role:
  - Teacher → /Teacher/Index
  - Student → /Student/Index
```

---

### 5.2. Quy trình Tạo đề thi (Giáo viên)

```
[Giáo viên] → /Exam/CreateExam (thủ công)
      HOẶC
[Upload file .docx/.pdf] → POST /Teacher/ProcessExamFile
      ↓
FileParserService.ExtractFromWord/Pdf() → text thô
FileParserService.FormatExamText()      → text chuẩn hóa
TempData["RawContent"] → Redirect → /Exam/CreateExam
      ↓
[Giáo viên xem/chỉnh sửa nội dung trên giao diện]
      ↓
JavaScript parse nội dung → tạo DTO
      ↓
POST /Exam/SaveExamApi (JSON)
      ↓
ExamService.CreateExamAsync()
  - Lưu Exam vào DB
  - Lưu từng Question vào DB
  - Trả về examId
```

---

### 5.3. Quy trình Giao bài thi cho lớp (Giáo viên)

```
[Giáo viên] → /Teacher/AssignExam
  - Chọn Lớp (ClassId)
  - Chọn Đề thi (ExamId)
  - Nhập AvailableFrom & DueDate
      ↓
POST /Teacher/AssignExam
      ↓
ClassService.AssignExamAsync()
  - Validate: DueDate > AvailableFrom
  - Lưu ClassAssignment vào DB
      ↓
Redirect → /Teacher/Index (hiển thị thông báo thành công)
```

---

### 5.4. Quy trình Học sinh tham gia lớp & làm bài

```
[Học sinh] → /Student/JoinClass
  - Nhập JoinCode (6 ký tự)
      ↓
ClassService.JoinClassAsync()
  - Tìm Class theo JoinCode
  - Kiểm tra đã tham gia chưa
  - Lưu ClassStudent vào DB
      ↓
/Student/ClassDetail/{classId}
  - Xem danh sách bài tập được giao
      ↓
/Exam/Index/{examId}
  - Kiểm tra học sinh chưa nộp (HasSubmittedAsync)
  - Hiển thị đề thi + bộ đếm giờ
      ↓
[Học sinh làm bài] → POST /Exam/SubmitExam (JSON)
      ↓
SubmissionService.SubmitExamAsync()
  - Chống nộp lại
  - Chấm từng câu → tính điểm
  - Lưu Submission + SubmissionDetails vào DB
  - Trả về kết quả (điểm, redirect URL)
      ↓
Redirect → /Student/MyAssignments
```

---

### 5.5. Quy trình Giáo viên xem báo cáo kết quả

```
[Giáo viên] → /Teacher/ClassAssignmentsList/{classId}
  - Xem danh sách đề đã giao
      ↓
Click "Xem báo cáo" → /Teacher/ExamSubmissionReport?classId=X&examId=Y
      ↓
SubmissionService.GetSubmissionReportAsync(classId, examId)
  - Lấy danh sách học sinh trong lớp
  - Join với Submissions theo ExamId
  - Trả về StudentReportDto[]:
    { StudentName, Email, IsSubmitted, Score, FinishedAt }
      ↓
Hiển thị bảng kết quả (sắp xếp: đã nộp trước, theo tên)
```

---

## 6. Dependency Injection & Cấu hình

### Đăng ký services (`DependencyInjection.cs`)

```csharp
// Gọi trong Program.cs:
builder.Services.AddInfrastructure(builder.Configuration);

// Bên trong AddInfrastructure():
services.AddDbContext<ATOZADbContext>(options =>
    options.UseSqlServer(connectionString,
        b => b.MigrationsAssembly("ATOZA.Infrastructure")));

services.AddScoped<IApplicationDbContext>(
    p => p.GetRequiredService<ATOZADbContext>());

services.AddScoped<IAuthService,       AuthService>();
services.AddScoped<IExamService,       ExamService>();
services.AddScoped<IClassService,      ClassService>();
services.AddScoped<ISubmissionService, SubmissionService>();
services.AddScoped<IFileParserService, FileParserService>();
```

### Cấu hình Middleware (`Program.cs`)

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        // Bắt buộc trước UseAuthorization
app.UseAuthorization();
app.MapControllerRoute(  // Route mặc định: {controller=Home}/{action=Index}/{id?}
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

### Session Configuration

| Tham số | Giá trị |
|---|---|
| `IdleTimeout` | 30 phút |
| `Cookie.HttpOnly` | `true` |
| `Cookie.IsEssential` | `true` |

---

## 7. Ghi chú & Việc cần làm tiếp theo

### ⚠️ Vấn đề bảo mật cần xử lý

- **MD5 Password Hashing**: MD5 đã bị coi là **không an toàn** cho mật khẩu. Cần chuyển sang `BCrypt`, `PBKDF2` hoặc `Argon2`.
- **Cookie RememberMe**: Hiện lưu `PasswordHash` vào Cookie – không an toàn. Cần thay bằng token ngẫu nhiên (ví dụ: Guid) tra cứu từ DB.
- **Authorization**: Không dùng `[Authorize]` attribute; chỉ kiểm tra Session thủ công trong từng action. Nên bổ sung middleware hoặc attribute filter.
- **Không có HTTPS enforcement** ở tầng application (chỉ có HSTS ở production).

### ⚠️ Cần bổ sung thông tin

- **Seed Data**: Chưa có dữ liệu khởi tạo mặc định (admin, demo account...).
- **ExamMode (Practice)**: Enum `Practice = 1` đã định nghĩa nhưng logic "Xem đáp án sau mỗi câu" chưa được triển khai trong flow làm bài.
- **Thời hạn AvailableFrom/DueDate của ClassAssignment**: Entity có trường `AvailableFrom`/`DueDate` nhưng `ExamController.Index` chưa kiểm tra học sinh có trong thời hạn thi không.
- **Phân quyền upload file**: `ProcessExamFile` chỉ kiểm tra `IsTeacher()` nhưng không ràng buộc size/type file ở tầng controller.

### 📋 Việc cần làm tiếp theo

- [ ] Thay MD5 bằng thuật toán hash an toàn hơn (BCrypt)
- [ ] Triển khai logic Cookie RememberMe đúng chuẩn (lookup token)
- [ ] Kiểm tra `AvailableFrom` / `DueDate` trước khi cho học sinh vào thi
- [ ] Triển khai `ExamMode.Practice` (xem đáp án sau từng câu)
- [ ] Thêm seed data hoặc script tạo tài khoản admin ban đầu
- [ ] Cân nhắc bổ sung ASP.NET Core Identity hoặc JWT cho xác thực
- [ ] Viết unit test cho các Service (AuthService, SubmissionService)
- [ ] Thêm validation file size và MIME type trong `ProcessExamFile`

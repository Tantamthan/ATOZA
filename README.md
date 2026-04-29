# ATOZA – Hệ Thống Quản Lý Thi Trực Tuyến

> Nền tảng thi trắc nghiệm trực tuyến dành cho Giáo viên và Học sinh, xây dựng trên **ASP.NET Core MVC** theo chuẩn **Clean Architecture**.

---

## Mục Lục

1. [Giới thiệu](#giới-thiệu)
2. [Tính năng chính](#tính-năng-chính)
3. [Công nghệ sử dụng](#công-nghệ-sử-dụng)
4. [Cấu trúc solution](#cấu-trúc-solution)
5. [Hướng dẫn cài đặt](#hướng-dẫn-cài-đặt)
6. [Cấu hình](#cấu-hình)
7. [Tài khoản mặc định](#tài-khoản-mặc-định)

---

## Giới thiệu

**ATOZA** là ứng dụng web cho phép:

- **Giáo viên** tạo và quản lý lớp học, soạn đề thi (thủ công hoặc import từ file `.docx`/`.pdf`), chỉnh sửa đề thi, giao bài, xuất đề thi ra file Word, quản lý quyền truy cập đề (công khai / riêng tư) và theo dõi kết quả.
- **Học sinh** tham gia lớp học bằng mã `JoinCode`, làm bài thi trực tuyến (có kiểm tra thời hạn), xem lại kết quả và lịch sử nộp bài.

---

## Tính năng chính

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
| Authorization | `[Authorize]` attribute với role-based (`Teacher` / `Student`) |

---

## Cấu trúc solution

```
Atoza.slnx
├── ATOZA.Domain/           # Tầng Domain – Entities, Enums, Common, Exceptions
├── ATOZA.Application/      # Tầng Application – Abstractions, DTOs, Features
├── ATOZA.Infrastructure/   # Tầng Infrastructure – DbContext, Services, Migrations
└── Atoza/                  # Tầng Presentation – Controllers, Views, wwwroot
```

Xem chi tiết tại [ARCHITECTURE.md](./ARCHITECTURE.md).

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

⚠️ **Cần bổ sung:** Dự án hiện chưa có seed data mặc định. Cần đăng ký tài khoản thông qua màn hình `/Account/Register` và chọn vai trò `Teacher` hoặc `Student`.

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

- **Giáo viên** tạo và quản lý lớp học, soạn đề thi (thủ công hoặc import từ file `.docx`/`.pdf`), giao bài và theo dõi kết quả.
- **Học sinh** tham gia lớp học bằng mã `JoinCode`, làm bài thi trực tuyến, xem lại kết quả và lịch sử nộp bài.

---

## Tính năng chính

### Dành cho Giáo viên
| Tính năng | Mô tả |
|---|---|
| Quản lý lớp học | Tạo lớp, xem danh sách lớp, xuất danh sách học sinh ra CSV |
| Soạn đề thi | Tạo đề thi thủ công hoặc import từ file Word/PDF |
| Giao bài | Gán đề thi vào lớp với thời hạn `AvailableFrom` / `DueDate` |
| Báo cáo kết quả | Xem điểm và trạng thái nộp bài của từng học sinh theo lớp/đề |

### Dành cho Học sinh
| Tính năng | Mô tả |
|---|---|
| Tham gia lớp | Nhập mã `JoinCode` 6 ký tự để vào lớp |
| Làm bài thi | Thi trắc nghiệm 4 đáp án (A/B/C/D) có đếm giờ |
| Lịch sử nộp bài | Xem điểm và chi tiết từng bài đã nộp |
| Xem lại đáp án | Đối chiếu đáp án đúng/sai sau khi thi |

---

## Công nghệ sử dụng

| Thành phần | Phiên bản / Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10.0) |
| Language | C# |
| ORM | Entity Framework Core 9.0 |
| Database | SQL Server (LocalDB `(localdb)\MSSQLLocalDB`) |
| Frontend | Razor Pages (`.cshtml`), HTML5, CSS3, JavaScript |
| File Parsing | DocumentFormat.OpenXml (Word), PdfPig (PDF) |
| Password Hashing | MD5 (via `System.Security.Cryptography`) |
| Authentication | Session-based + Cookie "RememberMe" |

---

## Cấu trúc solution

```
Atoza.slnx
├── ATOZA.Domain/           # Tầng Domain – Entities, Enums, Common
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
| `Session:IdleTimeout` | `Program.cs` (hard-coded) | Thời gian hết phiên: **30 phút** |
| `RememberMe Cookie` | `AccountController.cs` | Thời gian lưu cookie: **30 ngày** |

---

## Tài khoản mặc định

⚠️ **Cần bổ sung:** Dự án hiện chưa có seed data mặc định. Cần đăng ký tài khoản thông qua màn hình `/Account/Register` và chọn vai trò `Teacher` hoặc `Student`.

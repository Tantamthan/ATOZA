# Tổng Quan Hệ Thống Kiến Trúc ATOZA

Dựa vào việc đọc hệ thống file, dự án **Atoza** được tổ chức một cách chuyên nghiệp theo hướng **Clean Architecture** (Kiến trúc sạch) trên nền tảng .NET Core.

---

## 1. Ngôn Ngữ & Công Nghệ Sử Dụng

- **Ngôn ngữ lập trình chính:** C#
- **Nền tảng / Framework:** ASP.NET Core MVC (.NET 10.0)
- **Tầng giao diện (Frontend):** Razor Pages (`.cshtml`), HTML5, CSS3, JavaScript. 
- **Tương tác Cơ sở dữ liệu (ORM):** Entity Framework Core 9.0.
- **Hệ quản trị CSDL:** SQL Server (Đang thiết lập sử dụng LocalDB `(localdb)\MSSQLLocalDB` với tên DB là `ATOZA_DB` tại Development environment).

---

## 2. Mô Hình Kiến Trúc Hệ Thống (Clean Architecture)

Hệ thống được chia nhỏ thành 4 project nhỏ (layers), giúp phân tách mối quan tâm (Separation of Concerns), dễ bảo trì và mở rộng:

1. **ATOZA.Domain (Tầng Cốt Lõi):** 
   - Chứa thực thế trung tâm của ứng dụng.
   - Không được phép phụ thuộc vào bất kỳ layer nào khác.
   - Chứa các Data Models quan trọng.

2. **ATOZA.Application (Tầng Ứng Dụng):**
   - Phụ thuộc vào `Domain`.
   - Chứa các quy tắc nghiệp vụ/business logic (`Features`), các Interfaces (`Contracts`, `Abstractions`), Data Transfer Objects (`DTOs`) và logic map dữ liệu (`Mapping`). 

3. **ATOZA.Infrastructure (Tầng Cơ Sở Hạ Tầng):**
   - Nơi kết nối phần cứng, thao tác dữ liệu với bên ngoài. 
   - Triển khai cụ thể các Database context (`DbContext`), các thiết lập kết nối tới CSQL (`Persistence`), cấu hình tiêm phụ thuộc (`DependencyInjection.cs`) và những thư viện/API bên thứ 3 mở rộng.

4. **Atoza (Tầng Giao Diện - Presentation Layer):**
   - Đóng vai trò là Web App (ASP.NET Core MVC).
   - Chứa các `Controllers` tiếp nhận HTTP request từ Client, gọi xuống `Application` layer để xử lý, sau đó trả logic cập nhật ra các `Views`. 

---

## 3. Các Hệ Thống File Và Thư Mục Quan Trọng

### 3.1. Tại Project Chứa Giao Diện (Atoza Web Project)
- **`Program.cs`**: Điểm bắt đầu (Entry point) của một ứng dụng .NET Core. Làm nhiệm vụ cài đặt host, inject dependency services, và cấu hình pipeline middleware (authentication, routing, ...).
- **`appsettings.json`**: File lưu các khóa cấu hình ứng dụng bao gồm `ConnectionStrings` (chuỗi kết nối db), thông tin phân quyền hoặc logging levels.
- **`Controllers/`**: Chứa logic điều tiết (Traffic cops) điều hướng HTTP Request và Response (ví dụ: đăng nhập, bài thi).
- **`Views/`**: Giao diện người dùng theo tác vụ cụ thể:
  - `teacher/`: Giao diện dành cho Giáo viên (`AssignExam.cshtml`, `CreateClass.cshtml`, `ClassDetail.cshtml`).
  - `student/`: Giao diện dành cho Học sinh (`ReviewExam.cshtml`).
  - `exam/` và `account/`: Hệ thống tài khoản và màn hình làm bài thi.
- **`wwwroot/`**: Các tài nguyên tĩnh công khai trực tiếp tới Client như các file `.css` (`exam.css`), `.js` (`exam.js`), `.txt`...

### 3.2. Tại Project Dữ Liệu (ATOZA.Domain & ATOZA.Infrastructure)
- **`ATOZA.Domain/Entities/`**: Trung tâm chứa dữ liệu cốt lõi phản ánh cấu trúc Schema của CSDL:
  - Về quyền người dùng: `User.cs`
  - Về quản lý học liệu: `Class.cs`, `ClassStudent.cs`, `ClassAssignment.cs`
  - Về bài thi: `Exam.cs`, `Question.cs`, `Submission.cs`, `SubmissionDetail.cs`
- **`ATOZA.Infrastructure/Persistence/ATOZADbContext.cs`**: File thiết lập cầu nối cho phép hệ thống C# có thể Map và truy vấn CSDL (SQL).
- **`ATOZA.Infrastructure/Migrations/`**: Nơi lưu các file lịch sử Database Schema, cho phép tracking tiến trình thay đổi cấu trúc bảng trong CSDL theo thời gian.

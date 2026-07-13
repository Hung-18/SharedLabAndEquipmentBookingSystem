# Shared Lab and Equipment Booking System

Hệ thống API hỗ trợ quản lý phòng thí nghiệm, thiết bị dùng chung và quy trình đặt lịch sử dụng tài nguyên trong trường học hoặc tổ chức.

> **Trạng thái:** Dự án đang trong quá trình phát triển. Hiện tại các chức năng xác thực, quản lý phòng lab và quản lý thiết bị đã được triển khai ở mức cơ bản. Những module còn lại đã có nền tảng database/repository nhưng chưa có đầy đủ service và API.

## 1. Mục tiêu dự án

Dự án hướng tới việc xây dựng một hệ thống tập trung để:

- Quản lý thông tin phòng thí nghiệm và thiết bị.
- Cho phép người dùng đặt lịch sử dụng phòng hoặc thiết bị.
- Hạn chế trùng lịch giữa booking và lịch bảo trì.
- Theo dõi quá trình sử dụng, vi phạm và danh sách chờ.
- Hỗ trợ quản trị viên, quản lý phòng lab và người đăng ký sử dụng tài nguyên.

## 2. Công nghệ sử dụng

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server
- JWT Bearer Authentication
- BCrypt.Net-Next
- AutoMapper
- Swagger / OpenAPI

## 3. Kiến trúc project

Project được tổ chức theo mô hình phân tầng:

```text
SharedLabAndEquipmentBookingSystem/
├── Domain/
│   ├── Entities/             # Các thực thể nghiệp vụ
│   ├── Interfaces/           # Repository interfaces
│   └── Enums.cs              # Các enum của hệ thống
│
├── Application/
│   ├── DTOs/                 # Request/response models
│   ├── Interfaces/           # Service interfaces
│   ├── Services/             # Xử lý nghiệp vụ
│   ├── AutoMapper/           # Cấu hình ánh xạ dữ liệu
│   └── DI/                   # Đăng ký dependency injection
│
├── Infrastructure/
│   ├── AppDbContext/         # DbContext và cấu hình database
│   ├── Repository/           # Repository implementations
│   └── Migrations/           # Entity Framework Core migrations
│
└── SharedLabAndEquipmentBookingSystem/
    ├── Controllers/          # API controllers
    ├── Middleware/           # Global exception middleware
    ├── Program.cs            # Cấu hình và khởi chạy ứng dụng
    └── appsettings*.json     # Cấu hình ứng dụng
```

## 4. Trạng thái chức năng hiện tại

| Module | Trạng thái | Nội dung hiện có |
|---|---|---|
| Authentication | Đang hoàn thiện | Đăng nhập, refresh token, đăng xuất, lấy người dùng hiện tại và tạo tài khoản |
| User | Đang hoàn thiện | Tạo người dùng bởi Admin, mã hóa mật khẩu bằng BCrypt |
| LabRoom | Đã có API cơ bản | Xem danh sách, xem chi tiết, tạo, cập nhật và soft delete |
| Equipment | Đã có API cơ bản | Xem danh sách, xem chi tiết, lọc theo phòng lab, tạo, cập nhật và soft delete |
| Maintenance | Chưa có API | Đã có entity, repository và cấu hình database |
| Booking | Chưa có API | Đã có entity, repository, database model và kiểm tra xung đột lịch |
| BookingItem | Chưa có API | Đã có entity, repository và database model |
| Waitlist | Chưa có API | Đã có entity, repository và database model |
| UsageLog | Chưa có API | Đã có entity, repository và database model |
| Violation | Chưa có API | Đã có entity, repository và database model |
| PriorityRule | Chưa có API | Đã có entity, repository và dữ liệu khởi tạo |
| Notification | Chưa có API | Đã có entity, repository và database model |
| AuditLog | Chưa có API | Đã có entity, repository và database model |

### Soft delete

- Khi xóa phòng lab, dữ liệu không bị xóa khỏi database; trạng thái phòng được chuyển thành `Inactive`.
- Khi xóa thiết bị, dữ liệu không bị xóa khỏi database; trạng thái thiết bị được chuyển thành `Retired`.

### Kiểm tra xung đột lịch

Database có các trigger nhằm ngăn chặn:

- Hai booking đang hoạt động sử dụng cùng tài nguyên trong thời gian bị trùng.
- Booking trùng với thời gian bảo trì.
- Hai lịch bảo trì của cùng một tài nguyên bị trùng nhau.

Các trigger được tạo hoặc cập nhật tự động khi ứng dụng khởi động.

## 5. Vai trò người dùng

Hệ thống định nghĩa ba vai trò:

| Vai trò | Mô tả |
|---|---|
| `Admin` | Quản trị hệ thống và tạo tài khoản người dùng |
| `LabManager` | Quản lý phòng thí nghiệm |
| `Requester` | Đăng ký sử dụng phòng hoặc thiết bị |

## 6. Yêu cầu môi trường

Cần cài đặt:

- .NET 10 SDK
- SQL Server
- Visual Studio, Visual Studio Code hoặc IDE hỗ trợ .NET
- `dotnet-ef` nếu muốn chạy migration thủ công

Cài đặt Entity Framework CLI nếu máy chưa có:

```bash
dotnet tool install --global dotnet-ef
```

## 7. Cấu hình ứng dụng

Không nên đẩy connection string hoặc JWT secret thật lên Git. Có thể sử dụng .NET User Secrets trong môi trường Development.

Khởi tạo User Secrets:

```bash
dotnet user-secrets init --project SharedLabAndEquipmentBookingSystem/API.csproj
```

Cấu hình chuỗi kết nối SQL Server:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=SharedLabBookingDb;Trusted_Connection=True;TrustServerCertificate=True;" --project SharedLabAndEquipmentBookingSystem/API.csproj
```

Cấu hình JWT:

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SECRET_KEY" --project SharedLabAndEquipmentBookingSystem/API.csproj
dotnet user-secrets set "Jwt:Issuer" "SharedLabAPI" --project SharedLabAndEquipmentBookingSystem/API.csproj
dotnet user-secrets set "Jwt:Audience" "SharedLabClient" --project SharedLabAndEquipmentBookingSystem/API.csproj
```

Có thể cấu hình tương đương bằng biến môi trường:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
Jwt__Audience
```

## 8. Cách chạy project

Tại thư mục gốc của repository, chạy:

```bash
dotnet restore
dotnet run --project SharedLabAndEquipmentBookingSystem/API.csproj
```

Khi ứng dụng khởi động, hệ thống sẽ:

1. Kết nối tới SQL Server.
2. Tự động áp dụng migration còn thiếu.
3. Tạo hoặc cập nhật các trigger bảo vệ lịch booking và maintenance.
4. Khởi chạy Web API.

Swagger có thể truy cập tại:

```text
https://localhost:7073/swagger
```

Hoặc:

```text
http://localhost:5253/swagger
```

Cổng thực tế có thể thay đổi tùy theo cấu hình trong `launchSettings.json`.

## 9. Chạy migration thủ công

Áp dụng migration hiện có:

```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project SharedLabAndEquipmentBookingSystem/API.csproj
```

Tạo migration mới:

```bash
dotnet ef migrations add TenMigration --project Infrastructure/Infrastructure.csproj --startup-project SharedLabAndEquipmentBookingSystem/API.csproj --output-dir Migrations
```

## 10. Dữ liệu khởi tạo

Migration hiện tại khởi tạo sẵn:

- Ba vai trò: `Admin`, `LabManager`, `Requester`.
- Bốn khoa/phòng ban: Information Technology, Electrical and Electronic Engineering, Biology và Physics.
- Bốn quy tắc ưu tiên theo mục đích booking.

Hiện tại project chưa tạo sẵn tài khoản Admin mặc định. Cần bổ sung tài khoản khởi tạo hoặc tạo dữ liệu người dùng phù hợp trước khi sử dụng endpoint chỉ dành cho Admin.

## 11. API hiện có

### Authentication

| Method | Endpoint | Quyền truy cập | Mô tả |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Public | Đăng nhập và nhận access token, refresh token |
| `POST` | `/api/Auth/refresh` | Public | Làm mới token |
| `POST` | `/api/Auth/logout` | Public | Thu hồi refresh token |
| `GET` | `/api/Auth/me` | Bearer token | Lấy thông tin người dùng hiện tại |
| `POST` | `/api/Auth/create-user` | Admin | Tạo tài khoản người dùng |

Ví dụ đăng nhập:

```json
{
  "email": "user@example.com",
  "password": "your-password"
}
```

### Lab rooms

| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/LabRooms` | Lấy danh sách phòng lab |
| `GET` | `/api/LabRooms/{id}` | Lấy chi tiết phòng lab |
| `POST` | `/api/LabRooms` | Tạo phòng lab |
| `PUT` | `/api/LabRooms/{id}` | Cập nhật phòng lab |
| `DELETE` | `/api/LabRooms/{id}` | Chuyển phòng lab sang trạng thái `Inactive` |

Ví dụ tạo phòng lab:

```json
{
  "labName": "Phòng thực hành mạng",
  "roomCode": "LAB-NET-01",
  "location": "Tầng 3 - Tòa A",
  "capacity": 30,
  "description": "Phòng thực hành mạng máy tính",
  "imageUrl": null,
  "usageGuideline": "Đăng ký trước khi sử dụng",
  "managerId": 1
}
```

`managerId` phải thuộc người dùng đang hoạt động và có vai trò `LabManager`.

### Equipment

| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/Equipments` | Lấy danh sách thiết bị |
| `GET` | `/api/Equipments/{id}` | Lấy chi tiết thiết bị |
| `GET` | `/api/Equipments/lab/{labId}` | Lấy thiết bị theo phòng lab |
| `POST` | `/api/Equipments` | Tạo thiết bị |
| `PUT` | `/api/Equipments/{id}` | Cập nhật thiết bị |
| `DELETE` | `/api/Equipments/{id}` | Chuyển thiết bị sang trạng thái `Retired` |

Ví dụ tạo thiết bị:

```json
{
  "labId": 1,
  "equipmentName": "Oscilloscope",
  "modelSpecs": "100 MHz, 2 channels",
  "imageUrl": null,
  "usageGuideline": "Kiểm tra dây nguồn trước khi sử dụng"
}
```

## 12. Sử dụng JWT trong Swagger

1. Gọi `/api/Auth/login` để lấy access token.
2. Nhấn nút **Authorize** trong Swagger.
3. Nhập token theo định dạng:

```text
Bearer YOUR_ACCESS_TOKEN
```

4. Gọi các endpoint yêu cầu xác thực.

## 13. Lộ trình phát triển

Thứ tự module dự kiến tiếp tục triển khai:

1. Maintenance
2. Booking và BookingItems
3. Waitlist
4. UsageLogs
5. Violations
6. PriorityRules
7. Notifications
8. AuditLogs

Sau đó cần bổ sung:

- Authorization theo vai trò cho các API quản lý phòng và thiết bị.
- Validation đầy đủ cho request DTO.
- Chuẩn hóa response và xử lý exception.
- Hoàn thiện endpoint `/api/Auth/me`.
- Tạo tài khoản Admin ban đầu theo cách an toàn.
- Unit test và integration test.
- Frontend cho người dùng và quản trị viên.
- Tài liệu API chi tiết hơn.

## 14. Lưu ý bảo mật

Trước khi đẩy project lên repository công khai:

- Không commit mật khẩu database.
- Không commit JWT secret thật.
- Không lưu tài khoản test có mật khẩu thật trong source code.
- Nên sử dụng User Secrets hoặc biến môi trường.
- Kiểm tra lịch sử Git nếu secret từng được commit trước đó.

## 15. Tình trạng kiểm thử

Project hiện chưa có test project tự động trong source code. Các API đang được kiểm thử thủ công qua Swagger.

---

Dự án phục vụ mục đích học tập và đang tiếp tục được hoàn thiện.

# ASC.Solution - Automobile Service Center

Ứng dụng ASP.NET Core MVC quản lý trung tâm dịch vụ ô tô.

## Công nghệ sử dụng

- ASP.NET Core MVC .NET 8
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server
- Redis Cache
- AutoMapper
- EPPlus
- MailKit
- Google Authentication
- Docker / Docker Compose
- xUnit + Moq

## Cấu trúc solution

```text
ASC.Solution
├── ASC.Web          # Web MVC, Razor Views, Areas, Identity
├── ASC.business     # Business services
├── ASC.DataAccess   # Repository + Unit of Work
├── ASC.Model        # Models, entities, enum, queries
├── ASC.Utilities    # Session/user utilities, test helpers
└── ASC.Tests        # Unit tests
```

## Chức năng chính

### Account / Identity

- Đăng nhập / đăng xuất
- Quản lý Customer
- Quản lý Service Engineer
- Bật/tắt Is Active
- Forgot Password / Reset Password
- Google Login
- Role: `Admin`, `Engineer`, `User`

### Master Data

- Quản lý Master Keys
- Quản lý Master Values
- Import Master Values từ Excel
- Bật/tắt Is Active
- Cache Master Data bằng Redis

### Service Request

- Customer tạo yêu cầu dịch vụ
- Dashboard danh sách yêu cầu
- Xem chi tiết Service Request
- Admin gán Service Engineer
- Admin/Engineer cập nhật trạng thái
- Tự cập nhật Completed Date khi status là `Completed`

### Logging / Error Handling

- Ghi log user activity bằng Action Filter
- Global Error Page
- Status Code Pages

## Yêu cầu môi trường

- .NET SDK 8
- SQL Server / SQL Server Express
- Redis
- Docker Desktop nếu chạy bằng Docker

Kiểm tra version:

```powershell
dotnet --version
docker --version
docker compose version
```

## Chạy project bằng .NET CLI

### 1. Clone project

```powershell
git clone https://github.com/thanhthien20051006-source/ASC.SolutionLab9-10-11.git
cd ASC.SolutionLab9-10-11
```

Nếu đang có source local:

```powershell
cd D:\ASC.Solution
```

### 2. Cấu hình database

Mở file:

```text
ASC.Web/appsettings.json
```

Cập nhật connection string SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ASC;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Hoặc dùng SQL Authentication:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ASC;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
}
```

> Không đưa mật khẩu thật lên GitHub.

### 3. Restore và build

```powershell
dotnet restore
dotnet build ASC.Solution.slnx
```

### 4. Chạy web

```powershell
dotnet run --project ASC.Web\ASC.Web.csproj
```

Mở trình duyệt:

```text
https://localhost:5001
```

hoặc xem URL trong terminal.

## Chạy project bằng Docker

### 1. Tạo file `.env`

```powershell
copy .env.example .env
```

Mở file:

```powershell
notepad .env
```

Cập nhật:

```env
ASC_DEFAULT_CONNECTION=Server=host.docker.internal,1433;Database=ASC;Trusted_Connection=False;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
ASC_REDIS_CONNECTION=host.docker.internal:6379
```

> Khi app chạy trong Docker, không dùng `localhost` để gọi SQL Server trên máy host. Dùng `host.docker.internal`.

### 2. Bật SQL Server TCP/IP

Nếu dùng SQL Server Express:

1. Mở `SQL Server Configuration Manager`
2. Vào `SQL Server Network Configuration`
3. Chọn đúng instance, ví dụ `Protocols for SQLEXPRESS`
4. Enable `TCP/IP`
5. Tab `IP Addresses` → `IPAll`
6. Sửa:

```text
TCP Dynamic Ports: để trống
TCP Port: 1433
```

Restart SQL Server bằng PowerShell Admin:

```powershell
Restart-Service 'MSSQL$SQLEXPRESS'
```

Nếu instance khác:

```powershell
Restart-Service 'MSSQL$SQLEXPRESS01'
Restart-Service 'MSSQL$SQLEXPRESS02'
```

Kiểm tra port:

```powershell
netstat -ano | findstr :1433
```

### 3. Chạy Redis nếu chưa có

```powershell
docker run -d --name asc-redis -p 6379:6379 redis:7
```

Nếu container đã tồn tại:

```powershell
docker start asc-redis
```

### 4. Build và chạy Docker Compose

```powershell
docker compose up --build
```

Chạy nền:

```powershell
docker compose up --build -d
```

Mở web:

```text
http://localhost:8080
```

### 5. Xem log Docker

```powershell
docker compose logs -f asc-web
```

### 6. Dừng Docker

```powershell
docker compose down
```

## Tài khoản mặc định

Tài khoản mặc định được seed từ `AppSettings` trong `appsettings.json`:

```json
"AppSettings": {
  "AdminEmail": "...",
  "AdminPassword": "...",
  "EngineerEmail": "...",
  "EngineerPassword": "..."
}
```

> Vì có mật khẩu thật, không commit `appsettings.json` chứa secret lên GitHub.

## Các trạng thái Service Request

```text
New
Denied
Pending
Initiated
InProgress
PendingCustomerApproval
RequestForInformation
Completed
```

## Build kiểm tra an toàn

Nếu app đang chạy và bị lock file, dùng lệnh build ra thư mục riêng:

```powershell
dotnet build D:\ASC.Solution\ASC.Solution.slnx --no-restore /p:UseAppHost=false /p:OutDir=D:\ASC.Solution\_buildcheck\
```

## Test

```powershell
dotnet test ASC.Solution.slnx
```

## Git workflow

Xem thay đổi:

```powershell
git status
```

Commit:

```powershell
git add .
git commit -m "Your commit message"
```

Push:

```powershell
git push origin main
```

Không commit các file local/secret:

```text
.env
ASC.Web/appsettings.json
_buildcheck/
```

## Troubleshooting

### Docker báo ERR_CONNECTION_REFUSED

Kiểm tra container có chạy không:

```powershell
docker compose ps
```

Xem log:

```powershell
docker compose logs --tail=100 asc-web
```

### Lỗi không kết nối SQL Server từ Docker

Kiểm tra:

```powershell
netstat -ano | findstr :1433
```

Connection string Docker nên dùng:

```env
ASC_DEFAULT_CONNECTION=Server=host.docker.internal,1433;Database=ASC;Trusted_Connection=False;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

### Không restart được SQL Server service

Mở PowerShell bằng quyền Administrator rồi chạy:

```powershell
Restart-Service 'MSSQL$SQLEXPRESS'
```

## Ghi chú bảo mật

- Không đưa mật khẩu SQL, SMTP, Google Client Secret lên GitHub
- Dùng `.env` cho Docker local
- Dùng Secret Manager / environment variables / Key Vault cho production

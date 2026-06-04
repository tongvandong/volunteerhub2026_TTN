# VolunteerHub

VolunteerHub là hệ thống quản lý hoạt động tình nguyện gồm backend `.NET 8` và frontend React/Vite.

Frontend chính đang dùng nằm ở `BaseCore.WebClient`.

## Kiến Trúc Chạy Local

Các service local:

| Service | Project | Port | URL |
| --- | --- | ---: | --- |
| API Gateway | `BaseCore.ApiGateway` | `5000` | `http://localhost:5000` |
| Core API | `BaseCore.APIService` | `5001` | `http://localhost:5001` |
| Auth API | `BaseCore.AuthService` | `5002` | `http://localhost:5002` |
| Frontend | `BaseCore.WebClient` | `3000` | `http://localhost:3000` |

Frontend gọi API qua `/api`, Vite proxy `/api` về Gateway `http://localhost:5000`.

Gateway route:

- `/api/auth/*` -> AuthService `5002`
- `/api/users/*` -> AuthService `5002`
- `/api/*` còn lại -> APIService `5001`

## Yêu Cầu Môi Trường

- .NET SDK 8+
- Node.js 20+ và npm
- SQL Server
- Git

## Cấu Hình SQL Server

Connection string hiện nằm trong:

- `BaseCore.APIService/appsettings.json`
- `BaseCore.AuthService/appsettings.json`

Mặc định hiện tại:

```json
"ConnectionStrings": {
  "ConnectedDb": "Data Source=LAPTOP-70RJA2GI\\SQLSERVER2022DEV;Initial Catalog=VolunteerHub;Integrated Security=True;Trust Server Certificate=True"
}
```

Khi chạy trên máy khác, đổi `Data Source` theo SQL Server instance local của bạn. Ví dụ:

```json
"ConnectionStrings": {
  "ConnectedDb": "Data Source=localhost;Initial Catalog=VolunteerHub;Integrated Security=True;Trust Server Certificate=True"
}
```

Hoặc nếu dùng SQL login:

```json
"ConnectionStrings": {
  "ConnectedDb": "Data Source=localhost;Initial Catalog=VolunteerHub;User Id=sa;Password=YOUR_PASSWORD;Trust Server Certificate=True"
}
```

Database tên `VolunteerHub`. Hai service `APIService` và `AuthService` tự chạy EF migration khi startup qua `DatabaseMigrationRunner`, nên chỉ cần SQL Server truy cập được.

## Cài Package

Backend restore:

```powershell
cd D:\FW\FW\BaseCore
dotnet restore BaseCore.sln
```

Frontend install:

```powershell
cd D:\FW\FW\BaseCore\BaseCore.WebClient
npm install
```

## Chạy Project Local

Mở 4 terminal riêng.

Terminal 1: AuthService

```powershell
cd D:\FW\FW\BaseCore
dotnet run --project BaseCore.AuthService\BaseCore.AuthService.csproj --urls http://localhost:5002
```

Terminal 2: APIService

```powershell
cd D:\FW\FW\BaseCore
dotnet run --project BaseCore.APIService\BaseCore.APIService.csproj --urls http://localhost:5001
```

Terminal 3: ApiGateway

```powershell
cd D:\FW\FW\BaseCore
dotnet run --project BaseCore.ApiGateway\BaseCore.ApiGateway.csproj --urls http://localhost:5000
```

Terminal 4: Frontend

```powershell
cd D:\FW\FW\BaseCore\BaseCore.WebClient
npm run dev -- --host 127.0.0.1
```

Mở app:

```text
http://localhost:3000
```

Swagger:

- Gateway: `http://localhost:5000/swagger`
- Core API: `http://localhost:5001/swagger`
- Auth API: `http://localhost:5002/swagger`

## Tài Khoản Demo

Các tài khoản này được seed trong `BaseCore.Repository/MySqlDbContext.cs` khi migration tạo database:

| Role | Username | Email | Password |
| --- | --- | --- | --- |
| Admin | `admin` | `admin@volunteerhub.vn` | `admin123` |
| Organizer | `organizer` | `organizer@volunteerhub.vn` | `organizer123` |
| Sponsor | `sponsor` | `sponsor@volunteerhub.vn` | `sponsor123` |
| Volunteer | `volunteer` | `volunteer@volunteerhub.vn` | `volunteer123` |

Bạn cũng có thể đăng ký tài khoản mới trực tiếp từ UI.

## Seed Data Demo

Migration mặc định seed dữ liệu nền:

- 4 user demo theo role.
- Category, product cũ của base project.
- Skill, event category, badge.
- Một số event mẫu, work shift, channel, registration, certificate.

File `seed_data.sql` có thêm dữ liệu demo mở rộng cho SQL Server. Chạy file này sau khi database đã được tạo và migration đã chạy.

Ví dụ bằng `sqlcmd`:

```powershell
sqlcmd -S "localhost" -d "VolunteerHub" -E -i ".\seed_data.sql"
```

Nếu dùng SQL login:

```powershell
sqlcmd -S "localhost" -d "VolunteerHub" -U "sa" -P "YOUR_PASSWORD" -i ".\seed_data.sql"
```

Lưu ý: `seed_data.sql` dùng ID cố định. Nếu database đã có dữ liệu test nhiều lần, nên dùng database sạch hoặc kiểm tra trùng ID trước khi chạy.

Reset database local về trạng thái demo:

```powershell
cd D:\FW\FW\BaseCore
.\scripts\reset-demo-data.ps1 -ConfirmReset
```

Mặc định script dùng LocalDB `(localdb)\MSSQLLocalDB` và database `VolunteerHub`. Có thể chỉ định SQL Server khác:

```powershell
.\scripts\reset-demo-data.ps1 -Server "localhost" -Database "VolunteerHub" -ConfirmReset
```

## Kiểm Tra Build

Backend:

```powershell
cd D:\FW\FW\BaseCore
dotnet build BaseCore.sln --no-incremental
```

Frontend:

```powershell
cd D:\FW\FW\BaseCore\BaseCore.WebClient
npm run build
```

Audit package backend:

```powershell
cd D:\FW\FW\BaseCore
dotnet list BaseCore.sln package --vulnerable --include-transitive
```

## Flow Demo Nhanh

1. Login `organizer / organizer123`.
2. Tạo event mới ở `Tạo sự kiện`.
3. Login `admin / admin123`.
4. Vào `Duyệt sự kiện`, approve event.
5. Login `volunteer / volunteer123`.
6. Vào danh sách sự kiện, mở event đã duyệt, đăng ký event.
7. Login lại organizer.
8. Vào `Sự kiện của tôi` -> quản lý event.
9. Xác nhận đăng ký, check-in bằng QR của event.
10. Hoàn thành event và xem lịch sử/chứng chỉ nếu có.

## Ghi Chú Project

- `BaseCore.WebClient` là frontend chính.
- `volunteerhub-frontend` là frontend cũ/khác, không phải app chính hiện tại.
- `Context/project-reading-guide.md` là living document ghi lại hiểu biết và trạng thái E2E gần nhất của project.

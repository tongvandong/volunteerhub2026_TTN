# Hướng dẫn cài đặt và chạy — VolunteerHub

## 1. Yêu cầu môi trường

| Phần mềm | Version tối thiểu | Kiểm tra |
|---|---|---|
| .NET SDK | 8.0+ | `dotnet --version` |
| Node.js | 20+ | `node --version` |
| npm | 9+ | `npm --version` |
| SQL Server | 2019+ (hoặc LocalDB) | SQL Server Management Studio hoặc `sqlcmd` |
| Git | 2.x | `git --version` |

## 2. Clone repository

```powershell
git clone https://github.com/taoladong/volunteerhub-group10.git
cd volunteerhub-group10
```

## 3. Cấu hình database

### 3.1. Tạo database

Hệ thống tự tạo database và chạy migration khi service khởi động. Chỉ cần SQL Server instance truy cập được.

### 3.2. Sửa connection string

Mở các file sau và đổi `Data Source` theo SQL Server instance trên máy bạn:

- `BaseCore.AuthService/appsettings.json`
- `BaseCore.EventService/appsettings.json`
- `BaseCore.FinanceService/appsettings.json`
- `BaseCore.APIService/appsettings.json` (nếu cần chạy legacy service)

Mặc định:
```json
"ConnectionStrings": {
  "ConnectedDb": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=VolunteerHub;Integrated Security=True;Trust Server Certificate=True"
}
```

Nếu dùng SQL Server instance có tên:
```json
"ConnectedDb": "Data Source=LAPTOP-ABC\\SQLSERVER2022DEV;Initial Catalog=VolunteerHub;Integrated Security=True;Trust Server Certificate=True"
```

Nếu dùng SQL login:
```json
"ConnectedDb": "Data Source=localhost;Initial Catalog=VolunteerHub;User Id=sa;Password=YOUR_PASSWORD;Trust Server Certificate=True"
```

## 4. Cài đặt package

### Backend
```powershell
dotnet restore BaseCore.sln
```

### Frontend
```powershell
cd BaseCore.WebClient
npm install
cd ..
```

## 5. Build kiểm tra

```powershell
dotnet build BaseCore.sln
```

Kỳ vọng: `Build succeeded. 0 Warning(s) 0 Error(s)`.

## 6. Chạy hệ thống

Mở **5 terminal riêng biệt**:

### Terminal 1 — Identity Service (port 5002)
```powershell
cd D:\path\to\volunteerhub-group10
dotnet run --project BaseCore.AuthService --urls http://localhost:5002
```

### Terminal 2 — Event Service (port 5003)
```powershell
cd D:\path\to\volunteerhub-group10
dotnet run --project BaseCore.EventService --urls http://localhost:5003
```

### Terminal 3 — Finance Service (port 5004)
```powershell
cd D:\path\to\volunteerhub-group10
dotnet run --project BaseCore.FinanceService --urls http://localhost:5004
```

### Terminal 4 — API Gateway (port 5000)
```powershell
cd D:\path\to\volunteerhub-group10
dotnet run --project BaseCore.ApiGateway --urls http://localhost:5000
```

### Terminal 5 — Frontend (port 3000)
```powershell
cd D:\path\to\volunteerhub-group10\BaseCore.WebClient
npm run dev -- --host 127.0.0.1
```

## 7. Kiểm tra hệ thống đã chạy

| URL | Kỳ vọng |
|---|---|
| http://localhost:3000 | Landing page VolunteerHub |
| http://localhost:5002/swagger | Swagger UI Identity Service |
| http://localhost:5003/swagger | Swagger UI Event Service |
| http://localhost:5004/swagger | Swagger UI Finance Service |
| http://localhost:5003/health | "Healthy" |
| http://localhost:5004/health | "Healthy" |

Gateway (5000) có endpoint kiểm tra nhanh `http://localhost:5000/health` và `http://localhost:5000/api/health`. Có thể kiểm tra route nghiệp vụ bằng `http://localhost:5000/api/events` — nếu trả danh sách event thì gateway hoạt động.

Lưu ý: Legacy service (5001) không bắt buộc phải chạy. Hệ thống VolunteerHub hoạt động đầy đủ chỉ với 3 service mới (5002 + 5003 + 5004) + gateway (5000). Legacy chỉ cần nếu muốn dùng API Product/Order cũ.

## 8. Tài khoản demo

Hệ thống tự seed 4 tài khoản khi database được tạo lần đầu:

| Role | Username | Password | Mô tả |
|---|---|---|---|
| Admin | `admin` | `admin123` | Quản trị viên hệ thống |
| Organizer | `organizer` | `organizer123` | Nhà tổ chức (đã verified) |
| Sponsor | `sponsor` | `sponsor123` | Nhà tài trợ (đã có SponsorProfile) |
| Volunteer | `volunteer` | `volunteer123` | Tình nguyện viên (đã KYC verified) |

Ngoài ra hệ thống seed sẵn:
- 4 danh mục sự kiện (Trồng cây, Dọn rác, Từ thiện, Bình dân học vụ số).
- 7 kỹ năng (Y tế, Giáo dục, CNTT, Hậu cần, Truyền thông, Ngoại ngữ, Lái xe).
- 3 huy hiệu (Chiến sĩ xanh, Đại sứ nhân ái, Chuyên gia tình nguyện).
- 3 sự kiện mẫu (1 Pending, 1 Approved, 1 Completed) với channel, registration, certificate.
- 1 SponsorProfile cho tài khoản sponsor demo (Công ty TNHH Tài trợ Demo).
- 1 OrganizerVerification cho tài khoản organizer demo (đã Verified).

## 9. Demo nhanh

1. Mở `http://localhost:3000` → xem landing page.
2. Đăng nhập `organizer / organizer123` → vào `/events/create` → tạo sự kiện (có thể set CheckInRadiusKm).
3. Đăng nhập `admin / admin123` → vào `/admin/events` → duyệt sự kiện.
4. Đăng nhập `volunteer / volunteer123` → vào `/events` → mở sự kiện đã duyệt → đăng ký.
5. Đăng nhập `organizer` → vào `/my-events` → quản lý → xác nhận volunteer → điểm danh → check-out → hoàn thành.
6. Đăng nhập `sponsor / sponsor123` → vào `/sponsor/profile` → xem/sửa hồ sơ → vào `/events` → mở sự kiện đã duyệt → gửi tài trợ → vào `/my-sponsorships` xem.
7. Đăng nhập `volunteer` → xem `/my-certificates` → xem `/my-registrations` → đánh giá organizer.

## 10. Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách fix |
|---|---|---|
| `502 Bad Gateway` qua frontend | Service backend chưa start | Kiểm tra terminal tương ứng đã chạy chưa |
| `Connection refused` khi start service | SQL Server chưa chạy hoặc connection string sai | Kiểm tra SQL Server instance + sửa appsettings.json |
| `Timed out waiting for migration lock` | Service khác đang chạy migration | Đợi 30 giây hoặc restart |
| `npm run dev` lỗi | Chưa `npm install` | Chạy `npm install` trong `BaseCore.WebClient/` |
| Build lỗi "file is locked" | Process cũ đang chạy | Tắt hết terminal cũ rồi build lại |
| 401 khi gọi API | User bị khóa (IsActive=false) hoặc token hết hạn | Kiểm tra trạng thái user hoặc refresh token |

## 11. Reset database demo

Nếu muốn đưa database về trạng thái sạch:

```powershell
# Xóa database rồi để service tự tạo lại khi start
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS VolunteerHub"
```

Sau đó restart bất kỳ service nào — migration sẽ tạo lại database + seed data (bao gồm SponsorProfile demo).

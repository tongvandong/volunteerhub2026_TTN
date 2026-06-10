# VolunteerHub - Thực tập nhóm

VolunteerHub là cổng sự kiện tình nguyện phục vụ môn Thực tập nhóm, lớp CNTT59.

## Thông tin

- Môn học: Thực tập nhóm
- Giảng viên: Phan Nguyên Hải
- Bộ môn: Công nghệ phần mềm
- Đề tài: VolunteerHub
- Repository: https://github.com/tongvandong/volunteerhub2026_TTN
- Trello: https://trello.com/b/q3SPEszi/b%E1%BA%A3ng-trello-c%E1%BB%A7a-toi
- Google Drive: https://drive.google.com/drive/u/0/folders/1qnu4XJEBNgHDcGQzNKacwwpaZxygxAdZ

## Phân công

| Thành viên | Vai trò | Phụ trách |
|---|---|---|
| Tống Văn Đông | Nhóm trưởng | Luồng sự kiện, đăng ký, điểm danh, chứng chỉ, tích hợp gateway/service |
| Phạm Tiến Dũng | Thành viên | Auth, hồ sơ, xác minh organizer/volunteer, phân quyền |
| Hồ Sỹ Vinh | Thành viên | Ủng hộ, đóng góp, sponsor, finance, donor stats, VietQR/bank info |

## Workflow Git

- `master`: nhánh tích hợp cuối.
- `feature/events-dong`: phần Tống Văn Đông.
- `feature/auth-verification-dung`: phần Phạm Tiến Dũng.
- `feature/finance-donation-vinh`: phần Hồ Sỹ Vinh.
- `docs/final-submission`: báo cáo, slide, biên bản và tài liệu nộp cuối.

Mỗi thành viên làm trên nhánh riêng, push nhánh cá nhân và tạo Pull Request vào `master` sau khi đã kiểm tra phần việc.

## Tài liệu kỹ thuật

- `docs/3-thiet-ke-he-thong.md`: kiến trúc hệ thống, service, database và các luồng nghiệp vụ chính.
- `docs/4-huong-dan-cai-dat.md`: yêu cầu môi trường, cấu hình database, build và chạy local.
- `docs/internal/`: các kế hoạch chi tiết cho phỏng vấn, quản lý sự kiện, layout, public event và admin.


# Kế hoạch nghiệm thu và hardening Nova Haven local

> **For agentic workers:** Dùng `superpowers:executing-plans` để thực hiện lần lượt; mỗi lỗi sửa theo TDD, test phải đỏ trước khi sửa.

**Mục tiêu:** Rà và hoàn thiện những lỗi có thể tái hiện trong website/CMS, API + SQL Server local và Flutter; lưu bằng commit thật trên checkout Git đang có.

**Kiến trúc:** Giữ modular monolith hiện hành: ASP.NET Core REST + EF Core/SQL Server, Next.js web/admin và Flutter reader. OpenSpec và hợp đồng OpenAPI là nguồn hành vi; không thêm dịch vụ, provider database hay tính năng gameplay mới.

**Tech stack:** .NET 10, SQL Server LocalDB, Next.js 16/React 19, Node 24, Flutter 3.44+.

**Spec:** `AGENTS.md`; `README.md`; `openspec/changes/add-user-notifications-webpush/{design.md,specs/user-accounts/spec.md,specs/notifications/spec.md}`; `design-system/nova-haven/MASTER.md`; roadmap `docs/roadmap/NOVA-HAVEN-ROADMAP.md`.

## Ràng buộc chung

- Chỉ SQL Server; không migrate/seed `NovaHaven_Local` hoặc bất kỳ DB có dữ liệu người dùng.
- Integration chỉ dùng fixture `NovaHaven_Integration_<GUID>` và chỉ dọn DB tạo trong lượt test.
- Giữ UI dark-earth, bản homepage đã duyệt, tiếng Việt, focus rõ, responsive và touch target đúng nền tảng.
- Không thêm tính năng ngoài OpenSpec để làm phình phạm vi; không ghi bí mật; không tuyên bố production/online hoàn tất.
- Commit lên nhánh feature hiện tại sau khi kiểm tra; không push remote trong kế hoạch này.

## Điểm cần soi

1. Migration notification/account chỉ được chứng nhận nếu fixture SQL Server độc lập migrate và API integration chạy thật.
2. API-unavailable, anonymous và unconfirmed-account states cần thông báo/điều hướng hợp lý trên web và mobile.
3. Admin mutations phải giữ server-side authorization, CSRF và ETag/concurrency.
4. 320–390px, landscape và chữ lớn không được tạo overflow/cắt nội dung Flutter.
5. Commit chỉ chứa source/tài liệu cần thiết, không có `.env`, log, database, build output, screenshot debug hoặc secret.

### Task 1: Chạy baseline trên checkout có Git

**Files:** không sửa source; ghi kết quả vào `docs/verification/2026-09-29-local-platform-hardening.md`.

- [x] Chạy `npm test`, backend domain/integration, web typecheck/build và Flutter test/analyze/build trên môi trường hiện tại.
- [x] Ghi riêng mọi lỗi, và phân biệt blocker môi trường với lỗi code; không sửa trước khi có nguyên nhân tái hiện.

### Task 2: Sửa lỗi API/CMS theo OpenSpec

**Files:** chỉ những test/source được baseline chỉ ra; ưu tiên `tests/backend/`, `backend/`, `tests/web/`, `apps/web/app/admin/`.

- [x] Bổ sung hoặc chỉnh test tái hiện lỗi và xác nhận RED.
- [x] Sửa tối thiểu theo convention đang dùng; không thay API/schema nếu không có spec.
- [x] Chạy lại test tập trung rồi full suite liên quan; LocalDB chỉ được dùng qua fixture database ngẫu nhiên.

### Task 3: Sửa website và mobile khi có lỗi tái hiện

**Files:** chỉ các route/component/model/test bị chứng minh lỗi; dùng `design-system/nova-haven/MASTER.md`.

- [x] Rà các trạng thái/lỗi frontend có thể kiểm chứng; không phát hiện lỗi UI mới cần thay đổi code ở lượt này nên không tạo thay đổi giao diện không có căn cứ.
- [x] Giữ nguyên UI tokens, homepage đã duyệt và các kiểm tra keyboard/focus, responsive, touch target hiện có; không thêm chuyển động/visual redesign ngoài phạm vi.
- [x] Chạy web typecheck/build/Node tests và Flutter test/analyze/debug APK.

### Task 4: Đối chiếu evidence và roadmap

**Files:** `docs/verification/2026-09-29-local-platform-hardening.md`, README/roadmap/OpenSpec tasks nếu kết quả mới yêu cầu.

- [x] Ghi command, kết quả, test count, và blocker chính xác; cập nhật README, roadmap, OpenSpec tasks và verification report.
- [x] Nêu các phần chưa thể xác nhận: SMTP/Web Push ngoài môi trường, browser E2E, native push, deploy.

### Task 5: Review và commit

- [x] Rà lại diff, secrets, file sinh tự động và các tiêu chí trong `Review Focus`.
- [x] Chạy lại verification cuối cùng sau mọi thay đổi.
- [x] Commit source trên nhánh feature hiện có với message mô tả đúng; chưa push.

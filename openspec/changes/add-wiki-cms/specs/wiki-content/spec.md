## Purpose

Cung cấp Wiki có thể được Admin biên tập và xuất bản theo phiên bản, trong khi website công khai và ứng dụng Flutter chỉ đọc cùng một nội dung đã xuất bản, bảo vệ bản nháp và dữ liệu riêng tư.

## ADDED Requirements

### Requirement: Admin-only editorial access
Hệ thống SHALL chỉ cho phép người dùng có quyền Admin tạo, sửa, xuất bản, hủy xuất bản, khôi phục và quản lý nội dung Wiki.

#### Scenario: Admin edits Wiki
- **GIVEN** người dùng đã xác thực có role Admin.
- **WHEN** người dùng gửi yêu cầu tạo hoặc sửa bài Wiki hợp lệ.
- **THEN** hệ thống cho phép thao tác theo quy tắc nghiệp vụ và trả kết quả phù hợp.

#### Scenario: Anonymous mutation denied
- **GIVEN** người dùng chưa xác thực.
- **WHEN** người dùng gọi endpoint chỉnh sửa Wiki.
- **THEN** hệ thống trả HTTP 401 và không thay đổi dữ liệu.

#### Scenario: Non-admin mutation denied
- **GIVEN** người dùng đã đăng nhập nhưng không có role Admin.
- **WHEN** người dùng gọi endpoint chỉnh sửa Wiki.
- **THEN** hệ thống trả HTTP 403 và không thay đổi dữ liệu, dù giao diện có hiển thị nút hay không.

### Requirement: Article draft validation
Hệ thống SHALL cho phép Admin lưu bài viết nháp với tiêu đề 1–120 ký tự, slug 3–120 ký tự theo quy tắc `a-z`, `0-9`, dấu nối đơn, summary tối đa 300 ký tự, Markdown 1–50.000 ký tự và một category hợp lệ.

#### Scenario: Valid draft saved
- **GIVEN** Admin gửi đầy đủ các trường hợp lệ và chọn category đang hoạt động.
- **WHEN** Admin tạo bài Wiki.
- **THEN** hệ thống lưu một bài ở trạng thái DRAFT, trả ID dạng chuỗi UUID và bài chưa xuất hiện qua API công khai.

#### Scenario: Invalid field rejected
- **GIVEN** Admin gửi title trống, slug sai định dạng hoặc category không hợp lệ.
- **WHEN** Admin lưu bài Wiki.
- **THEN** hệ thống trả HTTP 400 kèm lỗi trường tương ứng và không lưu bài không hợp lệ.

### Requirement: Slug uniqueness and stability
Hệ thống SHALL đảm bảo mỗi bài giữ slug duy nhất không phân biệt chữ hoa/thường; slug SHALL không thay đổi sau lần xuất bản đầu tiên và không được tự tái cấp cho bài khác.

#### Scenario: Duplicate slug rejected
- **GIVEN** slug `fishing-guide` đã thuộc một bài kể cả khi bài đang DRAFT hoặc UNPUBLISHED.
- **WHEN** Admin tạo bài thứ hai dùng cùng slug.
- **THEN** hệ thống trả HTTP 409, không tạo bài trùng và không thay đổi chủ sở hữu slug.

#### Scenario: Published slug cannot change
- **GIVEN** một bài từng được xuất bản.
- **WHEN** Admin cố đổi slug của bài.
- **THEN** hệ thống từ chối thao tác với lỗi hợp lệ và đường dẫn cũ vẫn giữ nguyên.

### Requirement: Category and tag management
Hệ thống SHALL cho phép Admin quản lý category và tag với tên/khóa duy nhất theo chuẩn hóa; mỗi bài phải có một primary category hợp lệ, tag là tùy chọn.

#### Scenario: Assign classification
- **GIVEN** category đang hoạt động và tag tồn tại.
- **WHEN** Admin tạo hoặc sửa bài với category và tag hợp lệ.
- **THEN** hệ thống lưu phân loại vào draft và dùng snapshot phân loại này khi xuất bản.

#### Scenario: Reject referenced category removal
- **GIVEN** category đang được một bài xuất bản sử dụng.
- **WHEN** Admin xóa hoặc vô hiệu category khi chưa chuyển bài sang category khác.
- **THEN** hệ thống từ chối và không làm mất đường dẫn phân loại của bài đang công khai.

### Requirement: Private draft preview
Hệ thống SHALL cho Admin xem bản nháp ở chế độ preview mà không công khai draft cho khách truy cập.

#### Scenario: Admin previews pending draft
- **GIVEN** một bài có draft mới hơn phiên bản đang xuất bản.
- **WHEN** Admin dùng endpoint preview có quyền.
- **THEN** hệ thống trả dữ liệu draft để hiển thị cùng nhãn Preview.

#### Scenario: Anonymous cannot preview
- **GIVEN** một bài chưa xuất bản.
- **WHEN** khách dùng public endpoint hoặc cố truy cập preview không xác thực.
- **THEN** public endpoint trả HTTP 404, preview trả HTTP 401 và không lộ draft.

### Requirement: Publish immutable revision
Hệ thống SHALL xuất bản một snapshot bất biến đầy đủ của bài viết trong một giao dịch và đặt snapshot đó thành phiên bản công khai hiện hành.

#### Scenario: First publication succeeds
- **GIVEN** Admin có draft hợp lệ, media tham chiếu hợp lệ và phiên bản biên tập mới nhất.
- **WHEN** Admin xuất bản với điều kiện phiên bản đúng.
- **THEN** hệ thống tạo revision mới và gắn nó làm revision công khai trong cùng một giao dịch.
- **AND** website và Flutter có thể đọc nội dung từ revision này qua public API.

#### Scenario: Publication fails atomically
- **GIVEN** draft có tham chiếu media không hợp lệ hoặc vi phạm ràng buộc dữ liệu.
- **WHEN** Admin cố xuất bản.
- **THEN** hệ thống từ chối, không xuất bản một phần và revision công khai trước đó giữ nguyên.

### Requirement: Draft edit isolation
Hệ thống SHALL tách nội dung nháp khỏi snapshot đang công khai; chỉnh sửa draft SHALL không tự động thay đổi nội dung người chơi đọc được.

#### Scenario: Editing live article leaves public unchanged
- **GIVEN** bài đang PUBLISHED với revision số 1.
- **WHEN** Admin sửa tiêu đề hoặc Markdown ở draft nhưng chưa xuất bản lại.
- **THEN** public API tiếp tục trả revision số 1, còn Admin preview hiển thị draft mới.

#### Scenario: Republish changes public snapshot
- **GIVEN** bài đang PUBLISHED và draft đã thay đổi.
- **WHEN** Admin xuất bản lại với điều kiện phiên bản hợp lệ.
- **THEN** hệ thống tạo revision số 2 và public API đọc revision số 2.

### Requirement: Unpublish without losing history
Hệ thống SHALL cho Admin ngừng công khai bài mà vẫn giữ lịch sử revision và slug đã sử dụng.

#### Scenario: Unpublish article
- **GIVEN** bài đang PUBLISHED.
- **WHEN** Admin hủy xuất bản với điều kiện phiên bản hợp lệ.
- **THEN** public article endpoint trả HTTP 404 ở lần yêu cầu mới tới backend; tìm kiếm không còn liệt kê bài.
- **AND** Admin vẫn truy cập được draft và lịch sử revision.

#### Scenario: Unknown or unpublished article hidden
- **GIVEN** bài không tồn tại, chỉ có draft hoặc đã hủy xuất bản.
- **WHEN** khách mở URL public theo slug.
- **THEN** hệ thống trả HTTP 404 và không tiết lộ nội dung hay tình trạng nội bộ của bài.

### Requirement: Revision history and restore
Hệ thống SHALL lưu lịch sử revision bất biến và cho Admin khôi phục một revision cũ thành draft, không trực tiếp thay đổi phiên bản đang công khai.

#### Scenario: Restore previous version
- **GIVEN** bài đang PUBLISHED với revision số 3 và có revision số 1.
- **WHEN** Admin chọn restore revision số 1 với điều kiện phiên bản hợp lệ.
- **THEN** draft nhận bản sao của revision số 1, lịch sử cũ giữ nguyên và public tiếp tục đọc revision số 3.

#### Scenario: Publish restored draft
- **GIVEN** Admin đã restore revision số 1 thành draft.
- **WHEN** Admin xuất bản draft với điều kiện phiên bản hợp lệ.
- **THEN** hệ thống tạo revision mới có số tăng tiếp theo và cập nhật nội dung công khai.

### Requirement: Concurrent edit protection
Hệ thống SHALL yêu cầu điều kiện phiên bản trên các thao tác sửa, xuất bản, hủy xuất bản và restore để không ghi đè thay đổi đồng thời.

#### Scenario: Missing edit precondition
- **GIVEN** Admin gửi yêu cầu thay đổi bài mà không có `If-Match`.
- **WHEN** backend xử lý yêu cầu.
- **THEN** hệ thống trả HTTP 428 và không thay đổi bài.

#### Scenario: Stale revision denied
- **GIVEN** hai Admin cùng đọc bài và Admin thứ nhất đã lưu thay đổi.
- **WHEN** Admin thứ hai gửi `If-Match` cũ.
- **THEN** hệ thống trả HTTP 412, không ghi đè dữ liệu và cho phép client tải lại phiên bản mới.

### Requirement: Safe image upload and visibility
Hệ thống SHALL chỉ cho Admin upload PNG/JPEG/WebP hợp lệ tối đa 5 MB, lưu với tên an toàn và chỉ cho khách truy cập media được phiên bản Wiki hiện đang công khai tham chiếu.

#### Scenario: Valid image upload
- **GIVEN** Admin upload ảnh đúng loại, signature, kích thước và giới hạn dung lượng.
- **WHEN** backend chấp nhận ảnh.
- **THEN** hệ thống lưu ảnh với storage key do server tạo và Admin có thể preview bằng endpoint có quyền.

#### Scenario: Reject unsafe upload
- **GIVEN** upload là file thực thi đổi phần mở rộng, loại không cho phép hoặc vượt giới hạn.
- **WHEN** Admin gửi upload.
- **THEN** hệ thống từ chối mà không lưu file có thể truy cập công khai.

#### Scenario: Private media not exposed
- **GIVEN** ảnh chỉ thuộc draft hoặc bài đã UNPUBLISHED, không được revision công khai nào khác tham chiếu.
- **WHEN** khách yêu cầu ảnh qua public media endpoint.
- **THEN** hệ thống trả HTTP 404 và không trả byte ảnh.

### Requirement: Safe article rendering
Hệ thống SHALL hiển thị Markdown của bài viết mà không cho thực thi HTML, script, event handler hoặc URL nguy hiểm từ nội dung người biên tập.

#### Scenario: Unsafe markup inert
- **GIVEN** nội dung Markdown chứa script, raw HTML hoặc link `javascript:`.
- **WHEN** website hoặc Flutter hiển thị nội dung.
- **THEN** nội dung nguy hiểm bị loại bỏ hoặc hiển thị như văn bản trơ và không thực thi mã.

### Requirement: Published-only public listing
Hệ thống SHALL cung cấp danh sách bài công khai và category chứa dữ liệu từ phiên bản xuất bản hiện hành, không lấy title/summary từ draft chưa publish.

#### Scenario: Anonymous listing
- **GIVEN** có bài đã xuất bản, bài nháp và bài ngừng xuất bản.
- **WHEN** khách gọi public list.
- **THEN** chỉ bài PUBLISHED xuất hiện và mỗi bài dùng metadata từ published revision.

#### Scenario: Category list excludes unpublished counts
- **GIVEN** một category chứa một bài PUBLISHED và hai bài DRAFT.
- **WHEN** khách gọi public category list.
- **THEN** số lượng bài công khai của category là 1, không tính draft.

### Requirement: Search and pagination
Hệ thống SHALL hỗ trợ tìm theo tiêu đề/tóm tắt đã xuất bản, lọc category/tag, phân trang từ 1 và sắp xếp ổn định theo thời điểm xuất bản giảm dần rồi ID.

#### Scenario: Search excludes draft text
- **GIVEN** draft của một bài PUBLISHED chứa từ khóa mới nhưng published revision chưa chứa từ khóa đó.
- **WHEN** khách tìm từ khóa mới.
- **THEN** bài không xuất hiện chỉ vì nội dung draft.

#### Scenario: Bounded paging
- **GIVEN** khách gửi `page=1&pageSize=20` với query hợp lệ.
- **WHEN** public list được xử lý.
- **THEN** response có `items`, `page`, `pageSize`, `total` và tối đa 20 mục theo thứ tự ổn định.

#### Scenario: Invalid page rejected
- **GIVEN** khách gửi `page=0` hoặc `pageSize=51`.
- **WHEN** public list được xử lý.
- **THEN** hệ thống trả HTTP 400, không chạy truy vấn không giới hạn.

### Requirement: Shared public read contract
Hệ thống SHALL cung cấp cùng một article revision, slug, metadata và nội dung từ API versioned cho web công khai và Flutter, không phụ thuộc việc Minecraft online.

#### Scenario: Two clients see same revision
- **GIVEN** một bài đã xuất bản với revision số 2.
- **WHEN** website và Flutter gọi public detail cho cùng slug.
- **THEN** hai client nhận cùng revision ID và nội dung từ cùng API contract.

#### Scenario: Minecraft server offline
- **GIVEN** Minecraft server không hoạt động.
- **WHEN** người chơi mở Wiki và tìm kiếm trên web hoặc Flutter.
- **THEN** các chức năng đọc Wiki vẫn dùng được nếu dịch vụ web/API/DB đang hoạt động.

### Requirement: Predictable publication freshness
Hệ thống SHALL giới hạn thời gian nội dung public bị cache cũ sau publish/unpublish tối đa 60 giây, không cache phản hồi Admin trong public cache.

#### Scenario: Publication visible after freshness window
- **GIVEN** Admin đã xuất bản hoặc hủy xuất bản bài thành công.
- **WHEN** người chơi truy cập qua website sau 60 giây.
- **THEN** website phản ánh trạng thái công khai mới; các yêu cầu trực tiếp tới API phản ánh giao dịch đã commit.

### Requirement: Consistent client-facing errors
Hệ thống SHALL trả mã HTTP đúng ngữ nghĩa cùng định dạng Problem Details an toàn cho lỗi HTTP API.

#### Scenario: Duplicate slug conflict
- **GIVEN** Admin dùng slug đã được đặt trước.
- **WHEN** Admin lưu bài.
- **THEN** API trả HTTP 409 với Problem Details và không trả stack trace.

#### Scenario: Invalid payload details
- **GIVEN** Admin gửi request thiếu title.
- **WHEN** API từ chối request.
- **THEN** API trả HTTP 400 với thông tin trường title và không tạo bản ghi.

### Requirement: Published Wiki card previews
Public article summaries SHALL expose nullable preview image URL and alternative text from media attached to the current published revision. A draft-only or unpublished image SHALL never appear in a public summary.

#### Scenario: Current revision has an image
- **GIVEN** the current published revision references one or more media records.
- **WHEN** an anonymous client lists Wiki articles.
- **THEN** the summary exposes the first referenced public media URL and its editor-provided alternative text.

#### Scenario: Current revision has no image
- **GIVEN** the current published revision has no media attachment.
- **WHEN** an anonymous client lists Wiki articles.
- **THEN** both preview fields are null and the website may use an attributed editorial fallback image.

# Kế hoạch Kiểm thử Toàn diện Hệ thống StartupConnect (Master Test Plan)

## 1. Tổng quan & Mục tiêu Kiểm thử
Tài liệu này cung cấp kịch bản kiểm thử chi tiết, toàn diện cho toàn bộ hệ thống StartupConnect bao gồm cả Frontend (Next.js 15 App Router, React 19, Tailwind CSS, Zustand, TanStack Query) và Backend (.NET 10 Minimal API, EF Core, PostgreSQL 17, SignalR, Ollama AI, AWS S3, Stripe).

Mục tiêu nhằm xác thực tính đúng đắn của logic nghiệp vụ, các kịch bản ngoại lệ (Edge Cases), khả năng xử lý đồng thời (Concurrency/Race Conditions), bảo mật hệ thống và trải nghiệm người dùng (UX) trên cả hai môi trường.

---

## 2. Phạm vi Kiểm thử Chi tiết (Detailed Test Scenarios)

### MÔ ĐUN 1: XÁC THỰC, PHÂN QUYỀN & QUẢN LÝ PHIÊN (AUTH & SECURITY)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **AUTH-01** | Đăng ký tài khoản & Xác thực Email | 1. Nhập thông tin hợp lệ tại `/auth/register`. <br>2. Nhấn Đăng ký. <br>3. Kiểm tra email outbox/SMTP nhận link xác thực. <br>4. Click link xác thực qua `/auth/verify-email`. | Tài khoản được tạo ở trạng thái chưa xác thực. Sau khi click link xác thực, trạng thái chuyển thành VerifiedUser. Người dùng đăng nhập thành công. | - Gửi yêu cầu gửi lại email xác thực (`/resend-verification`) liên tục nhiều lần (API phải rate limit). <br>- Sửa đổi token xác thực trên URL xem backend có chặn mã độc hash không. | Cao |
| **AUTH-02** | Xoay vòng Token (Refresh Token Rotation) | 1. Đăng nhập hệ thống, lấy cặp JWT Access Token và Refresh Token. <br>2. Đợi Access Token hết hạn (hoặc giả lập hết hạn). <br>3. Thực hiện một request gọi dữ liệu dự án công khai. | Axios Interceptor tự động bắt mã lỗi 401, gọi ngầm API `/auth/refresh-token`, nhận cặp token mới, cập nhật vào localStorage và hoàn tất request ban đầu mượt mà. | - Gửi đồng thời 5 request ngay khi Access Token vừa hết hạn (Race condition). Hệ thống chỉ được gọi refresh 1 lần duy nhất nhờ khóa đồng thời. <br>- Dùng lại Refresh Token cũ đã dùng rồi (Hệ thống phải phát hiện bất thường và hủy chuỗi token chain đó). | Chí mạng |
| **AUTH-03** | Bảo mật Phân quyền (RBAC & Policy) | 1. Đăng nhập tài khoản có role `User` bình thường. <br>2. Cố tình gõ trực tiếp URL `/moderator` hoặc `/admin` trên trình duyệt. <br>3. Dùng Postman gửi request đến các API nội bộ của Admin. | - Frontend chặn bằng `RoleGuard`, chuyển hướng về trang `/forbidden`. <br>- Backend chặn bằng Authorization Policy, trả về mã lỗi HTTP 403 Forbidden. | - Tài khoản bị khóa (`Suspended`/`Banned`) cố tình dùng token cũ chưa hết hạn để gọi API (Backend phải kiểm tra trạng thái thực tế trong DB và từ chối). | Cao |
| **AUTH-04** | Quên và Cấp lại Mật khẩu | 1. Yêu cầu đổi mật khẩu tại `/auth/forgot-password`. <br>2. Nhận link đổi pass qua email, click link dẫn tới `/auth/reset-password`. <br>3. Nhập mật khẩu mới và xác nhận. | Mật khẩu được đổi thành công trong DB dưới dạng mã hóa (hashing). Toàn bộ các phiên đăng nhập (Refresh Tokens) khác đang hoạt động của tài khoản này bị thu hồi (revoke) lập tức. | - Link đổi mật khẩu bị click lần thứ 2 (Phải báo link hết hạn/đã sử dụng). <br>- Request gửi quên mật khẩu cho một email không tồn tại (Hệ thống trả về thông báo chung chung, không làm lộ thông tin tài khoản có tồn tại hay không). | Trung bình |

---

### MÔ ĐUN 2: HỒ SƠ NGƯỜI DÙNG, SKILL & TẢI LÊN CV (PROFILE & STORAGE)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PROF-01** | Cấu hình Kỹ năng & Kinh nghiệm | 1. Vào trang cá nhân `/profile`. <br>2. Chọn danh sách kỹ năng từ danh mục và nhập số năm kinh nghiệm. <br>3. Bấm Lưu. | Dữ liệu được lưu chính xác vào DB. Frontend cập nhật lại State của TanStack Query ngay lập tức. | - Nhập số năm kinh nghiệm là số âm hoặc số lớn bất thường (vượt quá 50 năm). <br>- Thêm trùng lặp một kỹ năng đã tồn tại. | Thấp |
| **PROF-02** | Tải lên CV (PDF Upload) | 1. Vào mục quản lý CV `/cvs`. <br>2. Chọn một file PDF hợp lệ dưới 5MB. <br>3. Bấm Upload. | File được upload lên S3 bucket, metadata được lưu vào DB. Hệ thống sinh Presigned URL an toàn để hiển thị/tải về trên giao diện. | - Đổi đuôi một file thực thi nguy hiểm `.exe` thành `.pdf` rồi upload (Backend phải phát hiện sai Magic Signature `%PDF` và từ chối). <br>- Tên file chứa mã độc tấn công Path Traversal như `../../etc/passwd.pdf`. | Cao |

---

### MÔ ĐUN 3: QUẢN LÝ DỰ ÁN & VÒNG ĐỜI (PROJECT CORE & LIFECYCLE)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PROJ-01** | Tạo dự án nháp & Quản lý Phiên bản | 1. Nhập thông tin dự án tại `/projects/create`. <br>2. Bấm lưu nháp (Save Draft). | Hệ thống tự động thiết lập user tạo làm Founder member, khởi tạo bản ghi Dự án và sinh ra Project Version đầu tiên (v1). | - Để trống tất cả các trường bắt buộc rồi bấm lưu nháp (Hệ thống vẫn cho phép lưu nháp một phần dữ liệu nhưng sẽ cảnh báo khi gửi duyệt). | Trung bình |
| **PROJ-02** | Quy trình Chuyển đổi Trạng thái (State Flow) | 1. Từ trạng thái `Draft`, Founder bấm `Submit Review`. <br>2. Kiểm tra trạng thái chuyển sang `PendingReview`. <br>3. Moderator phê duyệt chuyển thành `Approved/Published`. | Toàn bộ quy trình diễn ra đúng State Machine. Khi đã `Published`, dự án xuất hiện công khai trên trang Tìm kiếm `/projects`. | - Dự án đang ở trạng thái `PendingReview`, Founder cố tình gửi chỉnh sửa dữ liệu cốt lõi (Hệ thống phải khóa không cho sửa hoặc thu hồi về trạng thái Draft). <br>- Hai Founder cùng bấm một hành động chuyển trạng thái tại cùng một thời điểm (Backend dùng Advisory Lock chặn race condition). | Cao |
| **PROJ-03** | Kiểm tra Quyền hiển thị (Visibility Rules) | 1. Thiết lập dự án ở chế độ `InvestorOnly` hoặc `Private`. <br>2. Dùng tài khoản Guest hoặc User thường tìm kiếm thông qua API `/api/v1/search/projects`. | Hệ thống lọc dự án ra khỏi kết quả tìm kiếm ở tầng Database/Service, đảm bảo không lộ thông tin ngay cả khi gọi API trực tiếp. | - Dùng trực tiếp link URL nội bộ của dự án ẩn để truy cập trực tiếp từ tài khoản không có quyền (Hệ thống phải hiển thị màn hình Forbidden/Not Found). | Cao |

---

### MÔ ĐUN 4: TÍCH HỢP AI SERVICES (OLLAMA SERVICES)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **AI-01** | Gợi ý Dự án & Đánh giá Chất lượng | 1. Tại không gian làm việc của dự án, bấm nút `AI Suggestions` hoặc `AI Review`. <br>2. Đợi phản hồi hiển thị kết quả. | Hệ thống gọi tầng điều phối `IAIProvider` qua Ollama API (mô hình qwen2.5/llama3.1). Trả về dữ liệu phân tích cấu trúc JSON hợp lệ và hiển thị lên giao diện. | - Hệ thống Ollama local bị sập hoặc quá tải phản hồi lâu hơn 30 giây (Frontend phải xử lý timeout mượt mà, hiển thị Skeleton hoặc nút Thử lại thay vì treo/crash trang). <br>- Tài khoản gọi vượt quá hạn ngạch sử dụng hàng ngày (Daily Quota) (Hệ thống phải trả về lỗi mã hữu hạn kèm thông báo nâng cấp gói). | Trung bình |

---

### MÔ ĐUN 5: QUY TRÌNH DUYỆT ĐƠN ỨNG TUYỂN & REALTIME (APPLICATION FLOW & SIGNALR)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **APP-01** | Nộp đơn Ứng tuyển & Chặn trùng lặp | 1. Dùng tài khoản `VerifiedUser` vào dự án công khai đang tuyển thành viên. <br>2. Chọn CV và điền Cover Letter. <br>3. Bấm Nộp đơn (Apply). | Bản ghi đơn ứng tuyển được tạo. Trạng thái là `Pending`. Founder nhận được thông báo ngay lập tức. | - Dùng công cụ gọi API nộp đơn 10 lần liên tục cùng 1 giây (Backend phải chặn, chỉ chấp nhận duy nhất 1 đơn nộp đang hoạt động - Active Application). <br>- Nộp đơn vào dự án đã đóng trạng thái tuyển dụng (`Recruiting State = Closed`). | Cao |
| **APP-02** | Đẩy thông tin Realtime qua Hub | 1. Thành viên A nộp đơn. Founder đang mở trang `/projects/[id]/applications`. <br>2. Founder chuyển trạng thái đơn sang `Shortlist` hoặc `Interview`. | - Giao diện của Founder tự cập nhật dòng dữ liệu ứng viên mà không cần F5. <br>- Thành viên A lập tức nhận được thông báo đẩy (Toast thông báo + tăng số lượng tin nhắn chưa đọc ở Chuông) thông qua SignalR Hub. | - Người dùng bị mất mạng trong 30 giây rồi có lại (SignalR phải tự động Reconnect và kéo bù các notification bị bỏ lỡ thông qua cơ chế lưu vết DB). <br>- User logout và đổi tài khoản khác trên cùng trình duyệt (Đảm bảo Hub Connection phải kết thúc và join vào đúng Group User mới, tránh rò rỉ thông tin dữ liệu cũ). | Cao |
| **APP-03** | Lên lịch Phỏng vấn (Interview Scheduling) | 1. Founder chọn đơn đã Shortlist, nhấn `Schedule Interview`. <br>2. Điền thời gian (UTC), loại hình (Online/InPerson), địa điểm/URL phòng họp. | Bản ghi lịch phỏng vấn được tạo. Hệ thống gửi email tự động và cập nhật trạng thái đồng bộ lên lịch cá nhân của cả hai bên tại `/interviews`. | - Chọn thời gian phỏng vấn rơi vào quá khứ (Hệ thống phải báo lỗi validate). <br>- Trùng lịch: Xếp 2 cuộc phỏng vấn cho cùng 1 Founder vào cùng một khung giờ biểu (Hệ thống phải cảnh báo xung đột lịch). | Trung bình |

---

### MÔ ĐUN 6: NHÀ ĐẦU TƯ, NDA & BẢO MẬT THÔNG TIN (INVESTOR & NDA)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **NDA-01** | Ký cam kết bảo mật NDA | 1. Tài khoản Investor bày tỏ sự quan tâm (`Express Interest`) tới dự án bảo mật. <br>2. Hệ thống chuyển trạng thái thành `AcceptedPendingNda`. <br>3. Investor thực hiện ký chấp thuận điều khoản NDA mẫu tại `/nda-agreements`. | Bản ghi NDA Agreement được lưu vết (gồm thông tin user, bản mẫu NDA immutable, dấu thời gian UTC). Quyền truy cập Access Grant của nhà đầu tư lập tức được kích hoạt mở khóa thông tin dự án chi tiết. | - Nhà đầu tư cố tình rút đơn quan tâm (`Withdraw Interest`) sau khi đã ký NDA (Quyền Access Grant phải bị thu hồi ngay lập tức). <br>- Admin cập nhật phiên bản NDA mới khi nhà đầu tư đang đọc dở bản cũ (Đảm bảo bản ghi lưu vết đúng phiên bản tại thời điểm ký). | Cao |

---

### MÔ ĐUN 7: CHAT, TRUYỀN TIN & BÁO CÁO (CHAT, REPORT & FEED)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **CHAT-01** | Tránh trùng lặp Hội thoại (De-duplication) | 1. User A bấm nút "Nhắn tin" với User B từ trang cá nhân của B. <br>2. Quay lại trang cá nhân của B bấm nút "Nhắn tin" một lần nữa. | Hệ thống không tạo phòng chat mới mà tự động điều hướng về Conversation ID đã tồn tại trước đó giữa 2 người (De-duplicate bằng DB advisory lock). | - Cả hai người cùng bấm nút nhắn tin với nhau cùng một thời điểm (Hệ thống chỉ tạo ra duy nhất 1 conversation). | Trung bình |
| **CHAT-02** | Cuộn trang tải tin nhắn cũ (Cursor Pagination) | 1. Vào phòng chat có sẵn hơn 100 tin nhắn. <br>2. Cuộn chuột ngược lên trên để xem tin nhắn cũ hơn. | Tin nhắn tải mượt mà theo từng cụm (page size capped) sử dụng Cursor Pagination (`before` messageId) thay vì Offet Pagination, tránh bỏ sót tin nhắn khi có tin mới đẩy vào liên tục. | - Tin nhắn bị xóa bởi người gửi (Phải hiển thị trạng thái "Tin nhắn đã bị thu hồi" cho cả hai bên). | Thấp |
| **GOV-01** | Báo cáo vi phạm (Report Module) | 1. Chọn một dự án hoặc người dùng vi phạm. <br>2. Nhấn Báo cáo, chọn lý do (Spam, FakeInformation...), điền chi tiết. | Tạo bản ghi báo cáo thành công, đẩy vào hàng đợi của Moderator. Nếu nhiều người cùng báo cáo 1 đối tượng, hệ thống tự động gộp (collapse) dữ liệu hiển thị. | - Người dùng cố tình tự gửi báo cáo vi phạm chính mình (Hệ thống phải chặn từ tầng Validate). <br>- Spam liên tục nút gửi báo cáo vi phạm (Chặn trùng lặp báo cáo đang xử lý). | Trung bình |

---

### MÔ ĐUN 8: THANH TOÁN, TÀI KHOẢN VÀ BACKGROUND JOBS (STRIPE & CRON JOBS)

| Mã TC | Chức năng / Tình huống | Các bước thực hiện (Test Steps) | Kết quả mong đợi (Expected Results) | Trường hợp ngoại lệ / Biên (Edge Cases) | Mức độ |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **PAY-01** | Thanh toán Gói dịch vụ qua Stripe | 1. Vào màn hình `/billing`, chọn gói Pro hoặc Business. <br>2. Nhấn Nâng cấp, hệ thống chuyển hướng sang Stripe Checkout URL. <br>3. Điền thông tin thẻ test thành công, bấm Thanh toán. | Stripe điều hướng về trang Return URL thành công. Hệ thống nhận sự kiện Webhook bất đồng bộ, xác thực chữ ký (Signature Verification), xử lý Idempotent lưu vết giao dịch và cộng Quota cho tài khoản. | - Người dùng tắt trình duyệt ngay sau khi thanh toán thành công bên giao diện Stripe, không đợi redirect về app (Hệ thống vẫn phải nâng cấp gói thành công cho họ nhờ luồng Webhook độc lập xử lý). <br>- Giả lập kẻ xấu dùng Postman bắn payload giả mạo tới endpoint webhook `/api/v1/webhooks/payments` (Backend bắt buộc phải từ diễn do thiếu/sai chữ ký bảo mật hóa). | Chí mạng |
| **JOB-01** | Quy trình Hàng đợi Email (Transactional Outbox) | 1. Cố tình ngắt kết nối/cấu hình sai dịch vụ SMTP gửi email. <br>2. Thực hiện hành động kích hoạt gửi mail như đổi mật khẩu hoặc mời thành viên vào dự án. | - API nghiệp vụ chính vẫn thực hiện thành công và trả về HTTP 200 (vì email được lưu vào bảng Email Outbox trong cùng 1 Database Transaction). <br>- Background Worker quét định kỳ phát hiện lỗi gửi, đánh dấu `Failed` và lưu vết số lần thử lại (Attempts). | - Sau khi sửa lại cấu hình SMTP, Admin vào màn hình quản lý `/admin/email-outbox` nhấn nút `Retry` thành công cho các mail lỗi. <br>- Nhiều API Instance cùng chạy song song (Phải dùng `FOR UPDATE SKIP LOCKED` để tránh các instance giành giật gửi trùng 1 email). | Cao |
| **JOB-02** | Dọn dẹp tài nguyên rác (Maintenance Jobs) | 1. Đăng ký tài khoản nhưng bỏ lửng không xác thực email, để quá 7 ngày. <br>2. Tạo bản nháp dự án tải file lên S3 sau đó xóa bản nháp đó. | Bản ghi xác thực hết hạn bị xóa. Tập tin mồ côi (Orphan Files) không còn metadata trong DB sẽ bị Background Worker tự động quét qua Storage Provider và xóa tận gốc file vật lý trên S3. | - Tiến trình dọn dẹp đang chạy thì hệ thống API bị khởi động lại đột ngột (Không làm mất trạng thái công việc nhờ tiến trình được ghi nhận trạng thái vào bảng Job Execution dưới DB). | Trung bình |

---

## 3. Quy trình Thực thi Kiểm thử khuyến nghị
1. **Kiểm thử Đơn vị & Tích hợp (CI/CD):** Chạy toàn bộ 103 automated tests hiện tại bằng lệnh `dotnet test`.
2. **Kiểm thử Khói (Smoke Test):** Chạy script `tools/smoke-test.sh` kiểm tra tính sẵn sàng của các endpoint live/ready, các tính năng search cốt lõi sau khi deploy bản dựng Docker.
3. **Kiểm thử Tải (Load Test):** Chạy `tools/load-test.mjs` giả lập 500 requests với mức độ đồng thời (concurrency) là 25 để đo lường chỉ số P95 phản hồi (<100ms) và tỷ lệ lỗi (0% lỗi).
4. **Kiểm thử Chức năng Thủ công (Manual QA):** Sử dụng bảng kịch bản chi tiết phía trên để tiến hành rà soát chéo các Edge Cases và ghi nhận log lỗi.

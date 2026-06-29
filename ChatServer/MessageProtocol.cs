// ============================================================
// Mô tả: Định nghĩa tất cả hằng số lệnh của giao thức text.
//        Cả Server và Client đều tham chiếu đến các hằng số này
//        để tránh lỗi gõ nhầm tên lệnh (magic string).
// ============================================================

namespace ChatServer
{
    public static class MessageProtocol
    {
        // -------------------------------------------------------
        // LỆNH TỪ CLIENT GỬI LÊN SERVER
        // -------------------------------------------------------

        // Đăng nhập: "LOGIN|username"
        public const string LOGIN = "LOGIN";

        // Yêu cầu danh sách phòng: "LIST_ROOM"
        public const string LIST_ROOM = "LIST_ROOM";

        // Tạo phòng mới: "CREATE_ROOM|tenPhong"
        public const string CREATE_ROOM = "CREATE_ROOM";

        // Tham gia phòng: "JOIN_ROOM|tenPhong"
        public const string JOIN_ROOM = "JOIN_ROOM";

        // Rời phòng hiện tại: "LEAVE_ROOM"
        public const string LEAVE_ROOM = "LEAVE_ROOM";

        // Gửi tin nhắn vào phòng: "ROOM_MSG|tenPhong|noiDung"
        public const string ROOM_MSG = "ROOM_MSG";

        // Gửi tin nhắn riêng: "PRIVATE|tenNguoiNhan|noiDung"
        public const string PRIVATE = "PRIVATE";

        // Yêu cầu danh sách online: "ONLINE"
        public const string ONLINE = "ONLINE";

        // -------------------------------------------------------
        // LỆNH TỪ SERVER TRẢ VỀ CHO CLIENT
        // -------------------------------------------------------

        // Đăng nhập thành công: "LOGIN_OK"
        public const string LOGIN_OK = "LOGIN_OK";

        // Đăng nhập thất bại: "LOGIN_FAIL|lyDo"
        public const string LOGIN_FAIL = "LOGIN_FAIL";

        // Danh sách phòng: "ROOM_LIST|phong1,phong2,phong3"
        public const string ROOM_LIST = "ROOM_LIST";

        // Xác nhận vào phòng: "ROOM_JOINED|tenPhong|thanhVien1,thanhVien2"
        public const string ROOM_JOINED = "ROOM_JOINED";

        // Tin nhắn trong phòng: "MSG|nguoiGui|tenPhong|noiDung"
        public const string MSG = "MSG";

        // Tin nhắn riêng đến: "PRIVATE_MSG|nguoiGui|noiDung"
        public const string PRIVATE_MSG = "PRIVATE_MSG";

        // Thông báo có người đăng nhập: "USER_JOINED|username"
        public const string USER_JOINED = "USER_JOINED";

        // Thông báo có người rời/mất kết nối: "USER_LEFT|username"
        public const string USER_LEFT = "USER_LEFT";

        // Danh sách người online: "ONLINE_LIST|user1,user2,user3"
        public const string ONLINE_LIST = "ONLINE_LIST";

        // Thông báo lỗi chung: "ERROR|moTaLoi"
        public const string ERROR = "ERROR";

        // -------------------------------------------------------
        // PHƯƠNG THỨC TIỆN ÍCH
        // -------------------------------------------------------

        /// <summary>
        /// Tách chuỗi lệnh thành mảng các phần.
        /// Dùng maxCount để nội dung tin nhắn không bị cắt nhầm
        /// khi người dùng gõ ký tự '|' trong tin nhắn.
        /// Ví dụ: "ROOM_MSG|CNTT|Hello|World" 
        ///         → parts[0]="ROOM_MSG", parts[1]="CNTT", parts[2]="Hello|World"
        /// </summary>
        public static string[] Parse(string message, int maxParts = 10)
        {
            if (string.IsNullOrEmpty(message))
                return new string[0];

            return message.Split(new char[] { '|' }, maxParts);
        }

        /// <summary>
        /// Ghép lệnh và các tham số lại thành chuỗi giao thức.
        /// Ví dụ: Build("MSG", "Son", "CNTT", "Xin chào") 
        ///         → "MSG|Son|CNTT|Xin chào"
        /// </summary>
        public static string Build(params string[] parts)
        {
            return string.Join("|", parts);
        }
    }
}
// ============================================================
// File: Protocol.cs
// Dự án: ChatClient
// Mô tả: Hằng số lệnh phía Client — hoàn toàn giống
//        MessageProtocol.cs bên Server để đảm bảo cả hai
//        phía dùng chung định dạng giao thức.
// ============================================================

namespace ChatClient
{
    public static class Protocol
    {
        // -------------------------------------------------------
        // LỆNH CLIENT GỬI LÊN SERVER
        // -------------------------------------------------------
        public const string LOGIN = "LOGIN";
        public const string LIST_ROOM = "LIST_ROOM";
        public const string CREATE_ROOM = "CREATE_ROOM";
        public const string JOIN_ROOM = "JOIN_ROOM";
        public const string LEAVE_ROOM = "LEAVE_ROOM";
        public const string ROOM_MSG = "ROOM_MSG";
        public const string PRIVATE = "PRIVATE";
        public const string ONLINE = "ONLINE";

        // -------------------------------------------------------
        // LỆNH SERVER TRẢ VỀ
        // -------------------------------------------------------
        public const string LOGIN_OK = "LOGIN_OK";
        public const string LOGIN_FAIL = "LOGIN_FAIL";
        public const string ROOM_LIST = "ROOM_LIST";
        public const string ROOM_JOINED = "ROOM_JOINED";
        public const string MSG = "MSG";
        public const string PRIVATE_MSG = "PRIVATE_MSG";
        public const string USER_JOINED = "USER_JOINED";
        public const string USER_LEFT = "USER_LEFT";
        public const string ONLINE_LIST = "ONLINE_LIST";
        public const string ERROR = "ERROR";

        // -------------------------------------------------------
        // PHƯƠNG THỨC TIỆN ÍCH
        // -------------------------------------------------------

        /// <summary>
        /// Tách chuỗi lệnh thành mảng, tối đa maxParts phần.
        /// maxParts giúp nội dung tin nhắn có dấu '|' không bị cắt nhầm.
        /// </summary>
        public static string[] Parse(string message, int maxParts = 10)
        {
            if (string.IsNullOrEmpty(message))
                return new string[0];

            return message.Split(new char[] { '|' }, maxParts);
        }

        /// <summary>
        /// Ghép lệnh và tham số thành chuỗi giao thức.
        /// Ví dụ: Build("ROOM_MSG", "CNTT", "Hello") → "ROOM_MSG|CNTT|Hello"
        /// </summary>
        public static string Build(params string[] parts)
        {
            return string.Join("|", parts);
        }
    }
}
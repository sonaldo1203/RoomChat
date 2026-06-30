
using System;
using System.Collections.Generic;

namespace ChatServer
{
    public class Room
    {
        // -------------------------------------------------------
        // THUỘC TÍNH
        // -------------------------------------------------------

        /// <summary>Tên phòng, dùng làm khóa trong Dictionary.</summary>
        public string RoomName { get; private set; }

        /// <summary>
        /// Danh sách các ClientHandler đang ở trong phòng.
        /// Dùng List vì cần duyệt tuần tự khi broadcast.
        /// </summary>
        private List<ClientHandler> _members;

        /// <summary>
        /// Object dùng để lock khi truy cập _members từ nhiều Thread.
        /// Mỗi Room có lock riêng → các phòng khác nhau không chờ nhau.
        /// </summary>
        private readonly object _lock = new object();

        // -------------------------------------------------------
        // KHỞI TẠO
        // -------------------------------------------------------

        public Room(string roomName)
        {
            RoomName = roomName;
            _members = new List<ClientHandler>();
        }

        // -------------------------------------------------------
        // PHƯƠNG THỨC QUẢN LÝ THÀNH VIÊN
        // -------------------------------------------------------

        /// <summary>
        /// Thêm client vào phòng.
        /// Dùng lock để đảm bảo an toàn khi nhiều Thread cùng gọi.
        /// </summary>
        public void AddMember(ClientHandler client)
        {
            lock (_lock)
            {
                if (!_members.Contains(client))
                {
                    _members.Add(client);
                }
            }
        }

        /// <summary>
        /// Xóa client khỏi phòng (khi rời phòng hoặc mất kết nối).
        /// </summary>
        public void RemoveMember(ClientHandler client)
        {
            lock (_lock)
            {
                _members.Remove(client);
            }
        }

        /// <summary>
        /// Kiểm tra phòng có còn thành viên không.
        /// Server dùng để quyết định xóa phòng rỗng hay giữ lại.
        /// </summary>
        public bool IsEmpty()
        {
            lock (_lock)
            {
                return _members.Count == 0;
            }
        }

        /// <summary>
        /// Lấy số lượng thành viên hiện tại.
        /// </summary>
        public int MemberCount()
        {
            lock (_lock)
            {
                return _members.Count;
            }
        }

        /// <summary>
        /// Lấy danh sách tên thành viên, dùng để gửi ROOM_JOINED.
        /// Trả về bản sao để tránh lỗi khi List thay đổi bên ngoài.
        /// </summary>
        public List<string> GetMemberNames()
        {
            lock (_lock)
            {
                List<string> names = new List<string>();
                foreach (ClientHandler member in _members)
                {
                    names.Add(member.Username);
                }
                return names;
            }
        }

        // -------------------------------------------------------
        // PHƯƠNG THỨC BROADCAST
        // -------------------------------------------------------

        /// <summary>
        /// Gửi một tin nhắn đến TẤT CẢ thành viên trong phòng.
        /// Tham số sender: nếu khác null thì bỏ qua người gửi
        ///                 (một số trường hợp server muốn gửi cho cả người gửi).
        /// </summary>
        public void Broadcast(string message, ClientHandler sender = null)
        {
            // Lấy bản sao danh sách để không giữ lock trong khi gửi
            // (việc gửi qua NetworkStream có thể chậm, không nên giữ lock lâu)
            List<ClientHandler> snapshot;
            lock (_lock)
            {
                snapshot = new List<ClientHandler>(_members);
            }

            foreach (ClientHandler member in snapshot)
            {
                // Nếu muốn bỏ qua người gửi, bỏ comment dòng dưới:
                // if (sender != null && member == sender) continue;
                try
                {
                    member.SendMessage(message);
                }
                catch (Exception ex)
                {
                    // Nếu gửi lỗi (client đã mất kết nối), ghi log và bỏ qua
                    Console.WriteLine($"[Room] Lỗi khi broadcast tới {member.Username}: {ex.Message}");
                }
            }
        }
    }
}
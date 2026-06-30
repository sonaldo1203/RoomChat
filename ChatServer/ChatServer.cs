
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    public class ChatServer
    {
        // -------------------------------------------------------
        // CÁC THÀNH PHẦN CỐT LÕI
        // -------------------------------------------------------

        /// <summary>Lắng nghe kết nối TCP đến từ client.</summary>
        private TcpListener _listener;

        /// <summary>
        /// Danh sách tất cả client đang kết nối.
        /// Key: username (string)
        /// Value: ClientHandler xử lý kết nối đó
        /// </summary>
        private Dictionary<string, ClientHandler> _clients;

        /// <summary>
        /// Danh sách tất cả phòng đang tồn tại.
        /// Key: tên phòng (string)
        /// Value: đối tượng Room
        /// </summary>
        private Dictionary<string, Room> _rooms;

        /// <summary>Lock bảo vệ _clients khi nhiều Thread truy cập.</summary>
        private readonly object _clientsLock = new object();

        /// <summary>Lock bảo vệ _rooms khi nhiều Thread truy cập.</summary>
        private readonly object _roomsLock = new object();

        /// <summary>Cổng server lắng nghe.</summary>
        private int _port;

        /// <summary>Cờ kiểm soát vòng lặp chấp nhận kết nối.</summary>
        private bool _isRunning;

        // -------------------------------------------------------
        // KHỞI TẠO
        // -------------------------------------------------------

        public ChatServer(int port)
        {
            _port = port;
            _clients = new Dictionary<string, ClientHandler>();
            _rooms = new Dictionary<string, Room>();
            _isRunning = false;
        }

        // -------------------------------------------------------
        // KHỞI ĐỘNG SERVER
        // -------------------------------------------------------

        /// <summary>
        /// Khởi động TcpListener và bắt đầu vòng lặp chấp nhận kết nối.
        /// Chạy trong Thread của Program.cs (Main thread).
        /// </summary>
        public void Start()
        {
            // IPAddress.Any = lắng nghe trên tất cả network interface của máy
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"[Server] Đã khởi động, đang lắng nghe trên cổng {_port}...");
            Console.WriteLine("[Server] Nhấn Ctrl+C để dừng server.");

            // Vòng lặp chờ và chấp nhận kết nối mới
            while (_isRunning)
            {
                try
                {
                    // AcceptTcpClient() block cho đến khi có client kết nối vào
                    TcpClient tcpClient = _listener.AcceptTcpClient();

                    Console.WriteLine($"[Server] Kết nối mới từ: {tcpClient.Client.RemoteEndPoint}");

                    // Tạo ClientHandler để quản lý client vừa kết nối
                    ClientHandler handler = new ClientHandler(tcpClient, this);

                    // Khởi động Thread riêng cho client này
                    // Main thread tiếp tục quay lại AcceptTcpClient() để chờ client tiếp theo
                    handler.Start();
                }
                catch (SocketException ex) when (!_isRunning)
                {
                    // Server đang dừng, bỏ qua lỗi
                    Console.WriteLine("[Server] Server đã dừng.");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"[Server] Lỗi khi chấp nhận kết nối: {ex.Message}");
                }
            }
        }

        /// <summary>Dừng server và giải phóng tài nguyên.</summary>
        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("[Server] Đã dừng.");
        }

        // -------------------------------------------------------
        // QUẢN LÝ CLIENT
        // -------------------------------------------------------

        /// <summary>
        /// Đăng ký client mới sau khi đăng nhập thành công.
        /// Trả về true nếu username hợp lệ (không trùng).
        /// Trả về false nếu username đã tồn tại.
        /// </summary>
        public bool TryRegisterClient(string username, ClientHandler handler)
        {
            lock (_clientsLock)
            {
                if (_clients.ContainsKey(username))
                    return false;

                _clients[username] = handler;
                return true;
            }
        }

        /// <summary>
        /// Xóa client khỏi danh sách khi ngắt kết nối.
        /// </summary>
        public void UnregisterClient(string username)
        {
            lock (_clientsLock)
            {
                _clients.Remove(username);
            }
        }

        /// <summary>
        /// Lấy danh sách username của tất cả người đang online.
        /// Trả về chuỗi "user1,user2,user3".
        /// </summary>
        public string GetOnlineList()
        {
            lock (_clientsLock)
            {
                return string.Join(",", _clients.Keys);
            }
        }

        // -------------------------------------------------------
        // QUẢN LÝ PHÒNG
        // -------------------------------------------------------

        /// <summary>
        /// Tạo phòng mới. Trả về false nếu phòng đã tồn tại.
        /// </summary>
        public bool CreateRoom(string roomName)
        {
            lock (_roomsLock)
            {
                if (_rooms.ContainsKey(roomName))
                    return false;

                _rooms[roomName] = new Room(roomName);
                Console.WriteLine($"[Server] Phòng '{roomName}' được tạo.");
                return true;
            }
        }

        /// <summary>
        /// Thêm client vào phòng. Trả về false nếu phòng không tồn tại.
        /// </summary>
        public bool JoinRoom(string roomName, ClientHandler handler)
        {
            lock (_roomsLock)
            {
                if (!_rooms.ContainsKey(roomName))
                    return false;

                _rooms[roomName].AddMember(handler);
                return true;
            }
        }

        /// <summary>
        /// Xóa client khỏi phòng.
        /// </summary>
        public void LeaveRoom(string roomName, ClientHandler handler)
        {
            lock (_roomsLock)
            {
                if (_rooms.ContainsKey(roomName))
                {
                    _rooms[roomName].RemoveMember(handler);
                }
            }
        }

        /// <summary>
        /// Lấy danh sách tên phòng, ngăn cách bởi dấu phẩy.
        /// Trả về "CNTT,KTPM,ATTT" hoặc "" nếu không có phòng.
        /// </summary>
        public string GetRoomList()
        {
            lock (_roomsLock)
            {
                return string.Join(",", _rooms.Keys);
            }
        }

        /// <summary>
        /// Lấy danh sách tên thành viên trong phòng.
        /// Trả về "Son,Huy,Nam".
        /// </summary>
        public string GetRoomMembers(string roomName)
        {
            lock (_roomsLock)
            {
                if (!_rooms.ContainsKey(roomName))
                    return "";

                List<string> names = _rooms[roomName].GetMemberNames();
                return string.Join(",", names);
            }
        }

        // -------------------------------------------------------
        // PHƯƠNG THỨC BROADCAST
        // -------------------------------------------------------

        /// <summary>
        /// Gửi tin nhắn đến TẤT CẢ client đang online.
        /// excludeClient: bỏ qua client này (thường là người gửi).
        /// </summary>
        public void BroadcastToAll(string message, ClientHandler excludeClient = null)
        {
            // Lấy bản sao để không giữ lock trong lúc gửi
            List<ClientHandler> snapshot;
            lock (_clientsLock)
            {
                snapshot = new List<ClientHandler>(_clients.Values);
            }

            foreach (ClientHandler client in snapshot)
            {
                if (excludeClient != null && client == excludeClient)
                    continue;

                try { client.SendMessage(message); }
                catch { /* bỏ qua client đã mất kết nối */ }
            }
        }

        /// <summary>
        /// Gửi tin nhắn đến tất cả thành viên trong một phòng cụ thể.
        /// excludeClient: bỏ qua client này.
        /// </summary>
        public void BroadcastToRoom(string roomName, string message, ClientHandler excludeClient = null)
        {
            Room room;
            lock (_roomsLock)
            {
                if (!_rooms.ContainsKey(roomName)) return;
                room = _rooms[roomName];
            }

            // Gọi Broadcast của Room (Room có lock riêng bên trong)
            room.Broadcast(message, excludeClient);
        }

        /// <summary>
        /// Gửi tin nhắn riêng tới đúng một người.
        /// Trả về false nếu người nhận không tồn tại.
        /// </summary>
        public bool SendPrivateMessage(string targetUsername, string message)
        {
            ClientHandler target;
            lock (_clientsLock)
            {
                if (!_clients.ContainsKey(targetUsername))
                    return false;

                target = _clients[targetUsername];
            }

            try
            {
                target.SendMessage(message);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
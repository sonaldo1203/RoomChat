// ============================================================
// File: ClientHandler.cs
// Dự án: ChatServer
// Mô tả: Xử lý kết nối của MỘT client cụ thể.
//        Chạy trong Thread riêng, đọc lệnh từ client,
//        gọi ChatServer để xử lý logic và phản hồi.
// ============================================================

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    public class ClientHandler
    {
        // -------------------------------------------------------
        // THUỘC TÍNH
        // -------------------------------------------------------

        /// <summary>Tên người dùng, được gán sau khi LOGIN thành công.</summary>
        public string Username { get; private set; }

        /// <summary>Phòng hiện tại client đang ở. Null nếu chưa vào phòng nào.</summary>
        public string CurrentRoom { get; set; }

        // Kết nối TCP và các luồng đọc/ghi
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        // Tham chiếu đến ChatServer để gọi các phương thức chung
        private ChatServer _server;

        // Thread xử lý việc lắng nghe tin nhắn từ client
        private Thread _listenerThread;

        // Cờ kiểm soát vòng lặp đọc
        private bool _isRunning;

        // Lock cho việc ghi dữ liệu ra stream (tránh hai Thread cùng ghi)
        private readonly object _writeLock = new object();

        // -------------------------------------------------------
        // KHỞI TẠO
        // -------------------------------------------------------

        public ClientHandler(TcpClient tcpClient, ChatServer server)
        {
            _tcpClient = tcpClient;
            _server = server;
            _isRunning = true;
            CurrentRoom = null;
            Username = null;

            // Lấy NetworkStream từ TcpClient
            _stream = _tcpClient.GetStream();

            // StreamReader: đọc từng dòng văn bản từ stream
            // UTF8 để hỗ trợ tiếng Việt
            _reader = new StreamReader(_stream, System.Text.Encoding.UTF8);

            // StreamWriter: ghi từng dòng văn bản ra stream
            // autoFlush = true để dữ liệu được gửi ngay, không chờ buffer đầy
            _writer = new StreamWriter(_stream, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        // -------------------------------------------------------
        // KHỞI ĐỘNG THREAD LẮNG NGHE
        // -------------------------------------------------------

        /// <summary>
        /// Tạo và khởi động Thread riêng để lắng nghe tin nhắn từ client.
        /// IsBackground = true: Thread tự kết thúc khi chương trình chính đóng.
        /// </summary>
        public void Start()
        {
            _listenerThread = new Thread(ListenForMessages);
            _listenerThread.IsBackground = true;
            _listenerThread.Name = $"ClientThread_{_tcpClient.Client.RemoteEndPoint}";
            _listenerThread.Start();
        }

        // -------------------------------------------------------
        // VÒNG LẶP CHÍNH: ĐỌC TIN NHẮN TỪ CLIENT
        // -------------------------------------------------------

        /// <summary>
        /// Chạy trong Thread riêng.
        /// Liên tục đọc từng dòng từ client và xử lý lệnh.
        /// Thoát khi client ngắt kết nối hoặc xảy ra lỗi.
        /// </summary>
        private void ListenForMessages()
        {
            try
            {
                string message;

                // ReadLine() sẽ block (chờ) cho đến khi có dữ liệu mới
                // Khi client ngắt kết nối, ReadLine() trả về null
                while (_isRunning && (message = _reader.ReadLine()) != null)
                {
                    // Bỏ qua tin nhắn rỗng
                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    Console.WriteLine($"[Server] Nhận từ {Username ?? "Chưa đăng nhập"}: {message}");

                    // Xử lý lệnh nhận được
                    ProcessCommand(message);
                }
            }
            catch (IOException)
            {
                // IOException xảy ra khi kết nối bị ngắt đột ngột (mất mạng, tắt chương trình)
                Console.WriteLine($"[Server] Client {Username ?? "Unknown"} mất kết nối đột ngột.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Lỗi không xác định từ {Username ?? "Unknown"}: {ex.Message}");
            }
            finally
            {
                // Dù xảy ra lỗi gì, vẫn phải dọn dẹp
                Disconnect();
            }
        }

        // -------------------------------------------------------
        // XỬ LÝ LỆNH
        // -------------------------------------------------------

        /// <summary>
        /// Phân tích chuỗi lệnh và thực hiện hành động tương ứng.
        /// Đây là "bộ não" của ClientHandler.
        /// </summary>
        private void ProcessCommand(string message)
        {
            // Tách lệnh thành các phần, tối đa 10 phần
            string[] parts = MessageProtocol.Parse(message, 10);
            if (parts.Length == 0) return;

            string command = parts[0].ToUpper().Trim();

            switch (command)
            {
                case MessageProtocol.LOGIN:
                    HandleLogin(parts);
                    break;

                case MessageProtocol.LIST_ROOM:
                    HandleListRoom();
                    break;

                case MessageProtocol.CREATE_ROOM:
                    HandleCreateRoom(parts);
                    break;

                case MessageProtocol.JOIN_ROOM:
                    HandleJoinRoom(parts);
                    break;

                case MessageProtocol.LEAVE_ROOM:
                    HandleLeaveRoom();
                    break;

                case MessageProtocol.ROOM_MSG:
                    HandleRoomMessage(parts);
                    break;

                case MessageProtocol.PRIVATE:
                    HandlePrivateMessage(parts);
                    break;

                case MessageProtocol.ONLINE:
                    HandleOnlineList();
                    break;

                default:
                    SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, $"Lệnh không hợp lệ: {command}"));
                    break;
            }
        }

        // -------------------------------------------------------
        // CÁC HANDLER XỬ LÝ TỪNG LỆNH
        // -------------------------------------------------------

        /// <summary>
        /// Xử lý đăng nhập.
        /// Lệnh: "LOGIN|username"
        /// </summary>
        private void HandleLogin(string[] parts)
        {
            // Kiểm tra định dạng lệnh
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.LOGIN_FAIL, "Thiếu username"));
                return;
            }

            string requestedUsername = parts[1].Trim();

            // Kiểm tra không cho đăng nhập lại nếu đã có tên
            if (Username != null)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.LOGIN_FAIL, "Bạn đã đăng nhập rồi"));
                return;
            }

            // Nhờ ChatServer kiểm tra username có trùng không
            bool success = _server.TryRegisterClient(requestedUsername, this);

            if (success)
            {
                Username = requestedUsername;
                SendMessage(MessageProtocol.LOGIN_OK);
                Console.WriteLine($"[Server] {Username} đã đăng nhập.");

                // Thông báo cho tất cả các client khác
                _server.BroadcastToAll(
                    MessageProtocol.Build(MessageProtocol.USER_JOINED, Username),
                    excludeClient: this
                );
            }
            else
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.LOGIN_FAIL, "Username đã tồn tại"));
            }
        }

        /// <summary>
        /// Xử lý yêu cầu danh sách phòng.
        /// Lệnh: "LIST_ROOM"
        /// </summary>
        private void HandleListRoom()
        {
            if (!IsLoggedIn()) return;

            string roomList = _server.GetRoomList();
            SendMessage(MessageProtocol.Build(MessageProtocol.ROOM_LIST, roomList));
        }

        /// <summary>
        /// Xử lý tạo phòng mới.
        /// Lệnh: "CREATE_ROOM|tenPhong"
        /// </summary>
        private void HandleCreateRoom(string[] parts)
        {
            if (!IsLoggedIn()) return;

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Thiếu tên phòng"));
                return;
            }

            string roomName = parts[1].Trim();
            bool created = _server.CreateRoom(roomName);

            if (created)
            {
                Console.WriteLine($"[Server] {Username} tạo phòng: {roomName}");
                // Sau khi tạo, tự động vào phòng luôn
                JoinRoomInternal(roomName);
            }
            else
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, $"Phòng '{roomName}' đã tồn tại"));
            }
        }

        /// <summary>
        /// Xử lý yêu cầu tham gia phòng.
        /// Lệnh: "JOIN_ROOM|tenPhong"
        /// </summary>
        private void HandleJoinRoom(string[] parts)
        {
            if (!IsLoggedIn()) return;

            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Thiếu tên phòng"));
                return;
            }

            string roomName = parts[1].Trim();
            JoinRoomInternal(roomName);
        }

        /// <summary>
        /// Logic dùng chung cho cả JOIN và CREATE (tự vào sau khi tạo).
        /// </summary>
        private void JoinRoomInternal(string roomName)
        {
            // Nếu đang ở phòng khác, rời ra trước
            if (CurrentRoom != null && CurrentRoom != roomName)
            {
                HandleLeaveRoom();
            }

            bool joined = _server.JoinRoom(roomName, this);

            if (joined)
            {
                CurrentRoom = roomName;
                // Lấy danh sách thành viên hiện tại của phòng
                string members = _server.GetRoomMembers(roomName);
                SendMessage(MessageProtocol.Build(MessageProtocol.ROOM_JOINED, roomName, members));

                Console.WriteLine($"[Server] {Username} đã vào phòng: {roomName}");

                // Thông báo cho các thành viên khác trong phòng
                _server.BroadcastToRoom(
                    roomName,
                    MessageProtocol.Build(MessageProtocol.USER_JOINED, Username),
                    excludeClient: this
                );
            }
            else
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, $"Phòng '{roomName}' không tồn tại"));
            }
        }

        /// <summary>
        /// Xử lý rời phòng.
        /// Lệnh: "LEAVE_ROOM"
        /// </summary>
        private void HandleLeaveRoom()
        {
            if (!IsLoggedIn()) return;
            if (CurrentRoom == null)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Bạn chưa ở trong phòng nào"));
                return;
            }

            string roomName = CurrentRoom;
            _server.LeaveRoom(roomName, this);
            CurrentRoom = null;

            Console.WriteLine($"[Server] {Username} rời phòng: {roomName}");

            // Thông báo cho các thành viên còn lại
            _server.BroadcastToRoom(
                roomName,
                MessageProtocol.Build(MessageProtocol.USER_LEFT, Username)
            );
        }

        /// <summary>
        /// Xử lý gửi tin nhắn vào phòng.
        /// Lệnh: "ROOM_MSG|tenPhong|noiDung"
        /// </summary>
        private void HandleRoomMessage(string[] parts)
        {
            if (!IsLoggedIn()) return;

            if (parts.Length < 3)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Định dạng sai: ROOM_MSG|phong|noidung"));
                return;
            }

            string roomName = parts[1].Trim();

            // Ghép lại nội dung (phòng trường hợp nội dung có chứa '|')
            string content = string.Join("|", parts, 2, parts.Length - 2);

            if (CurrentRoom != roomName)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Bạn không ở trong phòng này"));
                return;
            }

            // Định dạng thời gian theo yêu cầu đề bài
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Gửi: "MSG|nguoiGui|tenPhong|[timestamp] noiDung"
            string broadcastMsg = MessageProtocol.Build(
                MessageProtocol.MSG,
                Username,
                roomName,
                $"[{timestamp}]",
                content
            );

            // Broadcast cho tất cả thành viên trong phòng (kể cả người gửi)
            _server.BroadcastToRoom(roomName, broadcastMsg);
        }

        /// <summary>
        /// Xử lý tin nhắn riêng.
        /// Lệnh: "PRIVATE|tenNguoiNhan|noiDung"
        /// </summary>
        private void HandlePrivateMessage(string[] parts)
        {
            if (!IsLoggedIn()) return;

            if (parts.Length < 3)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Định dạng sai: PRIVATE|nguoinhan|noidung"));
                return;
            }

            string targetUsername = parts[1].Trim();
            string content = string.Join("|", parts, 2, parts.Length - 2);

            // Không cho nhắn tin cho chính mình
            if (targetUsername == Username)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Không thể gửi tin nhắn cho chính mình"));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Nhờ Server tìm và gửi tới người nhận
            bool sent = _server.SendPrivateMessage(
                targetUsername,
                MessageProtocol.Build(MessageProtocol.PRIVATE_MSG, Username, $"[{timestamp}]", content)
            );

            if (!sent)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, $"Người dùng '{targetUsername}' không tồn tại hoặc đã offline"));
            }
        }

        /// <summary>
        /// Xử lý yêu cầu danh sách online.
        /// Lệnh: "ONLINE"
        /// </summary>
        private void HandleOnlineList()
        {
            if (!IsLoggedIn()) return;

            string onlineList = _server.GetOnlineList();
            SendMessage(MessageProtocol.Build(MessageProtocol.ONLINE_LIST, onlineList));
        }

        // -------------------------------------------------------
        // PHƯƠNG THỨC HỖ TRỢ
        // -------------------------------------------------------

        /// <summary>
        /// Kiểm tra client đã đăng nhập chưa.
        /// Nếu chưa, gửi thông báo lỗi và trả về false.
        /// </summary>
        private bool IsLoggedIn()
        {
            if (Username == null)
            {
                SendMessage(MessageProtocol.Build(MessageProtocol.ERROR, "Bạn chưa đăng nhập"));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gửi một tin nhắn (một dòng) đến client này.
        /// Dùng lock để tránh hai Thread cùng ghi vào StreamWriter.
        /// </summary>
        public void SendMessage(string message)
        {
            try
            {
                lock (_writeLock)
                {
                    _writer.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Không thể gửi tới {Username ?? "Unknown"}: {ex.Message}");
            }
        }

        /// <summary>
        /// Dọn dẹp tài nguyên khi client ngắt kết nối.
        /// Được gọi từ finally trong ListenForMessages().
        /// </summary>
        private void Disconnect()
        {
            if (!_isRunning) return; // Tránh gọi Disconnect hai lần
            _isRunning = false;

            // Nếu đang ở phòng, rời ra và thông báo
            if (CurrentRoom != null)
            {
                string roomName = CurrentRoom;
                _server.LeaveRoom(roomName, this);
                CurrentRoom = null;

                _server.BroadcastToRoom(
                    roomName,
                    MessageProtocol.Build(MessageProtocol.USER_LEFT, Username ?? "Unknown")
                );
            }

            // Xóa khỏi danh sách online toàn cục
            if (Username != null)
            {
                _server.UnregisterClient(Username);

                // Thông báo cho tất cả người khác
                _server.BroadcastToAll(
                    MessageProtocol.Build(MessageProtocol.USER_LEFT, Username)
                );

                Console.WriteLine($"[Server] {Username} đã ngắt kết nối.");
            }

            // Đóng kết nối và giải phóng tài nguyên
            try { _reader?.Close(); } catch { }
            try { _writer?.Close(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }
    }
}
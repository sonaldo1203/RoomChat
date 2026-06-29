// ============================================================
// File: ChatClient.cs
// Dự án: ChatClient
// Mô tả: Quản lý kết nối TCP tới Server.
//        - Kết nối / ngắt kết nối
//        - Gửi lệnh lên Server
//        - Thread riêng để liên tục nhận tin từ Server
//        - Dùng delegate/event để thông báo lên MainForm
// ============================================================

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ChatClient
{
    public class ChatClient
    {
        // -------------------------------------------------------
        // SỰ KIỆN (EVENT) — MainForm đăng ký để nhận dữ liệu
        // -------------------------------------------------------

        /// <summary>
        /// Sự kiện kích hoạt mỗi khi nhận được một dòng từ Server.
        /// MainForm lắng nghe sự kiện này để cập nhật giao diện.
        /// </summary>
        public event Action<string> MessageReceived;

        /// <summary>
        /// Sự kiện kích hoạt khi kết nối bị ngắt.
        /// MainForm dùng để thông báo và reset giao diện.
        /// </summary>
        public event Action Disconnected;

        // -------------------------------------------------------
        // THUỘC TÍNH
        // -------------------------------------------------------

        /// <summary>Username sau khi đăng nhập thành công.</summary>
        public string Username { get; private set; }

        /// <summary>Tên phòng hiện tại đang ở.</summary>
        public string CurrentRoom { get; set; }

        /// <summary>Trạng thái kết nối.</summary>
        public bool IsConnected { get; private set; }

        // -------------------------------------------------------
        // THÀNH PHẦN KẾT NỐI
        // -------------------------------------------------------
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        /// <summary>Thread liên tục đọc tin từ Server.</summary>
        private Thread _receiveThread;

        /// <summary>Lock tránh hai Thread cùng ghi vào StreamWriter.</summary>
        private readonly object _writeLock = new object();

        // -------------------------------------------------------
        // KẾT NỐI TỚI SERVER
        // -------------------------------------------------------

        /// <summary>
        /// Kết nối TCP tới Server và khởi động Thread nhận tin.
        /// Trả về true nếu kết nối thành công.
        /// Ném Exception nếu thất bại (MainForm catch và hiển thị lỗi).
        /// </summary>
        public bool Connect(string host, int port, string username)
        {
            // Tạo TcpClient và kết nối tới Server
            _tcpClient = new TcpClient();
            _tcpClient.Connect(host, port); // Ném SocketException nếu không kết nối được

            // Lấy stream và tạo reader/writer
            _stream = _tcpClient.GetStream();
            _reader = new StreamReader(_stream, System.Text.Encoding.UTF8);
            _writer = new StreamWriter(_stream, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };

            Username = username;
            IsConnected = true;

            // Khởi động Thread nhận tin — chạy song song với UI Thread
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Name = "ReceiveThread";
            _receiveThread.Start();

            return true;
        }

        // -------------------------------------------------------
        // VÒNG LẶP NHẬN TIN (CHẠY TRONG THREAD RIÊNG)
        // -------------------------------------------------------

        /// <summary>
        /// Liên tục đọc từng dòng từ Server.
        /// Mỗi dòng nhận được → kích hoạt sự kiện MessageReceived.
        /// MainForm nhận sự kiện và cập nhật giao diện.
        /// </summary>
        private void ReceiveLoop()
        {
            try
            {
                string message;

                // ReadLine() block cho đến khi có dữ liệu mới hoặc kết nối đóng
                while (IsConnected && (message = _reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    // Kích hoạt sự kiện — MainForm sẽ xử lý trên UI Thread
                    MessageReceived?.Invoke(message);
                }
            }
            catch (IOException)
            {
                // Server ngắt kết nối hoặc mạng bị đứt
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Client] ReceiveLoop lỗi: {ex.Message}");
            }
            finally
            {
                // Dù lỗi gì vẫn thông báo ngắt kết nối
                IsConnected = false;
                Disconnected?.Invoke();
            }
        }

        // -------------------------------------------------------
        // GỬI LỆNH LÊN SERVER
        // -------------------------------------------------------

        /// <summary>
        /// Gửi một chuỗi lệnh lên Server.
        /// Dùng lock để tránh xung đột khi nhiều thành phần cùng gửi.
        /// </summary>
        public void SendCommand(string command)
        {
            if (!IsConnected) return;

            try
            {
                lock (_writeLock)
                {
                    _writer.WriteLine(command);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Client] Gửi lệnh lỗi: {ex.Message}");
                IsConnected = false;
            }
        }

        // -------------------------------------------------------
        // CÁC PHƯƠNG THỨC TIỆN ÍCH GỬI LỆNH CỤ THỂ
        // (giúp MainForm gọi đơn giản, không cần biết Protocol)
        // -------------------------------------------------------

        public void Login(string username)
        {
            SendCommand(Protocol.Build(Protocol.LOGIN, username));
        }

        public void RequestRoomList()
        {
            SendCommand(Protocol.LIST_ROOM);
        }

        public void CreateRoom(string roomName)
        {
            SendCommand(Protocol.Build(Protocol.CREATE_ROOM, roomName));
        }

        public void JoinRoom(string roomName)
        {
            SendCommand(Protocol.Build(Protocol.JOIN_ROOM, roomName));
        }

        public void LeaveRoom()
        {
            SendCommand(Protocol.LEAVE_ROOM);
            CurrentRoom = null;
        }

        public void SendRoomMessage(string roomName, string content)
        {
            SendCommand(Protocol.Build(Protocol.ROOM_MSG, roomName, content));
        }

        public void SendPrivateMessage(string targetUser, string content)
        {
            SendCommand(Protocol.Build(Protocol.PRIVATE, targetUser, content));
        }

        public void RequestOnlineList()
        {
            SendCommand(Protocol.ONLINE);
        }

        // -------------------------------------------------------
        // NGẮT KẾT NỐI
        // -------------------------------------------------------

        /// <summary>
        /// Đóng kết nối và giải phóng tài nguyên.
        /// Gọi khi người dùng đóng cửa sổ hoặc bấm Disconnect.
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            try { _reader?.Close(); } catch { }
            try { _writer?.Close(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }
    }
}
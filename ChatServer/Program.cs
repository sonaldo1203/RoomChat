// ============================================================
// Mô tả: Điểm khởi động của Server.
//        Đọc cấu hình cổng, tạo ChatServer và khởi động.
// ============================================================

using System;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Room Chat Server";

            Console.WriteLine("============================================");
            Console.WriteLine("       ROOM-BASED CHAT SERVER");
            Console.WriteLine("============================================");

            // Đọc cổng từ tham số dòng lệnh hoặc dùng mặc định 9999
            int port = 9999;
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }

            Console.WriteLine($"[Config] Cổng: {port}");

            // Tạo và khởi động server
            ChatServer server = new ChatServer(port);

            // Bắt sự kiện Ctrl+C để dừng server sạch
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Ngăn chương trình thoát ngay lập tức
                Console.WriteLine("\n[Server] Đang dừng server...");
                server.Stop();
                Environment.Exit(0);
            };

            // Start() chứa vòng lặp vô tận AcceptTcpClient()
            // nên Main thread sẽ block tại đây cho đến khi server dừng
            server.Start();
        }
    }
}
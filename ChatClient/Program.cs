// ============================================================
// File: Program.cs
// Dự án: ChatClient
// ============================================================

using System.Windows.Forms;

namespace ChatClient
{
    static class Program
    {
        [System.STAThread]
        static void Main()
        {
            // Chỉ rõ namespace để tránh nhầm lẫn với System.Net.Mime
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new MainForm());
        }
    }
}
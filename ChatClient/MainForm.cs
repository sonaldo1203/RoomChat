// ============================================================
// File: MainForm.cs
// Dự án: ChatClient (.NET 10 WinForms)
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class MainForm : Form
    {
        // ── Thành phần ──
        private ChatClient _client;
        private string _currentRoom = null;

        // ── Controls khu vực kết nối ──
        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtUsername;
        private Button btnConnect;
        private Button btnDisconnect;
        private Label lblStatus;

        // ── Controls khu vực phòng ──
        private ListBox lstRooms;
        private TextBox txtNewRoom;
        private Button btnCreateRoom;
        private Button btnJoinRoom;
        private Button btnLeaveRoom;
        private Button btnRefreshRooms;

        // ── Controls khu vực chat ──
        private RichTextBox rtbChat;
        private TextBox txtMessage;
        private Button btnSend;
        private Label lblCurrentRoom;

        // ── Controls khu vực online ──
        private ListBox lstOnline;
        private Button btnPrivate;
        private Button btnRefreshOnline;

        // -------------------------------------------------------
        // KHỞI TẠO
        // -------------------------------------------------------
        public MainForm()
        {
            // KHÔNG gọi InitializeComponent() — UI xây bằng code
            InitializeChatUI();
            SetupEventHandlers();
            SetConnectedState(false);
        }

        // -------------------------------------------------------
        // XÂY DỰNG GIAO DIỆN
        // -------------------------------------------------------
        private void InitializeChatUI()
        {
            this.Text = "Room Chat Application";
            this.Size = new Size(900, 650);
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);

            // ── PANEL TRÊN: kết nối ──
            Panel panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(44, 62, 80),
                Padding = new Padding(8)
            };

            Label lblHost = new Label
            {
                Text = "Host:",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 17)
            };
            txtHost = new TextBox { Text = "127.0.0.1", Location = new Point(50, 14), Width = 110 };

            Label lblPort = new Label
            {
                Text = "Port:",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(170, 17)
            };
            txtPort = new TextBox { Text = "9999", Location = new Point(205, 14), Width = 55 };

            Label lblUser = new Label
            {
                Text = "Username:",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(270, 17)
            };
            txtUsername = new TextBox { Location = new Point(340, 14), Width = 110 };

            btnConnect = new Button
            {
                Text = "Kết nối",
                Location = new Point(462, 13),
                Width = 75,
                Height = 26,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnDisconnect = new Button
            {
                Text = "Ngắt",
                Location = new Point(544, 13),
                Width = 60,
                Height = 26,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            lblStatus = new Label
            {
                Text = "● Chưa kết nối",
                ForeColor = Color.FromArgb(231, 76, 60),
                AutoSize = true,
                Location = new Point(616, 17),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            panelTop.Controls.AddRange(new Control[] {
                lblHost, txtHost, lblPort, txtPort,
                lblUser, txtUsername, btnConnect, btnDisconnect, lblStatus
            });

            // ── PANEL CHÍNH ──
            Panel panelMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };

            // ── CỘT TRÁI: phòng ──
            Panel panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.White,
                Padding = new Padding(5)
            };

            Label lblRoomTitle = new Label
            {
                Text = "Danh sách phòng",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            lstRooms = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f)
            };

            Panel panelRoomButtons = new Panel { Dock = DockStyle.Bottom, Height = 130 };

            txtNewRoom = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 26,
                PlaceholderText = "Tên phòng mới..."
            };

            btnCreateRoom = new Button
            {
                Text = "Tạo phòng",
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnJoinRoom = new Button
            {
                Text = "Vào phòng",
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnLeaveRoom = new Button
            {
                Text = "Rời phòng",
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnRefreshRooms = new Button
            {
                Text = "Làm mới",
                Dock = DockStyle.Top,
                Height = 26,
                FlatStyle = FlatStyle.Flat
            };

            panelRoomButtons.Controls.Add(btnRefreshRooms);
            panelRoomButtons.Controls.Add(btnLeaveRoom);
            panelRoomButtons.Controls.Add(btnJoinRoom);
            panelRoomButtons.Controls.Add(btnCreateRoom);
            panelRoomButtons.Controls.Add(txtNewRoom);

            panelLeft.Controls.Add(lstRooms);
            panelLeft.Controls.Add(panelRoomButtons);
            panelLeft.Controls.Add(lblRoomTitle);

            // ── CỘT PHẢI: online ──
            Panel panelRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 160,
                BackColor = Color.White,
                Padding = new Padding(5)
            };

            Label lblOnlineTitle = new Label
            {
                Text = "Đang online",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            lstOnline = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f)
            };

            Panel panelOnlineButtons = new Panel { Dock = DockStyle.Bottom, Height = 60 };

            btnPrivate = new Button
            {
                Text = "Nhắn tin riêng",
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnRefreshOnline = new Button
            {
                Text = "Làm mới",
                Dock = DockStyle.Top,
                Height = 26,
                FlatStyle = FlatStyle.Flat
            };

            panelOnlineButtons.Controls.Add(btnRefreshOnline);
            panelOnlineButtons.Controls.Add(btnPrivate);

            panelRight.Controls.Add(lstOnline);
            panelRight.Controls.Add(panelOnlineButtons);
            panelRight.Controls.Add(lblOnlineTitle);

            // ── GIỮA: chat ──
            Panel panelCenter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            lblCurrentRoom = new Label
            {
                Text = "Chưa vào phòng nào",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            rtbChat = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            Panel panelInput = new Panel { Dock = DockStyle.Bottom, Height = 40 };

            txtMessage = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Nhập tin nhắn..."
            };

            btnSend = new Button
            {
                Text = "Gửi",
                Dock = DockStyle.Right,
                Width = 70,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            panelInput.Controls.Add(txtMessage);
            panelInput.Controls.Add(btnSend);

            panelCenter.Controls.Add(rtbChat);
            panelCenter.Controls.Add(panelInput);
            panelCenter.Controls.Add(lblCurrentRoom);

            panelMain.Controls.Add(panelCenter);
            panelMain.Controls.Add(panelRight);
            panelMain.Controls.Add(panelLeft);

            this.Controls.Add(panelMain);
            this.Controls.Add(panelTop);
        }

        // -------------------------------------------------------
        // GÁN SỰ KIỆN
        // -------------------------------------------------------
        private void SetupEventHandlers()
        {
            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;
            btnCreateRoom.Click += BtnCreateRoom_Click;
            btnJoinRoom.Click += BtnJoinRoom_Click;
            btnLeaveRoom.Click += BtnLeaveRoom_Click;
            btnRefreshRooms.Click += BtnRefreshRooms_Click;
            btnSend.Click += BtnSend_Click;
            btnPrivate.Click += BtnPrivate_Click;
            btnRefreshOnline.Click += BtnRefreshOnline_Click;

            txtMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnSend_Click(s, e);
                }
            };

            this.FormClosing += (s, e) => { _client?.Disconnect(); };
        }

        // -------------------------------------------------------
        // XỬ LÝ SỰ KIỆN BUTTON
        // -------------------------------------------------------
        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string portText = txtPort.Text.Trim();
            string username = txtUsername.Text.Trim();

            if (string.IsNullOrEmpty(host) ||
                string.IsNullOrEmpty(portText) ||
                string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Host, Port và Username.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(portText, out int port))
            {
                MessageBox.Show("Port phải là số nguyên.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                _client = new ChatClient();
                _client.MessageReceived += OnMessageReceived;
                _client.Disconnected += OnDisconnected;
                _client.Connect(host, port, username);
                _client.Login(username);
                AppendSystemMessage($"Đang kết nối tới {host}:{port}...");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối: {ex.Message}",
                    "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _client = null;
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            _client?.LeaveRoom();
            _client?.Disconnect();
            SetConnectedState(false);
            _currentRoom = null;
            lblCurrentRoom.Text = "Chưa vào phòng nào";
            AppendSystemMessage("Đã ngắt kết nối.");
        }

        private void BtnCreateRoom_Click(object sender, EventArgs e)
        {
            string roomName = txtNewRoom.Text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                MessageBox.Show("Vui lòng nhập tên phòng.", "Thiếu tên phòng",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _client?.CreateRoom(roomName);
            txtNewRoom.Clear();
        }

        private void BtnJoinRoom_Click(object sender, EventArgs e)
        {
            if (lstRooms.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn phòng muốn vào.", "Chưa chọn phòng",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _client?.JoinRoom(lstRooms.SelectedItem.ToString());
        }

        private void BtnLeaveRoom_Click(object sender, EventArgs e)
        {
            if (_currentRoom == null)
            {
                MessageBox.Show("Bạn chưa ở trong phòng nào.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _client?.LeaveRoom();
            _currentRoom = null;
            lblCurrentRoom.Text = "Chưa vào phòng nào";
            rtbChat.Clear();
            AppendSystemMessage("Bạn đã rời phòng.");
        }

        private void BtnRefreshRooms_Click(object sender, EventArgs e)
        {
            _client?.RequestRoomList();
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            string content = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(content)) return;

            if (_currentRoom == null)
            {
                MessageBox.Show("Bạn chưa vào phòng nào.", "Chưa vào phòng",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _client?.SendRoomMessage(_currentRoom, content);
            txtMessage.Clear();
            txtMessage.Focus();
        }

        private void BtnPrivate_Click(object sender, EventArgs e)
        {
            if (lstOnline.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn người muốn nhắn tin riêng.",
                    "Chưa chọn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetUser = lstOnline.SelectedItem.ToString();

            if (targetUser == _client?.Username)
            {
                MessageBox.Show("Không thể nhắn tin cho chính mình.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Form nhỏ thay thế InputBox — không cần Microsoft.VisualBasic
            Form inputForm = new Form
            {
                Text = $"Nhắn tin riêng tới [{targetUser}]",
                Size = new Size(400, 140),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            TextBox inputBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Nhập nội dung tin nhắn..."
            };

            Button btnOk = new Button
            {
                Text = "Gửi",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            Button btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Bottom,
                Height = 32,
                FlatStyle = FlatStyle.Flat
            };

            inputForm.Controls.Add(inputBox);
            inputForm.Controls.Add(btnOk);
            inputForm.Controls.Add(btnCancel);
            inputForm.AcceptButton = btnOk;
            inputForm.CancelButton = btnCancel;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                string content = inputBox.Text.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    _client?.SendPrivateMessage(targetUser, content);
                    AppendPrivateMessage($"[Bạn → {targetUser}]: {content}", isSent: true);
                }
            }
        }

        private void BtnRefreshOnline_Click(object sender, EventArgs e)
        {
            _client?.RequestOnlineList();
        }

        // -------------------------------------------------------
        // XỬ LÝ TIN NHẮN TỪ SERVER
        // -------------------------------------------------------
        private void OnMessageReceived(string message)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<string>(ProcessServerMessage), message);
            else
                ProcessServerMessage(message);
        }

        private void ProcessServerMessage(string message)
        {
            string[] parts = Protocol.Parse(message, 10);
            if (parts.Length == 0) return;

            string command = parts[0].ToUpper().Trim();

            switch (command)
            {
                case Protocol.LOGIN_OK:
                    SetConnectedState(true);
                    AppendSystemMessage($"Đăng nhập thành công! Xin chào {_client.Username}.");
                    _client.RequestRoomList();
                    _client.RequestOnlineList();
                    break;

                case Protocol.LOGIN_FAIL:
                    string reason = parts.Length > 1 ? parts[1] : "Không xác định";
                    MessageBox.Show($"Đăng nhập thất bại: {reason}", "Lỗi đăng nhập",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _client.Disconnect();
                    _client = null;
                    SetConnectedState(false);
                    break;

                case Protocol.ROOM_LIST:
                    UpdateRoomList(parts.Length > 1 ? parts[1] : "");
                    break;

                case Protocol.ROOM_JOINED:
                    if (parts.Length >= 2)
                    {
                        _currentRoom = parts[1];
                        _client.CurrentRoom = _currentRoom;
                        lblCurrentRoom.Text = $"Phòng: {_currentRoom}";
                        rtbChat.Clear();
                        AppendSystemMessage($"Bạn đã vào phòng [{_currentRoom}].");
                        if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
                            AppendSystemMessage($"Thành viên: {parts[2]}");
                    }
                    break;

                case Protocol.MSG:
                    // MSG|nguoiGui|tenPhong|[timestamp]|noiDung
                    if (parts.Length >= 5)
                    {
                        string sender = parts[1];
                        string timestamp = parts[3];
                        string content = string.Join("|", parts, 4, parts.Length - 4);
                        AppendChatMessage(sender, content, timestamp);
                    }
                    break;

                case Protocol.PRIVATE_MSG:
                    // PRIVATE_MSG|nguoiGui|[timestamp]|noiDung
                    if (parts.Length >= 4)
                    {
                        string sender = parts[1];
                        string timestamp = parts[2];
                        string content = string.Join("|", parts, 3, parts.Length - 3);
                        AppendPrivateMessage($"[{timestamp}] {sender} → Bạn: {content}", isSent: false);
                    }
                    break;

                case Protocol.USER_JOINED:
                    if (parts.Length >= 2)
                    {
                        AppendSystemMessage($"▶ {parts[1]} đã tham gia.");
                        _client.RequestOnlineList();
                    }
                    break;

                case Protocol.USER_LEFT:
                    if (parts.Length >= 2)
                    {
                        AppendSystemMessage($"◀ {parts[1]} đã rời.");
                        _client.RequestOnlineList();
                    }
                    break;

                case Protocol.ONLINE_LIST:
                    UpdateOnlineList(parts.Length > 1 ? parts[1] : "");
                    break;

                case Protocol.ERROR:
                    string errorMsg = parts.Length > 1 ? parts[1] : "Lỗi không xác định";
                    AppendSystemMessage($"[Lỗi] {errorMsg}");
                    break;

                default:
                    AppendSystemMessage($"[?] Lệnh không rõ: {message}");
                    break;
            }
        }

        private void OnDisconnected()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OnDisconnected));
                return;
            }
            SetConnectedState(false);
            _currentRoom = null;
            lblCurrentRoom.Text = "Chưa vào phòng nào";
            AppendSystemMessage("Kết nối đã bị ngắt.");
        }

        // -------------------------------------------------------
        // CẬP NHẬT DANH SÁCH
        // -------------------------------------------------------
        private void UpdateRoomList(string data)
        {
            lstRooms.Items.Clear();
            if (string.IsNullOrEmpty(data)) return;
            foreach (string room in data.Split(','))
                if (!string.IsNullOrWhiteSpace(room))
                    lstRooms.Items.Add(room.Trim());
        }

        private void UpdateOnlineList(string data)
        {
            lstOnline.Items.Clear();
            if (string.IsNullOrEmpty(data)) return;
            foreach (string user in data.Split(','))
                if (!string.IsNullOrWhiteSpace(user))
                    lstOnline.Items.Add(user.Trim());
        }

        // -------------------------------------------------------
        // HIỂN THỊ TIN NHẮN
        // -------------------------------------------------------
        private void AppendChatMessage(string sender, string content, string timestamp)
        {
            rtbChat.SelectionColor = Color.Gray;
            rtbChat.AppendText($"{timestamp}\n");

            rtbChat.SelectionColor = Color.FromArgb(44, 62, 80);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            rtbChat.AppendText($"{sender}:\n");

            rtbChat.SelectionColor = Color.Black;
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"{content}\n\n");

            rtbChat.ScrollToCaret();
        }

        private void AppendPrivateMessage(string text, bool isSent)
        {
            rtbChat.SelectionColor = isSent
                ? Color.FromArgb(142, 68, 173)
                : Color.FromArgb(155, 89, 182);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Italic);
            rtbChat.AppendText($"[Riêng] {text}\n\n");
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.ScrollToCaret();
        }

        private void AppendSystemMessage(string text)
        {
            rtbChat.SelectionColor = Color.FromArgb(39, 174, 96);
            rtbChat.SelectionFont = new Font(rtbChat.Font.FontFamily, 8.5f, FontStyle.Italic);
            rtbChat.AppendText($"── {text} ──\n");
            rtbChat.SelectionFont = new Font(rtbChat.Font.FontFamily, rtbChat.Font.Size);
            rtbChat.ScrollToCaret();
        }

        // -------------------------------------------------------
        // TRẠNG THÁI KẾT NỐI
        // -------------------------------------------------------
        private void SetConnectedState(bool connected)
        {
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            txtHost.Enabled = !connected;
            txtPort.Enabled = !connected;
            txtUsername.Enabled = !connected;
            btnCreateRoom.Enabled = connected;
            btnJoinRoom.Enabled = connected;
            btnLeaveRoom.Enabled = connected;
            btnRefreshRooms.Enabled = connected;
            btnSend.Enabled = connected;
            btnPrivate.Enabled = connected;
            btnRefreshOnline.Enabled = connected;
            txtMessage.Enabled = connected;

            lblStatus.Text = connected ? "● Đã kết nối" : "● Chưa kết nối";
            lblStatus.ForeColor = connected
                ? Color.FromArgb(39, 174, 96)
                : Color.FromArgb(231, 76, 60);

            if (!connected)
            {
                lstRooms.Items.Clear();
                lstOnline.Items.Clear();
            }
        }
    }
}
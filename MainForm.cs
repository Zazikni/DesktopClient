using System;
using System.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopClient
{

    public partial class MainForm : Form
    {
        private TextBox messageTextBox;
        private Button sendButton;
        private ListBox messageListBox;
        private System.Windows.Forms.Timer _reconnectTimer;


        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private string _ip;
        private int _port;
        bool _connected = true;

        public MainForm()
        {
            InitializeComponent();
            LoadConfig();
            InitializeReconnectTimer();
            ConnectToServer();
        }
        private void InitializeComponent()
        {
            this.messageTextBox = new TextBox();
            this.sendButton = new Button();
            this.messageListBox = new ListBox();
            this.SuspendLayout();
            //
            // messageTextBox
            //
            this.messageTextBox.Anchor = ((AnchorStyles)(((AnchorStyles.Bottom | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.messageTextBox.Location = new Point(12, 350);
            this.messageTextBox.Name = "messageTextBox";
            this.messageTextBox.Size = new Size(710, 20);
            this.messageTextBox.TabIndex = 0;
            //
            // sendButton
            //
            this.sendButton.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
            this.sendButton.Location = new Point(728, 348);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new Size(88, 23);
            this.sendButton.TabIndex = 1;
            this.sendButton.Text = "Отправить";
            this.sendButton.Enabled = false;
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new EventHandler(this.sendButton_Click);
            //
            // messageListBox
            //
            this.messageListBox.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
            | AnchorStyles.Left)
            | AnchorStyles.Right)));
            this.messageListBox.FormattingEnabled = true;
            this.messageListBox.Location = new Point(12, 12);
            this.messageListBox.Name = "messageListBox";
            this.messageListBox.Size = new Size(804, 330);
            this.messageListBox.TabIndex = 2;
            //
            // MainForm
            //
            this.AcceptButton = sendButton; // Назначаем кнопку по умолчанию
            this.ClientSize = new Size(828, 382);
            this.MinimumSize = new Size(828, 382);
            this.Controls.Add(this.messageListBox);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.messageTextBox);
            this.Name = "MainForm";
            this.Text = "Chat Client";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadConfig()
        {
            _ip = ConfigurationManager.AppSettings["ServerIP"];
            _port = int.Parse(ConfigurationManager.AppSettings["ServerPort"]);
        }

        private void InitializeReconnectTimer()
        {
            _reconnectTimer = new System.Windows.Forms.Timer();
            _reconnectTimer.Interval = 5000;
            _reconnectTimer.Tick += ReconnectTimer_Tick;
        }

        private async void ConnectToServer()
        {
            try
            {
                
                
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_ip, _port);
                _stream = _tcpClient.GetStream();
                messageTextBox.Enabled = true;
                sendButton.Enabled = true;
                _reconnectTimer.Stop();
                if (!_connected)
                {
                    messageListBox.Items.Add($"Подключение установлено!");
                }
                _connected = true;
                Task.Run(() => ReceiveMessagesAsync());
            }
            catch (Exception ex)
            {
                if (_connected)
                {
                    messageListBox.Items.Add($"Ошибка при подключении к серверу: {ex.Message}");
                    messageListBox.Items.Add($"Производится попытка подключения....");
                    _connected = false;
                }
                messageTextBox.Enabled = false;
                sendButton.Enabled = false;
                _reconnectTimer.Start();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Invoke(new Action(() =>
                    {
                        messageListBox.Items.Add(message);
                    }));
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    messageListBox.Items.Add("Потеряно соеденение с сервером.");
                }));
                Invoke(new Action(() =>
                {
                    Disconnect();
                }));
                
            }
        }

        private async Task SendMessageAsync(string message)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    messageListBox.Items.Add($"Не удалось отправить сообщение.");
                }));
                Invoke(new Action(() =>
                {
                    Disconnect();
                }));
               
                
            }
        }

        private void Disconnect()
        {
            _stream?.Close();
            _tcpClient?.Close();
            messageTextBox.Enabled = false;
            sendButton.Enabled = false;
            _reconnectTimer.Start();
        }

        private async void sendButton_Click(object sender, EventArgs e)
        {
            string message = messageTextBox.Text;
            if (message == String.Empty)
            {
                return;
            }
            await SendMessageAsync(message);
            messageListBox.Items.Add($"ВЫ: {message}");
            messageTextBox.Clear();
            messageTextBox.Focus(); // Устанавливаем фокус на поле ввода
        }

        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            ConnectToServer();
        }


    }
}

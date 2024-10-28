using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class CustomContextMenuStrip : ContextMenuStrip {
    private static readonly System.Drawing.Color MenuBackColor = ColorTranslator.FromHtml("#181819");
    private static readonly System.Drawing.Color MenuForeColor = System.Drawing.Color.White;
    private static readonly System.Drawing.Color ItemHoverColor = ColorTranslator.FromHtml("#2A3A75");
    private static readonly System.Drawing.Color ItemPressColor = ColorTranslator.FromHtml("#101010");
    private static readonly System.Drawing.Color SeparatorColor = ColorTranslator.FromHtml("#28282A");
    private const int MenuItemPadding = 5;
    private const int MenuCornerRadius = 4;
    private static readonly System.Drawing.Font MenuItemFont = new("Segoe UI", 9F);

    public CustomContextMenuStrip() {
        Renderer = new CustomContextMenuRenderer();
        BackColor = MenuBackColor;
        ForeColor = MenuForeColor;
        ShowImageMargin = false;
        Padding = new Padding(2);
        Font = MenuItemFont;
        
        Opening += (s, e) => {
            foreach (ToolStripItem item in Items) {
                if (item is ToolStripMenuItem menuItem) {
                    menuItem.BackColor = MenuBackColor;
                    menuItem.ForeColor = MenuForeColor;
                    menuItem.Padding = new Padding(MenuItemPadding, 2, MenuItemPadding, 4);
                    menuItem.Font = MenuItemFont;
                }
            }
        };
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            MenuItemFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    private class CustomContextMenuRenderer : ToolStripProfessionalRenderer {
        public CustomContextMenuRenderer() : base(new CustomColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            var g = e.Graphics;
            var bounds = e.AffectedBounds;
            using var path = CreateRoundedRectanglePath(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1, MenuCornerRadius);
            using var pen = new Pen(SeparatorColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawPath(pen, path);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            var g = e.Graphics;
            var menuItem = e.Item as ToolStripMenuItem;
            var bounds = e.Item.ContentRectangle;

            if (menuItem != null) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                if (menuItem.Selected) {
                    using var path = CreateRoundedRectanglePath(bounds.X + 1, bounds.Y, bounds.Width - 2, bounds.Height, MenuCornerRadius - 2);
                    using var brush = new SolidBrush(ItemHoverColor);
                    g.FillPath(brush, path);
                } else if (menuItem.Pressed) {
                    using var path = CreateRoundedRectanglePath(bounds.X + 1, bounds.Y, bounds.Width - 2, bounds.Height, MenuCornerRadius - 2);
                    using var brush = new SolidBrush(ItemPressColor);
                    g.FillPath(brush, path);
                } else {
                    using var brush = new SolidBrush(MenuBackColor);
                    g.FillRectangle(brush, bounds);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            var g = e.Graphics;
            var bounds = e.Item.ContentRectangle;
            var y = bounds.Height / 2;
            using var pen = new Pen(SeparatorColor);
            g.DrawLine(pen, bounds.Left + MenuItemPadding, y, bounds.Right - MenuItemPadding, y);
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(float x, float y, float width, float height, float radius) {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            
            // Top left arc
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            // Top right arc
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            // Bottom right arc
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            // Bottom left arc
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }

    private class CustomColorTable : ProfessionalColorTable {
        public override System.Drawing.Color MenuBorder => SeparatorColor;
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.Transparent;
        public override System.Drawing.Color MenuItemSelected => ItemHoverColor;
        public override System.Drawing.Color MenuItemSelectedGradientBegin => ItemHoverColor;
        public override System.Drawing.Color MenuItemSelectedGradientEnd => ItemHoverColor;
        public override System.Drawing.Color MenuItemPressedGradientBegin => ItemPressColor;
        public override System.Drawing.Color MenuItemPressedGradientEnd => ItemPressColor;
        public override System.Drawing.Color ToolStripDropDownBackground => MenuBackColor;
        public override System.Drawing.Color ImageMarginGradientBegin => MenuBackColor;
        public override System.Drawing.Color ImageMarginGradientMiddle => MenuBackColor;
        public override System.Drawing.Color ImageMarginGradientEnd => MenuBackColor;
    }
}

public class TrayManager : MonoBehaviour {
    private NotifyIcon trayIcon;
    private CustomContextMenuStrip trayMenu;
    private NetworkSettingsForm settingsForm;
    private SynchronizationContext mainThread;
    private VRBro vrBro;
    private Thread trayThread;
    private bool isRunning = true;
    public bool lastConnectionState = false;
    private readonly CancellationTokenSource cts = new();
    private float pingInterval = 2f;
    private float lastPingTime;

    private async void Update() {
        if (trayIcon == null || vrBro == null || vrBro._net == null) return;
        if (Time.time - lastPingTime >= pingInterval) {
            lastPingTime = Time.time;
            await UpdateConnectionState();
        }
    }

    public async Task UpdateConnectionState() {
        if (trayIcon == null || vrBro == null || vrBro._net == null) return;
        bool isConnected = await vrBro._net.CheckConnected();
        if (isConnected != lastConnectionState) {
            lastConnectionState = isConnected;
            await Task.Run(() => {
                if (trayIcon != null) trayIcon.Text = isConnected ? "VRBro - Connected" : "VRBro - Lost Connection";
            });
        }
    }

    private void Awake() {
        mainThread = SynchronizationContext.Current;
        vrBro = FindFirstObjectByType<VRBro>();
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon() {
        if (!System.Windows.Forms.Application.MessageLoop) {
            trayThread = new Thread(() => {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                CreateTrayIcon();
                while (isRunning) System.Windows.Forms.Application.DoEvents();
            });
            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.IsBackground = true;
            trayThread.Start();
        }
    }

    private void CreateTrayIcon() {
        trayMenu = new CustomContextMenuStrip();
        
        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += OnOpenSettings;
        trayMenu.Items.Add(settingsItem);
        
        trayMenu.Items.Add(new ToolStripSeparator());
        
        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += OnQuit;
        trayMenu.Items.Add(quitItem);

        trayIcon = new NotifyIcon {
            Text = "VRBro - No Server",
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        trayIcon.DoubleClick += (s, e) => OnOpenSettings(s, e);

        string iconPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Textures", "VRBro_logo-32x32.ico");
        try {
            trayIcon.Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
        } catch (Exception ex) {
            Debug.LogError($"Failed to load tray icon: {ex.Message}");
            trayIcon.Icon = SystemIcons.Application;
        }
    }

    private void OnOpenSettings(object sender, EventArgs e) {
        if (settingsForm == null || settingsForm.IsDisposed)
            settingsForm = new NetworkSettingsForm(vrBro, this);
        
        if (!settingsForm.Visible) settingsForm.Show();
        else settingsForm.BringToFront();
    }

    private void OnQuit(object sender, EventArgs e) => ExecuteOnMainThread(() => {
        isRunning = false;
        cts.Cancel();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            UnityEngine.Application.Quit();
        #endif
    });

    private void ExecuteOnMainThread(Action action) => mainThread?.Post(_ => action(), null);

    private void OnDestroy() {
        isRunning = false;
        cts.Cancel();
        cts.Dispose();
        
        if (trayIcon != null) {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
        
        settingsForm?.Dispose();
        trayThread?.Join(100);
    }
}

public class NetworkSettingsForm : Form {
    private TextBox ipAddressBox;
    private NumericUpDown portNumeric;
    private RoundedButton saveButton;
    private RoundedButton cancelButton;
    private readonly VRBro vrBro;
    private readonly TrayManager trayManager;
    private readonly Settings settings;
    private bool isConnecting;
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    public NetworkSettingsForm(VRBro vrBro, TrayManager trayManager) {
        this.vrBro = vrBro;
        this.trayManager = trayManager;
        this.settings = Settings.Load();
        InitializeComponent();
        LoadCurrentSettings();
    }

    private void InitializeComponent() {
        BackColor = ColorTranslator.FromHtml("#181819");
        ForeColor = System.Drawing.Color.White;
        Text = "VRBro Network Settings";
        Size = new Size(320, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Label ipLabel = new() {
            Text = "Server IP:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        ipAddressBox = new TextBox {
            Location = new Point(20, 40),
            Size = new Size(260, 20),
            BackColor = ColorTranslator.FromHtml("#36393F"),
            ForeColor = System.Drawing.Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        ipAddressBox.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                if (!isConnecting) portNumeric.Focus();
            }
        };

        Label portLabel = new() {
            Text = "Port:",
            Location = new Point(20, 70),
            AutoSize = true
        };

        portNumeric = new NumericUpDown {
            Location = new Point(20, 90),
            Size = new Size(260, 20),
            Minimum = 1,
            Maximum = 65535,
            BackColor = ColorTranslator.FromHtml("#36393F"),
            ForeColor = System.Drawing.Color.White
        };
        portNumeric.KeyDown += async (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                if (!isConnecting) await AttemptConnection();
            }
        };
        portNumeric.Controls[0].BackColor = ColorTranslator.FromHtml("#36393F");
        portNumeric.Controls[0].ForeColor = System.Drawing.Color.White;

        saveButton = new RoundedButton {
            Text = "Save",
            Location = new Point(95, 120),
            Size = new Size(85, 30),
            BackColor = ColorTranslator.FromHtml("#162458"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = System.Drawing.Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.Click += async (s, e) => await AttemptConnection();

        cancelButton = new RoundedButton {
            Text = "Cancel",
            Location = new Point(195, 120),
            Size = new Size(85, 30),
            BackColor = ColorTranslator.FromHtml("#28282A"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = System.Drawing.Color.White,
            FlatStyle = FlatStyle.Flat
        };
        cancelButton.Click += (s, e) => Close();

        Controls.AddRange(new Control[] { ipLabel, ipAddressBox, portLabel, portNumeric, saveButton, cancelButton });
    }

    private void LoadCurrentSettings() {
        if (vrBro != null && vrBro._net != null) {
            ipAddressBox.Text = settings.ServerAddress;
            portNumeric.Value = settings.ServerPort;
        }
    }

    private async Task AttemptConnection() {
        if (string.IsNullOrWhiteSpace(ipAddressBox.Text)) {
            ShowCustomError("Please enter a valid IP address.");
            return;
        }

        try {
            await connectionLock.WaitAsync();
            if (isConnecting) return;
            isConnecting = true;

            saveButton.Enabled = false;
            cancelButton.Enabled = false;
            ipAddressBox.Enabled = false;
            portNumeric.Enabled = false;

            if (vrBro != null && vrBro._net != null) {
                string newAddress = ipAddressBox.Text;
                int newPort = (int)portNumeric.Value;

                vrBro._net.serverAddr = newAddress;
                vrBro._net.serverPort = newPort;
                vrBro._net.Close();

                await Task.Delay(1000);
                bool isConnected = await vrBro._net.CheckConnected();
                
                if (!isConnected) {
                    ShowCustomError("Failed to connect to server. Please check your settings.");
                    return;
                }

                settings.ServerAddress = newAddress;
                settings.ServerPort = newPort;
                settings.Save();

                if (trayManager != null) await trayManager.UpdateConnectionState();
                Hide();
            }
        } finally {
            saveButton.Enabled = true;
            cancelButton.Enabled = true;
            ipAddressBox.Enabled = true;
            portNumeric.Enabled = true;
            isConnecting = false;
            connectionLock.Release();
        }
    }

    private void ShowCustomError(string message) {
        using var customError = new Form {
            Text = "Connection Error",
            Size = new Size(300, 150),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ColorTranslator.FromHtml("#181819"),
            ForeColor = System.Drawing.Color.White,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        Label messageLabel = new() {
            Text = message,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        RoundedButton okButton = new() {
            Text = "OK",
            Size = new Size(80, 30),
            BackColor = ColorTranslator.FromHtml("#162458"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = System.Drawing.Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okButton.Click += (s, e) => customError.Close();

        Panel buttonPanel = new() {
            Height = 50,
            Dock = DockStyle.Bottom,
            BackColor = ColorTranslator.FromHtml("#181819")
        };

        customError.Load += (s, e) => {
            okButton.Location = new Point(
                (buttonPanel.ClientSize.Width - okButton.Width) / 2,
                (buttonPanel.ClientSize.Height - okButton.Height) / 2
            );
        };

        buttonPanel.Controls.Add(okButton);
        customError.Controls.AddRange(new Control[] { messageLabel, buttonPanel });
        customError.ShowDialog(this);
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        if (e.CloseReason == CloseReason.UserClosing) {
            e.Cancel = true;
            ipAddressBox.Text = settings.ServerAddress;
            portNumeric.Value = settings.ServerPort;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) connectionLock?.Dispose();
        base.Dispose(disposing);
    }
}

public class RoundedButton : Button
{
    private const int Radius = 4;
    private System.Drawing.Color normalColor;
    private System.Drawing.Color hoverColor;
    private System.Drawing.Color pressColor;
    private bool isHovering;
    private bool isPressed;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
        FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Radius, Radius));
        
        MouseEnter += OnMouseEnterButton;
        MouseLeave += OnMouseLeaveButton;
        MouseDown += OnMouseDownButton;
        MouseUp += OnMouseUpButton;
    }

    public System.Drawing.Color HoverColor
    {
        get => hoverColor;
        set {
            hoverColor = value;
            UpdateButtonColor();
        }
    }

    public System.Drawing.Color PressColor
    {
        get => pressColor;
        set {
            pressColor = value;
            UpdateButtonColor();
        }
    }

    public new System.Drawing.Color BackColor
    {
        get => base.BackColor;
        set {
            normalColor = value;
            UpdateButtonColor();
        }
    }
    
    private void UpdateButtonColor()
    {
        if (isPressed && !pressColor.IsEmpty)
            base.BackColor = pressColor;
        else if (isHovering && !hoverColor.IsEmpty)
            base.BackColor = hoverColor;
        else
            base.BackColor = normalColor;
    }
    
    private void OnMouseEnterButton(object sender, EventArgs e)
    {
        isHovering = true;
        UpdateButtonColor();
    }

    private void OnMouseLeaveButton(object sender, EventArgs e)
    {
        isHovering = false;
        isPressed = false;
        UpdateButtonColor();
    }

    private void OnMouseDownButton(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isPressed = true;
            UpdateButtonColor();
        }
    }

    private void OnMouseUpButton(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            isPressed = false;
            UpdateButtonColor();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Radius, Radius));
    }

    [System.Runtime.InteropServices.DllImport("Gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, 
        int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
}

public static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this System.Drawing.Graphics graphics, Pen pen, float x, float y, float width, float height, float radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        
        // Top left arc
        path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
        // Top right arc
        path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
        // Bottom right arc
        path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
        // Bottom left arc
        path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();

        graphics.DrawPath(pen, path);
    }
}
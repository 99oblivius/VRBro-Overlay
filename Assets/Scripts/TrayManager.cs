using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UnityEngine;

using Application = System.Windows.Forms.Application;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;

public class TrayManager : MonoBehaviour {
    #region Serialized Fields
    [SerializeField] private VRBro vrBro;
    [SerializeField] private Controller streamingController;
    #endregion

    #region Private Fields
    private readonly CancellationTokenSource cts = new();
    private NotifyIcon trayIcon;
    private CustomContextMenuStrip trayMenu;
    private NetworkSettingsForm settingsForm;
    private SynchronizationContext mainThread;
    private Thread trayThread;
    private bool isRunning = true;
    #endregion

    #region Properties
    private string CurrentAddress => Settings.Instance.ServerAddress;
    private int CurrentPort => Settings.Instance.ServerPort;
    #endregion

    #region Unity Lifecycle
    private void Awake() {
        mainThread = SynchronizationContext.Current;
        Settings.Instance.OnSettingsChanged += OnSettingsChanged;
        streamingController.OnConnectionStateChanged += HandleConnectionStateChanged;
        InitializeTrayIcon();
    }

    private void OnDestroy() {
        Cleanup();
    }
    #endregion

    #region Initialization
    private void InitializeTrayIcon() {
        if (!Application.MessageLoop) {
            trayThread = new Thread(() => {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                CreateTrayIcon();
                while (isRunning) Application.DoEvents();
            });
            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.IsBackground = true;
            trayThread.Start();
        }
    }

    private void CreateTrayIcon() {
        trayMenu = new CustomContextMenuStrip();
        var settingsItem = new ToolStripMenuItem("Settings");
        var quitItem = new ToolStripMenuItem("Quit");
        
        settingsItem.Click += OnOpenSettings;
        quitItem.Click += OnQuit;
        
        trayMenu.Items.AddRange(new ToolStripItem[] {
            settingsItem,
            new ToolStripSeparator(),
            quitItem
        });

        trayIcon = new NotifyIcon {
            Text = "VRBro - Disconnected",
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        trayIcon.DoubleClick += OnOpenSettings;
        LoadTrayIcon();
    }

    private void LoadTrayIcon() {
        string iconPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Textures", "VRBro_logo-32x32.ico");
        try {
            trayIcon.Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
        } catch (Exception ex) {
            Debug.LogError($"Failed to load tray icon: {ex.Message}");
            trayIcon.Icon = SystemIcons.Application;
        }
    }
    #endregion

    #region Event Handlers
    private void HandleConnectionStateChanged(bool isConnected) {
        if (trayIcon == null) return;
        trayIcon.Text = isConnected ? "VRBro - Connected" : "VRBro - Disconnected";
    }

    private void OnSettingsChanged() {
        if (vrBro?._net != null) {
            vrBro._net.serverAddr = Settings.Instance.ServerAddress;
            vrBro._net.serverPort = Settings.Instance.ServerPort;
        }
    }

    private void OnOpenSettings(object sender, EventArgs e) {
        try {
            if (settingsForm == null || settingsForm.IsDisposed) {
                settingsForm = new NetworkSettingsForm(CurrentAddress, CurrentPort, OnSaveConnection);
            }
            
            if (!settingsForm.Visible) {
                ExecuteOnMainThread(() => {
                    settingsForm.UpdateCurrentValues(CurrentAddress, CurrentPort);
                    settingsForm.Show();
                });
            } else {
                settingsForm.BringToFront();
            }
        } catch (Exception ex) {
            Debug.LogError($"Error opening settings: {ex}");
        }
    }

    private void OnQuit(object sender, EventArgs e) {
        ExecuteOnMainThread(() => {
            isRunning = false;
            cts.Cancel();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        });
    }
    #endregion

    #region Connection Management
    private async Task<bool> OnSaveConnection(string address, int port) {
        if (vrBro?._net == null) return false;

        try {
            string originalAddress = vrBro._net.serverAddr;
            int originalPort = vrBro._net.serverPort;

            vrBro._net.serverAddr = address;
            vrBro._net.serverPort = port;
            vrBro._net.Close();

            var isConnected = await vrBro._net.CheckConnected();
            if (!isConnected) {
                vrBro._net.serverAddr = originalAddress;
                vrBro._net.serverPort = originalPort;
                vrBro._net.Close();
            }
            
            return isConnected;
        } catch {
            return false;
        }
    }
    #endregion

    #region Utility Methods
    private void ExecuteOnMainThread(Action action) {
        mainThread?.Post(_ => action(), null);
    }

    private void Cleanup() {
        isRunning = false;
        cts.Cancel();
        cts.Dispose();
        trayIcon?.Dispose();
        settingsForm?.Dispose();
        trayThread?.Join(100);
        Settings.Instance.OnSettingsChanged -= OnSettingsChanged;
        streamingController.OnConnectionStateChanged -= HandleConnectionStateChanged;
    }
    #endregion
}

public class CustomContextMenuStrip : ContextMenuStrip {
    #region Constants
    private static readonly Color MenuBackColor = ColorTranslator.FromHtml("#181819");
    private static readonly Color MenuForeColor = Color.White;
    private static readonly Color ItemHoverColor = ColorTranslator.FromHtml("#2A3A75");
    private static readonly Color ItemPressColor = ColorTranslator.FromHtml("#101010");
    private static readonly Color SeparatorColor = ColorTranslator.FromHtml("#28282A");
    private const int MenuItemPadding = 5;
    private const int MenuCornerRadius = 4;
    private static readonly Font MenuItemFont = new("Segoe UI", 9F);
    #endregion

    #region Initialization
    public CustomContextMenuStrip() {
        InitializeMenuStyle();
        Opening += OnMenuOpening;
    }

    private void InitializeMenuStyle() {
        Renderer = new CustomContextMenuRenderer();
        BackColor = MenuBackColor;
        ForeColor = MenuForeColor;
        ShowImageMargin = false;
        Padding = new Padding(6);
        Font = MenuItemFont;
    }
    #endregion

    #region Event Handlers
    private void OnMenuOpening(object sender, System.ComponentModel.CancelEventArgs e) {
        foreach (ToolStripItem item in Items) {
            if (item is ToolStripMenuItem menuItem) {
                menuItem.BackColor = MenuBackColor;
                menuItem.ForeColor = MenuForeColor;
                menuItem.Padding = new Padding(MenuItemPadding, 2, MenuItemPadding, 4);
                menuItem.Font = MenuItemFont;
            }
        }
    }
    #endregion

    #region Cleanup
    protected override void Dispose(bool disposing) {
        if (disposing) {
            MenuItemFont?.Dispose();
        }
        base.Dispose(disposing);
    }
    #endregion

    #region Custom Renderer
    private class CustomContextMenuRenderer : ToolStripProfessionalRenderer {
        public CustomContextMenuRenderer() : base(new CustomColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            using var path = CreateRoundedRectanglePath(
                e.AffectedBounds.X, 
                e.AffectedBounds.Y, 
                e.AffectedBounds.Width - 1, 
                e.AffectedBounds.Height - 1, 
                MenuCornerRadius
            );
            using var pen = new Pen(SeparatorColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            if (e.Item is not ToolStripMenuItem menuItem) return;
            
            var g = e.Graphics;
            var bounds = e.Item.ContentRectangle;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var brush = new SolidBrush(GetItemBackgroundColor(menuItem));
            if (menuItem.Selected || menuItem.Pressed) {
                using var path = CreateRoundedRectanglePath(
                    bounds.X + 1, 
                    bounds.Y, 
                    bounds.Width - 2, 
                    bounds.Height, 
                    MenuCornerRadius - 2
                );
                g.FillPath(brush, path);
            } else {
                g.FillRectangle(brush, bounds);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            var g = e.Graphics;
            var bounds = e.Item.ContentRectangle;
            var y = bounds.Height / 2;
            using var pen = new Pen(SeparatorColor);
            g.DrawLine(pen, bounds.Left + MenuItemPadding, y, bounds.Right - MenuItemPadding, y);
        }

        private Color GetItemBackgroundColor(ToolStripMenuItem menuItem) {
            if (menuItem.Pressed) return ItemPressColor;
            if (menuItem.Selected) return ItemHoverColor;
            return MenuBackColor;
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(
            float x, float y, float width, float height, float radius) {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private class CustomColorTable : ProfessionalColorTable {
        public override Color MenuBorder => SeparatorColor;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => ItemHoverColor;
        public override Color MenuItemSelectedGradientBegin => ItemHoverColor;
        public override Color MenuItemSelectedGradientEnd => ItemHoverColor;
        public override Color MenuItemPressedGradientBegin => ItemPressColor;
        public override Color MenuItemPressedGradientEnd => ItemPressColor;
        public override Color ToolStripDropDownBackground => MenuBackColor;
        public override Color ImageMarginGradientBegin => MenuBackColor;
        public override Color ImageMarginGradientMiddle => MenuBackColor;
        public override Color ImageMarginGradientEnd => MenuBackColor;
    }
    #endregion
}

public class NetworkSettingsForm : Form {
    #region Fields
    private readonly TextBox ipAddressBox;
    private readonly NumericUpDown portNumeric;
    private readonly RoundedButton saveButton;
    private readonly RoundedButton cancelButton;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly Func<string, int, Task<bool>> onConnectionAttempt;
    private bool isConnecting;
    #endregion

    #region Constructor
    public NetworkSettingsForm(string currentAddress, int currentPort, Func<string, int, Task<bool>> onConnectionAttempt) {
        this.onConnectionAttempt = onConnectionAttempt;
        InitializeFormStyle();
        
        ipAddressBox = CreateIPAddressBox(currentAddress);
        portNumeric = CreatePortNumeric(currentPort);
        saveButton = CreateSaveButton();
        cancelButton = CreateCancelButton();
        
        var ipLabel = CreateLabel("Server IP:", new Point(20, 20));
        var portLabel = CreateLabel("Port:", new Point(20, 70));
        
        Controls.AddRange(new Control[] { 
            ipLabel, ipAddressBox, 
            portLabel, portNumeric,
            saveButton, cancelButton 
        });
    }
    #endregion

    #region Initialization
    private void InitializeFormStyle() {
        BackColor = ColorTranslator.FromHtml("#181819");
        ForeColor = Color.White;
        Text = "VRBro Network Settings";
        Size = new Size(320, 200);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
    }

    private TextBox CreateIPAddressBox(string currentAddress) {
        var box = new TextBox {
            Location = new Point(20, 40),
            Size = new Size(260, 20),
            Text = currentAddress,
            BackColor = ColorTranslator.FromHtml("#36393F"),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        box.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                if (!isConnecting) portNumeric.Focus();
            }
        };
        return box;
    }

    private NumericUpDown CreatePortNumeric(int currentPort) {
        var numeric = new NumericUpDown {
            Location = new Point(20, 90),
            Size = new Size(260, 20),
            Minimum = 1,
            Maximum = 65535,
            Value = currentPort,
            BackColor = ColorTranslator.FromHtml("#36393F"),
            ForeColor = Color.White
        };
        numeric.KeyDown += async (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;
                if (!isConnecting) await AttemptConnection();
            }
        };
        numeric.Controls[0].BackColor = ColorTranslator.FromHtml("#36393F");
        numeric.Controls[0].ForeColor = Color.White;
        return numeric;
    }

    private Label CreateLabel(string text, Point location) {
        return new Label {
            Text = text,
            Location = location,
            AutoSize = true
        };
    }

    private RoundedButton CreateSaveButton() {
        var button = new RoundedButton {
            Text = "Save",
            Location = new Point(95, 120),
            Size = new Size(85, 30),
            BackColor = ColorTranslator.FromHtml("#162458"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.Click += async (s, e) => await AttemptConnection();
        return button;
    }

    private RoundedButton CreateCancelButton() {
        var button = new RoundedButton {
            Text = "Cancel",
            Location = new Point(195, 120),
            Size = new Size(85, 30),
            BackColor = ColorTranslator.FromHtml("#28282A"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.Click += (s, e) => Close();
        return button;
    }
    #endregion

    #region Public Methods
    public void UpdateCurrentValues(string address, int port) {
        if (InvokeRequired) {
            Invoke(new Action(() => UpdateCurrentValues(address, port)));
            return;
        }
        ipAddressBox.Text = address;
        portNumeric.Value = port;
    }
    #endregion

    #region Connection Management
    private async Task AttemptConnection() {
        if (!ValidateInput() || isConnecting) return;
        
        try {
            isConnecting = true;
            EnableControls(false);
            saveButton.Text = "Connecting...";

            var result = await onConnectionAttempt(ipAddressBox.Text, (int)portNumeric.Value);

            if (result) {
                Hide();
            } else {
                ShowCustomError("Failed connection to server.\nCheck your network settings.");
            }
        } finally {
            isConnecting = false;
            EnableControls(true);
            saveButton.Text = "Save";
        }
    }

    private bool ValidateInput() {
        if (string.IsNullOrWhiteSpace(ipAddressBox.Text)) {
            ShowCustomError("Please enter a valid IP address.");
            return false;
        }

        if (!System.Net.IPAddress.TryParse(ipAddressBox.Text, out _) && 
            ipAddressBox.Text.ToLower() != "localhost") {
            ShowCustomError("Please enter a valid IP address format.");
            return false;
        }

        return true;
    }
    #endregion

    #region UI Helpers
    private void ShowCustomError(string message) {
        using var customError = new Form {
            Text = "Connection Error",
            Size = new Size(300, 150),
            StartPosition = FormStartPosition.CenterParent,
            BackColor = ColorTranslator.FromHtml("#181819"),
            ForeColor = Color.White,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var messageLabel = new Label {
            Text = message,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var okButton = new RoundedButton {
            Text = "OK",
            Size = new Size(80, 30),
            BackColor = ColorTranslator.FromHtml("#162458"),
            HoverColor = ColorTranslator.FromHtml("#2A3A75"),
            PressColor = ColorTranslator.FromHtml("#101010"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okButton.Click += (s, e) => customError.Close();

        var buttonPanel = new Panel {
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

    private void EnableControls(bool enabled) {
        if (InvokeRequired) {
            Invoke(new Action(() => EnableControls(enabled)));
            return;
        }
        saveButton.Enabled = enabled;
        cancelButton.Enabled = enabled;
        ipAddressBox.Enabled = enabled;
        portNumeric.Enabled = enabled;
    }
    #endregion

    #region Cleanup
    protected override void Dispose(bool disposing) {
        if (disposing) {
            connectionLock?.Dispose();
            ipAddressBox?.Dispose();
            portNumeric?.Dispose();
            saveButton?.Dispose();
            cancelButton?.Dispose();
        }
        base.Dispose(disposing);
    }
    #endregion
}

public class RoundedButton : Button {
    #region Fields
    private const int Radius = 4;
    private Color normalColor;
    private Color hoverColor;
    private Color pressColor;
    private bool isHovering;
    private bool isPressed;
    #endregion

    #region Properties
    public Color HoverColor {
        get => hoverColor;
        set { hoverColor = value; UpdateButtonColor(); }
    }

    public Color PressColor {
        get => pressColor;
        set { pressColor = value; UpdateButtonColor(); }
    }

    public new Color BackColor {
        get => base.BackColor;
        set { normalColor = value; UpdateButtonColor(); }
    }
    #endregion

    #region Initialization
    public RoundedButton() {
        InitializeStyle();
        SetupEventHandlers();
    }

    private void InitializeStyle() {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
    }

    private void SetupEventHandlers() {
        MouseEnter += (s, e) => { isHovering = true; UpdateButtonColor(); };
        MouseLeave += (s, e) => { isHovering = false; isPressed = false; UpdateButtonColor(); };
        MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isPressed = true; UpdateButtonColor(); } };
        MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) { isPressed = false; UpdateButtonColor(); } };
    }
    #endregion

    #region Color Management
    private void UpdateButtonColor() {
        base.BackColor = isPressed && !pressColor.IsEmpty ? pressColor :
                        isHovering && !hoverColor.IsEmpty ? hoverColor :
                        normalColor;
    }
    #endregion
}
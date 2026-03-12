using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;

[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.Reflection.AssemblyTitle("SafeZip")]
[assembly: System.Reflection.AssemblyDescription("Create and verify ZIP archives")]
[assembly: System.Reflection.AssemblyCompany("Ioannis Karras")]
[assembly: System.Reflection.AssemblyProduct("SafeZip")]
[assembly: System.Reflection.AssemblyCopyright("© 2026 Ioannis Karras")]
[assembly: System.Reflection.AssemblyVersion("0.5.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.5.0.0")]

namespace SafeZip
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }

    // ── Polished button (flat, rectangular — no GDI corner artifacts) ─────────
    class Btn : Button
    {
        public bool Primary { get; set; }
        Color _n, _h, _p, _border;
        bool _over, _down;

        public Btn(Color normal, Color hover, Color press, Color border)
        {
            _n = normal; _h = hover; _p = press; _border = border;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize   = 1;
            FlatAppearance.BorderColor  = border;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            Cursor   = Cursors.Hand;
            TabStop  = false;
            UseVisualStyleBackColor = false;
        }

        public void Retheme(Color normal, Color hover, Color press, Color border)
        {
            _n = normal; _h = hover; _p = press; _border = border;
            FlatAppearance.BorderColor = border;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _over = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _over = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g    = e.Graphics;
            Color bg = _down ? _p : _over ? _h : _n;
            var rc   = ClientRectangle;

            // fill
            using (var b = new SolidBrush(bg))
                g.FillRectangle(b, rc);

            // border
            using (var pen = new Pen(_border, 1f))
                g.DrawRectangle(pen, 0, 0, rc.Width - 1, rc.Height - 1);

            // text
            var sf = new StringFormat {
                Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center
            };
            using (var tb = new SolidBrush(ForeColor))
                g.DrawString(Text, Font, tb, rc, sf);
        }
    }


    // ── Drop zone (rectangular — no GDI corner artifacts) ─────────────────────
    class DropZone : Panel
    {
        public Color BorderCol { get; set; }
        public Color TextCol    { get; set; }
        public bool  Highlight  { get; set; }
        public string DropText  { get; set; }
        Font _fnt, _fntBold;

        public DropZone(Font f)
        {
            _fnt = f;
            _fntBold = new Font(f.FontFamily, f.Size, FontStyle.Bold);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            var rc = ClientRectangle;

            // fill with own BackColor (same as parent BG — no corners issue)
            using (var b = new SolidBrush(BackColor))
                g.FillRectangle(b, rc);

            // border
            using (var pen = new Pen(BorderCol, Highlight ? 2f : 1f))
            {
                if (!Highlight) {
                    pen.DashStyle   = DashStyle.Custom;
                    pen.DashPattern = new float[] { 6f, 4f };
                }
                g.DrawRectangle(pen, 1, 1, rc.Width - 2, rc.Height - 2);
            }

            // arrow icon
            int cx = rc.Width / 2;
            int cy = rc.Height / 2 - 16;
            int aw = 14, ah = 18;
            using (var pen = new Pen(TextCol, 2.5f))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap   = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawLine(pen, cx, cy,           cx,          cy + ah);
                g.DrawLine(pen, cx, cy + ah,      cx - aw / 2, cy + ah - aw / 2);
                g.DrawLine(pen, cx, cy + ah,      cx + aw / 2, cy + ah - aw / 2);
                g.DrawLine(pen, cx - aw, cy + ah + 6, cx + aw, cy + ah + 6);
            }

            // text
            string txt = DropText ?? "Drop files or folders here";
            var sf = new StringFormat { Alignment = StringAlignment.Center };
            using (var tb = new SolidBrush(TextCol))
                g.DrawString(txt, _fntBold, tb,
                    new RectangleF(0, cy + ah + 12, rc.Width, 26), sf);
        }
    }


    // ── Main form ─────────────────────────────────────────────────────────────
    class MainForm : Form
    {
        // ── Theme ─────────────────────────────────────────────────────────────
        // ── DWM dark title bar ────────────────────────────────────────────────
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        void SetTitleBarTheme(bool dark)
        {
            int val = dark ? 1 : 0;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
        }

        static bool IsDarkMode()
        {
            try {
                var v = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", 1);
                return v != null && (int)v == 0;
            } catch { return false; }
        }

        // Light
        static readonly Color L_BG      = Color.FromArgb(245, 245, 245);
        static readonly Color L_SURFACE = Color.FromArgb(250, 250, 250);
        static readonly Color L_CARD    = Color.FromArgb(255, 255, 255);
        static readonly Color L_ACCENT  = Color.FromArgb(0,   120, 212);
        static readonly Color L_ACCENTD = Color.FromArgb(0,    98, 178);
        static readonly Color L_ACCENTH = Color.FromArgb(0,   140, 240);
        static readonly Color L_MUTED   = Color.FromArgb(96,   96,  96);
        static readonly Color L_BORDER  = Color.FromArgb(213, 213, 213);
        static readonly Color L_BTNBDR  = Color.FromArgb(196, 196, 196);
        static readonly Color L_TEXT    = Color.FromArgb(28,   28,  28);
        static readonly Color L_DROPHI  = Color.FromArgb(235, 245, 255);
        static readonly Color L_DROPBD  = Color.FromArgb(0,   120, 212);

        // Dark
        static readonly Color D_BG      = Color.FromArgb(32,  32,  32);
        static readonly Color D_SURFACE = Color.FromArgb(40,  40,  40);
        static readonly Color D_CARD    = Color.FromArgb(52,  52,  52);
        static readonly Color D_ACCENT  = Color.FromArgb(76,  164, 255);
        static readonly Color D_ACCENTD = Color.FromArgb(50,  140, 230);
        static readonly Color D_ACCENTH = Color.FromArgb(100, 180, 255);
        static readonly Color D_MUTED   = Color.FromArgb(160, 160, 160);
        static readonly Color D_BORDER  = Color.FromArgb(65,  65,  65);
        static readonly Color D_BTNBDR  = Color.FromArgb(80,  80,  80);
        static readonly Color D_TEXT    = Color.FromArgb(228, 228, 228);
        static readonly Color D_DROPHI  = Color.FromArgb(40,  55,  75);
        static readonly Color D_DROPBD  = Color.FromArgb(76,  164, 255);

        // Shared
        static readonly Color C_OK   = Color.FromArgb(16, 124,  16);
        static readonly Color C_ERR  = Color.FromArgb(196,  43,  28);
        static readonly Color C_WARN = Color.FromArgb(157, 111,   0);
        static readonly Color C_OK_D = Color.FromArgb(78,  201, 176);
        static readonly Color C_ERR_D = Color.FromArgb(255,  99,  71);
        static readonly Color C_WARN_D = Color.FromArgb(255, 198,  77);

        Color BG, SURFACE, CARD, ACCENT, ACCENTD, ACCENTH, MUTED, BORDER, BTNBDR, TEXT, DROPHI, DROPBD;
        Color OK, ERR, WARN;
        bool _dark;

        void LoadTheme()
        {
            _dark = IsDarkMode();
            BG      = _dark ? D_BG      : L_BG;
            SURFACE = _dark ? D_SURFACE : L_SURFACE;
            CARD    = _dark ? D_CARD    : L_CARD;
            ACCENT  = _dark ? D_ACCENT  : L_ACCENT;
            ACCENTD = _dark ? D_ACCENTD : L_ACCENTD;
            ACCENTH = _dark ? D_ACCENTH : L_ACCENTH;
            MUTED   = _dark ? D_MUTED   : L_MUTED;
            BORDER  = _dark ? D_BORDER  : L_BORDER;
            BTNBDR  = _dark ? D_BTNBDR  : L_BTNBDR;
            TEXT    = _dark ? D_TEXT    : L_TEXT;
            DROPHI  = _dark ? D_DROPHI  : L_DROPHI;
            DROPBD  = _dark ? D_DROPBD  : L_DROPBD;
            OK      = _dark ? C_OK_D    : C_OK;
            ERR     = _dark ? C_ERR_D   : C_ERR;
            WARN    = _dark ? C_WARN_D  : C_WARN;
        }

        void ApplyTheme()
        {
            LoadTheme();
            BackColor = BG;
            ForeColor = TEXT;

            if (_header != null) { _header.BackColor = SURFACE; _header.Invalidate(true); }
            if (_divider != null) { _divider.BackColor = BORDER; }
            if (_toolbar != null) { _toolbar.BackColor = BG; }
            if (_logPanel != null) { _logPanel.BackColor = SURFACE; _logPanel.Invalidate(); }
            if (_statusBar != null) { _statusBar.BackColor = SURFACE; _statusBar.Invalidate(); }

            if (_dropZone != null) {
                _dropZone.BackColor = BG;
                _dropZone.BorderCol = BORDER;
                _dropZone.TextCol   = TEXT;
                _dropZone.DropText  = "Drop files or folders here";
                _dropZone.Highlight = false;
                _dropZone.Invalidate();
            }
            if (_rtb != null) { _rtb.BackColor = CARD; _rtb.ForeColor = TEXT; }

            // labels
            if (_lblTitle  != null) { _lblTitle.BackColor  = SURFACE; _lblTitle.ForeColor  = TEXT; }
            if (_lblSub    != null) { _lblSub.BackColor    = SURFACE; _lblSub.ForeColor    = MUTED; }
            if (_lblLogHdr != null) _lblLogHdr.ForeColor = MUTED;
            if (_lblStatus != null) _lblStatus.ForeColor = MUTED;
            if (_lblOutDir != null) _lblOutDir.ForeColor = MUTED;

            // buttons
            if (_btnBrowse != null) {
                _btnBrowse.Retheme(ACCENT, ACCENTH, ACCENTD, Color.FromArgb(0, 84, 166));
                _btnBrowse.ForeColor = Color.White;
            }
            if (_btnClear  != null) {
                _btnClear.Retheme(CARD, Color.FromArgb(
                    Math.Min(255, CARD.R + (CARD.R > 200 ? -10 : 15)),
                    Math.Min(255, CARD.G + (CARD.G > 200 ? -10 : 15)),
                    Math.Min(255, CARD.B + (CARD.B > 200 ? -10 : 15))),
                    SURFACE, BTNBDR);
                _btnClear.ForeColor = TEXT;
            }
            if (_btnOutDir != null) {
                _btnOutDir.Retheme(CARD, Color.FromArgb(
                    Math.Min(255, CARD.R + (CARD.R > 200 ? -10 : 15)),
                    Math.Min(255, CARD.G + (CARD.G > 200 ? -10 : 15)),
                    Math.Min(255, CARD.B + (CARD.B > 200 ? -10 : 15))),
                    SURFACE, BTNBDR);
                _btnOutDir.ForeColor = TEXT;
            }

            if (_btnAbout != null) {
                _btnAbout.Retheme(CARD, Color.FromArgb(
                    Math.Min(255, CARD.R + (CARD.R > 200 ? -10 : 15)),
                    Math.Min(255, CARD.G + (CARD.G > 200 ? -10 : 15)),
                    Math.Min(255, CARD.B + (CARD.B > 200 ? -10 : 15))),
                    SURFACE, BTNBDR);
                _btnAbout.ForeColor = TEXT;
            }

            SetTitleBarTheme(_dark);
            Invalidate(true);
        }

        void ApplyThemeRaw() { ApplyTheme(); }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x001A) BeginInvoke(new Action(ApplyThemeRaw));
            base.WndProc(ref m);
        }

        // ── Fields ────────────────────────────────────────────────────────────
        Font _fntTitle, _fntBody, _fntMono, _fntSmall, _fntSemi, _fntDrop;

        Panel    _header, _divider, _toolbar, _logPanel, _statusBar;
        DropZone _dropZone;
        RichTextBox _rtb;
        Btn      _btnBrowse, _btnClear, _btnOutDir, _btnAbout;
        Label    _lblTitle, _lblSub, _lblLogHdr, _lblStatus, _lblOutDir;

        string _outputDir;
        string _sevenZip = @"C:\Program Files\7-Zip\7z.exe";
        int _ok, _err;
        bool _busy;

        public MainForm(string[] args)
        {
            _outputDir = AppDomain.CurrentDomain.BaseDirectory;
            _fntTitle  = new Font("Segoe UI", 14f, FontStyle.Bold);
            _fntBody   = new Font("Segoe UI",  9.5f);
            _fntMono   = new Font("Consolas",  9f);
            _fntSmall  = new Font("Segoe UI",  8.5f);
            _fntSemi   = new Font("Segoe UI Semibold", 9.5f);
            _fntDrop   = new Font("Segoe UI",  10f);
            LoadTheme();
            InitUI();
            ApplyTheme();   // force correct colours on first render
            if (args != null && args.Length > 0)
                BeginInvoke(new Action(() => ProcessItems(args)));
        }

        void InitUI()
        {
            Text            = "SafeZip";
            Size            = new Size(740, 600);
            MinimumSize     = new Size(600, 500);
            BackColor       = BG;
            ForeColor       = TEXT;
            Font            = _fntBody;
            StartPosition   = FormStartPosition.CenterScreen;
            AllowDrop       = true;
            DoubleBuffered  = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            Icon            = LoadEmbeddedIcon();

            DragEnter += OnDragEnter;
            DragDrop  += OnDragDrop;
            DragLeave += OnDragLeave;
            Resize    += OnResize;

            // ── Header ────────────────────────────────────────────────────────
            _header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = SURFACE };
            _header.Paint += (s, e) => {
                _header.BackColor = SURFACE;
                using (var p = new Pen(BORDER, 1))
                    e.Graphics.DrawLine(p, 0, _header.Height - 1, _header.Width, _header.Height - 1);
            };
            Controls.Add(_header);

            _lblTitle = new Label {
                Text = "SafeZip", Font = _fntTitle, ForeColor = TEXT,
                AutoSize = true, BackColor = SURFACE,
                Location = new Point(18, 10)
            };
            _header.Controls.Add(_lblTitle);

            _lblSub = new Label {
                Text = "Create (Store-Only Archiving), Verify, ZIP archives — drag, drop, done.",
                Font = _fntSmall, ForeColor = MUTED,
                AutoSize = true, BackColor = SURFACE,
                Location = new Point(20, 40)
            };
            _header.Controls.Add(_lblSub);


            // ── Drop zone ─────────────────────────────────────────────────────
            _dropZone = new DropZone(_fntDrop) {
                BackColor = BG, BorderCol = BORDER, TextCol = TEXT,
                DropText  = "Drop files or folders here",
                AllowDrop = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(24, 82),
                Size      = new Size(100, 116)
            };
            _dropZone.DragEnter += OnDragEnter;
            _dropZone.DragDrop  += OnDragDrop;
            _dropZone.DragLeave += OnDragLeave;
            Controls.Add(_dropZone);

            // ── Thin divider ──────────────────────────────────────────────────
            _divider = new Panel {
                BackColor = BORDER,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(24, 210),
                Size      = new Size(100, 1)
            };
            Controls.Add(_divider);

            // ── Toolbar ───────────────────────────────────────────────────────
            _toolbar = new Panel {
                BackColor = BG,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(24, 214),
                Size      = new Size(100, 40)
            };
            Controls.Add(_toolbar);

            _btnBrowse = new Btn(ACCENT, ACCENTH, ACCENTD, Color.FromArgb(0, 84, 166)) {
                Text = "Browse…", Font = _fntSemi, ForeColor = Color.White,
                Size = new Size(96, 32), Location = new Point(0, 4), Primary = true
            };
            _btnBrowse.Click += BtnBrowse_Click;

            Color btnHov = Color.FromArgb(CARD.R > 200 ? CARD.R - 10 : CARD.R + 15,
                                          CARD.G > 200 ? CARD.G - 10 : CARD.G + 15,
                                          CARD.B > 200 ? CARD.B - 10 : CARD.B + 15);

            _btnClear = new Btn(CARD, btnHov, SURFACE, BTNBDR) {
                Text = "Clear Log", Font = _fntSmall, ForeColor = TEXT,
                Size = new Size(88, 32), Location = new Point(110, 4)
            };
            _btnClear.Click += (s, e) => { _rtb.Clear(); _ok = _err = 0; SetStatus("Log cleared."); };

            _btnOutDir = new Btn(CARD, btnHov, SURFACE, BTNBDR) {
                Text = "Output Dir…", Font = _fntSmall, ForeColor = TEXT,
                Size = new Size(100, 32), Location = new Point(206, 4)
            };
            _btnOutDir.Click += BtnOutDir_Click;

            _lblOutDir = new Label {
                Text      = TruncatePath(_outputDir, 44),
                Font      = _fntSmall, ForeColor = MUTED,
                AutoSize  = false, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Location  = new Point(316, 4),
                Size      = new Size(340, 32)
            };

            _btnAbout = new Btn(CARD, btnHov, SURFACE, BTNBDR) {
                Text   = "About", Font = _fntSmall, ForeColor = TEXT,
                Size   = new Size(88, 32), Location = new Point(600, 4)
            };
            _btnAbout.Click += (s, e) => ShowAbout();

            _toolbar.Controls.AddRange(new Control[] { _btnBrowse, _btnClear, _btnOutDir, _lblOutDir, _btnAbout });

            // ── Log section ───────────────────────────────────────────────────
            _logPanel = new Panel {
                BackColor = SURFACE,
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(24, 260),
                Size      = new Size(100, 240)
            };
            _logPanel.Paint += (s, e) => {
                _logPanel.BackColor = SURFACE;
                using (var p = new Pen(BORDER, 1))
                    e.Graphics.DrawRectangle(p, 0, 0, _logPanel.Width - 1, _logPanel.Height - 1);
            };
            Controls.Add(_logPanel);

            _lblLogHdr = new Label {
                Text      = "Activity Log",
                Font      = _fntSmall, ForeColor = MUTED,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(10, 8)
            };
            _logPanel.Controls.Add(_lblLogHdr);

            _rtb = new RichTextBox {
                BackColor   = CARD,
                ForeColor   = TEXT,
                Font        = _fntMono,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location    = new Point(1, 26),
                Size        = new Size(646, 212)
            };
            _logPanel.Controls.Add(_rtb);

            // ── Status bar ────────────────────────────────────────────────────
            _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 32, BackColor = SURFACE };
            _statusBar.Paint += (s, e) => {
                _statusBar.BackColor = SURFACE;
                using (var p = new Pen(BORDER, 1))
                    e.Graphics.DrawLine(p, 0, 0, _statusBar.Width, 0);
            };
            Controls.Add(_statusBar);

            var tbl = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _statusBar.Controls.Add(tbl);

            _lblStatus = new Label {
                Text = "Ready — drop files above or click Browse",
                Font = _fntSmall, ForeColor = MUTED,
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 0, 0)
            };
            tbl.Controls.Add(_lblStatus, 0, 0);

            // force correct initial layout
            OnResize(this, EventArgs.Empty);

            // startup log
            AppendLog("SafeZip ready.", TEXT);
            AppendLog("7-Zip  : " + _sevenZip, MUTED);
            AppendLog("Output : " + _outputDir, MUTED);
            AppendLog(new string('\u2500', 68), BORDER);
        }

        // ── Drag-drop ─────────────────────────────────────────────────────────
        void OnDragEnter(object s, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) { e.Effect = DragDropEffects.None; return; }
            e.Effect = DragDropEffects.Copy;
            _dropZone.BackColor = DROPHI;
            _dropZone.BorderCol = DROPBD;
            _dropZone.Highlight = true;
            _dropZone.DropText  = "Release to create ZIPs…";
            _dropZone.Invalidate();
        }

        void OnDragLeave(object s, EventArgs e)
        {
            _dropZone.BackColor = BG;
            _dropZone.BorderCol = BORDER;
            _dropZone.Highlight = false;
            _dropZone.DropText  = "Drop files or folders here";
            _dropZone.Invalidate();
        }

        void OnDragDrop(object s, DragEventArgs e)
        {
            OnDragLeave(s, e);
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0) ProcessItems(files);
        }

        // ── Buttons ───────────────────────────────────────────────────────────
        void BtnBrowse_Click(object s, EventArgs e)
        {
            using (var dlg = new OpenFileDialog {
                Title = "Select files to ZIP", Multiselect = true, Filter = "All Files (*.*)|*.*"
            })
            if (dlg.ShowDialog() == DialogResult.OK) ProcessItems(dlg.FileNames);
        }

        void BtnOutDir_Click(object s, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog {
                Description = "Choose where ZIP files are saved", SelectedPath = _outputDir
            })
            if (dlg.ShowDialog() == DialogResult.OK) {
                _outputDir = dlg.SelectedPath;
                _lblOutDir.Text = TruncatePath(_outputDir, 44);
                SetStatus("Output folder: " + _outputDir);
            }
        }

        // ── Resize ────────────────────────────────────────────────────────────
        void OnResize(object s, EventArgs e)
        {
            int margin = 24;
            int w = ClientSize.Width - margin * 2;
            if (_dropZone  != null) { _dropZone.Width = w;    _dropZone.Location  = new Point(margin, _dropZone.Top); }
            if (_divider   != null) { _divider.Width  = w;    _divider.Location   = new Point(margin, _divider.Top); }
            if (_toolbar   != null) { _toolbar.Width  = w;    _toolbar.Location   = new Point(margin, _toolbar.Top); }
            if (_logPanel  != null) {
                _logPanel.Width    = w;
                _logPanel.Location = new Point(margin, _logPanel.Top);
                _logPanel.Height   = ClientSize.Height - _logPanel.Top - 36;
                if (_rtb != null)  _rtb.Size = new Size(w - 2, _logPanel.Height - 27);
            }
            if (_btnAbout  != null) _btnAbout.Location  = new Point(w - 88, 4);
            if (_lblOutDir != null) _lblOutDir.Width = w - 316 - 98;
        }

        static string TruncatePath(string path, int max)
        {
            return path.Length <= max ? path : "..." + path.Substring(path.Length - (max - 1));
        }

        // ── Core logic ─────────────────────────────────────────────────────────
        void ProcessItems(string[] items)
        {
            if (_busy) { SetStatus("Already processing — please wait."); return; }

            if (!File.Exists(_sevenZip)) {
                MessageBox.Show(
                    "7-Zip not found at:\n" + _sevenZip +
                    "\n\nPlease install 7-Zip from https://www.7-zip.org\n",
                    "7-Zip Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _busy = true;
            _btnBrowse.Enabled = false;
            _ok = _err = 0;

            System.Threading.ThreadPool.QueueUserWorkItem(_ => {
                for (int i = 0; i < items.Length; i++) {
                    string item = items[i];
                    string name = Path.GetFileNameWithoutExtension(item);
                    if (string.IsNullOrEmpty(Path.GetExtension(item)))
                        name = Path.GetFileName(item.TrimEnd('\\', '/'));

                    string outZip = Path.Combine(_outputDir, name + ".zip");

                    Invoke(new Action(() => {
                        AppendLog(new string('─', 72), BORDER);
                        AppendLog(string.Format("[{0}/{1}]  {2}", i + 1, items.Length, item), TEXT);
                        AppendLog("        → " + outZip, MUTED);
                        SetStatus(string.Format("Processing {0} of {1}  —  {2}", i + 1, items.Length, name));
                    }));

                    // Create
                    int exitCreate = RunProcess(_sevenZip,
                        string.Format("a -tzip -mx=0 \"{0}\" \"{1}\"", outZip, item));

                    if (exitCreate != 0) {
                        Invoke(new Action(() => AppendLog("  [FAIL] Creation failed (exit " + exitCreate + ")", ERR)));
                        _err++;
                        continue;
                    }

                    Invoke(new Action(() => AppendLog("  [ZIP]  Archive created.", MUTED)));

                    // Verify
                    int exitTest = RunProcess(_sevenZip, string.Format("t \"{0}\"", outZip));

                    Invoke(new Action(() => {
                        if (exitTest == 0) {
                            AppendLog("  [OK]   " + name + ".zip  ✔  verified", OK);
                            _ok++;
                        } else {
                            AppendLog("  [ERR]  Verification FAILED for " + name + ".zip!", ERR);
                            _err++;
                        }
                    }));
                }

                Invoke(new Action(() => {
                    AppendLog(new string('─', 72), BORDER);
                    AppendLog(string.Format("Done.  ✔ {0} succeeded    ✘ {1} failed", _ok, _err),
                        _err == 0 ? OK : WARN);
                    SetStatus(string.Format("✔ {0} succeeded   ✘ {1} failed  —  Ready", _ok, _err));
                    _busy = false;
                    _btnBrowse.Enabled = true;
                }));
            });
        }

        static int RunProcess(string exe, string args)
        {
            var psi = new ProcessStartInfo {
                FileName               = exe,
                Arguments              = args,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };
            using (var p = Process.Start(psi)) {
                p.WaitForExit();
                return p.ExitCode;
            }
        }


        void AppendLog(string text, Color color)
        {
            if (_rtb == null) return;
            if (_rtb.InvokeRequired) { _rtb.Invoke(new Action(() => AppendLog(text, color))); return; }
            _rtb.SelectionStart  = _rtb.TextLength;
            _rtb.SelectionLength = 0;
            _rtb.SelectionColor  = color;
            _rtb.AppendText(text + "\n");
            _rtb.ScrollToCaret();
        }

        void SetStatus(string msg)
        {
            if (_lblStatus == null) return;
            if (_lblStatus.InvokeRequired) { _lblStatus.Invoke(new Action(() => SetStatus(msg))); return; }
            _lblStatus.Text = msg;
        }

        // ── About dialog ──────────────────────────────────────────────────
        void ShowAbout()
        {
            string text;
            try {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("README.md"))
                using (var r = new System.IO.StreamReader(s, System.Text.Encoding.UTF8))
                    text = r.ReadToEnd();
            } catch {
                text = "README.md resource not found.";
            }

            var dlg = new Form {
                Text            = "About SafeZip",
                Size            = new Size(600, 520),
                MinimumSize     = new Size(480, 400),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor       = BG,
                ForeColor       = TEXT,
                Font            = _fntBody,
                Icon            = this.Icon
            };
            var rtb = new RichTextBox {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                BackColor   = CARD,
                ForeColor   = TEXT,
                Font        = _fntBody,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Padding     = new Padding(12),
                Text        = text
            };
            dlg.Controls.Add(rtb);
            dlg.ShowDialog(this);
        }

        static Icon LoadEmbeddedIcon()
        {
            try {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("SafeZip.ico"))
                    if (s != null) return new Icon(s);
            } catch { }
            return SystemIcons.Application;
        }
    }
}

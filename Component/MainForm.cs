using StickyNotes.model;
using StickyNotes.State;
using StickyNotes.Store;
using System.Drawing;

using System.Drawing;

public class MainForm : Form
{
    // ===== 数据 =====
    private readonly NoteStore.Root _root = NoteStore.Load();
    private readonly List<NoteItem> _notes = new();
    private NoteItem? _current;
    private bool _loading;

    private enum ViewMode { List, Detail }
    private ViewMode _mode = ViewMode.List;

    // ===== 页面 =====
    private readonly Panel _listPage = new();
    private readonly Panel _detailPage = new();

    // 列表页
    private readonly Panel _listTitleBar = new();
    private readonly ListBox _listBox = new();
    private readonly Label _lblEmpty = new();
    private Button? _btnPinList;

    // 详情页
    private readonly Panel _detailTitleBar = new();
    private readonly TextBox _txtTitle = new();
    private readonly RichTextBox _editor = new();
    private Button? _btnPinDetail;

    // 窗口层
    private readonly Label _grip = new();
    private bool _dragging;
    private Point _dragOffset;
    private bool _resizing;
    private Point _resizeStart;
    private Size _resizeStartSize;

    // 悬停提示
    private readonly ToolTip _tip = new()
    {
        InitialDelay = 300,
        ReshowDelay = 100,
        AutoPopDelay = 5000
    };

    // 保存防抖
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 800 };
    private bool _dirty;

    public MainForm()
    {
        BuildWindow();
        ApplyWindowState(_root.Window);
        BuildListPage();
        BuildDetailPage();

        _notes.AddRange(_root.Notes);

        ShowPage(ViewMode.List);
        RefreshList();
        UpdatePinButtons();

        _saveTimer.Tick += (_, _) => SaveNow();
    }

    // =========================================================
    // 窗口层
    // =========================================================
    private void BuildWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        MinimumSize = new Size(280, 360);
        Size = new Size(360, 520);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(200, 200);
        Padding = new Padding(1);
        BackColor = Color.Gray;
        TopMost = true;

        _grip.Text = "◢";
        _grip.Size = new Size(16, 16);
        _grip.TextAlign = ContentAlignment.MiddleCenter;
        _grip.Cursor = Cursors.SizeNWSE;
        _grip.BackColor = Color.Transparent;
        _grip.MouseDown += Grip_MouseDown;
        _grip.MouseMove += Grip_MouseMove;
        _grip.MouseUp += (_, _) => { _resizing = false; MarkDirty(); };

        Controls.Add(_listPage);
        Controls.Add(_detailPage);
        Controls.Add(_grip);
        _grip.BringToFront();

        Resize += (_, _) => PlaceGrip();
        PlaceGrip();
    }

    private void ApplyWindowState(WindowState s)
    {
        var rect = new Rectangle(s.X, s.Y, s.Width, s.Height);
        bool visible = Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(rect));
        if (!visible)
        {
            var wa = Screen.PrimaryScreen!.WorkingArea;
            rect.X = wa.X + 100;
            rect.Y = wa.Y + 100;
        }
        Bounds = rect;
        TopMost = s.TopMost;
    }

    private void PlaceGrip()
    {
        _grip.Location = new Point(ClientSize.Width - _grip.Width - 2,
                                   ClientSize.Height - _grip.Height - 2);
    }

    // =========================================================
    // 列表页
    // =========================================================
    private void BuildListPage()
    {
        _listPage.Dock = DockStyle.Fill;
        _listPage.BackColor = Color.WhiteSmoke;

        _listTitleBar.Height = 28;
        _listTitleBar.Dock = DockStyle.Top;
        _listTitleBar.BackColor = Color.FromArgb(235, 235, 215);
        _listTitleBar.Cursor = Cursors.SizeAll;
        _listTitleBar.MouseDown += TitleBar_MouseDown;
        _listTitleBar.MouseMove += TitleBar_MouseMove;
        _listTitleBar.MouseUp += (_, _) => { _dragging = false; MarkDirty(); };

        var lblListTitle = new Label
        {
            Text = "我的便签",
            Dock = DockStyle.Left,
            Width = 120,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        var btnNew = MakeBarButton("＋");
        btnNew.Click += (_, _) => CreateNewNote();
        _tip.SetToolTip(btnNew, "新建便签");

        var btnCloseList = MakeBarButton("✕");
        btnCloseList.Click += (_, _) => Close();
        _tip.SetToolTip(btnCloseList, "关闭程序");

        _btnPinList = MakeBarButton("📌");
        _btnPinList.Click += (_, _) => ToggleTopMost();

        _listTitleBar.Controls.Add(btnCloseList);
        _listTitleBar.Controls.Add(_btnPinList);
        _listTitleBar.Controls.Add(btnNew);
        _listTitleBar.Controls.Add(lblListTitle);

        _listBox.Dock = DockStyle.Fill;
        _listBox.BorderStyle = BorderStyle.None;
        _listBox.Font = new Font("Microsoft YaHei", 10f);
        _listBox.ItemHeight = 36;
        _listBox.DrawMode = DrawMode.OwnerDrawFixed;
        _listBox.DrawItem += ListBox_DrawItem;
        _listBox.DoubleClick += (_, _) => OpenCurrentFromList();
        _listBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) OpenCurrentFromList();
        };

        _lblEmpty.Text = "还没有便签，点 ＋ 新建";
        _lblEmpty.Dock = DockStyle.Fill;
        _lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _lblEmpty.ForeColor = Color.Gray;
        _lblEmpty.Visible = false;

        _listPage.Controls.Add(_listBox);
        _listPage.Controls.Add(_lblEmpty);
        _listPage.Controls.Add(_listTitleBar);
        _lblEmpty.BringToFront();
    }

    private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _notes.Count) return;
        var item = _notes[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;

        using var bg = new SolidBrush(selected ? Color.FromArgb(210, 230, 255) : Color.White);
        e.Graphics.FillRectangle(bg, e.Bounds);

        using var fg = new SolidBrush(Color.Black);
        var rect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y,
                                 e.Bounds.Width - 20, e.Bounds.Height);
        e.Graphics.DrawString(item.Title, e.Font ?? Font, fg, rect,
            new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            });
    }

    // =========================================================
    // 详情页
    // =========================================================
    private void BuildDetailPage()
    {
        _detailPage.Dock = DockStyle.Fill;
        _detailPage.BackColor = Color.LightYellow;
        _detailPage.Visible = false;

        _detailTitleBar.Height = 28;
        _detailTitleBar.Dock = DockStyle.Top;
        _detailTitleBar.BackColor = Color.FromArgb(235, 235, 215);
        _detailTitleBar.Cursor = Cursors.SizeAll;
        _detailTitleBar.MouseDown += TitleBar_MouseDown;
        _detailTitleBar.MouseMove += TitleBar_MouseMove;
        _detailTitleBar.MouseUp += (_, _) => { _dragging = false; MarkDirty(); };

        var btnBack = MakeBarButton("←", DockStyle.Left);
        btnBack.Click += (_, _) => BackToList();
        _tip.SetToolTip(btnBack, "返回列表");

        var btnCloseDetail = MakeBarButton("✕");
        btnCloseDetail.Click += (_, _) => Close();
        _tip.SetToolTip(btnCloseDetail, "关闭程序");

        _btnPinDetail = MakeBarButton("📌");
        _btnPinDetail.Click += (_, _) => ToggleTopMost();

        _txtTitle.Dock = DockStyle.Fill;
        _txtTitle.BorderStyle = BorderStyle.None;
        _txtTitle.Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold);
        _txtTitle.BackColor = Color.FromArgb(235, 235, 215);
        _txtTitle.TextChanged += (_, _) =>
        {
            if (_loading || _current == null) return;
            _current.Title = string.IsNullOrWhiteSpace(_txtTitle.Text) ? "无标题" : _txtTitle.Text;
            _current.UpdatedAt = DateTime.Now;
            MarkDirty();
        };

        _detailTitleBar.Controls.Add(btnCloseDetail);
        _detailTitleBar.Controls.Add(_btnPinDetail);
        _detailTitleBar.Controls.Add(_txtTitle);
        _detailTitleBar.Controls.Add(btnBack);

        _editor.Dock = DockStyle.Fill;
        _editor.BorderStyle = BorderStyle.None;
        _editor.Font = new Font("Microsoft YaHei", 10f);
        _editor.ScrollBars = RichTextBoxScrollBars.Vertical;
        _editor.BackColor = Color.LightYellow;
        _editor.TextChanged += (_, _) =>
        {
            if (_loading || _current == null) return;
            _current.Rtf = _editor.Rtf;
            _current.UpdatedAt = DateTime.Now;
            MarkDirty();
        };

        _detailPage.Controls.Add(_editor);
        _detailPage.Controls.Add(_detailTitleBar);
    }

    // =========================================================
    // 按钮工厂
    // =========================================================
    private Button MakeBarButton(string text, DockStyle dock = DockStyle.Right)
    {
        var btn = new Button
        {
            Text = text,
            Dock = dock,
            Width = 32,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            TabStop = false,
            UseVisualStyleBackColor = false,
            BackColor = Color.Transparent,
            Font = new Font("Microsoft YaHei", 9f)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // =========================================================
    // 页面切换与数据流
    // =========================================================
    private void ShowPage(ViewMode mode)
    {
        _mode = mode;
        bool isList = mode == ViewMode.List;
        _listPage.Visible = isList;
        _detailPage.Visible = !isList;
        _listPage.BringToFront();
        _detailPage.BringToFront();
        _grip.BringToFront();
    }

    private void OpenCurrentFromList()
    {
        if (_listBox.SelectedIndex < 0 || _listBox.SelectedIndex >= _notes.Count) return;
        OpenNote(_notes[_listBox.SelectedIndex]);
    }

    private void OpenNote(NoteItem note)
    {
        _current = note;
        _loading = true;
        try
        {
            _txtTitle.Text = note.Title;
            _editor.Rtf = string.IsNullOrEmpty(note.Rtf) ? "" : note.Rtf;
        }
        finally
        {
            _loading = false;
        }

        ShowPage(ViewMode.Detail);

        if (string.IsNullOrEmpty(note.Rtf)) _editor.Focus();
        else _txtTitle.Focus();
    }

    private void CreateNewNote()
    {
        var note = new NoteItem();
        _notes.Insert(0, note);
        RefreshList();
        _listBox.SelectedIndex = 0;
        OpenNote(note);
        _txtTitle.Focus();
        _txtTitle.SelectAll();
        MarkDirty();
    }

    private void BackToList()
    {
        _current = null;
        ShowPage(ViewMode.List);
        RefreshList();
        if (_listBox.Items.Count > 0 && _listBox.SelectedIndex < 0)
            _listBox.SelectedIndex = 0;
    }

    private void RefreshList()
    {
        _notes.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));

        _listBox.BeginUpdate();
        _listBox.Items.Clear();
        foreach (var n in _notes) _listBox.Items.Add(n.Title);
        _listBox.EndUpdate();

        bool empty = _notes.Count == 0;
        _lblEmpty.Visible = empty;
        _listBox.Visible = !empty;
    }

    // =========================================================
    // 置顶
    // =========================================================
    private void ToggleTopMost()
    {
        TopMost = !TopMost;
        UpdatePinButtons();
        MarkDirty();
    }

    private void UpdatePinButtons()
    {
        var onColor = Color.FromArgb(200, 230, 255);
        var offColor = Color.Transparent;
        var tipText = TopMost ? "取消置顶" : "置顶窗口";

        if (_btnPinList != null)
        {
            _btnPinList.Text = TopMost ? "📌" : "📍";
            _btnPinList.BackColor = TopMost ? onColor : offColor;
            _tip.SetToolTip(_btnPinList, tipText);
        }
        if (_btnPinDetail != null)
        {
            _btnPinDetail.Text = TopMost ? "📌" : "📍";
            _btnPinDetail.BackColor = TopMost ? onColor : offColor;
            _tip.SetToolTip(_btnPinDetail, tipText);
        }
    }

    // =========================================================
    // 拖动 / 缩放
    // =========================================================
    private void TitleBar_MouseDown(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragOffset = e.Location;
    }

    private void TitleBar_MouseMove(object? s, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = Cursor.Position;
        Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
    }

    private void Grip_MouseDown(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _resizing = true;
        _resizeStart = Cursor.Position;
        _resizeStartSize = Size;
    }

    private void Grip_MouseMove(object? s, MouseEventArgs e)
    {
        if (!_resizing) return;
        var p = Cursor.Position;
        int w = Math.Max(MinimumSize.Width, _resizeStartSize.Width + (p.X - _resizeStart.X));
        int h = Math.Max(MinimumSize.Height, _resizeStartSize.Height + (p.Y - _resizeStart.Y));
        Size = new Size(w, h);
    }

    // =========================================================
    // 持久化
    // =========================================================
    private void MarkDirty()
    {
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _saveTimer.Stop();
        if (!_dirty) return;

        _root.Notes = _notes.ToList();
        _root.Window = new WindowState
        {
            X = Location.X,
            Y = Location.Y,
            Width = Width,
            Height = Height,
            TopMost = TopMost
        };
        NoteStore.Save(_root);
        _dirty = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveNow();
        base.OnFormClosing(e);
    }
}
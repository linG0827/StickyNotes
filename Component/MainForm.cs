using StickyNotes.model;
using StickyNotes.State;
using StickyNotes.Store;
using StickyNotes.UI;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

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
    private readonly FlowLayoutPanel _cardList = new();
    private readonly Label _lblEmpty = new();
    private IconButton? _btnPinList;

    // 详情页
    private readonly Panel _titleBox = new();
    private readonly TextBox _txtTitle = new();
    private readonly Panel _editorBox = new();
    private readonly RichTextBox _editor = new();
    private IconButton? _btnPinDetail;

    // 窗口层
    private readonly Panel _grip = new();
    private bool _dragging;
    private Point _dragOffset;
    private bool _resizing;
    private Point _resizeStart;
    private Size _resizeStartSize;

    // 提示
    private readonly ToolTip _tip = new() { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 5000 };

    // 保存
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 800 };
    private bool _dirty;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int v = 2;
            DwmSetWindowAttribute(Handle, 33, ref v, sizeof(int));
        }
        catch { }
    }

    // =========================================================
    // 窗口层
    // =========================================================
    private void BuildWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        MinimumSize = new Size(300, 400);
        Size = new Size(380, 560);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(200, 200);
        BackColor = Theme.Bg;
        TopMost = true;

        _grip.Size = new Size(16, 16);
        _grip.BackColor = Color.Transparent;
        _grip.Cursor = Cursors.SizeNWSE;
        _grip.MouseDown += Grip_MouseDown;
        _grip.MouseMove += Grip_MouseMove;
        _grip.MouseUp += (_, _) => { _resizing = false; MarkDirty(); };
        _grip.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(Theme.TextSub);
            for (int i = 0; i < 3; i++)
                e.Graphics.FillEllipse(b, 8 - i * 4, 8 + i * 2, 2, 2);
            for (int i = 0; i < 3; i++)
                e.Graphics.FillEllipse(b, 10 - i * 4, 11 + i * 2, 2, 2);
        };

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
    // 顶部栏
    // =========================================================
    private Panel MakeTitleBar(out IconButton? pinBtn)
    {
        var bar = new Panel
        {
            Height = 44,
            Dock = DockStyle.Top,
            BackColor = Theme.TitleBar,
            Cursor = Cursors.SizeAll
        };
        bar.MouseDown += TitleBar_MouseDown;
        bar.MouseMove += TitleBar_MouseMove;
        bar.MouseUp += (_, _) => { _dragging = false; MarkDirty(); };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Divider);
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };

        pinBtn = new IconButton { Glyph = "📌", Dock = DockStyle.Right };
        pinBtn.Click += (_, _) => ToggleTopMost();
        bar.Controls.Add(pinBtn);

        return bar;
    }

    private IconButton MakeNewButton()
    {
        var b = new IconButton { Glyph = "＋", Dock = DockStyle.Right };
        b.Click += (_, _) => CreateNewNote();
        _tip.SetToolTip(b, "新建便签");
        return b;
    }

    private IconButton MakeCloseButton()
    {
        var b = new IconButton { Glyph = "✕", Dock = DockStyle.Right };
        b.Click += (_, _) => Close();
        _tip.SetToolTip(b, "关闭程序");
        return b;
    }

    private IconButton MakeBackButton()
    {
        var b = new IconButton { Glyph = "←", Dock = DockStyle.Left };
        b.Click += (_, _) => BackToList();
        _tip.SetToolTip(b, "返回列表");
        return b;
    }

    // =========================================================
    // 列表页：先加 Fill 内容，再加 Top 栏
    // =========================================================
    private void BuildListPage()
    {
        _listPage.Dock = DockStyle.Fill;
        _listPage.BackColor = Theme.Bg;

        _cardList.Dock = DockStyle.Fill;
        _cardList.AutoScroll = true;
        _cardList.FlowDirection = FlowDirection.TopDown;
        _cardList.WrapContents = false;
        _cardList.BackColor = Theme.Bg;
        _cardList.Padding = new Padding(0, 6, 0, 6);
        _cardList.Resize += (_, _) => ResizeCards();

        _lblEmpty.Text = "还没有便签，点 ＋ 新建";
        _lblEmpty.Dock = DockStyle.Fill;
        _lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
        _lblEmpty.ForeColor = Theme.TextSub;
        _lblEmpty.Font = Theme.FontBody;
        _lblEmpty.Visible = false;

        var bar = MakeTitleBar(out _btnPinList);
        var btnClose = MakeCloseButton();
        var btnNew = MakeNewButton();

        // Dock=Right 先加靠最右：✕ → ＋ → 📌
        bar.Controls.Add(btnClose);
        bar.Controls.Add(btnNew);
        bar.Controls.Add(new Label
        {
            Text = "My_Notes",
            Dock = DockStyle.Left,
            Width = 120,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Font = Theme.FontTitle,
            ForeColor = Theme.TextMain
        });

        _listPage.Controls.Add(_cardList);
        _listPage.Controls.Add(_lblEmpty);
        _listPage.Controls.Add(bar);
        _lblEmpty.BringToFront();
    }

    private void ResizeCards()
    {
        int w = _cardList.ClientSize.Width - 20;
        foreach (Control c in _cardList.Controls)
            c.Width = w;
    }

    // =========================================================
    // 详情页：Fill → Top(TitleBox) → Top(Bar)
    // =========================================================
    private void BuildDetailPage()
    {
        _detailPage.Dock = DockStyle.Fill;
        _detailPage.BackColor = Theme.Bg;
        _detailPage.Visible = false;

        _titleBox.Dock = DockStyle.Top;
        _titleBox.Height = 52;
        _titleBox.Padding = new Padding(14, 12, 14, 8);
        _titleBox.BackColor = Theme.Bg;
        _titleBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(10, 4, _titleBox.Width - 21, _titleBox.Height - 9);
            using var path = Theme.RoundRect(r, 10);
            using var b = new SolidBrush(Theme.Surface);
            e.Graphics.FillPath(b, path);
        };

        _txtTitle.BorderStyle = BorderStyle.None;
        _txtTitle.Font = Theme.FontTitle;
        _txtTitle.BackColor = Theme.Surface;
        _txtTitle.ForeColor = Theme.TextMain;
        _txtTitle.Dock = DockStyle.Fill;
        _txtTitle.TextChanged += (_, _) =>
        {
            if (_loading || _current == null) return;
            _current.Title = string.IsNullOrWhiteSpace(_txtTitle.Text) ? "无标题" : _txtTitle.Text;
            _current.UpdatedAt = DateTime.Now;
            MarkDirty();
        };
        _titleBox.Controls.Add(_txtTitle);

        _editorBox.Dock = DockStyle.Fill;
        _editorBox.Padding = new Padding(14, 4, 14, 14);
        _editorBox.BackColor = Theme.Bg;
        _editorBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(10, 0, _editorBox.Width - 21, _editorBox.Height - 11);
            using var path = Theme.RoundRect(r, 10);
            using var b = new SolidBrush(Theme.Surface);
            e.Graphics.FillPath(b, path);
        };

        _editor.BorderStyle = BorderStyle.None;
        _editor.Font = Theme.FontBody;
        _editor.BackColor = Theme.Surface;
        _editor.ForeColor = Theme.TextMain;
        _editor.ScrollBars = RichTextBoxScrollBars.Vertical;
        _editor.Dock = DockStyle.Fill;
        _editor.TextChanged += (_, _) =>
        {
            if (_loading || _current == null) return;
            _current.Rtf = _editor.Rtf;
            _current.UpdatedAt = DateTime.Now;
            MarkDirty();
        };
        _editorBox.Controls.Add(_editor);

        var bar = MakeTitleBar(out _btnPinDetail);

        var btnDelete = new IconButton { Glyph = "🗑", Dock = DockStyle.Right, GlyphColor = Theme.Danger };
        btnDelete.Click += (_, _) => DeleteCurrentNote();
        _tip.SetToolTip(btnDelete, "删除当前便签");

        var btnClose = MakeCloseButton();
        var btnBack = MakeBackButton();

        // Dock=Right 先加靠最右：✕ → 🗑 → 📌
        bar.Controls.Add(btnClose);
        bar.Controls.Add(btnDelete);
        bar.Controls.Add(btnBack);

        _detailPage.Controls.Add(_editorBox);   // Fill
        _detailPage.Controls.Add(_titleBox);    // Top，在 bar 下方
        _detailPage.Controls.Add(bar);          // Top，最顶部
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

    private void OpenNote(NoteItem note)
    {
        _current = note;
        _loading = true;
        try
        {
            _txtTitle.Text = note.Title;
            _editor.Rtf = string.IsNullOrEmpty(note.Rtf) ? "" : note.Rtf;
        }
        finally { _loading = false; }

        ShowPage(ViewMode.Detail);
        if (string.IsNullOrEmpty(note.Rtf)) _editor.Focus();
        else _txtTitle.Focus();
    }

    private void CreateNewNote()
    {
        var note = new NoteItem();
        _notes.Insert(0, note);
        RefreshList();
        OpenNote(note);
        _txtTitle.Focus();
        _txtTitle.SelectAll();
        MarkDirty();
    }

    private void DeleteCurrentNote()
    {
        if (_current == null) return;
        var res = MessageBox.Show(this, $"确定删除便签「{_current.Title}」？", "删除便签",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (res != DialogResult.Yes) return;

        _notes.Remove(_current);
        _current = null;
        ShowPage(ViewMode.List);
        RefreshList();
        MarkDirty();
    }

    private void DeleteNoteFromCard(NoteItem note)
    {
        var res = MessageBox.Show(this, $"确定删除便签「{note.Title}」？", "删除便签",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (res != DialogResult.Yes) return;

        _notes.Remove(note);
        if (_current == note) _current = null;
        RefreshList();
        MarkDirty();
    }

    private void BackToList()
    {
        _current = null;
        ShowPage(ViewMode.List);
        RefreshList();
    }

    private void RefreshList()
    {
        _notes.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));

        _cardList.SuspendLayout();
        foreach (Control c in _cardList.Controls.Cast<Control>().ToList())
            c.Dispose();
        _cardList.Controls.Clear();

        foreach (var n in _notes)
        {
            var card = new NoteCard(n) { Selected = (_current?.Id == n.Id) };
            card.OpenRequested += c => OpenNote(c.Note);
            card.DeleteRequested += c => DeleteNoteFromCard(c.Note);
            _cardList.Controls.Add(card);
        }
        ResizeCards();
        _cardList.ResumeLayout();

        bool empty = _notes.Count == 0;
        _lblEmpty.Visible = empty;
        _cardList.Visible = !empty;
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
        var tipText = TopMost ? "取消置顶" : "置顶窗口";
        if (_btnPinList != null)
        {
            _btnPinList.Active = TopMost;
            _tip.SetToolTip(_btnPinList, tipText);
        }
        if (_btnPinDetail != null)
        {
            _btnPinDetail.Active = TopMost;
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
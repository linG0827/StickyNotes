using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNotes.Component
{
    // NoteForm.cs —— 第一层：无边框 + 拖动 + 缩放 + 关闭
    public class NoteForm : Form
    {
        private readonly RichTextBox _editor = new();
        private readonly Panel _titleBar = new();
        private readonly Button _btnClose = new();
        private readonly Label _grip = new();

        private bool _dragging;
        private Point _dragOffset;
        private bool _resizing;
        private Point _resizeStart;
        private Size _resizeStartSize;

        public NoteForm()
        {
            // ---- 窗体 ----
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            MinimumSize = new Size(160, 120);
            Size = new Size(260, 220);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(200, 200);
            Padding = new Padding(1);
            BackColor = Color.Gray;          // 1px 边框色
            TopMost = true;

            // ---- 顶部拖动条 ----
            _titleBar.Height = 24;
            _titleBar.Dock = DockStyle.Top;
            _titleBar.BackColor = Color.FromArgb(235, 235, 215);
            _titleBar.Cursor = Cursors.SizeAll;
            _titleBar.MouseDown += TitleBar_MouseDown;
            _titleBar.MouseMove += TitleBar_MouseMove;
            _titleBar.MouseUp += (_, _) => _dragging = false;

            // ---- 关闭按钮 ----
            _btnClose.Text = "✕";
            _btnClose.Size = new Size(24, 24);
            _btnClose.Dock = DockStyle.Right;
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.TabStop = false;
            _btnClose.Click += (_, _) => Close();   // 想改隐藏就换成 Hide();

            // ---- 编辑区 ----
            _editor.Dock = DockStyle.Fill;
            _editor.BorderStyle = BorderStyle.None;
            _editor.Font = new Font("Microsoft YaHei", 10f);
            _editor.ScrollBars = RichTextBoxScrollBars.Vertical;
            _editor.BackColor = Color.LightYellow;

            _titleBar.Controls.Add(_btnClose);

            // ---- 右下角缩放手柄 ----
            _grip.Text = "◢";
            _grip.Size = new Size(16, 16);
            _grip.TextAlign = ContentAlignment.MiddleCenter;
            _grip.Cursor = Cursors.SizeNWSE;
            _grip.BackColor = Color.Transparent;
            _grip.MouseDown += Grip_MouseDown;
            _grip.MouseMove += Grip_MouseMove;
            _grip.MouseUp += (_, _) => _resizing = false;

            // ---- 叠放（顺序重要）----
            Controls.Add(_editor);      // Fill 先加
            Controls.Add(_titleBar);    // Top 后加
            Controls.Add(_grip);        // 浮层最后加
            _grip.BringToFront();       // 保证手柄在最上

            // 手柄位置随窗体尺寸走
            Resize += (_, _) => PlaceGrip();
            PlaceGrip();
        }

        private void PlaceGrip()
        {
            _grip.Location = new Point(
                ClientSize.Width - _grip.Width - 2,
                ClientSize.Height - _grip.Height - 2);
        }

        // ---------- 拖动 ----------
        private void TitleBar_MouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragOffset = e.Location;   // 鼠标相对拖动条的偏移
        }

        private void TitleBar_MouseMove(object? s, MouseEventArgs e)
        {
            if (!_dragging) return;
            var p = Cursor.Position;    // 屏幕坐标
            Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
        }

        // ---------- 缩放 ----------
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

        // ---------- 关闭 ----------
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 第一层先不做“关闭即隐藏”，直接放行真关闭。
            // 等托盘做好后，把下面两行打开、去掉 base 之外的处理：
            // if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }
    }
}

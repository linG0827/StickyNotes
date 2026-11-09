using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using global::StickyNotes.model;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace StickyNotes.UI
{
    

    public class NoteCard : Control
    {
        public NoteItem Note { get; }
        public bool Selected { get; set; }
        private bool _hover;

        public event Action<NoteCard>? OpenRequested;
        public event Action<NoteCard>? DeleteRequested;

        public NoteCard(NoteItem note)
        {
            Note = note;
            Height = 64;
            Margin = new Padding(10, 4, 10, 4);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            // 右键菜单删除
            var menu = new ContextMenuStrip();
            var del = new ToolStripMenuItem("删除便签");
            del.ForeColor = Theme.Danger;
            del.Click += (_, _) => DeleteRequested?.Invoke(this);
            menu.Items.Add(del);
            ContextMenuStrip = menu;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { OpenRequested?.Invoke(this); base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundRect(r, 10);
            using var bg = new SolidBrush(Selected ? Theme.PrimarySoft : (_hover ? Theme.Hover : Theme.Surface));
            g.FillPath(bg, path);

            if (Selected)
            {
                var bar = new Rectangle(4, 12, 3, Height - 24);
                using var barPath = Theme.RoundRect(bar, 2);
                using var bb = new SolidBrush(Theme.Primary);
                g.FillPath(bb, barPath);
            }

            using var ft = new SolidBrush(Theme.TextMain);
            var titleRect = new Rectangle(18, 10, Width - 30, 24);
            g.DrawString(Note.Title, Theme.FontTitle, ft, titleRect,
                new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });

            using var fs = new SolidBrush(Theme.TextSub);
            var timeRect = new Rectangle(18, 34, Width - 30, 18);
            g.DrawString(Note.UpdatedAt.ToString("MM-dd HH:mm"), Theme.FontSmall, fs, timeRect);
        }
    }
}

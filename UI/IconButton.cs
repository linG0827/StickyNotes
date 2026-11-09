using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace StickyNotes.UI
{
   

    public class IconButton : Control
    {
        public string Glyph { get; set; } = "";
        public Color GlyphColor { get; set; } = Theme.TextMain;
        public bool Active { get; set; }
        private bool _hover;

        public IconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(36, 32);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hover || Active)
            {
                var r = new Rectangle(2, 2, Width - 5, Height - 5);
                using var path = Theme.RoundRect(r, 6);
                using var b = new SolidBrush(Active ? Theme.PrimarySoft : Theme.Hover);
                g.FillPath(b, path);
            }

            using var f = new Font("Segoe UI Symbol", 11f);
            using var fg = new SolidBrush(GlyphColor);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Glyph, f, fg, ClientRectangle, sf);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace StickyNotes.UI
{


    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(250, 249, 246);
        public static readonly Color Surface = Color.White;
        public static readonly Color Primary = Color.FromArgb(66, 133, 244);
        public static readonly Color PrimarySoft = Color.FromArgb(232, 240, 254);
        public static readonly Color Danger = Color.FromArgb(217, 48, 37);
        public static readonly Color TextMain = Color.FromArgb(32, 33, 36);
        public static readonly Color TextSub = Color.FromArgb(128, 134, 139);
        public static readonly Color Divider = Color.FromArgb(230, 228, 224);
        public static readonly Color Hover = Color.FromArgb(240, 240, 240);
        public static readonly Color TitleBar = Color.FromArgb(252, 251, 248);

        public static readonly Font FontTitle = new("Microsoft YaHei UI", 11f, FontStyle.Bold);
        public static readonly Font FontBody = new("Microsoft YaHei UI", 10f);
        public static readonly Font FontSmall = new("Microsoft YaHei UI", 8.5f);

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

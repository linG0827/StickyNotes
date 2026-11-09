using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace StickyNotes.UI
{


    using System.Drawing;
    using System.Drawing.Drawing2D;

    public static class Theme
    {
        // 底色：暖米黄
        public static readonly Color Bg = Color.FromArgb(250, 243, 232);
        // 卡片：奶白
        public static readonly Color Surface = Color.FromArgb(255, 252, 247);
        // 顶部栏：暖米
        public static readonly Color TitleBar = Color.FromArgb(244, 234, 220);
        // 分割线：暖褐灰
        public static readonly Color Divider = Color.FromArgb(226, 212, 194);
        // 悬停：暖米加深
        public static readonly Color Hover = Color.FromArgb(244, 232, 218);

        // 主色：柔和珊瑚橘
        public static readonly Color Primary = Color.FromArgb(230, 126, 96);
        // 选中浅底：蜜桃
        public static readonly Color PrimarySoft = Color.FromArgb(253, 232, 222);
        // 危险：暖红
        public static readonly Color Danger = Color.FromArgb(200, 92, 78);

        // 辅助绿：低饱和的鼠尾草绿，温和不刺眼
        public static readonly Color Accent = Color.FromArgb(122, 160, 126);
        // 绿的浅底：淡薄荷米
        public static readonly Color AccentSoft = Color.FromArgb(232, 242, 231);

        // 文字：暖褐黑
        public static readonly Color TextMain = Color.FromArgb(74, 58, 46);
        // 次要文字：暖灰褐
        public static readonly Color TextSub = Color.FromArgb(156, 136, 118);

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

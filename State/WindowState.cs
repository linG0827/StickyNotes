using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNotes.State
{
    public class WindowState
    {
        public int X { get; set; } = 200;
        public int Y { get; set; } = 200;
        public int Width { get; set; } = 360;
        public int Height { get; set; } = 520;
        public bool TopMost { get; set; } = true;
    }
}

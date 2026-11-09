using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StickyNotes.model
{
    public class NoteItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "New Notes";
        public string Rtf { get; set; } = "";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

using global::StickyNotes.model;
using global::StickyNotes.State;
using System.Text.Json;

namespace StickyNotes.Store
{

    public static class NoteStore
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyStickies");
        private static readonly string FilePath = Path.Combine(Dir, "data.json");
        private static readonly string TempPath = FilePath + ".tmp";

        public static string DataDir => Dir;

        private static readonly JsonSerializerOptions Opt = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public class Root
        {
            public List<NoteItem> Notes { get; set; } = new();
            public WindowState Window { get; set; } = new();
        }

        public static Root Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new Root();
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<Root>(json, Opt) ?? new Root();
            }
            catch { return new Root(); }
        }

        public static void Save(Root root)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var json = JsonSerializer.Serialize(root, Opt);
                File.WriteAllText(TempPath, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(TempPath, FilePath);
            }
            catch { }
        }
    }
}

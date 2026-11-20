using System.Diagnostics;
using System.Text.Json;

namespace RunCat365
{
    internal class TodoRepository
    {
        private const string TODOS_FILE_NAME = "todos.json";

        private readonly string filePath;
        private List<TodoItem> items = [];

        internal TodoRepository()
        {
            var basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RunCat365"
            );
            Directory.CreateDirectory(basePath);
            filePath = Path.Combine(basePath, TODOS_FILE_NAME);
            Load();
        }

        internal List<TodoItem> GetAll()
        {
            return [.. items];
        }

        internal bool Add(string title)
        {
            items.Add(new TodoItem { Title = title });
            return TrySave();
        }

        internal bool SetCompleted(int index, bool isCompleted)
        {
            if (index < 0 || items.Count <= index) return false;
            items[index].IsCompleted = isCompleted;
            return TrySave();
        }

        internal bool RemoveAt(int index)
        {
            if (index < 0 || items.Count <= index) return false;
            items.RemoveAt(index);
            return TrySave();
        }

        private void Load()
        {
            if (!File.Exists(filePath))
            {
                items = [];
                return;
            }
            try
            {
                var json = File.ReadAllText(filePath);
                items = JsonSerializer.Deserialize<List<TodoItem>>(json) ?? [];
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to load todos: {ex.Message}");
                items = [];
            }
        }

        private bool TrySave()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(items, options);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to write todos: {ex.Message}");
                return false;
            }
        }
    }
}

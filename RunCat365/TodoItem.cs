using System.Text.Json.Serialization;

namespace RunCat365
{
    internal class TodoItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }
    }
}

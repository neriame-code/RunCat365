using System.Diagnostics;
using System.Text.Json;
using System.Xml;

namespace RunCat365
{
    internal sealed class UserSettings
    {
        private const string SETTINGS_FILE_NAME = "settings.json";
        private const string LEGACY_COMPANY_DIRECTORY_NAME = "Studio_Kyome";
        private const string LEGACY_APPLICATION_DIRECTORY_PATTERN = "RunCat_365_Url_*";
        private const string LEGACY_SETTINGS_FILE_NAME = "user.config";

        private static readonly string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string directoryPath = Path.Combine(localApplicationDataPath, "RunCat365");
        private static readonly string filePath = Path.Combine(directoryPath, SETTINGS_FILE_NAME);

        internal static UserSettings Default { get; } = Load();

        public string Runner { get; set; } = "Cat";
        public string Theme { get; set; } = string.Empty;
        public string TemperatureUnit { get; set; } = string.Empty;
        public string FPSMaxLimit { get; set; } = "FPS40";
        public bool FirstLaunch { get; set; } = true;
        public string SpeedSource { get; set; } = "CPU";
        public string CustomRunnerName { get; set; } = string.Empty;

        internal void Save()
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to write settings: {ex.Message}");
            }
        }

        private static UserSettings Load()
        {
            if (!File.Exists(filePath))
            {
                var migratedSettings = new UserSettings();
                migratedSettings.ImportLegacySettings();
                migratedSettings.Save();
                return migratedSettings;
            }
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
                return new UserSettings();
            }
        }

        private void ImportLegacySettings()
        {
            var legacyFilePath = FindLatestLegacySettingsFile();
            if (legacyFilePath is null) return;
            try
            {
                using var reader = XmlReader.Create(legacyFilePath);
                while (reader.ReadToFollowing("setting"))
                {
                    var name = reader.GetAttribute("name");
                    if (name is null || !reader.ReadToDescendant("value")) continue;
                    ApplyLegacyValue(name, reader.ReadElementContentAsString());
                }
            }
            catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to import legacy settings: {ex.Message}");
            }
        }

        private static string? FindLatestLegacySettingsFile()
        {
            var companyPath = Path.Combine(localApplicationDataPath, LEGACY_COMPANY_DIRECTORY_NAME);
            if (!Directory.Exists(companyPath)) return null;
            try
            {
                return Directory.EnumerateDirectories(companyPath, LEGACY_APPLICATION_DIRECTORY_PATTERN)
                    .SelectMany(applicationPath => Directory.EnumerateDirectories(applicationPath))
                    .Select(versionPath => Path.Combine(versionPath, LEGACY_SETTINGS_FILE_NAME))
                    .Where(File.Exists)
                    .MaxBy(File.GetLastWriteTimeUtc);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Failed to find legacy settings: {ex.Message}");
                return null;
            }
        }

        private void ApplyLegacyValue(string name, string value)
        {
            switch (name)
            {
                case nameof(Runner):
                    Runner = value;
                    break;
                case nameof(Theme):
                    Theme = value;
                    break;
                case nameof(TemperatureUnit):
                    TemperatureUnit = value;
                    break;
                case nameof(FPSMaxLimit):
                    FPSMaxLimit = value;
                    break;
                case nameof(FirstLaunch):
                    if (bool.TryParse(value, out var firstLaunch)) FirstLaunch = firstLaunch;
                    break;
                case nameof(SpeedSource):
                    SpeedSource = value;
                    break;
                case nameof(CustomRunnerName):
                    CustomRunnerName = value;
                    break;
            }
        }
    }
}
